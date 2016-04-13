Slant library for Entity Framework
==============

[![Join the chat at https://gitter.im/slantdotnet/Slant.Entity](https://badges.gitter.im/slantdotnet/Slant.Entity.svg)](https://gitter.im/slantdotnet/Slant.Entity?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![NuGet version (Slant.Entity)](https://img.shields.io/nuget/v/Slant.Entity.svg?style=flat)](https://www.nuget.org/packages/Slant.Entity/)
[![Build Status](https://travis-ci.org/slantdotnet/Slant.Entity.svg?branch=master)](https://travis-ci.org/slantdotnet/Slant.Entity)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/slantdotnet/Slant.Entity/master/license.txt)

## Overview

Library extends an [Slant.Linq](https://github.com/slantdotnet/Slant.Linq) for simplify build LINQ expressions with EF query provider.

It contains powerful implementation of `DbContextScope` - source of the forked project with the same name as class.

####Slant.Linq 

Slant.Entity use extensions for LINQ and Entity Framework power users. 

It comprises the following:

* An extensible implementation of AsExpandable()
* A public expression visitor base class (ExpressionVisitor)
* PredicateBuilder
* Linq.Expr and Linq.Func shortcut methods

With Slant.Linq and Slant.Entity, you can:

* Plug expressions into EntitySets and EntityCollections
* Use expression variables in subqueries
* Combine expressions (have one expression call another)
* Dynamically build predicates
* Leverage AsExpandable to add your own extensions.

####Slant.Entity

It provides simple and flexible way to manage your Entity Framework DbContext instances.

`DbContextScope` was created out of the need for a better way to manage DbContext instances in Entity Framework-based applications. 

The commonly advocated method of injecting DbContext instances works fine for single-threaded web applications where each web request implements exactly one business transaction. But it breaks down quite badly when console apps, Windows Services, parallelism and requests that need to implement multiple independent business transactions make their appearance.

The alternative of manually instantiating DbContext instances and manually passing them around as method parameters is (speaking from experience) more than cumbersome. 

`DbContextScope` implements the ambient context pattern for DbContext instances. 

It's something that NHibernate users or anyone who has used the `TransactionScope` class to manage ambient database transactions will be familiar with.

It doesn't force any particular design pattern or application architecture to be used. It works beautifully with dependency injection. 

And it works beautifully without. It of course works perfectly with async execution flows, including with the new async / await support introduced in .NET 4.5 and EF6. 

And most importantly, at the time of writing, `DbContextScope` has been battle-tested in a large-scale applications for a long time. 

## Installation

We recommended installing [the NuGet package](https://www.nuget.org/packages/Slant.Entity). Install on the command line from your solution directory or use the Package Manager console in Visual Studio:

```powershell

PM> Install-Package Slant.Entity

```

## Usage

Check out wiki of the [Slant.Entity](https://github.com/slantdotnet/Slant.Entity) to see typical and non-trivial usages and detailed examples:

[Using LINQ Extensions](https://github.com/slantdotnet/Slant.Entity/wiki/Using-LINQ-Extensions)

## Contributing

Check out [this wiki page](https://github.com/slantdotnet/Slant.Entity/wiki/Contributing) for complete guide.

## Thanks to

Jetbrains Community Support for providing great tools for our team

[![Jetbrains Resharper](http://nspectator.org/assets/icon_ReSharper.png)](https://www.jetbrains.com/resharper/)



