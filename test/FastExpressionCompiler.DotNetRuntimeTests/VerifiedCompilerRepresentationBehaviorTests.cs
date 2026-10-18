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
}
