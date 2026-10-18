using System;
using System.Reflection;
using S = System.Linq.Expressions.Expression;
using SysLambda = System.Linq.Expressions.LambdaExpression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
using Lambda = FastExpressionCompiler.LightExpression.LambdaExpression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
using Lambda = System.Linq.Expressions.LambdaExpression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct RuntimeExpressionInvocationRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Computed_expressions_are_evaluated_once_and_recompiled_for_each_invocation(t);
        Parameter_and_concrete_expression_types_preserve_runtime_delegate_signatures(t);
        Runtime_compilation_preserves_operand_spill_and_exception_order(t);
        Void_and_ignored_invocations_preserve_effects(t);
        Ref_arguments_preserve_storage_copyback_and_exceptions(t);
#if !NETFRAMEWORK
        // Framework DynamicMethod does not support by-ref return types.
        Ref_return_expression_invocation_preserves_the_reference(t);
#endif
    }

    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
    public void Computed_expressions_are_evaluated_once_and_recompiled_for_each_invocation(TestContext t)
    {
        var x = E.Parameter(typeof(int)); var y = E.Parameter(typeof(int));
        var expression = E.Lambda<Func<int, int, int>>(E.Invoke(E.Call(Method(nameof(Compute)), y), x), x, y);
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            Trace = ""; ReceiverCalls = 0;
            t.AreEqual(14, fast(5, 9)); t.AreEqual(40, fast(5, 8));
            t.AreEqual(2, ReceiverCalls); t.AreEqual("receiver;receiver;", Trace);
        }
    }
    public void Parameter_and_concrete_expression_types_preserve_runtime_delegate_signatures(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, int>> first = x => x + 2;
        System.Linq.Expressions.Expression<Func<int, int>> second = x => x * 3;
        var data = E.Parameter(typeof(System.Linq.Expressions.Expression<Func<int, int>>));
        var x = E.Parameter(typeof(int));
        var expression = E.Lambda<Func<System.Linq.Expressions.Expression<Func<int, int>>, int, int>>(E.Invoke(data, x), data, x);
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(7, fast(first, 5)); t.AreEqual(15, fast(second, 5));
        }
        // Constant's inferred type is an internal Expression<TDelegate> subclass.
        var constant = E.Lambda<Func<int>>(E.Invoke(E.Constant(first), E.Constant(5)));
        Compare(t, constant, () => new object[0], expectedResult: 7);
    }
    public void Runtime_compilation_preserves_operand_spill_and_exception_order(TestContext t)
    {
        var argument = E.TryFinally(E.Call(Method(nameof(Argument))), E.Call(Method(nameof(Cleanup))));
        var expression = E.Lambda<Func<int>>(E.Invoke(E.Call(Method(nameof(RuntimeData))), argument));
        var plain = E.Invoke(E.Call(Method(nameof(RuntimeData))), E.Call(Method(nameof(Argument))));
        foreach (var mode in new[] { 0, 1, 2, 3, 4, 5 })
        {
            Mode = mode;
            Compare(t, expression, () => new object[0], mode == 0 ? (object)42 : null);
            Compare(t, E.Lambda<Func<int>>(plain), () => new object[0], mode == 0 ? (object)42 : null);
        }
        Mode = 0;
        Compare(t, E.Lambda<Func<int>>(E.Block(E.TryFinally(E.Empty(), E.Empty()), plain)), () => new object[0], 42);
        Compare(t, E.Lambda<Func<int>>(E.TryFinally(plain, E.Call(Method(nameof(Cleanup))))), () => new object[0], 42);
        var spilledReceiver = E.TryFinally(E.Call(Method(nameof(RuntimeData))), E.Call(Method(nameof(Cleanup))));
        Compare(t, E.Lambda<Func<int>>(E.Invoke(spilledReceiver, E.Call(Method(nameof(Argument))))), () => new object[0], 42);
        var inlineArgument = E.Invoke(E.Lambda<Func<int>>(E.Call(Method(nameof(Argument)))));
        Compare(t, E.Lambda<Func<int>>(E.Invoke(E.Call(Method(nameof(RuntimeData))), inlineArgument)), () => new object[0], 42);
    }
    public void Void_and_ignored_invocations_preserve_effects(TestContext t)
    {
        Mode = 0;
        var ignored = E.Lambda<Action>(E.Invoke(E.Call(Method(nameof(RuntimeData))), E.Call(Method(nameof(Argument)))));
        Compare(t, ignored, () => new object[0]);
        var action = E.Parameter(typeof(System.Linq.Expressions.Expression<Action>));
        var invoke = E.Lambda<Action<System.Linq.Expressions.Expression<Action>>>(E.Invoke(action), action);
        System.Linq.Expressions.Expression<Action> body = () => RecordAction();
        Compare(t, invoke, () => new object[] { body });
    }
    public void Ref_arguments_preserve_storage_copyback_and_exceptions(TestContext t)
    {
        var box = E.Parameter(typeof(Box));
        var expression = E.Lambda<Func<Box, int>>(E.Invoke(E.Call(Method(nameof(RuntimeRefData))),
            E.Property(box, nameof(Box.Value)), E.Constant(3)), box);
        foreach (var mode in new[] { 0, 1, 4, 6, 7 })
        {
            Mode = mode;
            Compare(t, expression, () => new object[] { new Box() }, mode == 0 ? (object)8 : null);
            var spillArgument = E.Lambda<Func<Box, int>>(E.Invoke(E.Call(Method(nameof(RuntimeRefData))),
                E.Property(box, nameof(Box.Value)), E.TryFinally(E.Constant(3), E.Call(Method(nameof(Cleanup))))), box);
            CheckSpilledPropertyWriteback(t, spillArgument, mode);
        }
        Mode = 0;
        var number = E.Parameter(typeof(int).MakeByRefType());
        var direct = E.Lambda<RefCall>(E.Invoke(E.Call(Method(nameof(RuntimeRefData))), number, E.Constant(3)), number);
        foreach (var flags in Flags)
        {
            var fast = direct.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            var n = 5; t.AreEqual(8, fast(ref n)); t.AreEqual(8, n);
        }
    }
