#region [R# naming]
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NSpectator;
using NUnit.Framework;

namespace Slant.Linq.Tests
{
    [TestFixture]
    public class ExpressionCombinerTest : DebuggerShim
    {
        static Expression<Func<Tuple<int, string>, bool>> _ExprAsField_criteria1 = x => x.Item1 > 1000;
        static Expression<Func<Tuple<int, string>, int>> _ExprAsVariable_UsedAsParam_valueExpr = x => x.Item1;

        static int[] _possibleValues = new int[] { 1, 2, 3 };

        static string ExpressionAsParam_Method(Expression<Func<Tuple<int, string>, bool>> criteria1)
        {
            Expression<Func<Tuple<int, string>, bool>> criteria2 =
                x => criteria1.Invoke(x) || x.Item2.Contains("a");

            return criteria2.Expand().ToString();
        }

        static string ExpressionAsParam_UsedAsParam_Method(Expression<Func<Tuple<int, string>, int>> valueExpr)
        {
            Expression<Func<Tuple<int, string>, bool>> criteria = x => _possibleValues.Contains(valueExpr.Invoke(x));

            return criteria.Expand().ToString();
        }

        static string ConstExpressionString<TResult>(Expression<Func<TResult>> expr)
        {
            return expr.ToString().Substring(6);
        }

        [Test]
        public void Spectate() => DebugNestedTypes();

        class Describe_expressions : Spec
        {
            LinqBuilder<Tuple<int, string>> _linq = LinqBuilder<Tuple<int, string>>.Get();

            void Specify_expandable_criterion()
            {
                Expression<Func<Tuple<int, string>, bool>> criteria1 = x => x.Item1 > 1000;
                Expression<Func<Tuple<int, string>, bool>> criteria2 = x => criteria1.Invoke(x) || x.Item2.Contains("a");

                criteria2.Expand().ToString().Should().Be("x => ((x.Item1 > 1000) OrElse x.Item2.Contains(\"a\"))");
            }

            void Specify_expandable_criterion_with_linq()
            {
                var criteria1 = _linq.Predicate(x => x.Item1 > 1000);
                var criteria2 = _linq.Predicate(x => criteria1.Invoke(x) || x.Item2.Contains("a"));

                criteria2.Expand().ToString().Should().Be("x => ((x.Item1 > 1000) OrElse x.Item2.Contains(\"a\"))");
            }

            void Specify_expression_as_field()
            {
                Expression<Func<Tuple<int, string>, bool>> criteria2 =
                        x => _ExprAsField_criteria1.Invoke(x) || x.Item2.Contains("a");

                criteria2.Expand().ToString().Should().Be("x => ((x.Item1 > 1000) OrElse x.Item2.Contains(\"a\"))");
            }

            void Specify_expression_as_field_with_linq()
            {
                var criteria2 = _linq.Predicate(x => _ExprAsField_criteria1.Invoke(x) || x.Item2.Contains("a"));
                criteria2.Expand().ToString().Should().Be("x => ((x.Item1 > 1000) OrElse x.Item2.Contains(\"a\"))");
            }

            void Specify_expression_as_param()
            {
                ExpressionAsParam_Method(x => x.Item1 > 1000)
                    .Should()
                    .Be("x => ((x.Item1 > 1000) OrElse x.Item2.Contains(\"a\"))");
            }

            void Specify_expression_as_variable_used_as_param()
            {
                Expression<Func<Tuple<int, string>, int>> valueExpr = x => x.Item1;
                Expression<Func<Tuple<int, string>, bool>> criteria = x => _possibleValues.Contains(valueExpr.Invoke(x));

                criteria.Expand().ToString().Should().Be($"x => {ConstExpressionString(() => _possibleValues)}.Contains(x.Item1)");
            }

