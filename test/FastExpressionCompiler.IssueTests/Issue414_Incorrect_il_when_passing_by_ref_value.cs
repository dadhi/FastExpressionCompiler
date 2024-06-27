using NUnit.Framework;
using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue414_Incorrect_il_when_passing_by_ref_value : ITest
{
    public int Run()
    {
        // ReturnRefParameter();
        // PassByRefParameter();

        Issue413_VariableStructIndexer();
        Issue413_ParameterStructIndexer();
        
#if LIGHT_EXPRESSION
        // PassByRefVariable();

        // Issue415_ReturnRefParameterByRef();
        // Issue415_ReturnRefParameterByRef_ReturnRefCall();
        return 3;
#else
        return 2;
#endif
    }

    delegate int MyDelegate(ref int x);

    [Test]
    public void ReturnRefParameter()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegate>(p, p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ldind_I4,
            OpCodes.Ret
        );

        var x = 17;
        Assert.AreEqual(17, fs(ref x));

        var y = 18;
        Assert.AreEqual(18, ff(ref y));
    }

    public static void IncRef(ref int x) => ++x;

    [Test]
    public void PassByRefParameter()
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

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call,
            OpCodes.Ldarg_1,
            OpCodes.Ldind_I4,
            OpCodes.Ret
        );

        var x = 17;
        Assert.AreEqual(18, fs(ref x));

        var y = 18;
        Assert.AreEqual(19, ff(ref y));
    }

    public struct MyStruct
    {
        private int _a;
        public MyStruct() => _a = 17;
        public int this[int index] => _a * _a;
    }

    delegate int MyDelegateStruct(MyStruct x);

    [Test]
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

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        // ff.AssertOpCodes(
        //     OpCodes.Ldarg_1,
        //     OpCodes.Call,
        //     OpCodes.Ldarg_1,
        //     OpCodes.Ldind_I4,
        //     OpCodes.Ret
        // );

        Assert.AreEqual(289, fs(new MyStruct()));

        Assert.AreEqual(289, ff(new MyStruct()));
    }

    delegate int MyDelegateNoArgs();
    
    [Test]
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
        // todo: @wip
        // var @cs = (MyDelegateNoArgs)(() => //int
        // {
        //     MyStruct mystruct__32854180 = default;
        //     issue414_incorrect_il_when_passing_by_ref_value_mystruct__32854180 = new MyStruct();
        //     return issue414_incorrect_il_when_passing_by_ref_value_mystruct__32854180[1];
        // });


        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        // ff.AssertOpCodes(
        //     OpCodes.Ldarg_1,
        //     OpCodes.Call,
        //     OpCodes.Ldarg_1,
        //     OpCodes.Ldind_I4,
        //     OpCodes.Ret
        // );

        Assert.AreEqual(289, fs());

        Assert.AreEqual(289, ff());
    }

#if LIGHT_EXPRESSION
    delegate ref int MyDelegateByRef(ref int x);

    [Test]
    public void Issue415_ReturnRefParameterByRef()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegateByRef>(p, p);

        expr.PrintCSharp();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Ret
        );

        var x = 17;
        ++ff(ref x);
        Assert.AreEqual(18, x);
    }


    public static ref int ReturnRef(ref int x) => ref x;

    [Test]
    public void Issue415_ReturnRefParameterByRef_ReturnRefCall()
    {
        var p = Parameter(typeof(int).MakeByRefType());
        var expr = Lambda<MyDelegateByRef>(
            Call(GetType().GetMethod(nameof(ReturnRef)), p),
            p);

        expr.PrintCSharp();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        ff.AssertOpCodes(
            OpCodes.Ldarg_1,
            OpCodes.Call,
            OpCodes.Ret
        );

        var x = 17;
        ++ff(ref x);
        Assert.AreEqual(18, x);
    }

    delegate int MyDelegateNoPars();

    [Test]
    public void PassByRefVariable()
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

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
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

        Assert.AreEqual(18, ff());
    }
#endif
}