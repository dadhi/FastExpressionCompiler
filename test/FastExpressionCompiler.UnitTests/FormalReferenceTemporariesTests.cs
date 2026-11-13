using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct FormalReferenceTemporariesTests : ITestX
{
    public delegate int ChangeObject(ref object value);
    public delegate int RefString(ref string value);
    public sealed class Model { public string Field = "original"; public string Value => Field; }
    public sealed class Created { public int Result; public Created(ref object value) { Result = Change(ref value); } }
    public static int Change(ref object value) { var valid = value is string text && text == "original"; value = new object(); return valid ? 7 : 0; }
    public static int Increment(ref int value) => ++value;
    public void Run(TestRun t) { Covariant_arguments_use_formal_temporaries(t); Value_results_do_not_alias_their_inputs(t); Node_dispatch_preserves_addresses_and_value_temporaries(t); }
    public void Covariant_arguments_use_formal_temporaries(TestContext t)
    {
        var parameter = S.Parameter(typeof(string).MakeByRefType());
        var change = typeof(FormalReferenceTemporariesTests).GetMethod(nameof(Change));
        foreach (var kind in new[] { 0, 1, 2 })
        {
            System.Linq.Expressions.Expression body = kind == 0 ? S.Call(change, parameter) : kind == 1
                ? S.Field(S.New(typeof(Created).GetConstructor(new[] { typeof(object).MakeByRefType() }), parameter), nameof(Created.Result))
                : S.Invoke(S.Constant((ChangeObject)Change), parameter);
            var tree = S.Lambda<RefString>(body, parameter);
            foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
            {
                var fast = tree.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
                var text = "original"; t.AreEqual(7, fast(ref text)); t.AreEqual("original", text);
            }
        }
    }
    public void Value_results_do_not_alias_their_inputs(TestContext t)
    {
        var local = S.Variable(typeof(int));
        foreach (var argument in new System.Linq.Expressions.Expression[] { S.TryFinally(local, S.Empty()), S.Block(local), S.Add(local, S.Constant(0)) })
        {
            var tree = S.Lambda<Func<int>>(S.Block(new[] { local }, S.Assign(local, S.Constant(4)), S.Call(typeof(FormalReferenceTemporariesTests).GetMethod(nameof(Increment)), argument), local));
            t.AreEqual(4, tree.Compile()());
            var fast = tree.FromSysExpression().CompileFast(true); if (t.IsNotNull(fast)) t.AreEqual(4, fast());
        }
    }

    public sealed class Storage { public int Value = 4; }
    public static int Reads;
    public static int ReadFour() { ++Reads; return 4; }

    public void Node_dispatch_preserves_addresses_and_value_temporaries(TestContext t)
    {
        foreach (var kind in new[] { "parameter", "field", "index", "arrayIndex", "unbox", "call", "invoke", "constant" })
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
        {
#if NETFRAMEWORK
            // Framework's System compiler produces unverifiable IL for the unbox ref argument.
            // FEC's box-storage behavior is still asserted below on every runtime.
            if (kind != "unbox")
#endif
            {
                var expected = BuildReferenceCase(kind).Compile();
                Reads = 0;
                t.AreEqual(5, expected());
                t.AreEqual(kind == "call" || kind == "invoke" ? 1 : 0, Reads);
            }
            var fast = BuildReferenceCase(kind).FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Reads = 0;
            t.AreEqual(5, fast());
            t.AreEqual(kind == "call" || kind == "invoke" ? 1 : 0, Reads);
        }
    }

    private static System.Linq.Expressions.Expression<Func<int>> BuildReferenceCase(string kind)
    {
        var variable = S.Variable(typeof(int));
        var storage = new Storage();
        var array = new[] { 4 };
        object boxed = 4;
        System.Linq.Expressions.Expression argument;
        switch (kind)
        {
            case "parameter": argument = variable; break;
            case "field": argument = S.Field(S.Constant(storage), nameof(Storage.Value)); break;
            case "index": argument = S.ArrayAccess(S.Constant(array), S.Constant(0)); break;
            case "arrayIndex": argument = S.ArrayIndex(S.Constant(array), S.Constant(0)); break;
            case "unbox": argument = S.Unbox(S.Constant(boxed, typeof(object)), typeof(int)); break;
            case "call": argument = S.Call(typeof(FormalReferenceTemporariesTests).GetMethod(nameof(ReadFour))); break;
            case "invoke": argument = S.Invoke(S.Constant((Func<int>)ReadFour)); break;
            default: argument = S.Constant(4); break;
        }
        var call = S.Call(typeof(FormalReferenceTemporariesTests).GetMethod(nameof(Increment)), argument);
        System.Linq.Expressions.Expression body = kind == "call" || kind == "invoke" || kind == "constant"
            ? call : S.Block(call, argument);
        if (kind == "parameter") body = S.Block(new[] { variable }, S.Assign(variable, S.Constant(4)), body);
        return S.Lambda<Func<int>>(body);
    }
}
