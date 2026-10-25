using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct UnboxReceiverSpillRegressionTests : ITestX
{
    public struct Mutable
    {
        public int Value;
        public int Add(int value) => Value += value;
        public int AddRef(ref int value) { Value += value; ++value; return Value; }
    }
    public sealed class Holder { public int Number { get; set; } }
    public static string Trace;
    public static object Receiver(object value) { Trace += "receiver;"; return value; }
    public static int Argument(object original, bool fail)
    {
        Trace += "argument;";
        if (fail) throw new ApplicationException("argument");
        typeof(Mutable).GetField(nameof(Mutable.Value)).SetValue(original, 10);
        return 3;
    }
    public static void Finally() => Trace += "finally;";
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };

    public void Run(TestRun t)
    {
        Unbox_receiver_checks_precede_argument_effects_and_keep_original_storage(t);
        Property_ref_arguments_keep_writable_unbox_receiver(t);
    }

    private static System.Linq.Expressions.Expression<Func<object, int>> Build(object original, object replacement, bool fail, bool lower)
    {
        var value = S.Parameter(typeof(object));
        var receiver = S.Call(typeof(UnboxReceiverSpillRegressionTests).GetMethod(nameof(Receiver)), value);
        var argument = S.TryFinally(S.Block(S.Assign(value, S.Constant(replacement, typeof(object))),
            S.Call(typeof(UnboxReceiverSpillRegressionTests).GetMethod(nameof(Argument)), S.Constant(original, typeof(object)), S.Constant(fail))),
            S.Call(typeof(UnboxReceiverSpillRegressionTests).GetMethod(nameof(Finally))));
        var add = typeof(Mutable).GetMethod(nameof(Mutable.Add));
        if (!lower)
            return S.Lambda<Func<object, int>>(S.Call(S.Unbox(receiver, typeof(Mutable)), add, argument), value);
        var box = S.Variable(typeof(object)); var arg = S.Variable(typeof(int));
        // Framework System.Compile cannot spill a struct receiver across an EH
        // argument. Explicit ordinary locals express the same evaluation order.
        return S.Lambda<Func<object, int>>(S.Block(new[] { box, arg }, S.Assign(box, receiver),
            S.Unbox(box, typeof(Mutable)), S.Assign(arg, argument), S.Call(S.Unbox(box, typeof(Mutable)), add, arg)), value);
    }

    public void Unbox_receiver_checks_precede_argument_effects_and_keep_original_storage(TestContext t)
    {
        foreach (var fail in new[] { false, true })
        {
            object original = new Mutable(); object replacement = new Mutable { Value = 100 };
            var control = Build(original, replacement, fail, true).Compile();
            Check(t, control, original, replacement, fail);
            foreach (var flags in Flags)
            {
                var fast = Build(original, replacement, fail, false).FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(fast)) Check(t, fast, original, replacement, fail);
            }
        }
    }

    private static void Check(TestContext t, Func<object, int> call, object original, object replacement, bool fail)
    {
        Trace = ""; t.Throws<NullReferenceException>(() => call(null)); t.AreEqual("receiver;", Trace);
        Trace = ""; t.Throws<InvalidCastException>(() => call(new object())); t.AreEqual("receiver;", Trace);
        typeof(Mutable).GetField(nameof(Mutable.Value)).SetValue(original, 0);
        Trace = "";
        if (fail) t.Throws<ApplicationException>(() => call(original));
        else t.AreEqual(13, call(original));
        t.AreEqual(fail ? 0 : 13, ((Mutable)original).Value);
        t.AreEqual(100, ((Mutable)replacement).Value);
        t.AreEqual("receiver;argument;finally;", Trace);
    }

    public void Property_ref_arguments_keep_writable_unbox_receiver(TestContext t)
    {
        var box = S.Parameter(typeof(object)); var owner = S.Parameter(typeof(Holder));
        var body = S.TryFinally(S.Call(S.Unbox(S.Call(typeof(UnboxReceiverSpillRegressionTests).GetMethod(nameof(Receiver)), box), typeof(Mutable)),
            typeof(Mutable).GetMethod(nameof(Mutable.AddRef)), S.Property(owner, nameof(Holder.Number))), S.Empty());
        var source = S.Lambda<Func<object, Holder, int>>(body, box, owner);
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) continue;
            object value = new Mutable(); var holder = new Holder { Number = 3 }; Trace = "";
            t.AreEqual(3, fast(value, holder)); t.AreEqual(3, ((Mutable)value).Value);
            t.AreEqual(4, holder.Number); t.AreEqual("receiver;", Trace);
        }
    }
}
