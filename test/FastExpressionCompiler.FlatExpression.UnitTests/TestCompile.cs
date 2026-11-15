using System;
#if FLAT_EXPRESSION || LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression;
#endif
#if FLAT_EXPRESSION
using FastExpressionCompiler.FlatExpression;
#endif

namespace FastExpressionCompiler;

static class TestCompile
{
#if FLAT_EXPRESSION
    public static TDelegate CompileFast<TDelegate, TTest>(this LambdaIdx<TDelegate> expression, bool ifFastFailedReturnNull, in TTest test,
        CompilerFlags flags = CompilerFlags.Default)
        where TDelegate : Delegate
        where TTest : struct, ITestX
    {
        var expr = test.ExprTree;
        expr.RootIdx = expression.Idx;
        return expr.CompileFast<TDelegate>(ifFastFailedReturnNull, flags);
    }
#elif LIGHT_EXPRESSION
    public static TDelegate CompileFast<TDelegate, TTest>(this Expression<TDelegate> expression, bool ifFastFailedReturnNull, in TTest test,
        CompilerFlags flags = CompilerFlags.Default)
        where TDelegate : Delegate
        where TTest : struct =>
        // An optional CompilerFlags here binds as TTest and this method calls itself.
        ExpressionCompiler.CompileFast(expression, ifFastFailedReturnNull, flags);
#else
    public static TDelegate CompileFast<TDelegate, TTest>(this System.Linq.Expressions.Expression<TDelegate> expression, bool ifFastFailedReturnNull, in TTest test,
        CompilerFlags flags = CompilerFlags.Default)
        where TDelegate : Delegate
        where TTest : struct =>
        ExpressionCompiler.CompileFast(expression, ifFastFailedReturnNull, flags);
#endif
}
