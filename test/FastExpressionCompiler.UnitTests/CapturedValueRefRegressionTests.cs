using System;
using System.Reflection;
using System.Reflection.Emit;
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
        Writes_through_reference_members_keep_value_captures(t);
        Leading_value_assignments_do_not_allocate_capture_cells(t);
        Hoisted_metadata_rentals_clear_success_and_failure_state(t);
        Concurrent_compilation_keeps_hoisted_metadata_isolated(t);
        Static_indexed_properties_allow_captured_parameters(t);
        Private_mutable_reference_captures_reuse_value_slots(t);
        Shared_or_byref_reference_captures_keep_cells(t);
        Boxed_value_cells_share_struct_mutations_and_replacements(t);
        Boxed_value_cells_preserve_checked_and_enum_assignments(t);
        Boxed_default_struct_cells_do_not_call_user_constructors(t);
        Quote_and_runtime_variables_keep_all_cells_typed(t);
#if LIGHT_EXPRESSION
        Intrinsics_with_hidden_quotes_keep_typed_cells(t);
#endif
    }

    public void Static_indexed_properties_allow_captured_parameters(TestContext t)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("StaticCapturedIndexer"), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Main").DefineType("IndexedValues", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var getter = type.DefineMethod("get_Item", MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
            typeof(int), new[] { typeof(int) });
        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4, 40);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);
        var property = type.DefineProperty("Item", PropertyAttributes.None, typeof(int), new[] { typeof(int) });
        property.SetGetMethod(getter);
        var indexer = type.CreateType().GetProperty("Item");
        var value = SysExpr.Parameter(typeof(int));
        var expression = SysExpr.Lambda<Func<int, Func<int>>>(SysExpr.Lambda<Func<int>>(
            SysExpr.Add(SysExpr.MakeIndex(null, indexer, new[] { SysExpr.Constant(2) }), value)), value);
        var expected = expression.Compile()(3)();
        var fast = expression.FromSysExpression().CompileFast(true);
        if (t.IsNotNull(fast)) t.AreEqual(expected, fast(3)());
    }
    public static int Add(ref int value, int increment) => value += increment;
    public struct MutableCounter
    {
        public int Value;
        public int Increment() => ++Value;
    }
    public static int IncrementCounter(ref MutableCounter value) => ++value.Value;
    public struct DefaultCounter
    {
        public static int ConstructorCalls;
        public int Value;
        public DefaultCounter() { ++ConstructorCalls; Value = 99; }
        public int Increment() => ++Value;
    }
    public enum CounterState { First, Second }

    public void Boxed_value_cells_share_struct_mutations_and_replacements(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(MutableCounter));
        var replacement = SysExpr.Parameter(typeof(MutableCounter));
        var mutate = SysExpr.Lambda<Func<int>>(SysExpr.Call(value, typeof(MutableCounter).GetMethod(nameof(MutableCounter.Increment))));
        var replace = SysExpr.Lambda<Action<MutableCounter>>(SysExpr.Assign(value, replacement), replacement);
        var byRef = SysExpr.Lambda<Func<int>>(SysExpr.Call(typeof(CapturedValueRefRegressionTests).GetMethod(nameof(IncrementCounter)), value));
        var resultType = typeof(Tuple<Func<int>, Action<MutableCounter>, Func<int>>);
        var expression = SysExpr.Lambda<Func<MutableCounter, Tuple<Func<int>, Action<MutableCounter>, Func<int>>>>(
            SysExpr.New(resultType.GetConstructors()[0], mutate, replace, byRef), value);
        var factory = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(factory)) return;
        var actual = factory(new MutableCounter { Value = 3 });
        var separate = factory(new MutableCounter { Value = 30 });
        var cell = GetCapturedCell(actual.Item1);
        t.IsTrue(cell is MutableCounter);
        t.IsTrue(ReferenceEquals(cell, GetCapturedCell(actual.Item2)));
        t.IsTrue(ReferenceEquals(cell, GetCapturedCell(actual.Item3)));
        t.IsTrue(!ReferenceEquals(cell, GetCapturedCell(separate.Item1)));
        t.AreEqual(4, actual.Item1());
        t.AreEqual(5, actual.Item3());
        actual.Item2(new MutableCounter { Value = 40 });
        t.AreEqual(41, actual.Item1());
        t.AreEqual(41, ((MutableCounter)GetCapturedCell(actual.Item3)).Value);
        t.AreEqual(42, actual.Item3());
        t.AreEqual(31, separate.Item1());
        var expected = expression.Compile()(new MutableCounter { Value = 3 });
        t.AreEqual(4, expected.Item1());
        t.AreEqual(5, expected.Item3());
        expected.Item2(new MutableCounter { Value = 40 });
        t.AreEqual(41, expected.Item1());
        t.AreEqual(42, expected.Item3());
    }

    public void Boxed_value_cells_preserve_checked_and_enum_assignments(TestContext t)
    {
        var number = SysExpr.Parameter(typeof(int));
        var pairType = typeof(Tuple<Func<int>, Func<int>>);
        var expression = SysExpr.Lambda<Func<int, Tuple<Func<int>, Func<int>>>>(SysExpr.New(pairType.GetConstructors()[0],
            SysExpr.Lambda<Func<int>>(SysExpr.AddAssignChecked(number, SysExpr.Constant(1))), SysExpr.Lambda<Func<int>>(number)), number);
        var factory = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(factory)) return;
        var pair = factory(int.MaxValue);
        t.IsTrue(GetCapturedCell(pair.Item1) is int);
        t.Throws<OverflowException>(() => pair.Item1());
        t.AreEqual(int.MaxValue, pair.Item2());
        var state = SysExpr.Parameter(typeof(CounterState));
        var change = SysExpr.Lambda<Func<CounterState, Func<CounterState>>>(SysExpr.Lambda<Func<CounterState>>(
            SysExpr.Assign(state, SysExpr.Constant(CounterState.Second))), state).FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(change)) return;
        var changed = change(CounterState.First);
        t.IsTrue(GetCapturedCell(changed) is CounterState);
        t.AreEqual(CounterState.Second, changed());
    }

    public void Boxed_default_struct_cells_do_not_call_user_constructors(TestContext t)
    {
        var value = SysExpr.Variable(typeof(DefaultCounter));
        var expression = SysExpr.Lambda<Func<Func<int>>>(SysExpr.Block(new[] { value }, SysExpr.Lambda<Func<int>>(
            SysExpr.Call(value, typeof(DefaultCounter).GetMethod(nameof(DefaultCounter.Increment))))));
        DefaultCounter.ConstructorCalls = 0;
        var factory = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(factory)) return;
        var first = factory();
        var second = factory();
        t.IsTrue(GetCapturedCell(first) is DefaultCounter);
        t.AreEqual(1, first());
        t.AreEqual(2, first());
        t.AreEqual(1, second());
        t.AreEqual(0, DefaultCounter.ConstructorCalls);
    }

    public void Quote_and_runtime_variables_keep_all_cells_typed(TestContext t)
    {
        for (var mode = 0; mode < 3; ++mode)
        {
            var value = SysExpr.Parameter(typeof(int));
            var hasQuote = mode != 1;
            var hasVariables = mode != 0;
            var resultType = typeof(Tuple<Func<int>, System.Linq.Expressions.Expression<Func<int>>, System.Runtime.CompilerServices.IRuntimeVariables>);
            var expression = SysExpr.Lambda<Func<int, Tuple<Func<int>, System.Linq.Expressions.Expression<Func<int>>, System.Runtime.CompilerServices.IRuntimeVariables>>>(
                SysExpr.New(resultType.GetConstructors()[0], SysExpr.Lambda<Func<int>>(SysExpr.PreIncrementAssign(value)),
                    hasQuote ? (System.Linq.Expressions.Expression)SysExpr.Quote(SysExpr.Lambda<Func<int>>(value)) :
                        SysExpr.Constant(SysExpr.Lambda<Func<int>>(SysExpr.Constant(0)), typeof(System.Linq.Expressions.Expression<Func<int>>)),
                    hasVariables ? (System.Linq.Expressions.Expression)SysExpr.RuntimeVariables(value) :
                        SysExpr.Constant(null, typeof(System.Runtime.CompilerServices.IRuntimeVariables))), value);
            var factory = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(factory)) return;
            var actual = factory(3);
            t.IsTrue(GetCapturedCell(actual.Item1) is System.Runtime.CompilerServices.StrongBox<int>);
            t.AreEqual(4, actual.Item1());
            if (hasQuote) t.AreEqual(4, actual.Item2.Compile()());
            if (hasVariables)
            {
                actual.Item3[0] = 40;
                t.AreEqual(41, actual.Item1());
                if (hasQuote) t.AreEqual(41, actual.Item2.Compile()());
                t.AreEqual(41, (int)actual.Item3[0]);
            }
        }
    }

    private static object GetCapturedCell(Delegate captured)
    {
        if (captured.Target is ExpressionCompiler.ArrayClosureWithNonPassedParams direct) return direct.NonPassedParams[0];
        foreach (var field in captured.Target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.GetValue(captured.Target) is ExpressionCompiler.ArrayClosureWithNonPassedParams closure)
                return closure.NonPassedParams[0];
        throw new InvalidOperationException("Expected a captured cell.");
    }