            void Specify_expression_as_variable_used_as_param_with_linq()
            {
                var valueExpr = _linq.Getter(x => x.Item1);
                var criteria = _linq.Predicate(x => _possibleValues.Contains(valueExpr.Invoke(x)));

                criteria.Expand().ToString().Should().Be($"x => {ConstExpressionString(() => _possibleValues)}.Contains(x.Item1)");
            }

            void Specify_expression_as_field_used_as_param()
            {
                Expression<Func<Tuple<int, string>, bool>> criteria = x => _possibleValues.Contains(_ExprAsVariable_UsedAsParam_valueExpr.Invoke(x));

                criteria.Expand().ToString().Should().Be($"x => {ConstExpressionString(() => _possibleValues)}.Contains(x.Item1)");
            }

            void Specify_expression_as_field_used_as_param_with_linq()
            {
                var criteria = _linq.Predicate(x => _possibleValues.Contains(_ExprAsVariable_UsedAsParam_valueExpr.Invoke(x)));
                criteria.Expand().ToString().Should().Be($"x => {ConstExpressionString(() => _possibleValues)}.Contains(x.Item1)");
            }

            void Specify_expression_as_param_used_as_param()
            {
                ExpressionAsParam_UsedAsParam_Method(x => x.Item1).Should().Be($"x => {ConstExpressionString(() => _possibleValues)}.Contains(x.Item1)");
            }

            void Specify_nesting_member_access()
            {
                Expression<Func<Tuple<Tuple<int, DateTime>, string>, Tuple<int, DateTime>>> memberExpr1 = x => x.Item1;
                Expression<Func<Tuple<int, DateTime>, DateTime>> memberExpr2 = x => x.Item2;

                Expression<Func<Tuple<Tuple<int, DateTime>, string>, DateTime>> criteria =
                    x => memberExpr2.Invoke(memberExpr1.Invoke(x));

                criteria.Expand().ToString().Should().Be("x => x.Item1.Item2");
            }


            void Specify_expand_processes_arguments()
            {
                Expression<Func<Tuple<bool, bool>, bool>> expr1 = x => x.Item1 && x.Item2;
                Expression<Func<bool, Tuple<bool, bool>>> expr2 = y => new Tuple<bool, bool>(y, y);
                Expression<Func<bool, bool>> nestedExpression = z => expr1.Invoke(expr2.Invoke(z));

                var expandAgain = nestedExpression.Expand().Expand().ToString();
                expandAgain.Should().Be(nestedExpression.Expand().ToString());
            }

            void Specify_expand_processes_arguments_with_linq()
            {
                var expr1 = Let<Tuple<bool, bool>>.Predicate(x => x.Item1 && x.Item2);
                var expr2 = Let<bool>.Expr(y => new Tuple<bool, bool>(y, y));
                var nestedExpression = Let<bool>.Expr(z => expr1.Invoke(expr2.Invoke(z)));

                var expandAgain = nestedExpression.Expand().Expand().ToString();
                expandAgain.Should().Be(nestedExpression.Expand().ToString());
            }

            void Describe_anonymous_type_local_delegate()
            {
                // Expression<Func<int, int, ?>>
                var expr1 = Linq.Expr((int a, int b) => new {a, b});
                var expr2 = Linq.Expr((int c) => expr1.Invoke(c, 0));
                var expr3 = Let<int>.Getter(a => expr2.Invoke(a).a);

                context[$"{expr2.Expand()}"] = () =>
                {
                    it[$"expr(4).getter(a) should be 4"] = () =>
                    {
                        expr3.Expand().Invoke(4).Should().Be(4);
                    };
                };
               
            }

            void Specify_linq_builder()
            {
                int value = 0;
                var checker = Let<int>.Predicate(x => x == 0);

                checker.Invoke(value).Should().BeTrue();
                checker.Expand().Invoke(value).Should().BeTrue();

                var inc = Linq.Expr<int, int>(x => x + 1);
                value = inc.Invoke(value);

                value.Should().Be(1);

                checker.Invoke(value).Should().BeFalse();
            }
        }

    }
}
