using System;
using System.Reflection.Emit;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue426_Directly_passing_a_method_result_to_another_method_by_ref_fails_with_InvalidProgramException : ITest
{
    public int Run()
    {
        // Original_case();
        return 1;
    }

    public static class Numbers
    {
        public static int GetInt() => 40;
        public static int AddTwo(ref int value) => value + 2;
    }

    [Test]
    public void Original_case()
    {
        var getIntMethod = typeof(Numbers).GetMethod(nameof(Numbers.GetInt));
        var addTwoMethod = typeof(Numbers).GetMethod(nameof(Numbers.AddTwo));

        var expr = Lambda<Func<int>>(Call(addTwoMethod, Call(getIntMethod)));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Call,
            OpCodes.Stloc_0,
            OpCodes.Ldloca_S,
            OpCodes.Call,
            OpCodes.Ret
        );

        Assert.AreEqual(42, fs());
        Assert.AreEqual(42, ff());
    }
}