#region [R# naming]
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable InconsistentNaming
#endregion
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using NSpectator;
using NUnit.Framework;
using Slant.Linq;

namespace Slant.Entity.Tests
{
    [TestFixture]
    public class DbAsyncTest : DebuggerShim
    {
        static TestContext db;
        
        public static string RootPath => DirectoryOf(Assembly.GetExecutingAssembly());

        private static string DirectoryOf(Assembly assembly)
        {
            string filePath = new Uri(assembly.CodeBase).LocalPath;
            return Path.GetDirectoryName(filePath);
        }

        [Test]
        public void Spectate() => DebugNestedTypes();

        class Describe_Entities : Spec
        {
            void before_all()
            {
                var path = RootPath.Replace(@"\bin\Debug", "").Replace(@"\bin\Release", "");
                AppDomain.CurrentDomain.SetData("DataDirectory", path);

                db = new TestContext();

                db.Entities.AddRange(new[]
                {
                    new Entity { Value = 123.45m },
                    new Entity { Value = 67.89m },
                    new Entity { Value = 3.14m }
                });
                db.SaveChanges();
            }

            void Should_be_initialized_database()
            {
                it["should be 3 entities in db"] = () =>
                {
                    db.Entities.Count().Should().Be(3);
                };
            }

            void Describe_enumerate_work_async()
            {
                itAsync["should retrieve entities as expandable"] = async () =>
                {
                    var task = db.Entities.AsExpandable().ToListAsync();
                    var status = task.Status;
                    var result = await task;
                    var newStatus = task.Status;

                    newStatus.Should().Be(TaskStatus.RanToCompletion);
                    result.Should().HaveCount(3);
                    status.Should().NotBe(TaskStatus.RanToCompletion);
                };
            }

            void Describe_execute_async_work()
            {
                itAsync["should work async execution"] = ExecuteShouldWorkAsync;
            }

            void Describe_expression_invoke_async()
            {
                itAsync["should work expression invoke with async"] = ExecuteShouldWorkAsync;
            }

            void Describe_non_fail_database_clear()
            {
                it["removing all"] = () =>
                {
                    var entities = db.Entities.ToList();
                    foreach (var entity in entities)
                    {
                        db.Entities.Remove(entity);
                    }
                    db.SaveChanges();
                };
            }
        }

        public static async Task EnumerateShouldWorkAsync()
        {
            var task = db.Entities.AsExpandable().ToListAsync();
            var before = task.Status;
            var result = await task;
            var after = task.Status;

            after.Should().Be(TaskStatus.RanToCompletion);
            result.Should().HaveCount(3);
            before.Should().NotBe(TaskStatus.RanToCompletion);
        }

        public static async Task ExecuteShouldWorkAsync()
        {
            var task = db.Entities.AsExpandable().SumAsync(e => e.Value);
            var before = task.Status;
            var result = await task;
            var after = task.Status;

            after.Should().Be(TaskStatus.RanToCompletion);
            result.Should().Be(194.48m);
            before.Should().NotBe(TaskStatus.RanToCompletion);
        }

        public static async Task ExpressionInvokeAsync()
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
            public TestContext()
                : base("DefaultConnection")
            {
                Database.SetInitializer(new DropCreateDatabaseAlways<TestContext>());
            }

            public DbSet<Entity> Entities { get; set; }
        }

        public class Entity
        {
            public int Id { get; set; }

            public decimal Value { get; set; }
        }
    }
}
