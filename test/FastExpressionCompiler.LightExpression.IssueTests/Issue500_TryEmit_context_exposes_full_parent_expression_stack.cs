using System;
using System.Linq.Expressions;
using System.Reflection.Emit;
using FastExpressionCompiler.LightExpression.ImTools;

using static FastExpressionCompiler.LightExpression.Expression;
using SysExpr = System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.LightExpression.IssueTests
{
    public class Issue500_TryEmit_context_exposes_full_parent_expression_stack : ITestX
    {
        public void Run(TestRun t)
        {
            Compiler_context_contains_the_full_emit_expression_stack_for_intrinsic_expression();
        }

        public void Compiler_context_contains_the_full_emit_expression_stack_for_intrinsic_expression()
        {
            var marker = new InspectingIntrinsicExpression(41);
            var expr = Lambda<Func<int>>(Add(Constant(1), Convert(marker, typeof(int))));

            var compiled = expr.CompileFast(flags: CompilerFlags.ThrowOnNotSupportedExpression);

            Asserts.AreEqual(42, compiled());
            Asserts.AreEqual(1, marker.EmitCount);
        }

        private sealed class InspectingIntrinsicExpression : Expression
        {
            private readonly int _value;

            public int EmitCount;

            public InspectingIntrinsicExpression(int value) => _value = value;

            public override ExpressionType NodeType => ExpressionType.Extension;
            public override Type Type => typeof(int);
            public override bool IsIntrinsic => true;

            public override ExpressionCompiler.Result TryCollectInfo(ref ExpressionCompiler.CompilerContext context,
                ExpressionCompiler.NestedLambdaInfo nestedLambda,
                ref SmallList<ExpressionCompiler.NestedLambdaInfo> rootNestedLambdas) => 0;

            public override bool TryEmit(ref ExpressionCompiler.CompilerContext context, ILGenerator il, int byRefIndex = -1)
            {
                ++EmitCount;

                Asserts.AreSame(this, context.CurrentEmitExpression);
                Asserts.AreEqual(3, context.EmitExpressionCount);
                Asserts.AreEqual(ExpressionType.Convert, context.GetEmitExpression(1).NodeType);
                Asserts.AreEqual(ExpressionType.Add, context.GetEmitExpression(2).NodeType);
                Asserts.IsNull(context.GetEmitExpression(3));

                il.Emit(OpCodes.Ldc_I4, _value);
                return true;
            }

            protected internal override Expression Accept(ExpressionVisitor visitor) => this;

            internal override SysExpr CreateSysExpression(ref SmallList<LightAndSysExpr> exprsConverted) =>
                SysExpr.Constant(_value, Type);
        }
    }
}