#if LIGHT_EXPRESSION
    private sealed class HiddenQuote : ConstantExpression
    {
        private readonly Expression _quote;
        internal HiddenQuote(Expression quote) => _quote = quote;
        public override Type Type => _quote.Type;
        public override object Value => null;
        public override bool IsIntrinsic => true;
        public override ExpressionCompiler.Result TryCollectInfo(ref ExpressionCompiler.CompilerContext context,
            ExpressionCompiler.NestedLambdaInfo nestedLambda, ref ImTools.SmallList<ExpressionCompiler.NestedLambdaInfo> rootNestedLambdas) =>
            ExpressionCompiler.TryCollectInfo(ref context, _quote, nestedLambda, ref rootNestedLambdas);
        public override bool TryEmit(ref ExpressionCompiler.CompilerContext context, ILGenerator il,
            ExpressionCompiler.ParentFlags parent, int byRefIndex = -1) =>
            ExpressionCompiler.EmittingVisitor.TryEmit(_quote, il, ref context, parent, byRefIndex);
    }

    public void Intrinsics_with_hidden_quotes_keep_typed_cells(TestContext t)
    {
        var value = Expression.Parameter(typeof(int));
        var pairType = typeof(Tuple<Func<int>, System.Linq.Expressions.Expression<Func<int>>>);
        var expression = Expression.Lambda<Func<int, Tuple<Func<int>, System.Linq.Expressions.Expression<Func<int>>>>>(
            Expression.New(pairType.GetConstructors()[0], Expression.Lambda<Func<int>>(Expression.PreIncrementAssign(value)),
                new HiddenQuote(Expression.Quote(Expression.Lambda<Func<int>>(value)))), value);
        var factory = expression.CompileFast(true);
        if (!t.IsNotNull(factory)) return;
        var actual = factory(3);
        t.IsTrue(GetCapturedCell(actual.Item1) is System.Runtime.CompilerServices.StrongBox<int>);
        t.AreEqual(4, actual.Item1());
        t.AreEqual(4, actual.Item2.Compile()());
        t.AreEqual(5, actual.Item1());
        t.AreEqual(5, actual.Item2.Compile()());
    }
