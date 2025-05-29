using System;
using System.Reflection.Emit;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif


public class Issue418_Wrong_output_when_comparing_NaN_value : ITest
{
    public int Run()
    {
        Original_case();
        Case_with_int();
        Case_with_Uint();
        Case_with_less_then();
        Case_with_one();
        Case_with_Minus_one();
        Case_with_lte_instead_of_gte();
        return 7;
    }


    public void Original_case()
    {
        var p = Parameter(typeof(double));
        var expr = Lambda<Func<double, bool>>(GreaterThanOrEqual(p, Constant(0.0)), p);

        expr.PrintCSharp();
        // outputs
        var @cs = (Func<double, bool>)((double double__58225482) => //bool
            double__58225482 >= 0);

        Asserts.IsFalse(@cs(double.NaN));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_R8,
            OpCodes.Clt_Un,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Asserts.IsFalse(fs(double.NaN));
        Asserts.IsFalse(ff(double.NaN));
    }


    public void Case_with_int()
    {
        var p = Parameter(typeof(int));
        var expr = Lambda<Func<int, bool>>(GreaterThanOrEqual(p, Constant(0)), p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_I4_0,
            OpCodes.Clt,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Asserts.IsFalse(fs(int.MinValue));
        Asserts.IsFalse(ff(int.MinValue));
    }


    public void Case_with_Uint()
    {
        var p = Parameter(typeof(uint));
        var expr = Lambda<Func<uint, bool>>(GreaterThanOrEqual(p, Constant(0u, typeof(uint))), p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_I4_0,
            OpCodes.Clt_Un,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Asserts.IsTrue(fs(uint.MinValue));
        Asserts.IsTrue(ff(uint.MinValue));
    }


    public void Case_with_less_then()
    {
        var p = Parameter(typeof(double));
        var expr = Lambda<Func<double, bool>>(LessThan(p, Constant(0.0)), p);

        expr.PrintCSharp();
        // outputs
        var @cs = (Func<double, bool>)((double double__58225482) => //bool
            double__58225482 < 0);

        Asserts.IsFalse(@cs(double.NaN));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_R8,
            OpCodes.Clt,
            OpCodes.Ret
        );

        Asserts.IsFalse(fs(double.NaN));
        Asserts.IsFalse(ff(double.NaN));
    }


    public void Case_with_one()
    {
        var p = Parameter(typeof(double));
        var expr = Lambda<Func<double, bool>>(GreaterThanOrEqual(p, Constant(1.0)), p);

        expr.PrintCSharp();
        // outputs
        var @cs = (Func<double, bool>)((double double__58225482) => //bool
            double__58225482 >= 1.0);

        Asserts.IsFalse(@cs(double.NaN));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_R8,
            OpCodes.Clt_Un,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Asserts.IsFalse(fs(double.NaN));
        Asserts.IsFalse(ff(double.NaN));
    }


    public void Case_with_Minus_one()
    {
        var p = Parameter(typeof(double));
        var expr = Lambda<Func<double, bool>>(GreaterThanOrEqual(p, Constant(-1.0)), p);

        expr.PrintCSharp();
        // outputs
        var @cs = (Func<double, bool>)((double double__58225482) => //bool
            double__58225482 >= -1.0);

        Asserts.IsFalse(@cs(double.NaN));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_R8,
            OpCodes.Clt_Un,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Asserts.IsFalse(fs(double.NaN));
        Asserts.IsFalse(ff(double.NaN));
    }


    public void Case_with_lte_instead_of_gte()
    {
        var p = Parameter(typeof(double));
        var expr = Lambda<Func<double, bool>>(LessThanOrEqual(p, Constant(0.0)), p);

        expr.PrintCSharp();
        // outputs
        var @cs = (Func<double, bool>)((double double__58225482) => //bool
            double__58225482 <= 0);

        Asserts.IsFalse(@cs(double.NaN));

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldc_R8,
            OpCodes.Cgt_Un,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );

        Asserts.IsFalse(fs(double.NaN));
        Asserts.IsFalse(ff(double.NaN));
    }
}