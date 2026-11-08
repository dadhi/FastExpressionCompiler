using System;
using E = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ReadonlyRefFieldRegressionTests : ITestX
{
    public void Run(TestRun t) => Readonly_instance_fields_supply_temporary_references(t);

    public sealed class Owner
    {
        public readonly int Number = 3;
        public readonly string Text = "original";
    }

    public static int Add(ref int value) => ++value;
    public static int Replace(ref string value)
    {
        value = "replaced";
        return value.Length;
    }

    public void Readonly_instance_fields_supply_temporary_references(TestContext t)
    {
        foreach (var name in new[] { nameof(Owner.Number), nameof(Owner.Text) })
        {
            var parameter = E.Parameter(typeof(Owner));
            var method = typeof(ReadonlyRefFieldRegressionTests).GetMethod(
                name == nameof(Owner.Number) ? nameof(Add) : nameof(Replace));
            var tree = E.Lambda<Func<Owner, int>>(E.Call(method, E.Field(parameter, name)), parameter);
            var system = tree.Compile();
            var fast = tree.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;

            var expected = new Owner();
            var actual = new Owner();
            t.AreEqual(system(expected), fast(actual));
            t.AreEqual(expected.Number, actual.Number);
            t.AreEqual(expected.Text, actual.Text);
        }
    }
}
