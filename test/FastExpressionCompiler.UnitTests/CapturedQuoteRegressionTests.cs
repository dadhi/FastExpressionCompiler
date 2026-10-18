using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CapturedQuoteRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Quotes_and_sibling_delegates_share_live_writable_cells(t);
        Activations_and_loop_block_reentry_have_distinct_cells(t);
        Nested_quotes_keep_the_original_live_capture(t);
        Quote_bindings_shadow_reused_parameter_identities(t);
        Reference_and_nullable_cells_keep_their_exact_types(t);
        Mutable_struct_field_and_ref_writes_reach_the_shared_cell(t);
        Catch_filter_and_handler_quotes_share_the_exception_cell(t);
        More_than_64_captured_cells_remain_distinct(t);
        Quoted_runtime_variables_alias_captures_and_preserve_local_lifetimes(t);
        Quoted_explicit_block_types_are_preserved(t);
    }

    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default,
        CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };

    public void Quotes_and_sibling_delegates_share_live_writable_cells(TestContext t)
    {
        var x = S.Parameter(typeof(int), "x"); var bundle = S.Variable(typeof(Bundle<int>), "bundle");
        var expression = S.Lambda<Func<int, Bundle<int>>>(S.Block(new[] { bundle },
            S.Assign(bundle, BundleNode<int>(x)), S.Assign(x, S.Add(x, S.Constant(1))), bundle), x);
        var system = expression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); if (!t.IsNotNull(fast)) return;
            var expected = ObserveIntBundle(t, system(40), flags);
            var actual = ObserveIntBundle(t, fast(40), flags);
            t.AreEqual("41;100;123;456;456;", expected); t.AreEqual(expected, actual);
        }
    }

    public void Activations_and_loop_block_reentry_have_distinct_cells(TestContext t)
    {
        var factory = BundleFactory<int>(); var systemFactory = factory.Compile();
        var index = S.Variable(typeof(int), "index"); var cell = S.Variable(typeof(int), "cell");
        var bundles = S.Variable(typeof(Bundle<int>[]), "bundles"); var done = S.Label("done");
        var loopBody = S.Block(new[] { cell }, S.Assign(cell, index),
            S.Assign(S.ArrayAccess(bundles, index), BundleNode<int>(cell)), S.PostIncrementAssign(index), S.Empty());
        var loopExpression = S.Lambda<Func<Bundle<int>[]>>(S.Block(new[] { index, bundles },
            S.Assign(bundles, S.NewArrayBounds(typeof(Bundle<int>), S.Constant(3))),
            S.Loop(S.IfThenElse(S.LessThan(index, S.Constant(3)), loopBody, S.Break(done)), done), bundles));
        var systemLoop = loopExpression.Compile();
        foreach (var flags in Flags)
        {
            var fastFactory = Fast(factory, flags); var fastLoop = Fast(loopExpression, flags);
            if (!t.IsNotNull(fastFactory) || !t.IsNotNull(fastLoop)) return;
            CheckIndependentActivations(t, systemFactory, flags); CheckIndependentActivations(t, fastFactory, flags);
            var expected = ObserveLoop(t, systemLoop(), flags); var actual = ObserveLoop(t, fastLoop(), flags);
            t.AreEqual("0;1;2;0;77;2;", expected); t.AreEqual(expected, actual);
        }
    }

    public void Nested_quotes_keep_the_original_live_capture(TestContext t)
    {
        var x = S.Parameter(typeof(int), "x"); var value = S.Parameter(typeof(int), "value");
        var inner = S.Quote(S.Lambda<Func<int>>(x));
        var nested = S.Quote(S.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(inner));
        var expression = S.Lambda<Func<int, NestedBundle>>(S.New(typeof(NestedBundle).GetConstructors()[0],
            nested, S.Lambda<Action<int>>(S.Assign(x, value), value)), x);
        var system = expression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); if (!t.IsNotNull(fast)) return;
            t.AreEqual(ObserveNested(t, system(10), flags), ObserveNested(t, fast(10), flags));
        }
    }

    public void Quote_bindings_shadow_reused_parameter_identities(TestContext t)
    {
        var x = S.Parameter(typeof(int), "same");
        var parameterQuote = S.Quote(S.Lambda<Func<int, int>>(x, x));
        var blockQuote = S.Quote(S.Lambda<Func<int>>(S.Block(new[] { x }, S.Assign(x, S.Constant(9)), x)));
        var expression = S.Lambda<Func<int, System.Linq.Expressions.LambdaExpression[]>>(S.NewArrayInit(typeof(System.Linq.Expressions.LambdaExpression),
            S.Convert(parameterQuote, typeof(System.Linq.Expressions.LambdaExpression)), S.Convert(blockQuote, typeof(System.Linq.Expressions.LambdaExpression))), x);
        var exception = S.Parameter(typeof(Exception), "same");
        var catchBody = S.TryCatch(S.Throw(S.New(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }),
            S.Constant("inner")), typeof(string)), S.Catch(exception, S.Property(exception, nameof(Exception.Message))));
        var catchExpression = S.Lambda<Func<Exception, System.Linq.Expressions.Expression<Func<string>>>>(S.Quote(S.Lambda<Func<string>>(catchBody)), exception);
        var system = expression.Compile(); var systemCatch = catchExpression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); var fastCatch = Fast(catchExpression, flags);
            if (!t.IsNotNull(fast) || !t.IsNotNull(fastCatch)) return;
            foreach (var outer in new[] { system, fast })
            {
                var quotes = outer(123);
                var parameterTree = (System.Linq.Expressions.Expression<Func<int, int>>)quotes[0]; var blockTree = (System.Linq.Expressions.Expression<Func<int>>)quotes[1];
                t.AreEqual(7, parameterTree.Compile()(7)); t.AreEqual(9, blockTree.Compile()());
                var parameterFast = Fast(parameterTree, flags); var blockFast = Fast(blockTree, flags);
                if (!t.IsNotNull(parameterFast) || !t.IsNotNull(blockFast)) return;
                t.AreEqual(7, parameterFast(7)); t.AreEqual(9, blockFast());
            }
            foreach (var outer in new[] { systemCatch, fastCatch })
            {
                var tree = outer(new Exception("outer")); t.AreEqual("inner", tree.Compile()());
                var quotedFast = Fast(tree, flags); if (!t.IsNotNull(quotedFast)) return;
                t.AreEqual("inner", quotedFast());
            }
        }
    }

    public void Reference_and_nullable_cells_keep_their_exact_types(TestContext t)
    {
        var references = BundleFactory<Box>(); var nullables = BundleFactory<int?>();
        var systemReferences = references.Compile(); var systemNullables = nullables.Compile();
        foreach (var flags in Flags)
        {
            var fastReferences = Fast(references, flags); var fastNullables = Fast(nullables, flags);
            if (!t.IsNotNull(fastReferences) || !t.IsNotNull(fastNullables)) return;
            foreach (var outer in new[] { systemReferences, fastReferences })
            {
                var first = new Box { Value = 1 }; var second = new Box { Value = 2 }; var bundle = outer(first);
                var read = Fast(bundle.Read, flags); var write = Fast(bundle.QuotedWrite, flags);
                if (!t.IsNotNull(read) || !t.IsNotNull(write)) return;
                t.AreEqual(true, ReferenceEquals(first, read())); bundle.Write(second);
                t.AreEqual(true, ReferenceEquals(second, read())); t.AreEqual(true, ReferenceEquals(second, bundle.Sibling()));
                write(first); t.AreEqual(true, ReferenceEquals(first, bundle.Sibling()));
                first.Value = 42; t.AreEqual(42, bundle.Read.Compile()().Value);
            }
            foreach (var outer in new[] { systemNullables, fastNullables })
            {
                var bundle = outer(null); var read = Fast(bundle.Read, flags); var write = Fast(bundle.QuotedWrite, flags);
                if (!t.IsNotNull(read) || !t.IsNotNull(write)) return;
                t.AreEqual(typeof(int?), bundle.Read.Body.Type); t.AreEqual(null, (object)read());
                bundle.Write(42); t.AreEqual((int?)42, read()); write(null);
                t.AreEqual(null, (object)bundle.Sibling()); t.AreEqual(null, (object)bundle.Read.Compile()());
            }
        }
    }

    public void Mutable_struct_field_and_ref_writes_reach_the_shared_cell(TestContext t)
    {
        var cell = S.Parameter(typeof(Counter), "cell"); var value = S.Parameter(typeof(int), "value");
        var field = S.Field(cell, nameof(Counter.Value));
        var expression = S.Lambda<Func<Counter, StructBundle>>(S.New(typeof(StructBundle).GetConstructors()[0],
            S.Quote(S.Lambda<Func<int>>(field)), S.Quote(S.Lambda<Action<int>>(S.Assign(field, value), value)),
            S.Quote(S.Lambda<Action>(S.Call(Method(nameof(Increment)), cell))),
            S.Lambda<Func<int>>(field), S.Lambda<Action<int>>(S.Assign(field, value), value)), cell);
        var system = expression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); if (!t.IsNotNull(fast)) return;
            var expected = ObserveStruct(t, system(new Counter { Value = 1 }), flags);
            var actual = ObserveStruct(t, fast(new Counter { Value = 1 }), flags);
            t.AreEqual("10;20;21;22;99;", expected); t.AreEqual(expected, actual);
        }
    }

    public void Catch_filter_and_handler_quotes_share_the_exception_cell(TestContext t)
    {
        var exception = S.Parameter(typeof(Exception), "exception"); var replacement = S.Parameter(typeof(Exception), "replacement");
        var read = S.Quote(S.Lambda<Func<Exception>>(exception));
        var write = S.Quote(S.Lambda<Action<Exception>>(S.Assign(exception, replacement), replacement));
        var filter = S.Block(S.Assign(exception, S.New(typeof(ApplicationException).GetConstructor(new[] { typeof(string) }),
            S.Constant("filter"))), S.Call(Method(nameof(CaptureFilter)), read, write));
        var body = S.TryCatch(S.Throw(S.New(typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) }),
            S.Constant("body")), typeof(System.Linq.Expressions.Expression<Func<Exception>>)), S.Catch(exception, read, filter));
        var expression = S.Lambda<Func<System.Linq.Expressions.Expression<Func<Exception>>>>(body);
