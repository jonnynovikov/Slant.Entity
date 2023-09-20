using System.Data;

namespace Slant.Entity;

public class DbContextReadOnlyScope : IDbContextReadOnlyScope
{
    private readonly DbContextScope _internalScope;

    public IDbContextCollection DbContexts => _internalScope.DbContexts;

    public DbContextReadOnlyScope(IDbContextFactory? dbContextFactory = null)
        : this(joiningOption: DbContextScopeOption.JoinExisting, isolationLevel: null, dbContextFactory: dbContextFactory)
    { }

    public DbContextReadOnlyScope(IsolationLevel isolationLevel, IDbContextFactory? dbContextFactory = null)
        : this(joiningOption: DbContextScopeOption.ForceCreateNew, isolationLevel: isolationLevel, dbContextFactory: dbContextFactory)
    { }

    public DbContextReadOnlyScope(DbContextScopeOption joiningOption, IsolationLevel? isolationLevel, IDbContextFactory? dbContextFactory = null)
    {
        _internalScope = new DbContextScope(joiningOption: joiningOption, readOnly: true, isolationLevel: isolationLevel, dbContextFactory: dbContextFactory);
    }

    public void Dispose()
    {
        _internalScope.Dispose();
    }
}