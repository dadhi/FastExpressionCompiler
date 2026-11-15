using System;
using System.Reflection;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
using Lambda = FastExpressionCompiler.LightExpression.LambdaExpression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
using Lambda = System.Linq.Expressions.LambdaExpression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct RuntimeExpressionSpillOrderRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Structurally_spilled_arguments_precede_runtime_compilation(t);
        Deep_discarded_syntax_does_not_use_the_call_stack(t);
        Nested_invocations_propagate_discarded_spills(t);
        Nested_lambda_data_has_its_own_syntax_scope(t);
        Discarded_syntax_is_not_collected_or_reduced(t);
        Discarded_switch_and_initializer_syntax_preserves_order(t);
#if !LIGHT_EXPRESSION
        Already_reduced_live_extensions_preserve_order(t);
#endif
    }
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
    public void Structurally_spilled_arguments_precede_runtime_compilation(TestContext t)
    {
        for (var shape = 0; shape < 4; ++shape)
        {
            E argument = E.Call(Method(nameof(Argument)));
            if (shape == 0) { var target = E.Label(); argument = E.Block(argument, E.Goto(target), E.Label(target), E.Constant(5)); }
            if (shape == 1) { var target = E.Label(); argument = E.Block(E.Condition(E.Constant(true), argument, E.Goto(target, typeof(int))), E.Label(target), E.Constant(5)); }
            if (shape == 2) argument = E.Condition(E.Constant(true), argument, E.TryFinally(E.Constant(0), E.Empty()));
            if (shape == 3) argument = E.TryFinally(argument, E.Call(Method(nameof(Cleanup))));
            if (shape == 4 || shape == 5)
            {
                if (shape == 4) argument = E.Condition(E.Constant(true), argument, E.TryFinally(E.Constant(0), E.Empty()));
                else { var target = E.Label(); argument = E.Block(E.Condition(E.Constant(true), argument, E.Goto(target, typeof(int))), E.Label(target), E.Constant(5)); }
#if LIGHT_EXPRESSION
                argument = new FastExpressionCompiler.LightExpression.DynamicExpression(typeof(Func<System.Runtime.CompilerServices.CallSite, int, int>), new PassThroughBinder(), new[] { argument });
#else
                argument = S.MakeDynamic(typeof(Func<System.Runtime.CompilerServices.CallSite, int, int>), new PassThroughBinder(), argument);
#endif
            }
            var expression = E.Lambda<Func<int>>(E.Invoke(E.Call(Method(nameof(Data))), argument));
            var system = ToSystem(expression).Compile();
            foreach (var flags in Flags)
            {
                var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
                for (Mode = 0; Mode < 3; ++Mode)
                {
                    var expected = Execute(system); var actual = Execute(fast);
                    t.AreEqual(expected.Error, actual.Error); t.AreEqual(expected.Result, actual.Result); t.AreEqual(expected.Trace, actual.Trace);
                    t.AreEqual(Mode == 0 ? null : Mode == 1 ? typeof(NullReferenceException) : typeof(InvalidOperationException), actual.Error);
                    if (Mode != 0) t.AreEqual(shape == 3 ? "receiver;argument;finally;" : "receiver;argument;", actual.Trace);
                }
            }
        }
    }

    private static E DeadTry() => E.TryFinally(E.Constant(0), E.Empty());
    private static E Pruned(E discarded) => E.Condition(E.Constant(true), E.Call(Method(nameof(Argument))), discarded);
    private static Lambda RuntimeInvoke(E argument) => E.Lambda<Func<int>>(E.Invoke(E.Call(Method(nameof(Data))), argument));
    private static void Compare(TestContext t, Lambda expression, System.Linq.Expressions.LambdaExpression control = null,
        [System.Runtime.CompilerServices.CallerMemberName] string scenario = null)
    {
        var system = (control ?? ToSystem(expression)).Compile();
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
            for (Mode = 0; Mode < 3; ++Mode)
            {
                var expected = Execute(system);
                var actual = Execute(fast);
                t.AreEqual(expected.Error, actual.Error);
                t.AreEqual(expected.Result, actual.Result);
                t.AreEqual(scenario + "/" + flags + ":" + expected.Trace, scenario + "/" + flags + ":" + actual.Trace);
            }
        }
    }
    public void Deep_discarded_syntax_does_not_use_the_call_stack(TestContext t)
    {
        for (var hasSpill = 0; hasSpill != 2; ++hasSpill)
        {
            E discarded = hasSpill == 0 ? E.Constant(0) : DeadTry();
            var shallow = RuntimeInvoke(Pruned(discarded));
            for (var depth = 0; depth < 50000; ++depth) discarded = E.Negate(discarded);
            // System's recursive compiler is only used on the equivalent shallow control.
            Compare(t, RuntimeInvoke(Pruned(discarded)), ToSystem(shallow));
            var wide = new E[1001];
            for (var width = 0; width < 1000; ++width) wide[width] = E.Add(E.Constant(0), E.Constant(0));
            wide[1000] = hasSpill == 0 ? E.Constant(0) : DeadTry();
            Compare(t, RuntimeInvoke(Pruned(E.Block(wide))), ToSystem(shallow));
        }
    }
    public void Nested_invocations_propagate_discarded_spills(TestContext t)
    {
        var inner = E.Invoke(E.Call(Method(nameof(Data))), Pruned(DeadTry()));
        Compare(t, RuntimeInvoke(inner));
    }
    public void Nested_lambda_data_has_its_own_syntax_scope(TestContext t)
    {
        var nested = E.Lambda<Func<int>>(Pruned(DeadTry()));
        Compare(t, RuntimeInvoke(E.Call(Method(nameof(ReadLambda)), nested)));
        Compare(t, RuntimeInvoke(E.Call(Method(nameof(ReadQuote)), E.Quote(nested))));
        var parameter = E.Parameter(typeof(int));
        var identity = E.Lambda<Func<int, int>>(parameter, parameter);
        Compare(t, RuntimeInvoke(E.Invoke(identity, Pruned(DeadTry()))));
    }
    public void Discarded_syntax_is_not_collected_or_reduced(TestContext t)
    {
        var unbound = E.Parameter(typeof(int));
        var nested = E.Lambda<Func<int>>(DeadTry());
#if LIGHT_EXPRESSION
        var discarded = E.Block(nested, E.Quote(nested), unbound, E.Constant(0));
#else
        var discarded = E.Block(new DeadReduction(), nested, E.Quote(nested), unbound, E.Constant(0));
#endif
        foreach (var flags in Flags)
        {
#if !LIGHT_EXPRESSION
            DeadReduction.Count = 0;
#endif
            var fast = RuntimeInvoke(Pruned(discarded)).CompileFast(true, flags);
            if (!t.IsNotNull(fast)) return;
#if !LIGHT_EXPRESSION
            t.AreEqual(0, DeadReduction.Count);
#endif
            Mode = 0;
            var outcome = Execute(fast);
            t.AreEqual(null, outcome.Error);
            t.AreEqual(42, outcome.Result);
            t.AreEqual("receiver;compile;argument;body;", outcome.Trace);
#if !LIGHT_EXPRESSION
            t.AreEqual(0, DeadReduction.Count);
#endif
        }
    }
    public void Discarded_switch_and_initializer_syntax_preserves_order(TestContext t)
    {
        Compare(t, RuntimeInvoke(E.Switch(E.Constant(0), DeadTry(), E.SwitchCase(E.Call(Method(nameof(Argument))), E.Constant(0)))));
        Compare(t, RuntimeInvoke(E.Switch(E.Constant(0), E.Call(Method(nameof(Argument))), E.SwitchCase(DeadTry(), E.Constant(1)))));
        var value = typeof(Holder).GetField(nameof(Holder.Value));
        E[] discarded = {
            E.Block(E.MemberInit(E.New(typeof(Holder)), E.Bind(value, DeadTry())), E.Constant(0)),
            E.Block(E.ListInit(E.New(typeof(System.Collections.Generic.List<int>)), E.ElementInit(typeof(System.Collections.Generic.List<int>).GetMethod("Add"), DeadTry())), E.Constant(0)),
            E.Block(E.NewArrayInit(typeof(int), DeadTry()), E.Constant(0)),
            E.Block(E.NewArrayBounds(typeof(int), DeadTry()), E.Constant(0)),
            E.Add(E.Constant(0), DeadTry()),
            E.Call(typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }), DeadTry()),
            E.Condition(E.Constant(false), E.Constant(0), DeadTry()),
            E.Switch(E.Constant(0), E.Constant(0), E.SwitchCase(DeadTry(), E.Constant(1))),
            E.Switch(E.Constant(0), E.Constant(0), E.SwitchCase(E.Constant(0), DeadTry())),
            E.Block(E.MemberInit(E.New(typeof(Holder)), E.MemberBind(typeof(Holder).GetField(nameof(Holder.Child)),
                E.Bind(typeof(ChildHolder).GetField(nameof(ChildHolder.Value)), DeadTry()))), E.Constant(0)),
            E.Block(E.MemberInit(E.New(typeof(Holder)), E.ListBind(typeof(Holder).GetField(nameof(Holder.Items)),
                E.ElementInit(typeof(System.Collections.Generic.List<int>).GetMethod("Add"), DeadTry()))), E.Constant(0)),
            E.ArrayAccess(E.Constant(new[] { 0 }), DeadTry())
        };
        for (var i = 0; i < discarded.Length; ++i) Compare(t, RuntimeInvoke(Pruned(discarded[i])));
    }
    public static int ReadLambda(Func<int> value) => Argument();
    public static int ReadQuote(System.Linq.Expressions.Expression<Func<int>> value) => Argument();
    public class Holder
    {
        public int Value;
        public ChildHolder Child = new ChildHolder();
        public System.Collections.Generic.List<int> Items = new System.Collections.Generic.List<int>();
    }
    public class ChildHolder { public int Value; }
