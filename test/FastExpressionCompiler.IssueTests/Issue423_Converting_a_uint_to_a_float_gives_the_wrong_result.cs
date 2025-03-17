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
public class Issue423_Converting_a_uint_to_a_float_gives_the_wrong_result : ITest
{
    public int Run()
    {
        Original_case();
        return 1;
    }

    [Test]
    public void Original_case()
    {
        var p = Parameter(typeof(uint), "p");
        var expr = Lambda<Func<uint, float>>(Convert(p, typeof(float)), p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Conv_R_Un,
            OpCodes.Conv_R4,
            OpCodes.Ret
        );

        Asserts.AreEqual((float)uint.MaxValue, fs(uint.MaxValue));

        Asserts.AreEqual((float)uint.MaxValue, ff(uint.MaxValue));
    }
}