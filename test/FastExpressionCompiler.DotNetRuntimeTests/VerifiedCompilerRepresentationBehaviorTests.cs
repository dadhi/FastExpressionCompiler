using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Xunit;

namespace System.Linq.Expressions.Tests;

// These test executable behavior separately from assertions about System's private
// closure representation and its known invalid-IL static-initializer implementation.
[CollectionDefinition("Verified compiler representation", DisableParallelization = true)]
public sealed class CompilerRepresentationCollection { }

[Collection("Verified compiler representation")]
public class VerifiedCompilerRepresentationBehaviorTests
{
    private static Delegate Strict(LambdaExpression expression)
    {
        var result = expression.CompileFast(true);
        Assert.NotNull(result);
        return result;
    }

    private static void Constant<T>(T value)
    {
        var expression = Expression.Lambda(Expression.Constant(value, typeof(T)));
        Assert.Equal((object)value, Strict(expression).DynamicInvoke());
        Assert.Equal(expression.Compile().DynamicInvoke(), Strict(expression).DynamicInvoke());
    }

    [Fact]
    public void NonNullableConstants()
    {
        Constant(true); Constant('a'); Constant((sbyte)42); Constant((byte)42);
        Constant((short)42); Constant((ushort)42); Constant(42); Constant(42u);
        Constant(42L); Constant(42UL); Constant(3.14f); Constant(3.14); Constant(49.95m);
    }

    [Fact]
    public void NullableConstants()
    {
        Constant<bool?>(null); Constant<bool?>(true); Constant<char?>(null); Constant<char?>('a');
        Constant<sbyte?>(null); Constant<sbyte?>(42); Constant<byte?>(null); Constant<byte?>(42);
        Constant<short?>(null); Constant<short?>(42); Constant<ushort?>(null); Constant<ushort?>(42);
        Constant<int?>(null); Constant<int?>(42); Constant<uint?>(null); Constant<uint?>(42);
        Constant<long?>(null); Constant<long?>(42); Constant<ulong?>(null); Constant<ulong?>(42);
        Constant<float?>(null); Constant<float?>(3.14f); Constant<double?>(null); Constant<double?>(3.14);
        Constant<decimal?>(null); Constant<decimal?>(49.95m); Constant<DateTime?>(null);
    }

    [Fact] public void ReferenceConstants() { Constant<string>(null); Constant("bar"); }
    [Fact] public void EnumConstants() { Constant(ConstantsEnum.A); Constant<ConstantsEnum?>(null); Constant<ConstantsEnum?>(ConstantsEnum.A); }
    [Fact] public void CapturedConstants() { var value = new DateTime(2026, 10, 18); Constant(value); Constant<DateTime?>(value); }

    [Fact]
    public void SharedConstantReferences()
    {
        var value = new object();
        var expression = Expression.Lambda(Expression.Equal(Expression.Constant(value), Expression.Constant(value)));
        Assert.Equal(true, Strict(expression).DynamicInvoke());
        Assert.Same(value, Strict(Expression.Lambda(Expression.Constant(value))).DynamicInvoke());
        Assert.Equal(true, expression.Compile().DynamicInvoke());
    }

    [Fact]
    public void StaticPropertyBinding()
    {
        var type = typeof(BindTests).GetNestedType("PropertyAndFields", BindingFlags.NonPublic);
        var property = type.GetProperty("StaticStringProperty");
        var previous = property.GetValue(null);
        try
        {
            var expression = Expression.Lambda(Expression.MemberInit(Expression.New(type),
                Expression.Bind(property, Expression.Constant("ABC"))));
            Assert.Equal(type, Strict(expression).DynamicInvoke().GetType());
            Assert.Equal("ABC", property.GetValue(null));
        }
        finally { property.SetValue(null, previous); }
    }

    [Fact] public void StaticInnerFieldBinding() => CheckInner("StaticInnerField", 6);
    [Fact] public void StaticInnerPropertyBinding() => CheckInner("StaticInnerProperty", 5);
    [Fact] public void StaticReadonlyInnerFieldBinding() => CheckInner("StaticReadonlyInnerField", 6);
    [Fact] public void StaticReadonlyInnerPropertyBinding() => CheckInner("StaticReadonlyInnerProperty", 5);

