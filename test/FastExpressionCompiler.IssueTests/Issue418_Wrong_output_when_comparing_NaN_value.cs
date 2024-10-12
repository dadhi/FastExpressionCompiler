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
public class Issue418_Wrong_output_when_comparing_NaN_value : ITest
{
    public int Run()
    {
        // Original_case();
        return 1;
    }

    [Test]
    public void Original_case()
    {
        var p = Parameter(typeof(double));
        var expr = Lambda<Func<double, bool>>(GreaterThanOrEqual(p, Constant(0.0)), p);

        expr.PrintCSharp();
        // outputs
        var @cs = (Func<double, bool>)((double double__58225482) => //bool
            double__58225482 >= 0);

        Assert.IsFalse(@cs(double.NaN));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_R8,
            OpCodes.Clt_Un,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Assert.IsFalse(fs(double.NaN));
        Assert.IsFalse(ff(double.NaN));
    }
}