using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct ByRefCharacterWidthRegressionTests : ITest
{
    public delegate char Read(ref char value);
    public delegate char Write(ref char value, char replacement);
    private static readonly char[] Values = { '\0', '\u00FF', '\u0100', '\u1234', '\uABCD', '\uD800', '\uDC00', '\uFFFF' };

    public int Run()
    {
        Ref_character_reads_preserve_both_bytes();
        Ref_character_assignments_replace_both_bytes();
        return 2;
    }

    public void Ref_character_reads_preserve_both_bytes()
    {
        var parameter = S.Parameter(typeof(char).MakeByRefType());
        var tree = S.Lambda<Read>(parameter, parameter);
        var fast = tree.FromSysExpression().CompileFast(true);
        Asserts.IsNotNull(fast);
        var system = tree.Compile();
        foreach (var initial in Values)
        {
            var value = initial;
            Asserts.AreEqual(initial, fast(ref value));
            Asserts.AreEqual(system(ref value), fast(ref value));
            Asserts.AreEqual(initial, value);
        }
    }

    public void Ref_character_assignments_replace_both_bytes()
    {
        var parameter = S.Parameter(typeof(char).MakeByRefType());
        var replacement = S.Parameter(typeof(char));
        var tree = S.Lambda<Write>(S.Assign(parameter, replacement), parameter, replacement);
        var fast = tree.FromSysExpression().CompileFast(true);
        Asserts.IsNotNull(fast);
        var system = tree.Compile();
        foreach (var initial in Values) foreach (var next in Values)
        {
            var actual = initial;
            var expected = initial;
            Asserts.AreEqual(system(ref expected, next), fast(ref actual, next));
            Asserts.AreEqual(next, actual);
            Asserts.AreEqual(expected, actual);
        }
    }
}
