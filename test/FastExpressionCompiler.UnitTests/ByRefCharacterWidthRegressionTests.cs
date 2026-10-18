using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ByRefCharacterWidthRegressionTests : ITestX
{
    public delegate char Read(ref char value);
    public delegate char Write(ref char value, char replacement);
    private static readonly char[] Values = { '\0', '\u00FF', '\u0100', '\u1234', '\uABCD', '\uD800', '\uDC00', '\uFFFF' };

    public void Run(TestRun t)
    {
        Ref_character_reads_preserve_both_bytes(t);
        Ref_character_assignments_replace_both_bytes(t);
    }

    public void Ref_character_reads_preserve_both_bytes(TestContext t)
    {
        var parameter = S.Parameter(typeof(char).MakeByRefType());
        var tree = S.Lambda<Read>(parameter, parameter);
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var system = tree.Compile();
        foreach (var initial in Values)
        {
            var value = initial;
            t.AreEqual(initial, fast(ref value));
            t.AreEqual(system(ref value), fast(ref value));
            t.AreEqual(initial, value);
        }
    }

    public void Ref_character_assignments_replace_both_bytes(TestContext t)
    {
        var parameter = S.Parameter(typeof(char).MakeByRefType());
        var replacement = S.Parameter(typeof(char));
        var tree = S.Lambda<Write>(S.Assign(parameter, replacement), parameter, replacement);
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var system = tree.Compile();
        foreach (var initial in Values) foreach (var next in Values)
        {
            var actual = initial;
            var expected = initial;
            t.AreEqual(system(ref expected, next), fast(ref actual, next));
            t.AreEqual(next, actual);
            t.AreEqual(expected, actual);
        }
    }
}
