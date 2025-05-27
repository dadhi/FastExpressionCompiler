
using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.IssueTests;
#endif


public class Issue414_Incorrect_il_when_passing_by_ref_value : ITest
{
    public int Run()
    {
        Issue413_ParameterStructIndexer();
        Issue413_VariableStructIndexer();

        Issue414_ReturnRefParameter();
        Issue414_PassByRefParameter();

#if LIGHT_EXPRESSION
        Issue414_PassByRefVariable();
#endif

#if LIGHT_EXPRESSION && !NET472
        // NET472 does not support ref returns
        // todo: @fixme
        // Issue415_ReturnRefParameter_ReturnRefCall();
        Issue415_ReturnRefParameterByRef();
        Issue415_ReturnRefParameterByRef_ReturnRefCall();
        return 8;
#else
        return 5;
#endif
    }

    delegate int MyDelegate(ref int x);


    public void Issue414_ReturnRefParameter()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegate>(p, p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldind_I4,
            OpCodes.Ret
        );

        var x = 17;
        Asserts.AreEqual(17, fs(ref x));

        var y = 18;
        Asserts.AreEqual(18, ff(ref y));
    }

    public static void IncRef(ref int x) => ++x;


    public void Issue414_PassByRefParameter()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegate>(
            Block(
                Call(GetType().GetMethod(nameof(IncRef))!, p),
                p
            ),
            p
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call,
            OpCodes.Ldarg_1,
            OpCodes.Ldind_I4,
            OpCodes.Ret
        );

        var x = 17;
        Asserts.AreEqual(18, fs(ref x));

        var y = 18;
        Asserts.AreEqual(19, ff(ref y));
    }

    public struct MyStruct
    {
        private int _a;
        public MyStruct() => _a = 17;
        public int this[int index] => _a * _a;
    }

    delegate int MyDelegateStruct(MyStruct x);


    public void Issue413_ParameterStructIndexer()
    {
        var p = Parameter(typeof(MyStruct));
        var expr = Lambda<MyDelegateStruct>(
            Property(p, "Item", Constant(1)),
            p
        );

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarga_S,
            OpCodes.Ldc_I4_1,
            OpCodes.Call,
            OpCodes.Ret
        );

        Asserts.AreEqual(289, fs(new MyStruct()));
        Asserts.AreEqual(289, ff(new MyStruct()));
    }

    delegate int MyDelegateNoArgs();


    public void Issue413_VariableStructIndexer()
    {
        var p = Parameter(typeof(MyStruct));

        var expr = Lambda<MyDelegateNoArgs>(
            Block(
                new[] { p },
                Assign(p, New(typeof(MyStruct))),
                Property(p, "Item", Constant(1))
            )
        );

        expr.PrintCSharp();
        // var @cs = (MyDelegateNoArgs)(() => //int
        // {
        //     MyStruct mystruct__32854180 = default;
        //     mystruct__32854180 = new MyStruct();
        //     return mystruct__32854180[1];
        // });

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Newobj,
            OpCodes.Stloc_0,
            OpCodes.Ldloca_S,
            OpCodes.Ldc_I4_1,
            OpCodes.Call,
            OpCodes.Ret
        );

#if !NET472
        // todo: The .NET 472 version generates the wrong IL
        Asserts.AreEqual(289, fs());
#endif
        Asserts.AreEqual(289, ff());
    }

#if LIGHT_EXPRESSION
    delegate int MyDelegateNoPars();


    public void Issue414_PassByRefVariable()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegateNoPars>(
            Block(
                new[] { p },
                Assign(p, Constant(17)),
                Call(GetType().GetMethod(nameof(IncRef)), p),
                p
            )
        );

        expr.PrintCSharp();

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldc_I4_S,
            OpCodes.Stloc_0,
            OpCodes.Ldloca_S,
            OpCodes.Dup,
            OpCodes.Call,
            OpCodes.Ldind_I4,
            OpCodes.Ret
        );

        Asserts.AreEqual(18, ff());
    }
#endif

#if LIGHT_EXPRESSION && !NET472
    delegate ref int MyDelegateByRef(ref int x);

    
    public void Issue415_ReturnRefParameterByRef()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegateByRef>(p, p);

        expr.PrintCSharp();
        // var @cs = (MyDelegateByRef)((ref int int__32854180) => //Int32
        //     ref int__32854180);

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ret
        );

        var x = 17;
        ++ff(ref x);
        Asserts.AreEqual(18, x);
    }

    public static ref int ReturnRef(ref int x) => ref x;

    
    public void Issue415_ReturnRefParameterByRef_ReturnRefCall()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegateByRef>(
            Call(GetType().GetMethod(nameof(ReturnRef)), p),
            p);

        expr.PrintCSharp();
        // var @cs = (MyDelegateByRef)((ref int int__32854180) => //Int32
        //     ref Issue414_Incorrect_il_when_passing_by_ref_value.ReturnRef(ref int__32854180));

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call,
            OpCodes.Ret
        );

        var x = 17;
        ++ff(ref x);
        Asserts.AreEqual(18, x);
    }

    
    public void Issue415_ReturnRefParameter_ReturnRefCall()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegate>(
            Call(GetType().GetMethod(nameof(ReturnRef)), p),
            p);

        expr.PrintCSharp();
        // var @cs = (MyDelegate)((ref int int__32854180) => //Int32
        //     ref Issue414_Incorrect_il_when_passing_by_ref_value.ReturnRef(ref int__32854180));

        var ff = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        // ff.AssertOpCodes(
        //     OpCodes.Ldarg_1,
        //     OpCodes.Call,
        //     OpCodes.Ldind_I4,
        //     OpCodes.Ret
        // );

        var x = 17;
        var y = ff(ref x);
        Asserts.AreEqual(18, y);
    }

#endif
}