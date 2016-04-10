#region [R# naming]
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming
#endregion
using System;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using NSpectator;
using NUnit.Framework;

namespace Slant.Linq.Tests
{
    [TestFixture]
    public class PredicateBuilderTest : DebuggerShim
    {
        [Test]
        public void Spectate() => DebugNestedTypes();

        public class Foo { public Bar bar; }

        public class Bar { public bool baz; }

        class Describe_PredicateBuilder : Spec
        {
            void Specify_invoke_expression_combiner()
            {
                Expression<Func<Tuple<int, string>, bool>> criteria1 = x => x.Item1 > 1000;
                Expression<Func<Tuple<int, string>, bool>> criteria2 = y => y.Item2.Contains("a");
                Expression<Func<Tuple<int, string>, bool>> criteria3 = criteria1.Or(z => criteria2.Invoke(z));

                criteria3.Expand().ToString().Should().Be("x => ((x.Item1 > 1000) OrElse x.Item2.Contains(\"a\"))");
            }

            void Specify_expression_OrElse_predicate()
            {
                Expression<Func<bool, bool, bool, bool>> criteria = (a, b, c) => a || b || c;
                var exp = criteria.Expand().ToString();

                exp.Should().Be("(a, b, c) => ((a OrElse b) OrElse c)");
            }

            void Specify_expression_balance()
            {
                var exp = Expression.Equal(Expression.Constant(1), Expression.Constant(1));
                var list = Enumerable.Repeat(exp, 50000).ToArray();
                // Would throw stackoverflow with over 3100 items:
                // var combined = list.Aggregate(Expression.OrElse);
                // But this will work:
                var combined = list.AggregateBalanced(Expression.OrElse);
                var executed = combined.Expand().ToString();

                executed.Should().Contain("(1 == 1)");
            }

            void Specify_unbounded_variable_in_expanded_predicate()
            {
                Expression<Func<Foo, Bar>> barGetter = f => f.bar;
                Expression<Func<Bar, bool>> barPredicate = b => b.baz;
                Expression<Func<Foo, bool>> fooPredicate = x => barPredicate.Invoke(barGetter.Invoke(x));
                Expression<Func<Foo, bool>> inception = y => fooPredicate.Invoke(y);

                var expanded = inception.Expand(); // y => x.bar.baz
                var compiled = expanded.Compile(); // throws an InvalidOperationException
                var result = compiled.Invoke(new Foo { bar = new Bar() });

                result.Should().BeFalse();
            }
        }
    }
}
