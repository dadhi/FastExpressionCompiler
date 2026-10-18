using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using F = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using F = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public struct NonExceptionThrowPolicyRegressionTests : ITestX
{
    private static string Trace;
    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };

    public void Run(TestRun t)
    {
        Raw_and_wrapped_payloads_preserve_type_and_identity(t);
        External_thrower_wrapping_policy_does_not_change_matching(t);
        Filters_preserve_search_order_and_catch_variable_storage(t);
        Rethrow_preserves_original_payload_after_variable_assignment(t);
        Anonymous_methods_retain_private_generic_and_nested_access(t);
        Explicit_compilation_paths_preserve_their_module_policy(t);
    }

    public static void Mark(string value) => Trace += value + ";";
    public static bool Filter(string name, string value, bool accepted)
    { Trace += name + "(" + value + ");"; return accepted; }
    public static bool ThrowingFilter() { Mark("filterthrows"); throw new ApplicationException("filter"); }
    private static System.Linq.Expressions.Expression MarkCall(string value) =>
        S.Call(typeof(NonExceptionThrowPolicyRegressionTests).GetMethod(nameof(Mark)), S.Constant(value));
    private static System.Linq.Expressions.Expression FilterCall(string name, System.Linq.Expressions.Expression value, bool accepted) =>
        S.Call(typeof(NonExceptionThrowPolicyRegressionTests).GetMethod(nameof(Filter)), S.Constant(name), value, S.Constant(accepted));
    private static RuntimeWrappedException Wrap(object payload) => (RuntimeWrappedException)
        typeof(RuntimeWrappedException).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(object) }, null).Invoke(new[] { payload });

    public void Raw_and_wrapped_payloads_preserve_type_and_identity(TestContext t)
    {
        var payload = new object();
        var exception = new InvalidOperationException("inner");
        foreach (var original in new[] { payload, (object)exception, new string(new[] { 'r', 'a', 'w' }) })
        foreach (var wrapped in new[] { false, true })
        {
            var caught = S.Parameter(typeof(object));
            var thrown = wrapped ? (object)Wrap(original) : original;
            var source = S.Lambda<Func<object>>(S.TryCatch(S.Throw(S.Constant(thrown, typeof(object)), typeof(object)),
                S.Catch(caught, caught)));
            t.IsTrue(ReferenceEquals(original, source.Compile()()));
            foreach (var flags in Flags)
            {
                var fast = source.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(fast)) t.IsTrue(ReferenceEquals(original, fast()));
            }
        }
        var text = S.Parameter(typeof(string));
        Check(t, S.Lambda<Func<string>>(S.TryCatch(S.Throw(S.Constant(Wrap("original")), typeof(string)),
            S.Catch(typeof(RuntimeWrappedException), S.Constant("wrapper")),
            S.Catch(typeof(Exception), S.Constant("exception")), S.Catch(text, text))), "original");
        var escapes = S.Lambda<Action>(S.TryCatch(S.Throw(S.Constant("original")),
            S.Catch(typeof(RuntimeWrappedException), S.Empty())));
        t.AreEqual("original", t.Throws<RuntimeWrappedException>(() => escapes.Compile()()).WrappedException);
        foreach (var flags in Flags)
        {
            var fast = escapes.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fast)) t.AreEqual("original", t.Throws<RuntimeWrappedException>(() => fast()).WrappedException);
        }
    }

    private static MethodInfo ExternalThrower(bool wraps)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NonException" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        assembly.SetCustomAttribute(new CustomAttributeBuilder(typeof(RuntimeCompatibilityAttribute).GetConstructor(Type.EmptyTypes),
            new object[0], new[] { typeof(RuntimeCompatibilityAttribute).GetProperty(nameof(RuntimeCompatibilityAttribute.WrapNonExceptionThrows)) }, new object[] { wraps }));
        var type = assembly.DefineDynamicModule("M").DefineType("Thrower", TypeAttributes.Public);
        var method = type.DefineMethod("Throw", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        var il = method.GetILGenerator(); il.Emit(OpCodes.Ldstr, "external"); il.Emit(OpCodes.Throw); il.Emit(OpCodes.Ret);
        return type.CreateTypeInfo().GetMethod("Throw");
    }

    public void External_thrower_wrapping_policy_does_not_change_matching(TestContext t)
    {
        foreach (var wraps in new[] { false, true })
        {
            var caught = S.Parameter(typeof(string));
            Check(t, S.Lambda<Func<string>>(S.TryCatch(S.Block(S.Call(ExternalThrower(wraps)), S.Constant("none")),
                S.Catch(typeof(RuntimeWrappedException), S.Constant("wrapper")), S.Catch(caught, caught))), "external");
        }
    }

    public void Filters_preserve_search_order_and_catch_variable_storage(TestContext t)
    {
        var first = S.Parameter(typeof(string)); var second = S.Parameter(typeof(string));
        Check(t, S.Lambda<Func<string>>(S.TryCatch(S.TryFinally(S.Throw(S.Constant("original"), typeof(string)), MarkCall("finally")),
            S.Catch(first, S.Constant("bad"), S.Block(S.Assign(first, S.Constant("changed")), FilterCall("first", first, false))),
            S.Catch(second, S.Block(MarkCall("handler"), second), FilterCall("second", second, true)))),
            "original", "first(changed);second(original);finally;handler;", hasFilter: true);
        Check(t, S.Lambda<Func<string>>(S.TryCatch(S.Throw(S.Constant("original"), typeof(string)),
            S.Catch(first, first, S.Block(S.Assign(first, S.Constant("changed")), FilterCall("first", first, true))))),
            "changed", "first(changed);", hasFilter: true);
        Check(t, S.Lambda<Func<string>>(S.TryCatch(S.Throw(S.Constant("original"), typeof(string)),
            S.Catch(first, S.Constant("bad"), S.Call(typeof(NonExceptionThrowPolicyRegressionTests).GetMethod(nameof(ThrowingFilter)))),
            S.Catch(second, second))), "original", "filterthrows;", hasFilter: true);
    }

    public void Rethrow_preserves_original_payload_after_variable_assignment(TestContext t)
    {
        foreach (var wrapped in new[] { false, true })
        foreach (var rethrow in new[] { false, true })
        {
            var first = S.Parameter(typeof(string)); var second = S.Parameter(typeof(string));
            var thrown = wrapped ? (object)Wrap("original") : "original";
            Check(t, S.Lambda<Func<string>>(S.TryCatch(S.TryCatch(S.Throw(S.Constant(thrown, typeof(object)), typeof(string)),
                S.Catch(first, S.Block(S.Assign(first, S.Constant("changed")),
                    rethrow ? S.Rethrow(typeof(string)) : S.Throw(first, typeof(string))))), S.Catch(second, second))),
                rethrow ? "original" : "changed");
        }
    }

    private sealed class PrivateGeneric<T>
    {
        private readonly T _value;
        private PrivateGeneric(T value) => _value = value;
        private U Echo<U>(U value) => value;
    }
    private sealed class PrivateNoArgs
    {
        public readonly int Value;
        private PrivateNoArgs() => Value = 19;
    }
    private struct PrivatePayload { public int Value; }

    public void Anonymous_methods_retain_private_generic_and_nested_access(TestContext t)
    {
        var type = typeof(PrivateGeneric<int>);
        var ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
        var field = type.GetField("_value", BindingFlags.Instance | BindingFlags.NonPublic);
        var echo = type.GetMethod("Echo", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(int));
        var value = S.Parameter(typeof(int)); var owner = S.Variable(type);
        // Real EH forces the anonymous-method path, including access checks for
        // private user members and compiler-generated closure/quote helpers.
        var direct = S.Lambda<Func<int, int>>(S.TryFinally(S.Block(new[] { owner }, S.Assign(owner, S.New(ctor, value)),
            S.Add(S.Field(owner, field), S.Call(owner, echo, value))), S.Empty()), value);
        var owned = S.Lambda<Func<int, int>>(((System.Linq.Expressions.TryExpression)direct.Body).Body, value);
        t.AreEqual(34, direct.Compile()(17));
        var inner = S.Lambda<Func<int>>(S.TryFinally(S.Field(S.New(ctor, value), field), S.Empty()));
        var nested = S.Lambda<Func<int, Func<int>>>(S.TryFinally(inner, S.Empty()), value);
        var quoted = S.Lambda<Func<int, System.Linq.Expressions.Expression<Func<int>>>>(S.TryFinally(S.Quote(inner), S.Empty()), value);
        var noArgs = S.Lambda<Func<PrivateNoArgs>>(S.New(typeof(PrivateNoArgs).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
            null, Type.EmptyTypes, null)));
        t.AreEqual(19, noArgs.Compile()().Value);
        var without = direct.FromSysExpression().TryCompileWithoutClosure<Func<int, int>>();
        if (t.IsNotNull(without)) t.AreEqual(34, without(17));
        var noArgsWithout = noArgs.FromSysExpression().TryCompileWithoutClosure<Func<PrivateNoArgs>>();
        if (t.IsNotNull(noArgsWithout)) t.AreEqual(19, noArgsWithout().Value);
        var hidden = ctor.Invoke(new object[] { 17 });
        var constant = S.Constant(hidden, type);
        var privateCapture = S.Lambda<Func<int>>(S.Field(constant, field)).FromSysExpression()
            .TryCompileWithPreCreatedClosure<Func<int>>(F.Constant(hidden, type));
        if (t.IsNotNull(privateCapture)) t.AreEqual(17, privateCapture());
        // Reusing the same private reference constant forces a cached typed local
        // in the EH method's closure prologue.
        var repeatedConstant = S.Lambda<Func<int>>(S.TryFinally(
            S.Add(S.Field(constant, field), S.Field(constant, field)), S.Empty()));
        t.AreEqual(34, repeatedConstant.Compile()());
        foreach (var flags in Flags)
        {
            var fastConstant = repeatedConstant.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastConstant)) t.AreEqual(34, fastConstant());
            var fastDirect = direct.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastDirect)) t.AreEqual(34, fastDirect(17));
            var fastNested = nested.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastNested)) t.AreEqual(17, fastNested(17)());
            var fastQuote = quoted.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastQuote))
            {
                var tree = fastQuote(17); t.AreEqual(17, tree.Compile()());
                var compiled = tree.FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(compiled)) t.AreEqual(17, compiled());
            }
            var fastNoArgs = noArgs.FromSysExpression().CompileFast(true, flags);
            if (t.IsNotNull(fastNoArgs)) t.AreEqual(19, fastNoArgs().Value);
            // Alternate identical signatures across owned and anonymous methods.
            // Default pooling stays enabled, including private struct signatures.
            for (var i = 0; i < 4; ++i)
            {
                var next = (i % 2 == 0 ? owned : direct).FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(next)) t.AreEqual(2 * (17 + i), next(17 + i));
                var payload = S.Parameter(typeof(PrivatePayload));
                var body = i % 2 == 0 ? (System.Linq.Expressions.Expression)payload : S.TryFinally(payload, S.Empty());
                var identity = S.Lambda<Func<PrivatePayload, PrivatePayload>>(body, payload).FromSysExpression().CompileFast(true, flags);
                if (t.IsNotNull(identity)) t.AreEqual(i, identity(new PrivatePayload { Value = i }).Value);
            }
        }
    }

    public void Explicit_compilation_paths_preserve_their_module_policy(TestContext t)
    {
        var caught = S.Parameter(typeof(string));
        var source = S.Lambda<Func<string>>(S.TryCatch(S.Throw(S.Constant("original"), typeof(string)), S.Catch(caught, caught)));
        var without = source.FromSysExpression().TryCompileWithoutClosure<Func<string>>();
        if (t.IsNotNull(without)) t.AreEqual("original", without());
        var payload = new object(); var item = S.Parameter(typeof(object));
        var captured = S.Lambda<Func<object>>(S.TryCatch(S.Throw(S.Constant(payload, typeof(object)), typeof(object)), S.Catch(item, item)));
        var precreated = captured.FromSysExpression().TryCompileWithPreCreatedClosure<Func<object>>(F.Constant(payload, typeof(object)));
        if (t.IsNotNull(precreated)) t.IsTrue(ReferenceEquals(payload, precreated()));

        var action = S.Lambda<Action>(S.TryCatch(S.Throw(S.Constant("original")), S.Catch(typeof(string), S.Empty())));
        foreach (var anonymous in new[] { false, true })
        {
            var method = anonymous ? new DynamicMethod("caller", typeof(void), Type.EmptyTypes, true)
                : new DynamicMethod("caller", typeof(void), Type.EmptyTypes, typeof(NonExceptionThrowPolicyRegressionTests), true);
            if (!t.IsTrue(action.FromSysExpression().CompileFastToIL(method.GetILGenerator()))) continue;
            var compiled = (Action)method.CreateDelegate(typeof(Action));
            // Supplying IL keeps the caller's module policy; it cannot change ownership.
            if (anonymous) compiled(); else t.Throws<RuntimeWrappedException>(() => compiled());
        }
    }

    private static void Check(TestContext t, System.Linq.Expressions.Expression<Func<string>> source,
        string expected, string trace = "", bool hasFilter = false)
    {
#if NETFRAMEWORK
        if (hasFilter)
        {
            // Framework DynamicMethod cannot represent filters, independently of payload policy.
            t.Throws<NotSupportedException>(() => source.Compile());
            foreach (var flags in Flags)
                if ((flags & CompilerFlags.ThrowOnNotSupportedExpression) != 0)
                    t.Throws<NotSupportedExpressionException>(() => source.FromSysExpression().CompileFast(true, flags));
                else
                    t.IsTrue(source.FromSysExpression().CompileFast(true, flags) == null);
            return;
        }
#endif
        Trace = ""; t.AreEqual(expected, source.Compile()()); t.AreEqual(trace, Trace);
        foreach (var flags in Flags)
        {
            var fast = source.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(fast)) continue;
            Trace = ""; t.AreEqual(expected, fast()); t.AreEqual(trace, Trace);
        }
    }
}
