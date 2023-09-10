Slant.Entity
==============

Library for managing DbContext the right way with [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/).

[![NuGet version (Slant.Entity)](https://img.shields.io/nuget/v/Slant.Entity.svg?style=flat-square)](https://www.nuget.org/packages/Slant.Entity/)
[![NuGet](https://img.shields.io/nuget/dt/Slant.Entity.svg)](https://www.nuget.org/packages/Slant.Entity)
![Build Status](https://github.com/jonnynovikov/Slant.Entity/actions/workflows/dotnet.yml/badge.svg)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/jonnynovikov/Slant.Entity/master/LICENSE.txt)

## Overview

This package is based on the original [DbContextScope repository](https://github.com/mehdime/DbContextScope) by Mehdi El Gueddari with the following changes:

- projects were updated to .NET 6+ and Entity Framework Core;
- usages of `CallContext` were replaced with `AsyncLocal`;
- added fix for `RefreshEntitiesInParentScope` method so that it works correctly for entities with composite primary keys;
- added fix for `DbContextCollection`'s `Commit` and `CommitAsync` methods so that `SaveChanges` can be called more than once if there is a `DbUpdateConcurrencyException` (see [this](https://github.com/mehdime/DbContextScope/pull/31) unmerged pull request in the original `DbContextScope` repository);
- added the `RegisteredDbContextFactory` class as a concrete implementation of the `IDbContextFactory` interface, which allows users to easily register factory functions for one or more `DbContext` type(s) during startup; and
- added unit tests.

## Description

Library provides simple and flexible way to manage your Entity Framework Core DbContext instances.

`DbContextScope` was created out of the need for a better way to manage DbContext instances in Entity Framework-based applications.

The commonly advocated method of injecting DbContext instances works fine for single-threaded web applications where each web request implements exactly one business transaction. But it breaks down quite badly when console apps, Windows Services, parallelism and requests that need to implement multiple independent business transactions make their appearance.

The alternative of manually instantiating DbContext instances and manually passing them around as method parameters is (speaking from experience) more than cumbersome.

`DbContextScope` implements the ambient context pattern for DbContext instances. It doesn't force any particular design pattern or application architecture to be used. It works beautifully with dependency injection and works like a charm without any IoC container.

And most importantly, `DbContextScope` has been battle-tested in a large-scale applications for a long time.

Mehdi El Gueddari's original article describing the thinking behind the `DbContextScope` library can be found [here](https://mehdi.me/ambient-dbcontext-in-ef6/).

In summary, the library addresses the problem that injecting `DbContext` instances as a scoped dependency (which ordinarily results in one instance per web request) offers insufficient control over the lifetime of `DbContext` instances in more complex scenarios.

The `DbContextScope` library allows users to create scopes which control the lifetime of ambient `DbContext` instances, as well giving control over the exact time at which changes are saved.

For general usage instructions, see article referred to above and the original GitHub repository readme file.
Copy of original README with improved formatting and fixing broken links is included in this repository [here](./docs/DbContextScope.md). 

Please note the `Mehdime.Entity` namespace has been renamed to `Slant.Entity` due to naming conventions in Slant packages.

The new `RegisteredDbContextFactory` class can be used as follows:

- In `Startup.cs`, register a `RegisteredDbContextFactory` instance as a singleton and register one or more `DbContext` factory functions on that instance, e.g.:
``` csharp
using Slant.Entity;
...
public void ConfigureServices(IServiceCollection services)
{
    ...
    // Create an instance of the RegisteredDbContextFactory
    var dbContextFactory = new RegisteredDbContextFactory();

    // Register factory functions for each of the required DbContext types
    dbContextFactory.RegisterDbContextType<DbContextOne>(() =>
        new DbContextOne(Configuration.GetConnectionString("DatabaseOne")));
    dbContextFactory.RegisterDbContextType<DbContextTwo>(() =>
        new DbContextTwo(Configuration.GetConnectionString("DatabaseTwo")));

    // Register the RegisteredDbContextFactory instance as a singleton
    // with the dependency injection container.
    services.AddSingleton<IDbContextFactory>(dbContextFactory);
    ...
}
```

See also the unit tests for `RegisteredDbContextFactory` [here](./Slant.Entity.Tests/RegisteredDbContextFactoryTests.cs).

## Dependencies

- .NET 6+
- Entity Framework Core 7

## Installation

```powershell
dotnet add package Slant.Entity
```

## Acknowledgments

Many thanks to Mehdi El Gueddari for creating the original `DbContextScope` library.