#if NETFRAMEWORK
        // Framework DynamicMethod filters are unsupported by both compilers.
        // Check that contract, then exercise the same live catch cells without a filter.
        t.Throws<NotSupportedException>(() => expression.Compile());
        t.AreEqual(true, expression.FromSysExpression().CompileFast(true) == null);
        var unsupported = t.Throws<NotSupportedExpressionException>(() =>
            expression.FromSysExpression().CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression));
        if (t.IsNotNull(unsupported))
            t.AreEqual(ExpressionCompiler.Result.NotSupported_ExceptionCatchFilter, unsupported.Reason);
        expression = S.Lambda<Func<System.Linq.Expressions.Expression<Func<Exception>>>>(S.TryCatch(
            ((System.Linq.Expressions.TryExpression)body).Body, S.Catch(exception, S.Block(
                S.Assign(exception, S.New(typeof(ApplicationException).GetConstructor(new[] { typeof(string) }), S.Constant("filter"))),
                S.Call(Method(nameof(CaptureFilter)), read, write), read))));
#endif
        var system = expression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); if (!t.IsNotNull(fast)) return;
            var expected = ObserveFilter(t, system, flags); var actual = ObserveFilter(t, fast, flags);
            t.AreEqual(expected, actual);
        }
    }

    public void More_than_64_captured_cells_remain_distinct(TestContext t)
    {
        var variables = new System.Linq.Expressions.ParameterExpression[70]; var expressions = new List<S>(); S sum = S.Constant(0);
        for (var i = 0; i < variables.Length; ++i)
        {
            variables[i] = S.Variable(typeof(int), "v" + i); expressions.Add(S.Assign(variables[i], S.Constant(i)));
            sum = S.Add(sum, variables[i]);
        }
        var bundle = S.Variable(typeof(ManyBundle), "bundle");
        expressions.Add(S.Assign(bundle, S.New(typeof(ManyBundle).GetConstructors()[0], S.Quote(S.Lambda<Func<int>>(sum)),
            S.Lambda<Action>(S.AddAssign(variables[65], S.Constant(1000))),
            S.Quote(S.Lambda<Action>(S.Assign(variables[69], S.Constant(777)))))));
        expressions.Add(S.Assign(variables[0], S.Constant(100))); expressions.Add(bundle);
        var allVariables = new List<System.Linq.Expressions.ParameterExpression>(variables) { bundle };
        var expression = S.Lambda<Func<ManyBundle>>(S.Block(allVariables, expressions)); var system = expression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); if (!t.IsNotNull(fast)) return;
            var expected = ObserveMany(t, system(), flags); var actual = ObserveMany(t, fast(), flags);
            t.AreEqual("2515;3515;4223;4223;", expected); t.AreEqual(expected, actual);
        }
    }

    public void Quoted_runtime_variables_alias_captures_and_preserve_local_lifetimes(TestContext t)
    {
        var captured = S.Parameter(typeof(int), "captured"); var local = S.Variable(typeof(int), "local");
        var value = S.Parameter(typeof(int), "value");
        var tree = S.Quote(S.Lambda<Func<IRuntimeVariables>>(S.Block(new[] { local }, S.Assign(local, S.Constant(20)),
            S.RuntimeVariables(captured, local, captured))));
        var expression = S.Lambda<Func<int, VariablesBundle>>(S.New(typeof(VariablesBundle).GetConstructors()[0], tree,
            S.Lambda<Func<int>>(captured), S.Lambda<Action<int>>(S.Assign(captured, value), value)), captured);
        var system = expression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); if (!t.IsNotNull(fast)) return;
            var expected = ObserveVariables(t, system(10), flags); var actual = ObserveVariables(t, fast(10), flags);
            t.AreEqual(expected, actual);
        }
    }

    public void Quoted_explicit_block_types_are_preserved(TestContext t)
    {
        var captured = S.Parameter(typeof(int), "captured"); var local = S.Variable(typeof(int), "local");
        var block = S.Block(typeof(void), new[] { local }, S.Assign(local, S.Add(captured, S.Constant(1))),
            S.Call(Method(nameof(Record)), local), local);
        var expression = S.Lambda<Func<int, System.Linq.Expressions.Expression<Action>>>(S.Quote(S.Lambda<Action>(block)), captured);
        var system = expression.Compile();
        foreach (var flags in Flags)
        {
            var fast = Fast(expression, flags); if (!t.IsNotNull(fast)) return;
            foreach (var outer in new[] { system, fast })
            {
                var tree = outer(41); t.AreEqual(typeof(void), tree.Body.Type);
                t.AreEqual(typeof(int), ((System.Linq.Expressions.BlockExpression)tree.Body).Variables[0].Type);
                Trace = ""; tree.Compile()(); t.AreEqual("42;", Trace);
                var quotedFast = Fast(tree, flags); if (!t.IsNotNull(quotedFast)) return;
                Trace = ""; quotedFast(); t.AreEqual("42;", Trace);
            }
        }
    }

    private static S BundleNode<T>(System.Linq.Expressions.ParameterExpression variable)
    {
        var value = S.Parameter(typeof(T), "value");
        return S.New(typeof(Bundle<T>).GetConstructors()[0], S.Quote(S.Lambda<Func<T>>(variable)),
            S.Quote(S.Lambda<Action<T>>(S.Assign(variable, value), value)), S.Lambda<Func<T>>(variable),
            S.Lambda<Action<T>>(S.Assign(variable, value), value));
    }
    private static System.Linq.Expressions.Expression<Func<T, Bundle<T>>> BundleFactory<T>()
    {
        var value = S.Parameter(typeof(T), "value"); return S.Lambda<Func<T, Bundle<T>>>(BundleNode<T>(value), value);
    }
    private static TDelegate Fast<TDelegate>(System.Linq.Expressions.Expression<TDelegate> expression, CompilerFlags flags) where TDelegate : Delegate
    {
#if LIGHT_EXPRESSION
        return expression.ToLightExpression().CompileFast(true, flags);
#else
        return expression.CompileFast(true, flags);
#endif
    }
    private static string ObserveIntBundle(TestContext t, Bundle<int> bundle, CompilerFlags flags)
    {
        var read = Fast(bundle.Read, flags); var write = Fast(bundle.QuotedWrite, flags);
        if (!t.IsNotNull(read) || !t.IsNotNull(write)) return "unsupported";
        var result = read() + ";"; bundle.Write(100); result += read() + ";";
        bundle.QuotedWrite.Compile()(123); result += bundle.Sibling() + ";";
        write(456); return result + bundle.Sibling() + ";" + bundle.Read.Compile()() + ";";
    }
    private static void CheckIndependentActivations(TestContext t, Func<int, Bundle<int>> factory, CompilerFlags flags)
    {
        var first = factory(10); var second = factory(20); first.Write(99);
        var firstRead = Fast(first.Read, flags); var secondRead = Fast(second.Read, flags);
        if (!t.IsNotNull(firstRead) || !t.IsNotNull(secondRead)) return;
        t.AreEqual(99, firstRead()); t.AreEqual(20, secondRead()); t.AreEqual(20, second.Sibling());
    }
    private static string ObserveLoop(TestContext t, Bundle<int>[] bundles, CompilerFlags flags)
    {
        var reads = new Func<int>[3];
        for (var i = 0; i < reads.Length; ++i) { reads[i] = Fast(bundles[i].Read, flags); if (!t.IsNotNull(reads[i])) return "unsupported"; }
        var result = reads[0]() + ";" + reads[1]() + ";" + reads[2]() + ";";
        bundles[1].QuotedWrite.Compile()(77);
        return result + reads[0]() + ";" + bundles[1].Sibling() + ";" + reads[2]() + ";";
    }
    private static string ObserveNested(TestContext t, NestedBundle bundle, CompilerFlags flags)
    {
        var outerFast = Fast(bundle.Read, flags); if (!t.IsNotNull(outerFast)) return "unsupported";
        var fromSystem = bundle.Read.Compile()(); var fromFast = outerFast();
        var first = Fast(fromSystem, flags); var second = Fast(fromFast, flags);
        if (!t.IsNotNull(first) || !t.IsNotNull(second)) return "unsupported";
        bundle.Write(42);
        t.AreEqual(42, first()); t.AreEqual(42, second());
        return fromSystem.Compile()() + ";" + fromFast.Compile()() + ";" + first() + ";" + second() + ";";
    }
    private static string ObserveStruct(TestContext t, StructBundle bundle, CompilerFlags flags)
    {
        var read = Fast(bundle.Read, flags); var fieldWrite = Fast(bundle.FieldWrite, flags); var increment = Fast(bundle.Increment, flags);
        if (!t.IsNotNull(read) || !t.IsNotNull(fieldWrite) || !t.IsNotNull(increment)) return "unsupported";
        bundle.FieldWrite.Compile()(10); var result = bundle.Sibling() + ";";
        fieldWrite(20); result += bundle.Sibling() + ";"; bundle.Increment.Compile()(); result += read() + ";";
        increment(); result += bundle.Sibling() + ";"; bundle.Write(99); return result + bundle.Read.Compile()() + ";";
    }
    private static string ObserveFilter(TestContext t, Func<System.Linq.Expressions.Expression<Func<Exception>>> outer, CompilerFlags flags)
    {
        FilterRead = null; FilterWrite = null; var returned = outer();
        if (!t.IsNotNull(FilterRead) || !t.IsNotNull(FilterWrite)) return "missing filter capture";
        var read = Fast(returned, flags); var filterRead = Fast(FilterRead, flags); var write = Fast(FilterWrite, flags);
        if (!t.IsNotNull(read) || !t.IsNotNull(filterRead) || !t.IsNotNull(write)) return "unsupported";
        var result = returned.Compile()().Message + ";";
        FilterWrite.Compile()(new Exception("after")); result += read().Message + ";" + filterRead().Message + ";";
        write(new Exception("final")); t.AreEqual("final", read().Message); t.AreEqual("final", FilterRead.Compile()().Message);
        return result + read().Message + ";";
    }
    private static string ObserveMany(TestContext t, ManyBundle bundle, CompilerFlags flags)
    {
        var read = Fast(bundle.Read, flags); var write = Fast(bundle.Write, flags);
        if (!t.IsNotNull(read) || !t.IsNotNull(write)) return "unsupported";
        var result = read() + ";"; bundle.Increment(); result += read() + ";";
        bundle.Write.Compile()(); result += read() + ";"; write(); return result + bundle.Read.Compile()() + ";";
    }
    private static string ObserveVariables(TestContext t, VariablesBundle bundle, CompilerFlags flags)
    {
        var invoke = Fast(bundle.Read, flags); if (!t.IsNotNull(invoke)) return "unsupported";
        var first = bundle.Read.Compile()(); var second = invoke();
        t.AreEqual(3, first.Count); t.AreEqual(3, second.Count);
        first[0] = 30; t.AreEqual(30, bundle.Sibling()); t.AreEqual(30, first[2]); t.AreEqual(30, second[0]);
        first[1] = 99; t.AreEqual(20, second[1]); second[2] = 40; t.AreEqual(40, first[0]); t.AreEqual(40, bundle.Sibling());
        var expectedError = SetError(first, "wrong type"); var actualError = SetError(second, "wrong type");
        t.AreEqual(expectedError, actualError); t.AreEqual(40, bundle.Sibling()); bundle.Write(50);
        return first[0] + ";" + first[1] + ";" + first[2] + ";" + second[0] + ";" + second[1] + ";" + expectedError;
    }
    private static Type SetError(IRuntimeVariables variables, object value)
    {
        try { variables[0] = value; return null; } catch (Exception error) { return error.GetType(); }
    }
    private static MethodInfo Method(string name) => typeof(CapturedQuoteRegressionTests).GetMethod(name);
    public static string Trace;
    public static void Record(int value) => Trace += value + ";";
    public static void Increment(ref Counter value) => ++value.Value;
    public static System.Linq.Expressions.Expression<Func<Exception>> FilterRead;
    public static System.Linq.Expressions.Expression<Action<Exception>> FilterWrite;
    public static bool CaptureFilter(System.Linq.Expressions.Expression<Func<Exception>> read, System.Linq.Expressions.Expression<Action<Exception>> write)
    { FilterRead = read; FilterWrite = write; return true; }

    public sealed class Bundle<T>
    {
        public readonly System.Linq.Expressions.Expression<Func<T>> Read; public readonly System.Linq.Expressions.Expression<Action<T>> QuotedWrite;
        public readonly Func<T> Sibling; public readonly Action<T> Write;
        public Bundle(System.Linq.Expressions.Expression<Func<T>> read, System.Linq.Expressions.Expression<Action<T>> quotedWrite, Func<T> sibling, Action<T> write)
        { Read = read; QuotedWrite = quotedWrite; Sibling = sibling; Write = write; }
    }
    public sealed class NestedBundle
    {
        public readonly System.Linq.Expressions.Expression<Func<System.Linq.Expressions.Expression<Func<int>>>> Read; public readonly Action<int> Write;
        public NestedBundle(System.Linq.Expressions.Expression<Func<System.Linq.Expressions.Expression<Func<int>>>> read, Action<int> write) { Read = read; Write = write; }
    }
    public sealed class StructBundle
    {
        public readonly System.Linq.Expressions.Expression<Func<int>> Read; public readonly System.Linq.Expressions.Expression<Action<int>> FieldWrite;
        public readonly System.Linq.Expressions.Expression<Action> Increment; public readonly Func<int> Sibling; public readonly Action<int> Write;
        public StructBundle(System.Linq.Expressions.Expression<Func<int>> read, System.Linq.Expressions.Expression<Action<int>> fieldWrite, System.Linq.Expressions.Expression<Action> increment,
            Func<int> sibling, Action<int> write) { Read = read; FieldWrite = fieldWrite; Increment = increment; Sibling = sibling; Write = write; }
    }
    public sealed class ManyBundle
    {
        public readonly System.Linq.Expressions.Expression<Func<int>> Read; public readonly Action Increment; public readonly System.Linq.Expressions.Expression<Action> Write;
        public ManyBundle(System.Linq.Expressions.Expression<Func<int>> read, Action increment, System.Linq.Expressions.Expression<Action> write) { Read = read; Increment = increment; Write = write; }
    }
    public sealed class VariablesBundle
    {
        public readonly System.Linq.Expressions.Expression<Func<IRuntimeVariables>> Read; public readonly Func<int> Sibling; public readonly Action<int> Write;
        public VariablesBundle(System.Linq.Expressions.Expression<Func<IRuntimeVariables>> read, Func<int> sibling, Action<int> write)
        { Read = read; Sibling = sibling; Write = write; }
    }
    public sealed class Box { public int Value; }
    public struct Counter { public int Value; }
}
