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
    public void Run(TestRun t) { Structurally_spilled_arguments_precede_runtime_compilation(t); Escaping_jump_skips_runtime_compilation(t); }
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
    public void Structurally_spilled_arguments_precede_runtime_compilation(TestContext t)
    {
        for (var shape = 0; shape < 6; ++shape)
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
    public void Escaping_jump_skips_runtime_compilation(TestContext t)
    {
        var target = E.Label(typeof(int));
        var argument = E.Block(E.Call(Method(nameof(Argument))), E.Goto(target, E.Constant(99), typeof(int)));
        var expression = E.Lambda<Func<int>>(E.Block(E.Invoke(E.Call(Method(nameof(Data))), argument), E.Label(target, E.Constant(0))));
        var system = ToSystem(expression).Compile();
        foreach (var flags in Flags)
        {
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            for (Mode = 0; Mode < 3; ++Mode)
            {
                var expected = Execute(system); var actual = Execute(fast);
                t.AreEqual(expected.Error, actual.Error); t.AreEqual(expected.Result, actual.Result); t.AreEqual(expected.Trace, actual.Trace);
                t.AreEqual(null, actual.Error); t.AreEqual(99, actual.Result); t.AreEqual("receiver;argument;", actual.Trace);
            }
        }
    }
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