#if !NETFRAMEWORK
    public void Ref_return_expression_invocation_preserves_the_reference(TestContext t)
    {
        var data = S.Lambda<RefReturn>(S.Call(Method(nameof(GetStorage))));
        var expression = E.Lambda<RefReturn>(E.Invoke(E.Constant(data, typeof(System.Linq.Expressions.Expression<RefReturn>))));
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            Storage = 3;
            ref var result = ref fast(); result = 42;
            t.AreEqual(42, Storage);
        }
    }
    public delegate ref int RefReturn();
    public static int Storage;
    public static ref int GetStorage() => ref Storage;
#endif

    // The outer lambda is always strict FEC. Runtime Expression<TDelegate>.Compile is
    // the intrinsic operation requested by invoking expression data, not outer fallback.
    private static void Compare(TestContext t, Lambda expression, Func<object[]> arguments, object expectedResult = null)
    {
        var system = ToSystem(expression).Compile();
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            var expected = Execute(system, arguments());
            var actual = Execute(fast, arguments());
            t.AreEqual(expected.Error, actual.Error); t.AreEqual(expected.Result, actual.Result);
            t.AreEqual(expected.Trace, actual.Trace); t.AreEqual(expected.Calls, actual.Calls);
            t.AreEqual(expected.BoxValue, actual.BoxValue);
            if (expectedResult != null) t.AreEqual(expectedResult, actual.Result);
        }
    }
    private static void CheckSpilledPropertyWriteback(TestContext t, Lambda expression, int mode)
    {
#if !NETFRAMEWORK
        var system = ToSystem(expression).Compile();
#endif
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
#if !NETFRAMEWORK
            var expected = Execute(system, new object[] { new Box() });
#endif
            var actual = Execute(fast, new object[] { new Box() });
            if (mode == 0 || mode == 7)
            {
                // System's stack spiller loses property copyback when another argument spills.
                // Preserve FEC's ordinary ref-property semantics instead of copying that bug.
#if !NETFRAMEWORK
                t.AreEqual(null, expected.Error); t.AreEqual(8, expected.Result);
                t.AreEqual("receiver;get;finally;body;", expected.Trace); t.AreEqual(5, expected.BoxValue);
#endif
                t.AreEqual("receiver;get;finally;body;set=8;", actual.Trace);
                t.AreEqual(mode == 7 ? typeof(FormatException) : null, actual.Error);
                t.AreEqual(mode == 7 ? null : (object)8, actual.Result);
                t.AreEqual(mode == 7 ? 5 : 8, actual.BoxValue);
            }
            else
            {
#if NETFRAMEWORK
                // Framework's System compiler prohibits EH operands in ref invocations.
                // Keep strict FEC execution and assert the storage/effect semantics directly.
                t.AreEqual(mode == 1 ? typeof(NullReferenceException) : mode == 4 ? typeof(InvalidOperationException) : typeof(FormatException), actual.Error);
                t.AreEqual(null, actual.Result); t.AreEqual(5, actual.BoxValue);
                t.AreEqual(mode == 1 ? "receiver;get;finally;" : mode == 4 ? "receiver;get;finally;body;" : "receiver;get;", actual.Trace);
#else
                t.AreEqual(expected.Error, actual.Error); t.AreEqual(expected.Result, actual.Result);
                t.AreEqual(expected.Trace, actual.Trace); t.AreEqual(expected.BoxValue, actual.BoxValue);
#endif
            }
            t.AreEqual(1, actual.Calls);
        }
    }
    private static SysLambda ToSystem(Lambda expression)
    {
#if LIGHT_EXPRESSION
        return expression.ToLambdaExpression();
#else
        return expression;
#endif
    }
    private static Outcome Execute(Delegate function, object[] arguments)
    {
        Trace = ""; ReceiverCalls = 0;
        var outcome = new Outcome();
        try { outcome.Result = function.DynamicInvoke(arguments); }
        catch (TargetInvocationException error) { outcome.Error = error.InnerException.GetType(); }
        outcome.Trace = Trace; outcome.Calls = ReceiverCalls;
        outcome.BoxValue = arguments.Length != 0 && arguments[0] is Box box ? box.Stored : 0;
        return outcome;
    }
    private struct Outcome { public object Result; public Type Error; public string Trace; public int Calls; public int BoxValue; }
    private static MethodInfo Method(string name) => typeof(RuntimeExpressionInvocationRegressionTests).GetMethod(name);
    public static string Trace;
    public static int ReceiverCalls;
    public static int Mode;
    public static System.Linq.Expressions.Expression<Func<int, int>> Compute(int y)
    {
        ++ReceiverCalls; Trace += "receiver;";
        if ((y & 1) != 0) return x => x + y;
        return x => x * y;
    }
    public static System.Linq.Expressions.Expression<Func<int, int>> RuntimeData()
    {
        ++ReceiverCalls; Trace += "receiver;";
        if (Mode == 1) return null;
        if (Mode == 2) return S.Lambda<Func<int, int>>(S.Parameter(typeof(int)), S.Parameter(typeof(int)));
        if (Mode == 3) throw new ApplicationException("receiver");
        return S.Lambda<Func<int, int>>(new Reduction(), S.Parameter(typeof(int)));
    }
    public static int Argument() { Trace += "argument;"; if (Mode == 5) throw new FormatException("argument"); return 42; }
    public static void Cleanup() => Trace += "finally;";
    public static int Body() { Trace += "body;"; if (Mode == 4) throw new InvalidOperationException("body"); return 42; }
    public static void RecordAction() => Trace += "action;";
    public class Reduction : System.Linq.Expressions.Expression
    {
        public override Type Type => typeof(int);
        public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
        public override bool CanReduce => true;
        public override System.Linq.Expressions.Expression Reduce() { Trace += "compile;"; return S.Call(Method(nameof(Body))); }
    }
    public delegate int RefBody(ref int value, int increment);
    public delegate int RefCall(ref int value);
    public static System.Linq.Expressions.Expression<RefBody> RuntimeRefData()
    {
        ++ReceiverCalls; Trace += "receiver;";
        if (Mode == 1) return null;
        var n = S.Parameter(typeof(int).MakeByRefType()); var increment = S.Parameter(typeof(int));
        return S.Lambda<RefBody>(S.Call(Method(nameof(Mutate)), n, increment), n, increment);
    }
    public static int Mutate(ref int value, int increment)
    {
        Trace += "body;"; value += increment;
        if (Mode == 4) throw new InvalidOperationException("body");
        return value;
    }
    public class Box
    {
        private int _value = 5;
        public int Stored => _value;
        public int Value
        {
            get { Trace += "get;"; if (Mode == 6) throw new FormatException("getter"); return _value; }
            set { Trace += "set=" + value + ";"; if (Mode == 7) throw new FormatException("setter"); _value = value; }
        }
    }
}
