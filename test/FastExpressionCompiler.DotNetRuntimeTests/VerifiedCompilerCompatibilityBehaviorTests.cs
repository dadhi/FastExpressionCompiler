using System;
using System.Linq.Expressions;
using System.Reflection;
using FastExpressionCompiler;
using Xunit;

namespace System.Linq.Expressions.Tests;

// These controls classify backend-specific upstream expectations; they do not
// represent IL-layout exclusions or claim interpreter-only features execute in FEC.
public class VerifiedCompilerCompatibilityBehaviorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstantValueTypeAssignmentsFollowSystemCompiled(bool spillingIndex) =>
        CheckConstantValueTypeAssignments(spillingIndex, (tree, flags) => tree.CompileFast(true, flags));

    private static void CheckConstantValueTypeAssignments(bool spillingIndex,
        Func<LambdaExpression, CompilerFlags, Delegate> compile)
    {
        // Exact original Assign.ValueTypeIndexAssign / Left_ValueTypeContainsChildTryExpression
        // forms and their original private struct. Each compiler receives a fresh boxed constant.
        var systemTree = Assignment(spillingIndex, false, out var systemBox, out var indexer);
        var system = systemTree.Compile();
        Assert.False(system()); Assert.False(system());
        Assert.Equal(0, indexer.GetValue(systemBox, new object[] { 1 }));
        var interpretedTree = Assignment(spillingIndex, false, out var interpretedBox, out _);
        Assert.True(interpretedTree.Compile(preferInterpretation: true)());
        Assert.Equal(123, indexer.GetValue(interpretedBox, new object[] { 1 }));
        foreach (var flags in Flags)
        {
            var fastTree = Assignment(spillingIndex, false, out var fastBox, out _);
            var fast = (Func<bool>)compile(fastTree, flags); Assert.NotNull(fast);
            Assert.Equal(system(), fast()); Assert.False(fast());
            Assert.Equal(0, indexer.GetValue(fastBox, new object[] { 1 }));
            // A writable struct local still receives the assignment in both compiled backends.
            var writable = Assignment(spillingIndex, true, out _, out _);
            Assert.True(writable.Compile()());
            var writableFast = (Func<bool>)compile(writable, flags); Assert.NotNull(writableFast);
            Assert.True(writableFast());
        }
    }

    private static Expression<Func<bool>> Assignment(bool spillingIndex, bool writableLocal,
        out object boxed, out PropertyInfo indexer)
    {
        var type = typeof(Assign).GetNestedType("StructWithPropertiesAndFields", BindingFlags.NonPublic);
        boxed = Activator.CreateInstance(type); indexer = type.GetProperty("Item");
        var local = Expression.Variable(type, "value");
        Expression receiver = writableLocal ? local : Expression.Constant(boxed, type);
        Expression argument = Expression.Constant(1);
        if (spillingIndex) argument = Expression.TryFinally(argument, Expression.Empty());
        var index = Expression.Property(receiver, indexer, new[] { argument });
        Expression body = Expression.Block(Expression.Assign(index, Expression.Constant(123)),
            Expression.Equal(index, Expression.Constant(123)));
        if (writableLocal) body = Expression.Block(new[] { local }, body);
        return Expression.Lambda<Func<bool>>(body);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NestedFilterRegionsAreRejectedByCompiledBackends(int kind) =>
        CheckNestedFilterRegions(kind, (tree, flags) => tree.CompileFast(true, flags));

    private static void CheckNestedFilterRegions(int kind, Func<LambdaExpression, CompilerFlags, Delegate> compile)
    {
        // Same expression forms and TestException type as TryFinallyWithinFilter,
        // TryCatchWithinFilter and TryCatchThrowingWithinFilter (interpreter rows).
        var tree = Filter(kind);
        Assert.Equal(2, tree.Compile(preferInterpretation: true)());
        Assert.Throws<InvalidOperationException>(() => tree.Compile());
        foreach (var flags in Flags)
        {
            Assert.Throws<InvalidOperationException>(() => compile(tree, flags));
            // The rejection is specific to nested EH regions, not ordinary filters.
            var valid = Filter(-1);
            var fast = (Func<int>)compile(valid, flags); Assert.NotNull(fast);
            Assert.Equal(valid.Compile()(), fast()); Assert.Equal(2, fast());
        }
    }

    private static Expression<Func<int>> Filter(int kind)
    {
        var type = typeof(ExceptionHandlingExpressions).GetNestedType("TestException", BindingFlags.NonPublic);
        Expression NewException() => Expression.Constant(Activator.CreateInstance(type), type);
        Expression Condition(bool value)
        {
            Expression body = Expression.Constant(value);
            if (kind == 0) return Expression.TryFinally(body, Expression.Empty());
            if (kind == 1) return Expression.TryCatch(body, Expression.Catch(typeof(Expression), body));
            if (kind == 2) return Expression.TryCatch(Expression.Throw(NewException(), typeof(bool)),
                Expression.Catch(typeof(Exception), body));
            return body;
        }
        return Expression.Lambda<Func<int>>(Expression.TryCatch(Expression.Throw(NewException(), typeof(int)),
            Expression.Catch(type, Expression.Constant(1), Condition(false)),
            Expression.Catch(type, Expression.Constant(2), Condition(true)),
            Expression.Catch(type, Expression.Constant(3))));
    }

    private static readonly CompilerFlags[] Flags = { CompilerFlags.Default,
        CompilerFlags.NoInvocationLambdaInlining,
        CompilerFlags.EnableDelegateDebugInfo | CompilerFlags.ThrowOnNotSupportedExpression };
}
