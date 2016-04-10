﻿using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Linq;
// ReSharper disable CheckNamespace

namespace System.Linq
{
    /// <summary> 
    /// Extension methods for expression tree balancing. 
    /// </summary>
    public static class AggregateExtensions
    {
        /// <summary>
        /// Generates balanced binary trees for list of conditions.
        /// <para>E.g.: AndAlso or OrElse</para>
        /// <para>The reason is avoid StackOverFlowExceptions:</para>
        /// <para>var result = lambdas.Aggregate(AndAlso); // StackOverflow when lambdas.Lenght is 20 000</para>
        /// <para>var result = lambdas.AggregateBalanced(AndAlso); // Ok still when lambdas.Lenght is 1 000 000</para>
        /// </summary>
        public static TExpression AggregateBalanced<TExpression>(this TExpression[] lambdas, Func<Expression, Expression, TExpression> operationToDo)
            where TExpression : Expression
        {
            var items = lambdas.Length;
            switch (items)
            {
                case 0: throw new InvalidOperationException("Sequence contains no elements");
                case 1: return lambdas[0];
                case 2: return operationToDo(lambdas[0], lambdas[1]);
                default:
                    var half = items / 2;
                    var o1 = AggregateBalanced(lambdas.Take(half).ToArray(), operationToDo);
                    var o2 = AggregateBalanced(lambdas.Skip(half).ToArray(), operationToDo);
                    return operationToDo(o1, o2);
            }
        }

        /// <summary>
        /// Generates balanced binary trees for list of conditions. Generic version.
        /// <para>E.g.: AndAlso or OrElse</para>
        /// <para>The reason is avoid StackOverFlowExceptions:</para>
        /// <para>var result = lambdas.Aggregate(AndAlso); // StackOverflow when lambdas.Lenght is 20 000</para>
        /// <para>var result = lambdas.AggregateBalanced(AndAlso); // Ok still when lambdas.Lenght is 1 000 000</para>
        /// </summary>
        public static Expression<T> AggregateBalanced<T>(this Expression<T>[] lambdas, Func<Expression<T>, Expression<T>, Expression<T>> operationToDo)
        {
            var items = lambdas.Length;
            switch (items)
            {
                case 0: throw new InvalidOperationException("Sequence contains no elements");
                case 1: return lambdas[0];
                case 2: return operationToDo(lambdas[0], lambdas[1]);
                default:
                    var half = items / 2;
                    var o1 = AggregateBalanced(lambdas.Take(half).ToArray(), operationToDo);
                    var o2 = AggregateBalanced(lambdas.Skip(half).ToArray(), operationToDo);
                    return operationToDo(o1, o2);
            }
        }

        /// <summary>
        /// Generates balanced binary trees for list of conditions.
        /// <para>E.g.: AndAlso or OrElse</para>
        /// <para>The reason is avoid StackOverFlowExceptions:</para>
        /// <para>var result = lambdas.Aggregate(AndAlso); // StackOverflow when lambdas.Lenght is 20 000</para>
        /// <para>var result = lambdas.AggregateBalanced(AndAlso); // Ok still when lambdas.Lenght is 1 000 000</para>
        /// </summary>
        public static async System.Threading.Tasks.Task<TExpression> AggregateBalancedAsync<TExpression>(this TExpression[] lambdas, Func<Expression, Expression, TExpression> operationToDo)
            where TExpression : Expression
        {
            var items = lambdas.Length;
            switch (items)
            {
                case 0: throw new InvalidOperationException("Sequence contains no elements");
                case 1: return lambdas[0];
                case 2: return operationToDo(lambdas[0], lambdas[1]);
                default:
                    var half = items / 2;
                    var op1 = System.Threading.Tasks.Task.Run(() => AggregateBalanced(lambdas.Take(half).ToArray(), operationToDo));
                    var op2 = System.Threading.Tasks.Task.Run(() => AggregateBalanced(lambdas.Skip(half).ToArray(), operationToDo));
                    return operationToDo(await op1, await op2);
            }
        }

        /// <summary>
        /// Generates balanced binary trees for list of conditions. Generic version.
        /// <para>E.g.: AndAlso or OrElse</para>
        /// <para>The reason is avoid StackOverFlowExceptions:</para>
        /// <para>var result = lambdas.Aggregate(AndAlso); // StackOverflow when lambdas.Lenght is 20 000</para>
        /// <para>var result = lambdas.AggregateBalanced(AndAlso); // Ok still when lambdas.Lenght is 1 000 000</para>
        /// </summary>
        public static async System.Threading.Tasks.Task<Expression<T>> AggregateBalancedAsync<T>(this Expression<T>[] lambdas, Func<Expression<T>, Expression<T>, Expression<T>> operationToDo)
        {
            var items = lambdas.Length;
            switch (items)
            {
                case 0: throw new InvalidOperationException("Sequence contains no elements");
                case 1: return lambdas[0];
                case 2: return operationToDo(lambdas[0], lambdas[1]);
                default:
                    var half = items / 2;
                    var op1 = System.Threading.Tasks.Task.Run(() => AggregateBalanced(lambdas.Take(half).ToArray(), operationToDo));
                    var op2 = System.Threading.Tasks.Task.Run(() => AggregateBalanced(lambdas.Skip(half).ToArray(), operationToDo));
                    return operationToDo(await op1, await op2);
            }
        }
    }
}