    private static void CheckInner(string memberName, int expected)
    {
        var type = typeof(MemberBindTests.Outer);
        var member = type.GetMember(memberName)[0];
        var target = member is FieldInfo field ? field.GetValue(null) : ((PropertyInfo)member).GetValue(null);
        var inner = Assert.IsType<MemberBindTests.Inner>(target);
        var previous = inner.Value;
        try
        {
            var expression = Expression.Lambda(Expression.MemberInit(Expression.New(type),
                Expression.MemberBind(member, Expression.Bind(typeof(MemberBindTests.Inner).GetProperty("Value"), Expression.Constant(expected)))));
            Assert.IsType<MemberBindTests.Outer>(Strict(expression).DynamicInvoke());
            Assert.Equal(expected, inner.Value);
            Assert.Same(target, member is FieldInfo f ? f.GetValue(null) : ((PropertyInfo)member).GetValue(null));
        }
        finally { inner.Value = previous; }
    }

    [Fact]
    public void StaticListFieldBinding()
    {
        var type = typeof(ListBindTests).GetNestedType("ListWrapper`1", BindingFlags.NonPublic).MakeGenericType(typeof(int));
        var field = type.GetField("StaticListField");
        var previous = field.GetValue(null);
        try
        {
            var list = new List<int>(); field.SetValue(null, list);
            var expression = Expression.Lambda(Expression.MemberInit(Expression.New(type),
                Expression.ListBind(field, Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant(0)))));
            Assert.Equal(type, Strict(expression).DynamicInvoke().GetType());
            Assert.Equal(new[] { 0 }, list); Assert.Same(list, field.GetValue(null));
        }
        finally { field.SetValue(null, previous); }
    }

    [Fact]
    public void StaticListPropertyBinding()
    {
        var type = typeof(ListBindTests).GetNestedType("ListWrapper`1", BindingFlags.NonPublic).MakeGenericType(typeof(int));
        var property = type.GetProperty("StaticListProperty");
        var previous = property.GetValue(null);
        try
        {
            property.SetValue(null, null);
            var expression = Expression.Lambda(Expression.MemberInit(Expression.New(type),
                Expression.ListBind(property, Expression.ElementInit(typeof(List<int>).GetMethod("Add"), Expression.Constant(0)))));
            var compiled = Strict(expression);
            var error = Assert.Throws<TargetInvocationException>(() => compiled.DynamicInvoke());
            Assert.IsType<NullReferenceException>(error.InnerException);
            var list = new List<int>(); property.SetValue(null, list);
            Assert.Equal(type, compiled.DynamicInvoke().GetType());
            Assert.Equal(new[] { 0 }, list); Assert.Same(list, property.GetValue(null));
        }
        finally { property.SetValue(null, previous); }
    }

    [Fact]
    public void InterpreterInstructionSimpleBehavior()
    {
        Expression<Func<string, bool>> tree = s => s != null && unchecked(s.Substring(1).Length * 2) > 0;
        var fast = (Func<string, bool>)Strict(tree);
        var system = tree.Compile(true);
        foreach (var value in new[] { null, "a", "ab", "abc" }) Assert.Equal(system(value), fast(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => fast(""));
        Assert.Throws<ArgumentOutOfRangeException>(() => system(""));
    }

    [Fact]
    public void InterpreterInstructionExceptionBehavior()
    {
        var x = Expression.Parameter(typeof(int));
        var tree = Expression.Lambda<Func<int, int>>(Expression.TryCatchFinally(
            Expression.Call(typeof(Math).GetMethod(nameof(Math.Abs), new[] { typeof(int) }),
                Expression.Divide(Expression.Constant(42), x)), Expression.Empty(),
            Expression.Catch(typeof(DivideByZeroException), Expression.Constant(-1))), x);
        var fast = (Func<int, int>)Strict(tree);
        var system = tree.Compile(true);
        foreach (var value in new[] { int.MinValue, -2, -1, 0, 1, 2, 100, int.MaxValue }) Assert.Equal(system(value), fast(value));
    }

    [Fact]
    public void InlinedThrowingConstructorBehavior()
    {
        var type = typeof(InterpreterTests).GetNestedType("Thrower", BindingFlags.NonPublic);
        var constructor = type.GetConstructor(new[] { typeof(bool) });
        foreach (var fail in new[] { false, true })
        {
            var tree = Expression.Lambda(Expression.New(constructor, Expression.Constant(fail)));
            var fast = Strict(tree);
            var system = tree.Compile(true);
            if (fail)
            {
                Assert.IsType<Exception>(Assert.Throws<TargetInvocationException>(() => fast.DynamicInvoke()).InnerException);
                Assert.IsType<Exception>(Assert.Throws<TargetInvocationException>(() => system.DynamicInvoke()).InnerException);
            }
            else
            {
                Assert.Equal(type, fast.DynamicInvoke().GetType());
                Assert.Equal(type, system.DynamicInvoke().GetType());
            }
        }
    }

    [Fact]
    public void InterpreterThunkActionAndResultBehavior()
    {
        // The collection disables parallelization because Console.Out is process-wide.
        var previous = Console.Out;
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            Console.SetOut(output);
            Expression<Action> action = () => Console.WriteLine("");
            Expression<Action<int>> action1 = x => Console.WriteLine(x.ToString());
            Expression<Action<int, object>> action2 = (x, y) => Console.WriteLine(y);
            ((Action)Strict(action))();
            ((Action<int>)Strict(action1))(42);
            ((Action<int, object>)Strict(action2))(7, "payload");
            Assert.Equal(Environment.NewLine + "42" + Environment.NewLine + "payload" + Environment.NewLine, output.ToString());
        }
        finally { Console.SetOut(previous); }
        Expression<Func<object>> func = () => null;
        Expression<Func<object, object>> func1 = x => null;
        Expression<Func<object, object, object>> func2 = (x, y) => null;
        Assert.Null(((Func<object>)Strict(func))());
        Assert.Null(((Func<object, object>)Strict(func1))(new object()));
        Assert.Null(((Func<object, object, object>)Strict(func2))(new object(), new object()));
    }

    [Fact]
    public void EmptyRuntimeVariablesSpillBehavior() => CheckRuntimeVariablesSpillBehavior(false,
        (tree, flags) => tree.CompileFast(true, flags));

    [Fact]
    public void CapturedRuntimeVariablesSpillBehavior() => CheckRuntimeVariablesSpillBehavior(true,
        (tree, flags) => tree.CompileFast(true, flags));

    // These are the exact trees in StackSpillerTests.Spill_Optimizations_RuntimeVariables1/2.
    // Additional controls expose ordering, shared storage and lifetime; they do not assert IL shape.
    private static void CheckRuntimeVariablesSpillBehavior(bool captured,
        Func<LambdaExpression, CompilerFlags, Delegate> compile)
    {
        var consumer = Expression.Parameter(typeof(Action<System.Runtime.CompilerServices.IRuntimeVariables, int>), "f");
        var value = Expression.Parameter(typeof(int), "x");
        var variables = captured ? Expression.RuntimeVariables(value) : Expression.RuntimeVariables();
        var body = Expression.Invoke(consumer, variables,
            Expression.TryFinally(Expression.Constant(2), Expression.Empty()));
        LambdaExpression exact = captured
            ? Expression.Lambda<Action<Action<System.Runtime.CompilerServices.IRuntimeVariables, int>, int>>(body, consumer, value)
            : Expression.Lambda<Action<Action<System.Runtime.CompilerServices.IRuntimeVariables, int>>>(body, consumer);
        var system = exact.Compile();
        var flagsToCheck = new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
        foreach (var flags in flagsToCheck)
        {
            var fast = compile(exact, flags); Assert.NotNull(fast);
            if (captured)
            {
                CheckCapturedRuntimeVariablesSpill(system); CheckCapturedRuntimeVariablesSpill(fast);
                // Reuse the original body, then return a sibling reading the owner's x.
                var owner = Expression.Lambda<Func<Action<System.Runtime.CompilerServices.IRuntimeVariables, int>, int, Func<int>>>(
                    Expression.Block(body, Expression.Lambda<Func<int>>(value)), consumer, value);
                var systemOwner = owner.Compile();
                var fastOwner = (Func<Action<System.Runtime.CompilerServices.IRuntimeVariables, int>, int, Func<int>>)compile(owner, flags);
                Assert.NotNull(fastOwner);
                CheckRuntimeVariablesOwnerAlias(systemOwner); CheckRuntimeVariablesOwnerAlias(fastOwner);
            }
            else
            {
                CheckEmptyRuntimeVariablesSpill(system); CheckEmptyRuntimeVariablesSpill(fast);
            }
            for (var mode = 0; mode < 5; ++mode)
                Assert.Equal(ObserveRuntimeVariablesSpillEffects(captured, mode, (tree, _) => tree.Compile(), flags),
                    ObserveRuntimeVariablesSpillEffects(captured, mode, compile, flags));
        }
    }

    private static void CheckEmptyRuntimeVariablesSpill(Delegate compiled)
    {
        var invoke = (Action<Action<System.Runtime.CompilerServices.IRuntimeVariables, int>>)compiled;
        var calls = 0;
        Action<System.Runtime.CompilerServices.IRuntimeVariables, int> consumer = (variables, value) =>
        {
            ++calls; Assert.Equal(2, value); Assert.Equal(0, variables.Count);
            Assert.Throws<IndexOutOfRangeException>(() => { var unused = variables[0]; });
            Assert.Throws<IndexOutOfRangeException>(() => variables[0] = 1);
        };
        invoke(consumer); invoke(consumer); Assert.Equal(2, calls);
        var failure = new InvalidOperationException("consumer");
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => invoke((_, __) => throw failure)));
        Assert.Throws<NullReferenceException>(() => invoke(null));
    }

    private static void CheckCapturedRuntimeVariablesSpill(Delegate compiled)
    {
        var invoke = (Action<Action<System.Runtime.CompilerServices.IRuntimeVariables, int>, int>)compiled;
        var retained = new List<System.Runtime.CompilerServices.IRuntimeVariables>();
        var inputs = new[] { int.MinValue, -3, 0, 41, int.MaxValue };
        foreach (var input in inputs)
        {
            var calls = 0;
            invoke((variables, value) =>
            {
                ++calls; Assert.Equal(2, value); Assert.Equal(1, variables.Count);
                Assert.Equal(input, Assert.IsType<int>(variables[0])); variables[0] = input ^ value;
                retained.Add(variables);
            }, input);
            Assert.Equal(1, calls); Assert.Equal(input ^ 2, retained[retained.Count - 1][0]);
        }
        retained[0][0] = 314;
        for (var i = 1; i < retained.Count; ++i) Assert.Equal(inputs[i] ^ 2, retained[i][0]);
        Assert.Throws<IndexOutOfRangeException>(() => { var unused = retained[0][-1]; });
        Assert.Throws<IndexOutOfRangeException>(() => retained[0][1] = 0);
        Assert.Throws<InvalidCastException>(() => retained[0][0] = "wrong type");
        Assert.Equal(314, retained[0][0]);
        System.Runtime.CompilerServices.IRuntimeVariables afterThrow = null;
        var failure = new InvalidOperationException("consumer");
        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => invoke((variables, _) =>
        { afterThrow = variables; variables[0] = 99; throw failure; }, 7)));
        Assert.Equal(99, afterThrow[0]); Assert.Throws<NullReferenceException>(() => invoke(null, 7));
    }

    private static void CheckRuntimeVariablesOwnerAlias(
        Func<Action<System.Runtime.CompilerServices.IRuntimeVariables, int>, int, Func<int>> invoke)
    {
        System.Runtime.CompilerServices.IRuntimeVariables first = null, second = null;
        var readFirst = invoke((variables, amount) => { first = variables; variables[0] = (int)variables[0] + amount; }, 10);
        var readSecond = invoke((variables, amount) => { second = variables; variables[0] = (int)variables[0] + amount; }, 20);
        Assert.Equal(12, readFirst()); Assert.Equal(22, readSecond());
        first[0] = 100; Assert.Equal(100, readFirst()); Assert.Equal(22, readSecond());
        second[0] = 200; Assert.Equal(200, readSecond()); Assert.Equal(100, readFirst());
    }

    private static string ObserveRuntimeVariablesSpillEffects(bool captured, int mode,
        Func<LambdaExpression, CompilerFlags, Delegate> compile, CompilerFlags flags)
    {
        var state = new RuntimeVariablesSpillEffects(mode);
        var value = Expression.Parameter(typeof(int), "x"); var instance = Expression.Constant(state);
        var variables = captured ? Expression.RuntimeVariables(value) : Expression.RuntimeVariables();
        var body = Expression.Invoke(Expression.Call(instance, typeof(RuntimeVariablesSpillEffects).GetMethod(nameof(RuntimeVariablesSpillEffects.Receiver))),
            Expression.Call(instance, typeof(RuntimeVariablesSpillEffects).GetMethod(nameof(RuntimeVariablesSpillEffects.Variables)), variables),
            Expression.TryFinally(Expression.Block(Expression.Assign(value, Expression.Add(value, Expression.Constant(1))),
                Expression.Call(instance, typeof(RuntimeVariablesSpillEffects).GetMethod(nameof(RuntimeVariablesSpillEffects.Argument)))),
                Expression.Call(instance, typeof(RuntimeVariablesSpillEffects).GetMethod(nameof(RuntimeVariablesSpillEffects.Finally)))));
        var tree = Expression.Lambda<Func<int, int>>(Expression.Block(body, value), value);
        var invoke = (Func<int, int>)compile(tree, flags); Assert.NotNull(invoke);
        int? result = null; Exception error = null;
        try { result = invoke(40); } catch (Exception failure) { error = failure; }
        var expectedError = mode == 1 ? typeof(ApplicationException) : mode == 2 ? typeof(FormatException)
            : mode == 3 ? typeof(NullReferenceException) : mode == 4 ? typeof(InvalidOperationException) : null;
        Assert.Equal(expectedError, error?.GetType());
        Assert.Equal(mode == 1 ? "receiver;" : mode == 2 || mode == 3 ? "receiver;variables;argument;finally;"
            : "receiver;variables;argument;finally;consumer;", state.Trace);
        if (state.Retained != null) Assert.Equal(captured ? 1 : 0, state.Retained.Count);
        if (state.Calls != 0) Assert.Equal(2, state.Number);
        return result + "|" + error?.GetType().FullName + "|" + state.Trace + "|" + state.Calls + "|"
            + (captured && state.Retained != null ? state.Retained[0] : null);
    }

    private sealed class RuntimeVariablesSpillEffects
    {
        private readonly int _mode;
        public string Trace = ""; public int Calls, Number;
        public System.Runtime.CompilerServices.IRuntimeVariables Retained;
        internal RuntimeVariablesSpillEffects(int mode) => _mode = mode;
        public Action<System.Runtime.CompilerServices.IRuntimeVariables, int> Receiver()
        {
            Trace += "receiver;";
            if (_mode == 1) throw new ApplicationException("receiver");
            return _mode == 3 ? null : Consumer;
        }
        public System.Runtime.CompilerServices.IRuntimeVariables Variables(System.Runtime.CompilerServices.IRuntimeVariables variables)
        { Trace += "variables;"; Retained = variables; return variables; }
        public int Argument() { Trace += "argument;"; if (_mode == 2) throw new FormatException("argument"); return 2; }
        public void Finally() => Trace += "finally;";
        private void Consumer(System.Runtime.CompilerServices.IRuntimeVariables variables, int amount)
        {
            Trace += "consumer;"; ++Calls; Number = amount; Retained = variables;
            if (variables.Count != 0) variables[0] = (int)variables[0] + amount;
            if (_mode == 4) throw new InvalidOperationException("consumer");
        }
    }

}