#if !LIGHT_EXPRESSION
    public void Already_reduced_live_extensions_preserve_order(TestContext t) => Compare(t, RuntimeInvoke(new LiveReduction()));
    public class LiveReduction : E
    {
        public override Type Type => typeof(int);
        public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
        public override bool CanReduce => true;
        public override E Reduce() => Pruned(DeadTry());
    }
    public class DeadReduction : E
    {
        public static int Count;
        public override Type Type => typeof(int);
        public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
        public override bool CanReduce => true;
        public override E Reduce() { ++Count; return DeadTry(); }
    }
#endif

    private static System.Linq.Expressions.LambdaExpression ToSystem(Lambda expression)
    {
#if LIGHT_EXPRESSION
        return expression.ToLambdaExpression();
#else
        return expression;
#endif
    }
    private static Outcome Execute(Delegate function)
    {
        Trace = ""; var outcome = new Outcome();
        try { outcome.Result = function.DynamicInvoke(); } catch (TargetInvocationException error) { outcome.Error = error.InnerException.GetType(); }
        outcome.Trace = Trace; return outcome;
    }
    private struct Outcome { public object Result; public Type Error; public string Trace; }
    private static MethodInfo Method(string name) => typeof(RuntimeExpressionSpillOrderRegressionTests).GetMethod(name);
    public static string Trace; public static int Mode;
    public static System.Linq.Expressions.Expression<Func<int, int>> Data()
    {
        Trace += "receiver;";
        if (Mode == 1) return null;
        if (Mode == 2) return S.Lambda<Func<int, int>>(S.Parameter(typeof(int)), S.Parameter(typeof(int)));
        return S.Lambda<Func<int, int>>(new Reduction(), S.Parameter(typeof(int)));
    }
    public static int Argument() { Trace += "argument;"; return 5; }
    public static void Cleanup() => Trace += "finally;";
    public static int Body() { Trace += "body;"; return 42; }
    public class PassThroughBinder : System.Runtime.CompilerServices.CallSiteBinder
    {
        public override System.Linq.Expressions.Expression Bind(object[] args,
            System.Collections.ObjectModel.ReadOnlyCollection<System.Linq.Expressions.ParameterExpression> parameters,
            System.Linq.Expressions.LabelTarget target) => S.Return(target, parameters[0]);
    }
    public class Reduction : System.Linq.Expressions.Expression
    {
        public override Type Type => typeof(int);
        public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
        public override bool CanReduce => true;
        public override System.Linq.Expressions.Expression Reduce() { Trace += "compile;"; return S.Call(Method(nameof(Body))); }
    }
}
