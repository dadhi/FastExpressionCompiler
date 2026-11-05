using System;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

[RunTestX]
public struct RegisteredTestXTests : ITestX
{
    public void Run(TestRun tests)
    {
        Discovery_is_opt_in(tests);
        Discovery_has_deterministic_order(tests);
    }

    public void Discovery_is_opt_in(TestContext t)
    {
        var fixtures = RegisteredTestX.GetFixtureTypes();
        t.IsTrue(Array.IndexOf(fixtures, typeof(RegisteredTestXTests)) >= 0);
        t.IsTrue(Array.IndexOf(fixtures, typeof(UnregisteredFixture)) < 0);
        t.IsTrue(Array.IndexOf(fixtures, typeof(ParityTests)) < 0);
    }

    public void Discovery_has_deterministic_order(TestContext t)
    {
        var fixtures = RegisteredTestX.GetFixtureTypes();
        for (var i = 1; i < fixtures.Length; ++i)
            t.IsTrue(StringComparer.Ordinal.Compare(fixtures[i - 1].FullName, fixtures[i].FullName) < 0);
    }

    private struct UnregisteredFixture : ITestX
    {
        public void Run(TestRun tests) => throw new InvalidOperationException("Unregistered fixtures must not run.");
    }
}
