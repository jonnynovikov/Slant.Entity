using Slant.Entity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Slant.Entity.Tests.Helpers;
using Slant.Entity.Tests.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Slant.Entity.Tests;

public sealed class DbContextScopeTests : IDisposable
{
    private readonly SqliteMemoryDatabaseLifetimeManager _databaseManager;
    private readonly IDbContextFactory _dbContextFactory;
    private readonly DbContextScopeFactory _dbContextScopeFactory;

    public DbContextScopeTests()
    {
        // Create a SQLite in-memory database which will last the duration of the test
        _databaseManager = new SqliteMemoryDatabaseLifetimeManager();

        // Get the connection string and create a DbContextScopeFactory
        var connectionString = _databaseManager.ConnectionString;
        _dbContextFactory = new TestDbContextFactory(connectionString);
        _dbContextScopeFactory = new DbContextScopeFactory(_dbContextFactory);

        // Ensure the database is created
        using var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>();
        dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _databaseManager.Dispose();
    }

    [Fact]
    public void Nested_scopes_should_use_same_DbContext_by_default()
    {
        using var outerDbContextScope = _dbContextScopeFactory.Create();
        var outerDbContext = outerDbContextScope.DbContexts.Get<TestDbContext>();

        using var innerDbContextScope = _dbContextScopeFactory.Create();
        var innerDbContext = innerDbContextScope.DbContexts.Get<TestDbContext>();

        outerDbContext.Should().NotBeNull();
        innerDbContext.Should().NotBeNull();

        innerDbContext.Should().BeSameAs(outerDbContext);
    }

    [Fact]
    public void Calling_SaveChanges_on_a_nested_scope_has_no_effect()
    {
        var originalName = "Test User";
        var newName = "New name";

        // Arrange - add one user to the database
        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            dbContext.Users.Add(new User { Name = originalName });
            dbContext.SaveChanges();
        }

        // Act - create nested DbContextScopes and attempt to save on the inner scope
        using (var outerDbContextScope = _dbContextScopeFactory.Create())
        {
            using (var innerDbContextScope = _dbContextScopeFactory.Create())
            {
                var innerDbContext = innerDbContextScope.DbContexts.Get<TestDbContext>();
                var user = innerDbContext.Users.Single();
                user.Name = newName;
                innerDbContextScope.SaveChanges();
            }
        }

