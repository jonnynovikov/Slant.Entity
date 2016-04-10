using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections;
using System.Threading;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
// ReSharper disable CheckNamespace

namespace Slant.Linq
{
    /// <summary>
    /// An IQueryable wrapper that allows us to visit the query's expression tree just before LINQ to SQL gets to it.
    /// This is based on the excellent work of Tomas Petricek: http://tomasp.net/blog/linq-expand.aspx
    /// </summary>
    public class ExpandableQuery<T> : IOrderedQueryable<T>, IDbAsyncEnumerable<T>
    {
        private readonly ExpandableQueryProvider<T> _provider;

        // Original query, that we're wrapping
        internal IQueryable<T> InnerQuery { get; }

        internal ExpandableQuery(IQueryable<T> inner)
        {
            InnerQuery = inner;
            _provider = new ExpandableQueryProvider<T>(this);
        }

        Expression IQueryable.Expression => InnerQuery.Expression;

        Type IQueryable.ElementType => typeof(T);

        IQueryProvider IQueryable.Provider => _provider;

        /// <summary> 
        /// IQueryable enumeration 
        /// </summary>
        public IEnumerator<T> GetEnumerator() { return InnerQuery.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return InnerQuery.GetEnumerator(); }

        /// <summary> 
        /// IQueryable string presentation.  
        /// </summary>
        public override string ToString() { return InnerQuery.ToString(); }

        /// <summary> 
        /// Enumerator for async-await 
        /// </summary>
        public IDbAsyncEnumerator<T> GetAsyncEnumerator()
        {
            var asyncEnumerable = InnerQuery as IDbAsyncEnumerable<T>;
            if (asyncEnumerable != null)
                return asyncEnumerable.GetAsyncEnumerator();
            return new ExpandableDbAsyncEnumerator<T>(InnerQuery.GetEnumerator());
        }

        IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        {
            return this.GetAsyncEnumerator();
        }
    }


    internal class ExpandableQueryOfClass<T> : ExpandableQuery<T>
        where T: class
    {
        public ExpandableQueryOfClass(IQueryable<T> inner): base(inner)
        {
        }

        public IQueryable<T> Include(string path)
        {
            return InnerQuery.Include(path).AsExpandable();
        }
    }

    internal class ExpandableQueryProvider<T> : IDbAsyncQueryProvider
    {
        readonly ExpandableQuery<T> _query;

        internal ExpandableQueryProvider(ExpandableQuery<T> query)
        {
            _query = query;
        }

        // The following four methods first call ExpressionExpander to visit the expression tree, then call
        // upon the inner query to do the remaining work.
        
        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            return _query.InnerQuery.Provider.CreateQuery<TElement>(expression.Expand()).AsExpandable();
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            return _query.InnerQuery.Provider.CreateQuery(expression.Expand());
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            return _query.InnerQuery.Provider.Execute<TResult>(expression.Expand());
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return _query.InnerQuery.Provider.Execute(expression.Expand());
        }

		public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
		{
			var asyncProvider = _query.InnerQuery.Provider as IDbAsyncQueryProvider;
			if (asyncProvider != null)
				return asyncProvider.ExecuteAsync(expression.Expand(), cancellationToken);
			return Task.FromResult(_query.InnerQuery.Provider.Execute(expression.Expand()));
		}

		public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
		{
			var asyncProvider = _query.InnerQuery.Provider as IDbAsyncQueryProvider;
			if (asyncProvider != null)
				return asyncProvider.ExecuteAsync<TResult>(expression.Expand(), cancellationToken);
			return Task.FromResult(_query.InnerQuery.Provider.Execute<TResult>(expression.Expand()));
		}
    }
}
