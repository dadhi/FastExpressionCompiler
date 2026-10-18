using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Runtime.CompilerServices;
using S = System.Linq.Expressions.Expression;
using SysExpression = System.Linq.Expressions.Expression;
using SysParameter = System.Linq.Expressions.ParameterExpression;
using SysLabel = System.Linq.Expressions.LabelTarget;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct DynamicCallSiteRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Dynamic_binding_rebinds_for_runtime_argument_types(t);
        Typed_and_void_dynamic_results_preserve_their_types(t);
        Reused_dynamic_nodes_keep_distinct_occurrence_sites(t);
        Repeated_nested_dynamic_lambdas_have_separate_sites(t);
        Mutable_targets_are_loaded_for_each_invocation(t);
        Exception_operands_preserve_target_and_argument_order(t);
        Ref_arguments_preserve_original_storage(t);
        Ref_property_writeback_preserves_effects_and_mutable_targets(t);
    }
    private static E Dynamic(Type delegateType, CallSiteBinder binder, params E[] arguments)
    {
#if LIGHT_EXPRESSION
        return new FastExpressionCompiler.LightExpression.DynamicExpression(delegateType, binder, arguments);
#else
        return S.MakeDynamic(delegateType, binder, arguments);
#endif
    }
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };

    public void Dynamic_binding_rebinds_for_runtime_argument_types(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var binder = new AddBinder(); var a = E.Parameter(typeof(object)); var b = E.Parameter(typeof(object));
            var dynamic = Dynamic(typeof(Func<CallSite, object, object, object>), binder, a, b);
            var expression = E.Lambda<Func<object, object, object>>(dynamic, a, b);
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            t.AreEqual(42, fast(40, 2)); t.AreEqual("ab", fast("a", "b")); t.AreEqual(7, fast(3, 4));
            t.Throws<InvalidOperationException>(() => fast(new object(), new object()));
            t.AreEqual(true, binder.BindCount >= 2);
        }
    }
    public void Typed_and_void_dynamic_results_preserve_their_types(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var integer = Dynamic(typeof(Func<CallSite, int, int>), new TypedBinder(), E.Constant(41));
            t.AreEqual(typeof(int), integer.Type);
            var number = E.Lambda<Func<int>>(integer).CompileFast(true, flags);
            if (t.IsNotNull(number)) t.AreEqual(42, number());
            var boolean = Dynamic(typeof(Func<CallSite, int, bool>), new TypedBinder(), E.Constant(41));
            t.AreEqual(typeof(bool), boolean.Type);
            var predicate = E.Lambda<Func<bool>>(boolean).CompileFast(true, flags);
            if (t.IsNotNull(predicate)) t.AreEqual(true, predicate());
            var cell = Dynamic(typeof(Func<CallSite, int, Cell>), new TypedBinder(), E.Constant(41));
            t.AreEqual(typeof(Cell), cell.Type);
            var structure = E.Lambda<Func<Cell>>(cell).CompileFast(true, flags);
            if (t.IsNotNull(structure)) t.AreEqual(42, structure().Value);
            var action = Dynamic(typeof(Action<CallSite, int>), new TypedBinder(), E.Constant(41));
            t.AreEqual(typeof(void), action.Type);
            var invoke = E.Lambda<Action>(action).CompileFast(true, flags);
            Trace = ""; if (t.IsNotNull(invoke)) { invoke(); t.AreEqual("record=41;", Trace); }
            var ignored = E.Lambda<Action>(integer).CompileFast(true, flags);
            if (t.IsNotNull(ignored)) ignored();
        }
    }
    public void Reused_dynamic_nodes_keep_distinct_occurrence_sites(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var binder = new IdentityBinder();
            var dynamic = Dynamic(typeof(Func<CallSite, int, int>), binder, E.Constant(0));
            var expression = E.Lambda<Func<int>>(E.Add(dynamic, dynamic));
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            t.AreEqual(3, fast()); t.AreEqual(3, fast()); t.AreEqual(2, binder.Sites.Count);
            var conditionalBinder = new IdentityBinder();
            var reused = Dynamic(typeof(Func<CallSite, int, int>), conditionalBinder, E.Constant(0));
            var condition = E.Parameter(typeof(bool));
            var branches = E.Lambda<Func<bool, int>>(E.Condition(condition, reused, reused), condition).CompileFast(true, flags);
            if (!t.IsNotNull(branches)) return;
            t.AreEqual(1, branches(true)); t.AreEqual(2, branches(false)); t.AreEqual(1, branches(true));
            t.AreEqual(2, conditionalBinder.Sites.Count);
            var prunedBinder = new IdentityBinder();
            var shared = Dynamic(typeof(Func<CallSite, int, int>), prunedBinder, E.Constant(0));
            var pruned = E.Lambda<Func<int>>(E.Add(E.Condition(E.Constant(false), shared, E.Constant(0)), E.Add(shared, shared))).CompileFast(true, flags);
            if (t.IsNotNull(pruned)) { t.AreEqual(3, pruned()); t.AreEqual(2, prunedBinder.Sites.Count); }
            var nestedBinder = new IdentityBinder();
            var nested = Dynamic(typeof(Func<CallSite, int, int>), nestedBinder, E.Constant(0));
            var make = E.Lambda<Func<Func<int>>>(E.Lambda<Func<int>>(E.Add(nested, nested))).CompileFast(true, flags);
            if (t.IsNotNull(make)) { var function = make(); t.AreEqual(3, function()); t.AreEqual(2, nestedBinder.Sites.Count); }
            var inlineBinder = new IdentityBinder();
            var inlineDynamic = Dynamic(typeof(Func<CallSite, int, int>), inlineBinder, E.Constant(0));
            var invocation = E.Invoke(E.Lambda<Func<int>>(inlineDynamic));
            var inline = E.Lambda<Func<int>>(E.Add(invocation, invocation)).CompileFast(true, flags);
            if (t.IsNotNull(inline))
            {
                t.AreEqual(3, inline()); t.AreEqual(2, inlineBinder.Sites.Count);
            }
        }
    }
    public void Repeated_nested_dynamic_lambdas_have_separate_sites(TestContext t)
    {
        var systemBinder = new IdentityBinder();
        var systemBody = S.MakeDynamic(typeof(Func<CallSite, int, int>), systemBinder, S.Constant(0));
        var systemInvoke = S.Invoke(S.Lambda<Func<int>>(systemBody));
        var system = S.Lambda<Func<int>>(S.Add(systemInvoke, systemInvoke)).Compile();
        t.AreEqual(3, system()); t.AreEqual(3, system()); t.AreEqual(2, systemBinder.Sites.Count);
        foreach (var flags in Flags)
        {
            var binder = new IdentityBinder(); var n = E.Parameter(typeof(int));
            var body = E.Add(Dynamic(typeof(Func<CallSite, int, int>), binder, E.Constant(0)), n);
            var leaf = E.Lambda<Func<int>>(body);
            var middle = E.Lambda<Func<Func<int>>>(leaf);
            var expression = E.Lambda<Func<int, Func<Func<int>>[]>>(E.NewArrayInit(typeof(Func<Func<int>>), middle, middle), n);
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            var array = fast(10); var first = array[0](); var second = array[1]();
            t.AreEqual(11, first()); t.AreEqual(12, second()); t.AreEqual(11, first());
            var other = fast(20); t.AreEqual(21, other[0]()()); t.AreEqual(22, other[1]()());
            t.AreEqual(2, binder.Sites.Count);
            // Different signatures sharing a body keep distinct entries even when
            // conditional collection/emission visit branches in opposite orders.
            var arityBinder = new IdentityBinder(); var x = E.Parameter(typeof(int)); var y = E.Parameter(typeof(int));
            var shared = Dynamic(typeof(Func<CallSite, int, int>), arityBinder, x);
            var one = E.Lambda<Func<int, int>>(shared, x);
            var two = E.Lambda<Func<int, int, int>>(shared, x, y);
            var condition = E.Parameter(typeof(bool));
            var choice = E.Lambda<Func<bool, int>>(E.Condition(condition, E.Invoke(one, E.Constant(1)), E.Invoke(two, E.Constant(1), E.Constant(2))), condition).CompileFast(true, flags);
            if (!t.IsNotNull(choice)) return;
            t.AreEqual(1, choice(true)); t.AreEqual(2, choice(false)); t.AreEqual(1, choice(true)); t.AreEqual(2, arityBinder.Sites.Count);
        }
    }
    public void Mutable_targets_are_loaded_for_each_invocation(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var binder = new IdentityBinder();
            var expression = E.Lambda<Func<int>>(Dynamic(typeof(Func<CallSite, int, int>), binder, E.Constant(0)));
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            t.AreEqual(1, fast());
            ((CallSite<Func<CallSite, int, int>>)binder.Sites[0]).Target = (site, value) => 123;
            t.AreEqual(123, fast()); t.AreEqual(1, binder.Sites.Count);
        }
    }
    public void Exception_operands_preserve_target_and_argument_order(TestContext t)
    {
        foreach (var flags in Flags) foreach (var mode in new[] { 0, 1, 2, 3, 4 })
        {
            var expected = SystemTargetMutation(mode);
            var binder = new IdentityBinder();
            E argument = E.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Argument)));
            if (mode == 1) argument = E.TryFinally(argument, E.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Cleanup))));
            if (mode == 2) argument = E.Condition(E.Constant(true), argument, E.TryFinally(E.Constant(0), E.Empty()));
            if (mode == 4) { var target = E.Label(typeof(int)); argument = E.Block(argument, E.Goto(target, E.Constant(0), typeof(int)), E.Label(target, E.Constant(0))); }
            E body = Dynamic(typeof(Func<CallSite, int, int>), binder, argument);
            if (mode == 3) body = E.TryFinally(body, E.Empty());
            var expression = E.Lambda<Func<int>>(body);
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            ArgumentEffect = null; ThrowArgument = false; Trace = "";
            t.AreEqual(1, fast());
            var site = (CallSite<Func<CallSite, int, int>>)binder.Sites[0];
            ArgumentEffect = () => site.Target = (s, value) => 123;
            Trace = "";
            t.AreEqual(expected, fast());
            t.AreEqual(mode == 1 ? "argument;finally;" : "argument;", Trace);
            ThrowArgument = true; Trace = "";
            t.Throws<FormatException>(() => fast());
            t.AreEqual(mode == 1 ? "argument;finally;" : "argument;", Trace);
        }
        ArgumentEffect = null; ThrowArgument = false;
    }
    private static int SystemTargetMutation(int mode)
    {
        var binder = new IdentityBinder();
        SysExpression argument = S.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Argument)));
        if (mode == 1) argument = S.TryFinally(argument, S.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Cleanup))));
        if (mode == 2) argument = S.Condition(S.Constant(true), argument, S.TryFinally(S.Constant(0), S.Empty()));
        if (mode == 4) { var target = S.Label(typeof(int)); argument = S.Block(argument, S.Goto(target, S.Constant(0), typeof(int)), S.Label(target, S.Constant(0))); }
        SysExpression body = S.MakeDynamic(typeof(Func<CallSite, int, int>), binder, argument);
        if (mode == 3) body = S.TryFinally(body, S.Empty());
        var function = S.Lambda<Func<int>>(body).Compile();
        ArgumentEffect = null; ThrowArgument = false; function();
        var site = (CallSite<Func<CallSite, int, int>>)binder.Sites[0];
        ArgumentEffect = () => site.Target = (s, value) => 123;
        return function();
    }
    public void Ref_arguments_preserve_original_storage(TestContext t)
    {
        foreach (var flags in Flags)
        {
            var binder = new RefBinder(); var number = E.Parameter(typeof(int).MakeByRefType());
            var expression = E.Lambda<RefInvoke>(Dynamic(typeof(RefSite), binder, number), number);
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            var n = 41; t.AreEqual(42, fast(ref n)); t.AreEqual(42, n);
        }
    }
    public void Ref_property_writeback_preserves_effects_and_mutable_targets(TestContext t)
    {
        foreach (var flags in Flags) foreach (var spill in new[] { false, true })
        {
            var binder = new RefIncrementBinder(); var box = new Box();
            E argument = E.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Argument)));
            if (spill) argument = E.TryFinally(argument, E.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Cleanup))));
            var expression = E.Lambda<Func<int>>(Dynamic(typeof(RefIncrementSite), binder,
                E.Property(E.Constant(box), nameof(Box.Value)), argument));
            var fast = expression.CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
            ArgumentEffect = null; ThrowArgument = false; box.Stored = 41; fast();
            box.Stored = 41;
            ArgumentEffect = () => binder.Site.Target = (CallSite site, ref int value, int increment) => { Trace += "new;"; return value += 100; };
            Trace = "";
            t.AreEqual(spill ? 141 : 42, fast()); t.AreEqual(spill ? 141 : 42, box.Stored);
            t.AreEqual(spill ? "get;argument;finally;new;set=141;" : "get;argument;body;set=42;", Trace);
            box.Stored = 41; ThrowArgument = true; Trace = "";
            t.Throws<FormatException>(() => fast()); t.AreEqual(41, box.Stored);
            t.AreEqual(spill ? "get;argument;finally;" : "get;argument;", Trace);
            ThrowArgument = false; ArgumentEffect = null;
            binder.Site.Target = (CallSite site, ref int value, int increment) => { Trace += "throw;"; ++value; throw new InvalidOperationException(); };
            Trace = "";
            t.Throws<InvalidOperationException>(() => fast()); t.AreEqual(41, box.Stored);
            t.AreEqual(spill ? "get;argument;finally;throw;" : "get;argument;throw;", Trace);
        }
        ArgumentEffect = null; ThrowArgument = false;
