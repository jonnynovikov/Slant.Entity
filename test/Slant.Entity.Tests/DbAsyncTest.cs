using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Slant.Linq;

namespace Slant.Entity.Tests
{
    [TestFixture]
    public class DbAsyncTest : DebuggerShim
    {
        private readonly TestContext db = new TestContext();

        public DbAsyncTest()
        {
            db.Entities.RemoveRange(db.Entities.ToList());
            db.Entities.AddRange(new[]
            {
                new Entity { Value = 123.45m },
                new Entity { Value = 67.89m },
                new Entity { Value = 3.14m }
            });
            db.SaveChanges();
        }

        public async Task EnumerateShouldWorkAsync()
        {
            var task = db.Entities.AsExpandable().ToListAsync();
            var before = task.Status;
            var result = await task;
            var after = task.Status;

            after.Should().Be(TaskStatus.RanToCompletion);
            result.Should().HaveCount(3);
            before.Should().NotBe(TaskStatus.RanToCompletion);
        }

        public async Task ExecuteShouldWorkAsync()
        {
            var task = db.Entities.AsExpandable().SumAsync(e => e.Value);
            var before = task.Status;
            var result = await task;
            var after = task.Status;

            after.Should().Be(TaskStatus.RanToCompletion);
            result.Should().Be(194.48m);
            before.Should().NotBe(TaskStatus.RanToCompletion);
        }

        public async Task ExpressionInvokeTest()
        {
            var eParam = Expression.Parameter(typeof(Entity), "e");
            var eProp = Expression.PropertyOrField(eParam, "Value");

            var conditions =
                (from item in new List<decimal> { 1m, 2m, 3m, 4m }
                 select Expression.LessThan(eProp, Expression.Constant(item))).Aggregate(Expression.OrElse);

            var combined = Expression.Lambda<Func<Entity, bool>>(conditions, eParam);

            var q = from e in db.Entities.AsExpandable()
                    where combined.Invoke(e)
                    select new { e.Value };

            var res = await q.ToListAsync();

            res.Should().HaveCount(1);
            res.First().Value.Should().Be(3.14m);
        }
        
        public class TestContext : DbContext
        {
            public DbSet<Entity> Entities { get; set; }
        }

        public class Entity
        {
            public int Id { get; set; }

            public decimal Value { get; set; }
        }
    }
}
