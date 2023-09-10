using Microsoft.EntityFrameworkCore;
using System;

namespace Slant.Entity;

/// <summary>
/// A factory to create DbContext instances.
/// </summary>
/// <remarks>
/// It should be registered in a dependency injection container as a singleton.
/// </remarks>
public class RegisteredDbContextFactory : IDbContextFactory
{
    private readonly FuncDictionaryByType _dictionary = new();

    public void RegisterDbContextType<TDbContext>(Func<TDbContext> createDbContextFunc) where TDbContext : DbContext
    {
        if (createDbContextFunc == null)
        {
            throw new ArgumentException($"{nameof(createDbContextFunc)} must not be null.", nameof(createDbContextFunc));
        }

        _dictionary.Add(createDbContextFunc);
    }

    public TDbContext CreateDbContext<TDbContext>() where TDbContext : DbContext
    {
        var isTypeRegistered = _dictionary.TryGet<TDbContext>(out var createDbContextFunc);

        if (!isTypeRegistered)
        {
            throw new InvalidOperationException($"{typeof(TDbContext).Name} was not registered with the {typeof(RegisteredDbContextFactory).Name} instance. Make sure you call {nameof(RegisterDbContextType)}.");
        }

        if (createDbContextFunc != default)
        {
            return createDbContextFunc();
        }

        throw new InvalidOperationException($"Unrecognized DbContext type: {typeof(TDbContext).Name}.");
    }
}