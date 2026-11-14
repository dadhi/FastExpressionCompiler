using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct AtomicArithmeticSpillTests : ITestX
{
    public void Run(TestRun t)
    {
        Atomic_right_operands_follow_the_completed_finalizer(t);
#if LIGHT_EXPRESSION
        Opaque_intrinsic_operands_keep_exception_region_spills(t);
#endif
    }
    public void Atomic_right_operands_follow_the_completed_finalizer(TestContext t)
    {
        foreach(var kind in new[]{0,1,2})
        {
            var value=S.Parameter(typeof(int));
            var left=S.TryFinally(value,S.Block(S.Assign(value,S.Constant(7)),S.Empty()));
            var right=kind==0?(System.Linq.Expressions.Expression)value:kind==1?S.Constant(2):S.Default(typeof(int));
            var tree=S.Lambda<Func<int,int>>(S.Add(left,right),value);
            var fast=tree.FromSysExpression().CompileFast(true);
            if(t.IsNotNull(fast))t.AreEqual(tree.Compile()(3),fast(3));
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
                Expression.TryFinally(Expression.Constant(5), Expression.Empty()), nestedLambda, ref rootNestedLambdas, scopeIdx);
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

    public void Opaque_intrinsic_operands_keep_exception_region_spills(TestContext t)
    {
        var expression = Expression.Lambda<Func<int>>(Expression.Add(Expression.Constant(3), new TryConstant()));
        var actual = expression.CompileFast(true);
        if (!t.IsNotNull(actual)) return;
        t.AreEqual(8, actual());
    }
#endif
}
