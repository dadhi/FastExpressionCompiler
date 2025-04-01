#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

using System;
using System.Linq.Expressions;
using System.Reflection.Emit;

public struct Issue461_InvalidProgramException_when_null_checking_type_by_ref : ITest
{
    public int Run()
    {
        Case_not_equal_nullable_decimal();
        // Case_equal_nullable_and_object_null();
        // Case_equal_nullable_and_nullable_null_on_the_left();
        Original_case();
        Original_case_null_on_the_right();
        return 5;
    }

    private class Target
    {
        public int X { get; set; }
    }

    private delegate R InFunc<T, out R>(in T t);

    public void Original_case()
    {
        var p = Parameter(typeof(Target).MakeByRefType(), "p");

        var expr = Lambda<InFunc<Target, bool>>(
            MakeBinary(ExpressionType.Equal, p, Constant(null)),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        Asserts.IsTrue(fs(null));
        Asserts.IsFalse(fs(new Target()));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        Asserts.IsTrue(ff(null));
        Asserts.IsFalse(ff(new Target()));

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldind_Ref,
            OpCodes.Ldnull,
            OpCodes.Ceq,
            OpCodes.Ret
        );
    }

    public void Original_case_null_on_the_right()
    {
        var p = Parameter(typeof(Target).MakeByRefType(), "p");

        var expr = Lambda<InFunc<Target, bool>>(
            MakeBinary(ExpressionType.NotEqual, Constant(null), p),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        Asserts.IsFalse(fs(null));
        Asserts.IsTrue(fs(new Target()));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        Asserts.IsFalse(ff(null));
        Asserts.IsTrue(ff(new Target()));

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldind_Ref,
            OpCodes.Ldnull,
            OpCodes.Ceq,
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );
    }

    public struct XX { }

    public void Case_equal_nullable_and_object_null()
    {
        var p = Parameter(typeof(XX?).MakeByRefType(), "xx");

        var expr = Lambda<InFunc<XX?, bool>>(
            MakeBinary(ExpressionType.Equal, p, Constant(null)),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        Asserts.IsTrue(fs(null));
        Asserts.IsFalse(fs(new XX()));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        Asserts.IsTrue(ff(null));
        Asserts.IsFalse(ff(new XX()));

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call, // .get_HasValue()
            OpCodes.Ldc_I4_0,
            OpCodes.Ceq,
            OpCodes.Ret
        );
    }

    public void Case_equal_nullable_and_nullable_null_on_the_left()
    {
        var p = Parameter(typeof(XX?).MakeByRefType(), "xx");

        var expr = Lambda<InFunc<XX?, bool>>(
            MakeBinary(ExpressionType.NotEqual, Constant(null, typeof(XX?)), p),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        Asserts.IsFalse(fs(null));
        Asserts.IsTrue(fs(new XX()));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        Asserts.IsFalse(ff(null));
        Asserts.IsTrue(ff(new XX()));

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call, // .get_HasValue()
            OpCodes.Ret
        );
    }

    public void Case_not_equal_nullable_decimal()
    {
        var p = Parameter(typeof(Decimal?), "d");

        var expr = Lambda<Func<Decimal?, bool>>(
            MakeBinary(ExpressionType.NotEqual, p, Constant(null, typeof(Decimal?))),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        Asserts.IsTrue(fs(1.142m));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        Asserts.IsTrue(ff(1.142m));

        // ff.AssertOpCodes(
        //     OpCodes.Ldarg_1,
        //     OpCodes.Call, // .get_HasValue()
        //     OpCodes.Ldc_I4_0,
        //     OpCodes.Ceq,
        //     OpCodes.Ret
        // );
    }
}
