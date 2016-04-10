// ReSharper disable All

using System;
using System.Linq;
using System.Linq.Expressions;

namespace Slant.Linq
{
    public static class ExpandableExtensions
    {
        /// <summary> LinqKit: Returns wrapper that automatically expands expressions </summary>
        public static IQueryable<T> AsExpandable<T>(this IQueryable<T> query)
        {
            if (query is ExpandableQuery<T>) return query;
            return ExpandableQueryFactory<T>.Create(query);
        }

        private static class ExpandableQueryFactory<T>
        {
            public static readonly Func<IQueryable<T>, ExpandableQuery<T>> Create;

            static ExpandableQueryFactory()
            {
                if (!typeof(T).IsClass)
                {
                    Create = query => new ExpandableQuery<T>(query);
                    return;
                }

                var queryType = typeof(IQueryable<T>);
                var ctorInfo = typeof(ExpandableQueryOfClass<>).MakeGenericType(typeof(T)).GetConstructor(new[] { queryType });
                var queryParam = Expression.Parameter(queryType);
                var newExpr = Expression.New(ctorInfo, queryParam);
                var createExpr = Expression.Lambda<Func<IQueryable<T>, ExpandableQuery<T>>>(newExpr, queryParam);
                Create = createExpr.Compile();
            }
        }
    }
}