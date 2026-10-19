using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct InvocationArgumentContextRegressionTests : ITestX
{
    public void Run(TestRun t)
    {

        Atomic_arithmetic_operands_follow_try_argument_finalizers(t);

#if LIGHT_EXPRESSION
        Intrinsic_constants_keep_their_exception_region_spills(t);
#endif
    }

    public void Atomic_arithmetic_operands_follow_try_argument_finalizers(TestContext t)
    {
        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining })
        for (var rightKind = 0; rightKind < 3; ++rightKind)
        {
            var value = SysExpr.Parameter(typeof(int));
            var argument = SysExpr.Parameter(typeof(int));
            System.Linq.Expressions.Expression right = rightKind == 0 ? SysExpr.Constant(2) :
                rightKind == 1 ? (System.Linq.Expressions.Expression)value : SysExpr.Default(typeof(int));
            var inner = SysExpr.Lambda<Func<int, int>>(SysExpr.Add(argument, right), argument);
            var input = SysExpr.TryFinally(value, SysExpr.Block(SysExpr.Assign(value, SysExpr.Constant(7)), SysExpr.Empty()));
            var expression = SysExpr.Lambda<Func<int, int>>(SysExpr.Invoke(inner, input), value);
            var actual = expression.FromSysExpression().CompileFast(true, flags);
            if (!t.IsNotNull(actual)) return;
            t.AreEqual(rightKind == 0 ? 5 : rightKind == 1 ? 10 : 3, actual(3));
            t.AreEqual(expression.Compile()(3), actual(3));
        }
    }

#if LIGHT_EXPRESSION
    private sealed class TryConstant : ConstantExpression
    {
        public override Type Type => typeof(int);
        public override object Value => 5;
        public override bool IsIntrinsic => true;
        public override ExpressionCompiler.Result TryCollectInfo(ref ExpressionCompiler.CompilerContext context,
            ExpressionCompiler.NestedLambdaInfo nestedLambda, ref ImTools.SmallList<ExpressionCompiler.NestedLambdaInfo> rootNestedLambdas, short scopeIdx)
        {
            return ExpressionCompiler.TryCollectInfo(ref context,
                Expression.TryFinally(Expression.Constant(5), Expression.Empty()), nestedLambda, ref rootNestedLambdas);
        }
        public override bool TryEmit(ref ExpressionCompiler.CompilerContext context, System.Reflection.Emit.ILGenerator il,
            ExpressionCompiler.ParentFlags parent, int byRefIndex = -1)
        {
            var value = il.DeclareLocal(typeof(int));
            il.BeginExceptionBlock();
            il.Emit(System.Reflection.Emit.OpCodes.Ldc_I4_5);
            il.Emit(System.Reflection.Emit.OpCodes.Stloc, value);
            il.BeginFinallyBlock();
            il.Emit(System.Reflection.Emit.OpCodes.Nop);
            il.EndExceptionBlock();
            il.Emit(System.Reflection.Emit.OpCodes.Ldloc, value);
            context.LastEmitIsAddress = false;
            return true;
        }
    }

    public void Intrinsic_constants_keep_their_exception_region_spills(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(Expression.Add(Expression.Constant(3), new TryConstant()));
        var actual = expression.CompileFast(true);
        if (!t.IsNotNull(actual)) return;
        t.AreEqual(8, actual());
    }
#endif
}
