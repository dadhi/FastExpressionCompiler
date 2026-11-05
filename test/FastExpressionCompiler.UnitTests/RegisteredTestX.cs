using System;
using System.Collections.Generic;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

/// <summary>Opt a parameterless ITestX fixture into both console runners.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class RunTestXAttribute : Attribute { }

/// <summary>Only explicitly marked unit-test fixtures are discovered. Legacy tests and
/// runtime-accessor preflight contracts keep their existing entry points.</summary>
public static class RegisteredTestX
{
    public static Type[] GetFixtureTypes()
    {
        var fixtures = new List<Type>();
        foreach (var type in typeof(RunTestXAttribute).Assembly.GetTypes())
        {
            if (!type.IsDefined(typeof(RunTestXAttribute), inherit: false))
                continue;
            if (!typeof(ITestX).IsAssignableFrom(type))
                throw new InvalidOperationException($"[RunTestX] fixture {type.FullName} must implement ITestX.");
            fixtures.Add(type);
        }
        fixtures.Sort((left, right) => StringComparer.Ordinal.Compare(left.FullName, right.FullName));
        return fixtures.ToArray();
    }

    public static void Run(TestRun tests)
    {
        foreach (var type in GetFixtureTypes())
            tests.Run((ITestX)Activator.CreateInstance(type));
    }
}
