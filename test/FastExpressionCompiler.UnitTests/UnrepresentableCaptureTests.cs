using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct UnrepresentableCaptureTests : ITestX
{
    public delegate Func<int> RefFactory(ref int value);
#if NET8_0_OR_GREATER
    public delegate Func<int> SpanFactory(Span<int> value);
#endif
    public void Run(TestRun t) { Escaping_ref_captures_are_rejected(t); Inlined_ref_access_is_still_supported(t);
#if NET8_0_OR_GREATER
        Escaping_byref_like_captures_are_rejected(t);
#endif
    }
    public void Escaping_ref_captures_are_rejected(TestContext t)
    {
        var value=S.Parameter(typeof(int).MakeByRefType());var tree=S.Lambda<RefFactory>(S.Lambda<Func<int>>(value),value);
        t.Throws<Exception>(()=>tree.Compile());
        foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo})
            t.AreEqual(null,tree.FromSysExpression().CompileFast(true,flags));
        t.Throws<NotSupportedExpressionException>(()=>tree.FromSysExpression().CompileFast(true,CompilerFlags.ThrowOnNotSupportedExpression));
    }
    public delegate int Read(ref int value);
    public void Inlined_ref_access_is_still_supported(TestContext t)
    {
        var value=S.Parameter(typeof(int).MakeByRefType());var tree=S.Lambda<Read>(S.Invoke(S.Lambda<Func<int>>(value)),value);
        var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;var n=7;t.AreEqual(tree.Compile()(ref n),fast(ref n));
    }
#if NET8_0_OR_GREATER
    public void Escaping_byref_like_captures_are_rejected(TestContext t)
    {
        var value=S.Parameter(typeof(Span<int>));var tree=S.Lambda<SpanFactory>(S.Lambda<Func<int>>(S.Property(value,nameof(Span<int>.Length))),value);
        t.Throws<Exception>(()=>tree.Compile());t.AreEqual(null,tree.FromSysExpression().CompileFast(true));
        t.Throws<NotSupportedExpressionException>(()=>tree.FromSysExpression().CompileFast(true,CompilerFlags.ThrowOnNotSupportedExpression));
    }
#endif
}
