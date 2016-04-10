using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NSpectator.Domain;
using NSpectator.Domain.Formatters;
using FluentAssertions;
using NSpectator;
// ReSharper disable once CheckNamespace

/// <summary>
/// Howdy,
/// 
/// <para>NSpectator's DebuggerShim allow you to use Resharper's test runner to run specs that are in the same Assembly as this class.</para>
/// <para>You can also inherit your Program class from this class and explicitly call Debug or wrap it into any unit test.</para>
/// <para>For more information visit https://github.com/nspectator/NSpectator/wiki </para>
/// </summary>
public partial class DebuggerShim
{
    protected void DebugNestedTypes()
    {
        Debug(GetType().GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public));
    }

    public static void Debug(System.Type t)
    {
        Debug(new SpecFinder(new[] { t }, ""));
    }

    public static void Debug(IEnumerable<System.Type> types)
    {
        Debug(new SpecFinder(types.ToArray(), ""));
    }

    private static void Debug(SpecFinder finder)
    {
        var builder = new ContextBuilder(finder, new DefaultConventions());
        var runner = new ContextRunner(new Tags(), new ConsoleFormatter(), false);
        var results = runner.Run(builder.Contexts().Build());

        // assert that there aren't any failures
        results.Failures().Count().Should().Be(0, "all examples passed");
    }
}


