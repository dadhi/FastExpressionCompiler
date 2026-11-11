using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct BackwardTypedLabelTests : ITestX
{
 public void Run(TestRun t) { Backward_jumps_share_the_typed_result_label(t); }
 public void Backward_jumps_share_the_typed_result_label(TestContext t) {
  foreach(var kind in new[]{System.Linq.Expressions.GotoExpressionKind.Return,System.Linq.Expressions.GotoExpressionKind.Goto,System.Linq.Expressions.GotoExpressionKind.Break}) {
   var count=S.Variable(typeof(int));var label=S.Label(typeof(int));
   var tree=S.Lambda<Func<int>>(S.Block(new[]{count},S.Label(label,S.Constant(0)),S.PostIncrementAssign(count),S.IfThen(S.LessThan(count,S.Constant(3)),S.MakeGoto(kind,label,count,typeof(void))),count));
   var expected=tree.Compile()();foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo}) {var fast=tree.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(fast))t.AreEqual(expected,fast());}
  }
 }

}
