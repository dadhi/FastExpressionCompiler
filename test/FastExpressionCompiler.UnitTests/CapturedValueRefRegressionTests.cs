using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CapturedValueRefRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Curried_ref_calls_preserve_reads_and_mutations(t);
        Repeated_invocations_observe_mutated_capture(t);
        Separate_closures_keep_ref_mutations_isolated(t);
        Nullable_and_reference_captures_expose_writable_cells(t);
        Hoisted_local_ref_calls_update_the_shared_box(t);
        Readonly_captures_keep_the_original_value_storage(t);
        Later_assignment_keeps_captures_live(t);
        Capture_before_initialization_observes_the_assigned_value(t);
        Delegate_ref_arguments_keep_captures_live(t);
        Readonly_control_flow_and_container_captures_use_value_slots(t);
        Loop_and_finally_writes_keep_the_shared_capture_live(t);
        Never_assigned_locals_keep_the_default_value_slot(t);
        Mutating_struct_indexers_keep_the_capture_live(t);
    }
    public static int Add(ref int value, int increment) => value += increment;
    public static int? AddNullable(ref int? value, int increment) => value = value + increment;
    public static string Replace(ref string value) => value = "changed";
    public static object Identity(object value) => value;
    public sealed class ReferenceHolder
    {
        public readonly object Value;
        public ReferenceHolder(object value) => Value = value;
    }
    public delegate object ObjectRefUpdater(ref object value);
    private static readonly object ChangedReference = new object();
    public static object ReplaceObject(ref object value) => value = ChangedReference;

    public void Curried_ref_calls_preserve_reads_and_mutations(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int>>>[] expressions = {
            x => () => x * Add(ref x, 1),
            x => () => x * Add(ref x, 1) * Add(ref x, 2),
            x => () => x * Add(ref x, 1) * Add(ref x, 2) * x
        };
        var expectedValues = new[] { 12, 72, 432 };
        for (var i = 0; i < expressions.Length; ++i)
        {
            var fast = expressions[i].FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            t.AreEqual(expectedValues[i], fast(3)());
            t.AreEqual(expressions[i].Compile()(3)(), fast(3)());
        }
    }

    public void Repeated_invocations_observe_mutated_capture(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int>>> expression = x => () => Add(ref x, 1);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var actual = fast(3);
        var expected = expression.Compile()(3);
        for (var i = 4; i < 8; ++i)
        {
            t.AreEqual(i, actual());
            t.AreEqual(i, expected());
        }
    }

    public void Separate_closures_keep_ref_mutations_isolated(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int, Func<int>>> expression = x => () => Add(ref x, 1);
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var first = fast(3);
        var second = fast(30);
        t.AreEqual(4, first());
        t.AreEqual(31, second());
        t.AreEqual(5, first());
    }

    public void Nullable_and_reference_captures_expose_writable_cells(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<int?, Func<int?>>> nullable = x => () => AddNullable(ref x, 1);
        System.Linq.Expressions.Expression<Func<string, Func<string>>> reference = x => () => Replace(ref x);
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression })
        {
            var numbers = nullable.FromSysExpression().CompileFast(true, flags);
            var strings = reference.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(numbers) || !t.IsNotNull(strings)) return;
            var expected = nullable.Compile()(3);
            var first = numbers(3);
            var second = numbers(30);
            t.AreEqual(expected(), first()); t.AreEqual(expected(), first());
            t.AreEqual((int?)31, second()); t.AreEqual((int?)6, first());
            t.AreEqual(null, (object)numbers(null)());
            var text = strings("original");
            t.AreEqual(reference.Compile()("original")(), text());
            t.AreEqual("changed", text());
        }
    }

    public void Hoisted_local_ref_calls_update_the_shared_box(TestContext t)
    {
        var value = SysExpr.Variable(typeof(int));
        var getter = SysExpr.Variable(typeof(Func<int>));
        var expression = SysExpr.Lambda<Func<int>>(SysExpr.Block(new[] { value, getter },
            SysExpr.Assign(value, SysExpr.Constant(3)),
            SysExpr.Assign(getter, SysExpr.Lambda<Func<int>>(value)),
            SysExpr.Call(typeof(CapturedValueRefRegressionTests).GetMethod(nameof(Add)), value, SysExpr.Constant(1)),
            SysExpr.Invoke(getter)));
        var fast = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(4, fast());
        t.AreEqual(expression.Compile()(), fast());
    }
    public void Readonly_captures_keep_the_original_value_storage(TestContext t)
    {
        var value = new object();
        System.Linq.Expressions.Expression<Func<object, Func<object>>> parameter = x => () => x;
        var local = SysExpr.Variable(typeof(object));
        var localExpression = SysExpr.Lambda<Func<Func<object>>>(SysExpr.Block(new[] { local },
            SysExpr.Assign(local, SysExpr.Constant(value)), SysExpr.Lambda<Func<object>>(local)));
        var capturedParameter = parameter.FromSysExpression().CompileFast(true)(value);
        var capturedLocal = localExpression.FromSysExpression().CompileFast(true)();
        t.IsTrue(ReferenceEquals(value, capturedParameter()));
        t.IsTrue(ReferenceEquals(value, capturedLocal()));
        System.Linq.Expressions.Expression<Func<object, Func<object>>> call = x => () => Identity(x);
        System.Linq.Expressions.Expression<Func<object, Func<object>>> constructor = x => () => new ReferenceHolder(x).Value;
        var argument = SysExpr.Parameter(typeof(object));
        var invoke = SysExpr.Lambda<Func<object, Func<object>>>(SysExpr.Lambda<Func<object>>(
            SysExpr.Invoke(SysExpr.Constant((Func<object, object>)Identity), argument)), argument);
        foreach (var captured in new Delegate[] { capturedParameter, capturedLocal,
            call.FromSysExpression().CompileFast(true)(value), constructor.FromSysExpression().CompileFast(true)(value),
            invoke.FromSysExpression().CompileFast(true)(value) })
        {
            AssertOriginalValueStorage(t, (Func<object>)captured, value);
        }
    }

    public void Later_assignment_keeps_captures_live(TestContext t)
    {
        var value = SysExpr.Variable(typeof(object));
        var getter = SysExpr.Variable(typeof(Func<object>));
        var expected = new object();
        var expression = SysExpr.Lambda<Func<Func<object>>>(SysExpr.Block(new[] { value, getter },
            SysExpr.Assign(value, SysExpr.Constant(new object())),
            SysExpr.Assign(getter, SysExpr.Lambda<Func<object>>(value)),
            SysExpr.Assign(value, SysExpr.Constant(expected)), getter));
        var actual = expression.FromSysExpression().CompileFast(true)();
        t.IsTrue(ReferenceEquals(expected, actual()));
    }

    public void Capture_before_initialization_observes_the_assigned_value(TestContext t)
    {
        var value = SysExpr.Variable(typeof(object));
        var getter = SysExpr.Variable(typeof(Func<object>));
        var expected = new object();
        var expression = SysExpr.Lambda<Func<Func<object>>>(SysExpr.Block(new[] { value, getter },
            SysExpr.Assign(getter, SysExpr.Lambda<Func<object>>(value)),
            SysExpr.Assign(value, SysExpr.Constant(expected)), getter));
        var actual = expression.FromSysExpression().CompileFast(true)();
        t.IsTrue(ReferenceEquals(expected, actual()));
    }
    public void Delegate_ref_arguments_keep_captures_live(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(object));
        var expression = SysExpr.Lambda<Func<object, Func<object>>>(SysExpr.Lambda<Func<object>>(
            SysExpr.Block(SysExpr.Invoke(SysExpr.Constant((ObjectRefUpdater)ReplaceObject), value), value)), value);
        var actual = expression.FromSysExpression().CompileFast(true)(new object());
        t.IsTrue(ReferenceEquals(ChangedReference, actual()));
        t.IsTrue(ReferenceEquals(ChangedReference, actual()));
    }
    private static void AssertOriginalValueStorage(TestContext t, Func<object> captured, object value)
    {
        t.IsTrue(ReferenceEquals(value, captured()));
        var foundClosure = false;
        foreach (var field in captured.Target.GetType().GetFields(System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
        {
            if (field.GetValue(captured.Target) is ExpressionCompiler.ArrayClosureWithNonPassedParams closure)
            {
                foundClosure = true;
                t.IsTrue(ReferenceEquals(value, closure.NonPassedParams[0]));
            }
        }
        t.IsTrue(foundClosure);
    }

    public sealed class NestedReferenceNode { public object Value; }
    public sealed class ReferenceNode
    {
        public object Value;
        public NestedReferenceNode Child = new NestedReferenceNode();
        public ReferenceHolder Holder = new ReferenceHolder(null);
        public System.Collections.Generic.List<object> Values = new System.Collections.Generic.List<object>();
    }

    public void Readonly_control_flow_and_container_captures_use_value_slots(TestContext t)
    {
        var value = new object();
        var parameter = SysExpr.Parameter(typeof(object));
        var exit = SysExpr.Label(typeof(object));
        var result = SysExpr.Label(typeof(object));
        var dictionary = new System.Collections.Generic.Dictionary<object, object> { { value, value } };
        var nodeValue = typeof(ReferenceNode).GetField(nameof(ReferenceNode.Value));
        var nodeValues = typeof(ReferenceNode).GetField(nameof(ReferenceNode.Values));
        var add = typeof(System.Collections.Generic.List<object>).GetMethod("Add");
        var listConstructor = typeof(System.Collections.Generic.List<object>).GetConstructor(Type.EmptyTypes);
        var listIndexer = typeof(System.Collections.Generic.List<object>).GetProperty("Item");
        var memberInit = SysExpr.MemberInit(SysExpr.New(typeof(ReferenceNode)), SysExpr.Bind(nodeValue, parameter));
        var listInit = SysExpr.ListInit(SysExpr.New(listConstructor), SysExpr.ElementInit(add, parameter));
        var memberListInit = SysExpr.MemberInit(SysExpr.New(typeof(ReferenceNode)),
            SysExpr.ListBind(nodeValues, SysExpr.ElementInit(add, parameter)));
        var child = typeof(ReferenceNode).GetField(nameof(ReferenceNode.Child));
        var childValue = typeof(NestedReferenceNode).GetField(nameof(NestedReferenceNode.Value));
        var memberMemberInit = SysExpr.MemberInit(SysExpr.New(typeof(ReferenceNode)),
            SysExpr.MemberBind(child, SysExpr.Bind(childValue, parameter)));
        var shapes = new System.Linq.Expressions.Expression[]
        {
            SysExpr.TryFinally(parameter, SysExpr.Empty()),
            SysExpr.TryCatch(parameter, SysExpr.Catch(typeof(Exception), parameter)),
            SysExpr.Loop(SysExpr.Break(exit, parameter), exit),
            SysExpr.Block(SysExpr.Return(result, parameter), SysExpr.Label(result, SysExpr.Constant(null, typeof(object)))),
            SysExpr.Switch(SysExpr.Constant(0), SysExpr.Constant(null, typeof(object)), SysExpr.SwitchCase(parameter, SysExpr.Constant(0))),
            SysExpr.ArrayIndex(SysExpr.NewArrayInit(typeof(object), parameter), SysExpr.Constant(0)),
            SysExpr.Block(SysExpr.NewArrayBounds(typeof(object), SysExpr.Constant(1)), parameter),
            SysExpr.MakeIndex(SysExpr.Constant(dictionary), dictionary.GetType().GetProperty("Item"), new[] { parameter }),
            SysExpr.Field(memberInit, nodeValue),
            SysExpr.Field(SysExpr.Field(memberMemberInit, child), childValue),
#if !LIGHT_EXPRESSION
            SysExpr.MakeIndex(listInit, listIndexer, new[] { SysExpr.Constant(0) }),
#endif
            SysExpr.MakeIndex(SysExpr.Field(memberListInit, nodeValues), listIndexer, new[] { SysExpr.Constant(0) })
        };
        foreach (var body in shapes)
        {
            var expression = SysExpr.Lambda<Func<object, Func<object>>>(SysExpr.Lambda<Func<object>>(body), parameter);
            var actual = expression.FromSysExpression().CompileFast(true)(value);
            t.IsTrue(ReferenceEquals(expression.Compile()(value)(), actual()));
            AssertOriginalValueStorage(t, actual, value);
        }
#if LIGHT_EXPRESSION
        // Build ListInit natively; FromSysExpression does not convert this node yet.
        var lightParameter = Expression.Parameter(typeof(object));
        var lightList = Expression.ListInit(Expression.New(listConstructor), Expression.ElementInit(add, lightParameter));
        var lightBody = Expression.MakeIndex(lightList, listIndexer, new[] { Expression.Constant(0) });
        var lightExpression = Expression.Lambda<Func<object, Func<object>>>(Expression.Lambda<Func<object>>(lightBody), lightParameter);
        AssertOriginalValueStorage(t, lightExpression.CompileFast(true)(value), value);
#endif
    }

    public void Loop_and_finally_writes_keep_the_shared_capture_live(TestContext t)
    {
        foreach (var useFinally in new[] { false, true })
        {
            var value = SysExpr.Variable(typeof(object));
            var getter = SysExpr.Variable(typeof(Func<object>));
            var expected = new object();
            var exit = SysExpr.Label();
            var write = SysExpr.Assign(value, SysExpr.Constant(expected));
            var update = useFinally ? (System.Linq.Expressions.Expression)SysExpr.TryFinally(SysExpr.Empty(), write) :
                SysExpr.Loop(SysExpr.Block(write, SysExpr.Break(exit)), exit);
            var expression = SysExpr.Lambda<Func<Func<object>>>(SysExpr.Block(new[] { value, getter },
                SysExpr.Assign(value, SysExpr.Constant(new object())),
                SysExpr.Assign(getter, SysExpr.Lambda<Func<object>>(value)), update, getter));
            var actual = expression.FromSysExpression().CompileFast(true)();
            t.IsTrue(ReferenceEquals(expected, actual()));
            t.IsTrue(ReferenceEquals(expression.Compile()()(), actual()));
        }
    }

    public void Never_assigned_locals_keep_the_default_value_slot(TestContext t)
    {
        var value = SysExpr.Variable(typeof(object));
        var expression = SysExpr.Lambda<Func<Func<object>>>(SysExpr.Block(new[] { value }, SysExpr.Lambda<Func<object>>(value)));
        var actual = expression.FromSysExpression().CompileFast(true)();
        AssertOriginalValueStorage(t, actual, null);
    }
    public struct CountingIndexer
    {
        public int Value;
        public int this[int index] { get { return ++Value; } }
    }

    public void Mutating_struct_indexers_keep_the_capture_live(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(CountingIndexer));
        var expression = SysExpr.Lambda<Func<CountingIndexer, Func<int>>>(SysExpr.Lambda<Func<int>>(
            SysExpr.MakeIndex(value, typeof(CountingIndexer).GetProperty("Item"), new[] { SysExpr.Constant(0) })), value);
        var actual = expression.FromSysExpression().CompileFast(true)(new CountingIndexer { Value = 3 });
        var expected = expression.Compile()(new CountingIndexer { Value = 3 });
        t.AreEqual(4, actual());
        t.AreEqual(expected(), 4);
        t.AreEqual(5, actual());
        t.AreEqual(expected(), 5);
    }
}
