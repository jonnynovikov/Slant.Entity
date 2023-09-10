﻿using System;
using System.Data;

namespace Slant.Entity;

public class DbContextScopeFactory : IDbContextScopeFactory
{
    private readonly IDbContextFactory? _dbContextFactory;

    public DbContextScopeFactory(IDbContextFactory? dbContextFactory = null)
    {
        _dbContextFactory = dbContextFactory;
    }

    public IDbContextScope Create(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
        return new DbContextScope(
            joiningOption: joiningOption,
            readOnly: false,
            isolationLevel: null,
            dbContextFactory: _dbContextFactory);
    }

    public IDbContextReadOnlyScope CreateReadOnly(DbContextScopeOption joiningOption = DbContextScopeOption.JoinExisting)
    {
        return new DbContextReadOnlyScope(
            joiningOption: joiningOption,
            isolationLevel: null,
            dbContextFactory: _dbContextFactory);
    }

    public IDbContextScope CreateWithTransaction(IsolationLevel isolationLevel)
    {
        return new DbContextScope(
            joiningOption: DbContextScopeOption.ForceCreateNew,
            readOnly: false,
            isolationLevel: isolationLevel,
            dbContextFactory: _dbContextFactory);
    }

    public IDbContextReadOnlyScope CreateReadOnlyWithTransaction(IsolationLevel isolationLevel)
    {
        return new DbContextReadOnlyScope(
            joiningOption: DbContextScopeOption.ForceCreateNew,
            isolationLevel: isolationLevel,
            dbContextFactory: _dbContextFactory);
    }

    public IDisposable SuppressAmbientContext()
    {
        return new AmbientContextSuppressor();
    }
}