#endif
    public void Private_mutable_reference_captures_reuse_value_slots(TestContext t)
    {
        foreach (var local in new[] { false, true })
        {
            var value = SysExpr.Parameter(typeof(string));
            var update = SysExpr.Assign(value, SysExpr.Condition(SysExpr.Equal(value, SysExpr.Constant("first")),
                SysExpr.Constant("second"), SysExpr.Constant("first")));
            var child = SysExpr.Lambda<Func<int>>(SysExpr.Block(update, SysExpr.Property(value, nameof(string.Length))));
            var parameterExpression = SysExpr.Lambda<Func<string, Func<int>>>(child, value);
            var localExpression = SysExpr.Lambda<Func<Func<int>>>(SysExpr.Block(new[] { value },
                SysExpr.Assign(value, SysExpr.Constant("first")), child));
            foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
            {
                var actual = local ? localExpression.FromSysExpression().CompileFast(true, flags)() :
                    parameterExpression.FromSysExpression().CompileFast(true, flags)("first");
                var expected = local ? localExpression.Compile()() : parameterExpression.Compile()("first");
                for (var i = 0; i < 8; ++i) t.AreEqual(expected(), actual());
                t.IsTrue(SingleCaptureStorage(actual) is string);
            }
        }
    }

    public void Shared_or_byref_reference_captures_keep_cells(TestContext t)
    {
        var value = SysExpr.Parameter(typeof(string));
        var writer = SysExpr.Lambda<Func<string>>(SysExpr.Assign(value, SysExpr.Constant("second")));
        var reader = SysExpr.Lambda<Func<string>>(value);
        var expression = SysExpr.Lambda<Func<string, Func<string>[]>>(
            SysExpr.NewArrayInit(typeof(Func<string>), writer, reader), value);
        var pair = expression.FromSysExpression().CompileFast(true)("first");
        t.AreEqual("first", pair[1]());
        t.AreEqual("second", pair[0]());
        t.AreEqual("second", pair[1]());
        t.IsTrue(SingleCaptureStorage(pair[0]) is System.Runtime.CompilerServices.StrongBox<string>);
        var byref = SysExpr.Lambda<Func<string, Func<string>>>(SysExpr.Lambda<Func<string>>(
            SysExpr.Call(typeof(CapturedValueRefRegressionTests).GetMethod(nameof(Replace)), value)), value);
        var actual = byref.FromSysExpression().CompileFast(true)("first");
        t.AreEqual("changed", actual());
        t.IsTrue(SingleCaptureStorage(actual) is System.Runtime.CompilerServices.StrongBox<string>);

        var toggle = SysExpr.Lambda<Func<string>>(SysExpr.Assign(value, SysExpr.Condition(
            SysExpr.Equal(value, SysExpr.Constant("first")), SysExpr.Constant("second"), SysExpr.Constant("first"))));
        var repeated = SysExpr.Lambda<Func<string, Func<string>[]>>(SysExpr.Call(
            typeof(CapturedValueRefRegressionTests).GetMethod(nameof(PairReferences)), toggle, toggle), value);
        var repeatedPair = repeated.FromSysExpression().CompileFast(true)("first");
        t.AreEqual("second", repeatedPair[0]());
        t.AreEqual("first", repeatedPair[1]());
        t.IsTrue(SingleCaptureStorage(repeatedPair[0]) is System.Runtime.CompilerServices.StrongBox<string>);

        var saved = SysExpr.Variable(typeof(Func<string>));
        var outerWrite = SysExpr.Lambda<Func<string, Func<string>>>(SysExpr.Block(new[] { saved },
            SysExpr.Assign(saved, toggle), SysExpr.Assign(value, SysExpr.Constant("second")), saved), value);
        var afterOuterWrite = outerWrite.FromSysExpression().CompileFast(true)("first");
        var expected = outerWrite.Compile()("first");
        t.AreEqual(expected(), afterOuterWrite());
        t.AreEqual(expected(), afterOuterWrite());
        t.IsTrue(SingleCaptureStorage(afterOuterWrite) is System.Runtime.CompilerServices.StrongBox<string>);
    }

    public static Func<string>[] PairReferences(Func<string> first, Func<string> second) => new[] { first, second };

    private static object SingleCaptureStorage(Delegate captured)
    {
        foreach (var field in captured.Target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            if (field.GetValue(captured.Target) is ExpressionCompiler.ArrayClosureWithNonPassedParams closure)
                return closure.NonPassedParams[0];
        return null;
    }
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
        public object Property { get => Value; set => Value = value; }
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
    public void Writes_through_reference_members_keep_value_captures(TestContext t)
    {
        var holder = SysExpr.Parameter(typeof(ReferenceNode));
        var node = new ReferenceNode();
        var replace = typeof(CapturedValueRefRegressionTests).GetMethod(nameof(ReplaceObject));
        var fieldExpression = SysExpr.Lambda<Func<ReferenceNode, Func<object>>>(SysExpr.Lambda<Func<object>>(
            SysExpr.Block(SysExpr.Call(replace, SysExpr.Field(holder, nameof(ReferenceNode.Value))), SysExpr.Convert(holder, typeof(object)))), holder);
        AssertOriginalValueStorage(t, fieldExpression.FromSysExpression().CompileFast(true)(node), node);
        t.IsTrue(ReferenceEquals(ChangedReference, node.Value));
        node.Value = null;
        var propertyExpression = SysExpr.Lambda<Func<ReferenceNode, Func<object>>>(SysExpr.Lambda<Func<object>>(
            SysExpr.Block(SysExpr.Call(replace, SysExpr.Property(holder, nameof(ReferenceNode.Property))), SysExpr.Convert(holder, typeof(object)))), holder);
        AssertOriginalValueStorage(t, propertyExpression.FromSysExpression().CompileFast(true)(node), node);
        t.IsTrue(ReferenceEquals(ChangedReference, node.Value));
        var array = SysExpr.Parameter(typeof(object[]));
        var values = new object[1];
        var arrayExpression = SysExpr.Lambda<Func<object[], Func<object>>>(SysExpr.Lambda<Func<object>>(
            SysExpr.Block(SysExpr.Call(replace, SysExpr.ArrayAccess(array, SysExpr.Constant(0))), SysExpr.Convert(array, typeof(object)))), array);
        AssertOriginalValueStorage(t, arrayExpression.FromSysExpression().CompileFast(true)(values), values);
        t.IsTrue(ReferenceEquals(ChangedReference, values[0]));
    }
    public void Leading_value_assignments_do_not_allocate_capture_cells(TestContext t)
    {
        var value = SysExpr.Variable(typeof(object));
        var copiedDefault = SysExpr.Variable(typeof(object));
        var expected = new object();
        var expression = SysExpr.Lambda<Func<Func<object>>>(SysExpr.Block(new[] { value, copiedDefault },
            SysExpr.Assign(copiedDefault, value), SysExpr.Assign(value, SysExpr.Constant(expected)),
            SysExpr.Assign(value, value), SysExpr.Lambda<Func<object>>(value)));
        var actual = expression.FromSysExpression().CompileFast(true)();
        AssertOriginalValueStorage(t, actual, expected);
        t.IsTrue(ReferenceEquals(expression.Compile()()(), actual()));
    }
    private static System.Linq.Expressions.Expression<Func<int, string, object, Func<int>>> MutableCaptureExpression()
    {
        var number = SysExpr.Parameter(typeof(int));
        var text = SysExpr.Parameter(typeof(string));
        var reference = SysExpr.Parameter(typeof(object));
        var body = SysExpr.Block(
            SysExpr.Call(typeof(CapturedValueRefRegressionTests).GetMethod(nameof(Replace)), text),
            SysExpr.Call(typeof(CapturedValueRefRegressionTests).GetMethod(nameof(ReplaceObject)), reference),
            SysExpr.PreIncrementAssign(number));
        return SysExpr.Lambda<Func<int, string, object, Func<int>>>(SysExpr.Lambda<Func<int>>(body), number, text, reference);
    }

    public void Hoisted_metadata_rentals_clear_success_and_failure_state(TestContext t)
    {
        var expression = MutableCaptureExpression();
        var actual = expression.FromSysExpression().CompileFast(true)(3, "initial", new object());
        t.AreEqual(4, actual());
        AssertClearedHoistedMetadataPool(t);
        var target = SysExpr.Label();
        var broken = SysExpr.Lambda<Func<int, string, object, int>>(
            SysExpr.Block(expression.Body, SysExpr.Goto(target), SysExpr.Constant(0)), expression.Parameters);
        t.Throws<InvalidOperationException>(() => broken.FromSysExpression().CompileFast(true));
        AssertClearedHoistedMetadataPool(t);
        t.AreEqual(5, actual());
    }

    public void Concurrent_compilation_keeps_hoisted_metadata_isolated(TestContext t)
    {
        var expression = MutableCaptureExpression();
        var passed = new bool[64];
        System.Threading.Tasks.Parallel.For(0, passed.Length, i =>
        {
            var actual = expression.FromSysExpression().CompileFast(true)(i, "initial", new object());
            passed[i] = actual() == i + 1 && actual() == i + 2;
        });
        foreach (var pass in passed) t.IsTrue(pass);
        AssertClearedHoistedMetadataPool(t);
    }

    private static void AssertClearedHoistedMetadataPool(TestContext t)
    {
        const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
        var cache = typeof(ExpressionCompiler).GetField("_hoistedTypeCachePool",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
        if (!t.IsNotNull(cache)) return;
        var scratch = (Type[])cache.GetType().GetField("Scratch", fields).GetValue(cache);
        t.IsTrue(scratch[0] == null);
        var types = cache.GetType().GetField("Types", fields).GetValue(cache);
        t.AreEqual(0, (int)types.GetType().GetProperty("Count").GetValue(types));
        AssertNoPooledMetadataReferences(t, types);
    }

    private static void AssertNoPooledMetadataReferences(TestContext t, object value)
    {
        if (value == null) return;
        if (value is Array array)
        {
            foreach (var element in array) AssertNoPooledMetadataReferences(t, element);
            return;
        }
        var type = value.GetType();
        if (type.IsPrimitive || type.IsEnum) return;
        if (!t.IsTrue(type.IsValueType)) return;
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            AssertNoPooledMetadataReferences(t, field.GetValue(value));
    }
}
