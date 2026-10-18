using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Xunit;

namespace System.Linq.Expressions.Tests;

// These test executable behavior separately from assertions about System's private
// closure representation and its known invalid-IL static-initializer implementation.
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
}
