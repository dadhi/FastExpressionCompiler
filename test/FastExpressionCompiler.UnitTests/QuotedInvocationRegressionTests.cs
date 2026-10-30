using System;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct QuotedInvocationRegressionTests : ITestX
{
 public void Run(TestRun t){Direct_quote_invocation_uses_existing_lambda_paths(t);}
 public void Direct_quote_invocation_uses_existing_lambda_paths(TestContext t)
 {
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining})
  {
   var x=S.Parameter(typeof(int));
   var quote=S.Quote(S.Lambda<Func<int,int>>(S.Add(x,S.Constant(7)),x));
   var invocation=S.Invoke(quote,S.TryFinally(S.Constant(5),S.Empty()));
   var f=S.Lambda<Func<int>>(invocation).FromSysExpression().CompileFast(true,flags);
   if(t.IsNotNull(f))t.AreEqual(12,f());
   var ignored=S.Lambda<Action>(S.Block(invocation,S.Empty())).FromSysExpression().CompileFast(true,flags);
   if(t.IsNotNull(ignored))ignored();
   var variable=S.Variable(typeof(int));
   var mutate=S.Invoke(S.Quote(S.Lambda<Func<int>>(S.PreIncrementAssign(variable))));
   var callback=S.Lambda<Func<int>>(S.Block(new[]{variable},S.Assign(variable,S.Constant(7)),mutate,variable)).FromSysExpression().CompileFast(true,flags);
   if(t.IsNotNull(callback))t.AreEqual(8,callback());
  }
 }
}