#if !NETFRAMEWORK
        foreach (var spill in new[] { false, true })
        {
            var binder = new RefIncrementBinder(); var box = new Box();
            SysExpression argument = S.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Argument)));
            if (spill) argument = S.TryFinally(argument, S.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Cleanup))));
            var systemExpression = S.Lambda<Func<int>>(S.MakeDynamic(typeof(RefIncrementSite), binder,
                S.Property(S.Constant(box), nameof(Box.Value)), argument));
            if (spill)
            {
                // System rejects Dynamic with EH operands and by-ref parameters.
                // Strict FEC execution above proves value, effect and copyback semantics.
                t.Throws<NotSupportedException>(() => systemExpression.Compile());
                continue;
            }
            var system = systemExpression.Compile();
            box.Stored = 41; system(); box.Stored = 41;
            ArgumentEffect = () => binder.Site.Target = (CallSite site, ref int value, int increment) => { Trace += "new;"; return value += 100; };
            Trace = "";
            t.AreEqual(spill ? 141 : 42, system());
            t.AreEqual(42, box.Stored);
            t.AreEqual("get;argument;body;set=42;", Trace);
            ArgumentEffect = null;
        }
#endif
    }
    public class Box
    {
        public int Stored;
        public int Value { get { Trace += "get;"; return Stored; } set { Trace += "set=" + value + ";"; Stored = value; } }
    }
    public delegate int RefIncrementSite(CallSite site, ref int value, int increment);
    public sealed class RefIncrementBinder : CallSiteBinder
    {
        public CallSite<RefIncrementSite> Site;
        public override T BindDelegate<T>(CallSite<T> site, object[] args)
        {
            Site = (CallSite<RefIncrementSite>)(object)site;
            return (T)(object)new RefIncrementSite((CallSite s, ref int value, int increment) => { Trace += "body;"; return ++value; });
        }
        public override SysExpression Bind(object[] args, ReadOnlyCollection<SysParameter> parameters, SysLabel returnLabel) => throw new NotSupportedException();
    }
    public delegate int RefSite(CallSite site, ref int value);
    public delegate int RefInvoke(ref int value);
    public struct Cell { public int Value; public Cell(int value) { Value = value; } }
    public static string Trace;
    public static bool ThrowArgument;
    public static Action ArgumentEffect;
    public static int Argument() { Trace += "argument;"; ArgumentEffect?.Invoke(); if (ThrowArgument) throw new FormatException(); return 0; }
    public static void Cleanup() => Trace += "finally;";
    public static void Record(int value) => Trace += "record=" + value + ";";

    public sealed class AddBinder : BinaryOperationBinder
    {
        public int BindCount;
        public AddBinder() : base(System.Linq.Expressions.ExpressionType.Add) { }
        public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject suggestion)
        {
            ++BindCount;
            SysExpression result;
            if (target.Value is int && arg.Value is int)
                result = S.Convert(S.Add(S.Convert(target.Expression, typeof(int)), S.Convert(arg.Expression, typeof(int))), typeof(object));
            else if (target.Value is string && arg.Value is string)
                result = S.Call(typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) }), S.Convert(target.Expression, typeof(string)), S.Convert(arg.Expression, typeof(string)));
            else result = S.Throw(S.New(typeof(InvalidOperationException)), typeof(object));
            return new DynamicMetaObject(result, BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType)
                .Merge(BindingRestrictions.GetTypeRestriction(arg.Expression, arg.LimitType)));
        }
    }
    public sealed class TypedBinder : CallSiteBinder
    {
        public override SysExpression Bind(object[] args, ReadOnlyCollection<SysParameter> parameters, SysLabel returnLabel)
        {
            if (returnLabel.Type == typeof(void)) return S.Block(S.Call(typeof(DynamicCallSiteRegressionTests).GetMethod(nameof(Record)), parameters[0]), S.Return(returnLabel));
            SysExpression result = S.Add(parameters[0], S.Constant(1));
            if (returnLabel.Type == typeof(bool)) result = S.GreaterThan(parameters[0], S.Constant(0));
            if (returnLabel.Type == typeof(Cell)) result = S.New(typeof(Cell).GetConstructor(new[] { typeof(int) }), result);
            return S.Return(returnLabel, result);
        }
    }
    public sealed class IdentityBinder : CallSiteBinder
    {
        public readonly List<CallSite> Sites = new List<CallSite>();
        public override T BindDelegate<T>(CallSite<T> site, object[] args)
        {
            var index = Sites.IndexOf(site); if (index == -1) { index = Sites.Count; Sites.Add(site); }
            var identity = index + 1;
            return (T)(object)new Func<CallSite, int, int>((actualSite, value) => identity);
        }
        public override SysExpression Bind(object[] args, ReadOnlyCollection<SysParameter> parameters, SysLabel returnLabel) => throw new NotSupportedException();
    }
    public sealed class RefBinder : CallSiteBinder
    {
        public override T BindDelegate<T>(CallSite<T> site, object[] args) => (T)(object)new RefSite((CallSite s, ref int value) => ++value);
        public override SysExpression Bind(object[] args, ReadOnlyCollection<SysParameter> parameters, SysLabel returnLabel) => throw new NotSupportedException();
    }
}
