using Microsoft.EntityFrameworkCore;
using System;

namespace Slant.Entity;

/// <summary>
/// Maintains a list of lazily-created DbContext instances.
/// </summary>
public interface IDbContextCollection : IDisposable
{
    /// <summary>
    /// Get or create a DbContext instance of the specified type.
    /// </summary>
    TDbContext Get<TDbContext>() where TDbContext : DbContext;
}