        // Assert - name should NOT have changed
        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            var user = dbContext.Users.Single();
            user.Name.Should().Be(originalName);
        }
    }

    [Fact]
    public void Calling_SaveChanges_on_the_outer_scope_saves_changes()
    {
        var originalName = "Test User";
        var newName = "New name";

        // Arrange - add one user to the database
        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            dbContext.Users.Add(new User { Name = originalName });
            dbContext.SaveChanges();
        }

        // Act - create an outer DbContextScope and attempt to save
        using (var outerDbContextScope = _dbContextScopeFactory.Create())
        {
            var dbContext = outerDbContextScope.DbContexts.Get<TestDbContext>();
            var user = dbContext.Users.Single();
            user.Name = newName;
            outerDbContextScope.SaveChanges();
        }

        // Assert - name should have changed
        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            var user = dbContext.Users.Single();
            user.Name.Should().Be(newName);
        }
    }

    [Fact]
    public void Changes_can_only_be_saved_once_on_a_DbContextScope()
    {
        using var dbContextScope = _dbContextScopeFactory.Create();
        var dbContext = dbContextScope.DbContexts.Get<TestDbContext>();

        // Arrange - add a user and call SaveChanges once
        dbContext.Users.Add(new User { Name = "Test User" });
        dbContextScope.SaveChanges();

        // Act - call SaveChanges again
        var ex = Record.Exception(() => dbContextScope.SaveChanges());

        // Assert - an InvalidOperationException should have been thrown
        ex.Should().NotBeNull();
        ex.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void SaveChanges_can_be_called_again_after_a_DbUpdateConcurrencyException()
    {
        var originalName = "Test User";
        var newName1 = "New name 1";
        var newName2 = "New name 2";

        // Arrange - add one user to the database
        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            dbContext.Users.Add(new User { Name = originalName });
            dbContext.SaveChanges();
        }

        // Act
        var concurrencyExceptionThrown = false;

        using (var dbContextScope = _dbContextScopeFactory.Create())
        {
            var dbContext1 = dbContextScope.DbContexts.Get<TestDbContext>();
            var user = dbContext1.Users.Single();

            // Change the user's name in a separate DbContext to cause a DbUpdateConcurrencyException
            var dbContext2 = _dbContextFactory.CreateDbContext<TestDbContext>();
            var userFromNewContext = dbContext2.Users.Single();
            userFromNewContext.Name = newName1;
            dbContext2.SaveChanges();

            // Make a different change so we can save it on the DbContext obtained from the DbContextScope
            user.Name = newName2;

            // Call SaveChanges on the DbContextScope and handle any DbUpdateConcurrencyExceptions
            var saved = false;

            while (!saved)
            {
                try
                {
                    // Attempt to save changes to the database
                    dbContextScope.SaveChanges();
                    saved = true;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    concurrencyExceptionThrown = true;

                    foreach (var entry in ex.Entries)
                    {
                        var databaseValues = entry.GetDatabaseValues();

                        if (databaseValues == null)
                        {
                            throw new InvalidOperationException($"Unexpected error - {databaseValues} should never be null here.");
                        }

                        // Don't make any changes to the entry's CurrentValues, which effectively
                        // means "latest save wins"

                        // Refresh entry's OriginalValues property so we don't fail the next concurrency check
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
            }
        }

        // Assert
        concurrencyExceptionThrown.Should().Be(true);
        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            var user = dbContext.Users.Single();
            user.Name.Should().Be(newName2);
        }
    }

    [Fact]
    public void IDbContextReadOnlyScope_should_not_have_SaveChanges_method()
    {
        using (var dbContextScope = _dbContextScopeFactory.CreateReadOnly())
        {
            // Use reflection to check that there is no "SaveChanges" method on dbContextScope
            var type = dbContextScope.GetType();
            var publicMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var saveChangesMethod = publicMethods.Where(m => m.Name == "SaveChanges").SingleOrDefault();
            saveChangesMethod.Should().BeNull();
        }
    }

    [Fact]
    public void AmbientDbContextLocator_should_return_ambient_scope()
    {
        using (var dbContextScope = _dbContextScopeFactory.Create())
        {
            var dbContext = dbContextScope.DbContexts.Get<TestDbContext>();

            var contextLocator = new AmbientDbContextLocator();
            var ambientDbContext = contextLocator.Get<TestDbContext>();

            ambientDbContext.Should().NotBeNull();
            ambientDbContext.Should().BeSameAs(dbContext);
        }
    }

    [Fact]
    public void ForceCreateNew_option_should_create_new_DbContext_in_nested_scope()
    {
        using var outerDbContextScope = _dbContextScopeFactory.Create();
        var outerDbContext = outerDbContextScope.DbContexts.Get<TestDbContext>();

        using var innerDbContextScope = _dbContextScopeFactory.Create(DbContextScopeOption.ForceCreateNew);
        var innerDbContext = innerDbContextScope.DbContexts.Get<TestDbContext>();

        outerDbContext.Should().NotBeNull();
        innerDbContext.Should().NotBeNull();

        innerDbContext.Should().NotBeSameAs(outerDbContext);
    }

    [Fact]
    public void RefreshEntitiesInParentScope_should_reload_changed_data_from_database()
    {
        var originalName1 = "Test User 1";
        var originalName2 = "Test User 2";
        var newName1 = "New name 1";
        var newName2 = "New name 2";

        // Arrange - add two users to the database
        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            dbContext.Users.AddRange(new User[]
            {
                new User { Name = originalName1 },
                new User { Name = originalName2 }
            });
            dbContext.SaveChanges();
        }

        using (var outerDbContextScope = _dbContextScopeFactory.Create())
        {
            var outerDbContext = outerDbContextScope.DbContexts.Get<TestDbContext>();
            var outerUsers = outerDbContext.Users.ToList();

            outerUsers.Count.Should().Be(2);

            // Arrange - modify the entity in an inner scope created with ForceCreateNew
            using (var innerDbContextScope = _dbContextScopeFactory.Create(DbContextScopeOption.ForceCreateNew))
            {
                var innerDbContext = innerDbContextScope.DbContexts.Get<TestDbContext>();
                var innerUsers = innerDbContext.Users.ToList();
                innerUsers[0].Name = newName1;
                innerUsers[1].Name = newName2;

                innerDbContext.SaveChanges();

                // Entities in outer scope should be unchanged at this point
                // since we have not refreshed the entities yet
                outerUsers[0].Name.Should().Be(originalName1);
                outerUsers[1].Name.Should().Be(originalName2);

                // Act - only refresh first user in parent scope, but not the second
                innerDbContextScope.RefreshEntitiesInParentScope(new User[] { innerUsers[0] });

                // Assert
                outerUsers[0].Name.Should().Be(newName1);
                outerUsers[1].Name.Should().Be(originalName2);
            }
        }
    }

    [Fact]
    public void RefreshEntitiesInParentScope_should_refresh_entities_with_composite_primary_keys()
    {
        // Arrange(1) - create two new users with associated courses and grades
        var course1 = new Course { Name = "Computing" };
        var course2 = new Course { Name = "English" };

        using (var dbContext = _dbContextFactory.CreateDbContext<TestDbContext>())
        {
            dbContext.Users.AddRange(new User[]
            {
                new()
                {
                    Name = "Test User 1",
                    CoursesUsers = new CourseUser[]
                    {
                        new() { Course = course1, Grade = "A" },
                        new() { Course = course2, Grade = "C" }
                    }
                },
                new()
                {
                    Name = "Test User 2",
                    CoursesUsers = new CourseUser[]
                    {
                        new() { Course = course1, Grade = "F" }
                    }
                }
            });
            dbContext.SaveChanges();
        }

        using (var outerDbContextScope = _dbContextScopeFactory.Create())
        {
            var outerDbContext = outerDbContextScope.DbContexts.Get<TestDbContext>();
            var outerUsers = outerDbContext.Users
                .Include(u => u.CoursesUsers)
                .ThenInclude(cu => cu.Course)
                .ToList();

            outerUsers.Count.Should().Be(2);

            // Arrange(2) - modify the CourseUser entities in an inner scope created with ForceCreateNew
            using (var innerDbContextScope = _dbContextScopeFactory.Create(DbContextScopeOption.ForceCreateNew))
            {
                var innerDbContext = innerDbContextScope.DbContexts.Get<TestDbContext>();
                var innerUsers = innerDbContext.Users
                    .Include(u => u.CoursesUsers)
                    .ThenInclude(cu => cu.Course)
                    .ToList();

                var innerUser1CoursesUsers = innerUsers[0].CoursesUsers.ToList();
                innerUser1CoursesUsers[0].Grade = "B";
                innerUser1CoursesUsers[1].Grade = "D";

                var innerUser2CoursesUsers = innerUsers[1].CoursesUsers.ToList();
                innerUser2CoursesUsers[0].Grade = "E";

                innerDbContext.SaveChanges();

                // Entities in outer scope should be unchanged at this point
                // since we have not refreshed the entities yet
                var outerUser1CoursesUsers = outerUsers[0].CoursesUsers.ToList();
                outerUser1CoursesUsers[0].Grade.Should().Be("A");
                outerUser1CoursesUsers[1].Grade.Should().Be("C");

                var outerUser2CoursesUsers = outerUsers[1].CoursesUsers.ToList();
                outerUser2CoursesUsers[0].Grade.Should().Be("F");

                // Act - only refresh the first user's CoursesUsers in the parent scope,
                // but NOT the second user's
                innerDbContextScope.RefreshEntitiesInParentScope(new CourseUser[]
                {
                    outerUser1CoursesUsers[0],
                    outerUser1CoursesUsers[1]
                });

                // Assert
                outerUser1CoursesUsers[0].Grade.Should().Be("B"); // new value
                outerUser1CoursesUsers[1].Grade.Should().Be("D"); // new value
                outerUser2CoursesUsers[0].Grade.Should().Be("F"); // unchanged from original value
            }
        }
    }

    [Fact]
    public void Calling_SuppressAmbientContext_should_suppress_ambient_DbContextScope()
    {
        using var dbContextScope = _dbContextScopeFactory.Create();
        dbContextScope.Should().NotBeNull();

        var outerAmbientContextLocator = new AmbientDbContextLocator();
        var outerContext1 = outerAmbientContextLocator.Get<TestDbContext>();
        outerContext1.Should().NotBeNull();

        using (var suppressor = _dbContextScopeFactory.SuppressAmbientContext())
        {
            var suppressedAmbientContextLocator = new AmbientDbContextLocator();

            // Since we have suppressed the ambient DbContextScope here, we should
            // not be able to get a DbContext from the innerAmbientContextLocator
            var suppressedContext = suppressedAmbientContextLocator.Get<TestDbContext>();
            suppressedContext.Should().BeNull();

            // And any new DbContextScope should not join the existing one
            using (var innerDbContextScope = _dbContextScopeFactory.Create())
            {
                innerDbContextScope.Should().NotBeNull();

                var innerAmbientContextLocator = new AmbientDbContextLocator();
                var innerContext = innerAmbientContextLocator.Get<TestDbContext>();
                innerContext.Should().NotBeNull();
                innerContext.Should().NotBeSameAs(outerContext1);
            }
        }

        // The original ambient DbContextScope should be restored here
        var outerContext2 = outerAmbientContextLocator.Get<TestDbContext>();
        outerContext1.Should().NotBeNull();
        outerContext2.Should().BeSameAs(outerContext1);
    }

    [Fact]
    public void Multiple_threads_which_create_a_DbContextScope_use_separate_DbContexts()
    {
        const int threadCount = 4;

        // We will use an ObjectIDGenerator to get a unique ID for each unique object
        var idGenerator = new ObjectIDGenerator();

        // We need a lock object because ObjectIDGenerator is not thread-safe
        var lockObject = new object();

        // Initialize some collections to hold the object IDs of the DbContextScope
        // and DbContext entities we will create
        var dbContextScopeIds = new List<long>();
        var dbContextIds = new List<long>();

        Parallel.For(
            fromInclusive: 0,
            toExclusive: threadCount,
            parallelOptions: new ParallelOptions { MaxDegreeOfParallelism = threadCount },
            body: i =>
            {
                using var dbContextScope = _dbContextScopeFactory.Create();
                dbContextScope.Should().NotBeNull();

                lock (lockObject)
                {
                    var dbContextScopeId = idGenerator.GetId(dbContextScope, out bool _);
                    dbContextScopeIds.Add(dbContextScopeId);
                }

                var dbContext = dbContextScope.DbContexts.Get<TestDbContext>();
                dbContext.Should().NotBeNull();

                lock (lockObject)
                {
                    var dbContextId = idGenerator.GetId(dbContext, out bool _);
                    dbContextIds.Add(dbContextId);
                }
            });

        // We should have a unique DbContextScope and DbContext for each thread
        dbContextScopeIds.Count.Should().Be(threadCount);
        dbContextIds.Count.Should().Be(threadCount);

        dbContextScopeIds.Should().OnlyHaveUniqueItems();
        dbContextIds.Should().OnlyHaveUniqueItems();
    }
}