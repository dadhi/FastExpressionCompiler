using System;
using System.Runtime.CompilerServices;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E=FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E=System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct CaptureFreeQuoteRegressionTests : ITestX
{
 public void Run(TestRun t)
 {
  Quote_preserves_tree_and_delegate_type(t);
  Quote_preserves_locally_bound_variables(t);
  Quote_rejects_external_captures(t);
  RuntimeVariables_empty_and_nonempty_rejection(t);
 }
 public void Quote_preserves_tree_and_delegate_type(TestContext t)
 {
  var x=E.Parameter(typeof(int),"x");
  var quote=E.Quote(E.Lambda<Func<int,int>>(x,x));
  t.AreEqual(typeof(System.Linq.Expressions.Expression<Func<int,int>>),quote.Type);
  var compiled=E.Lambda<Func<System.Linq.Expressions.Expression<Func<int,int>>>>(quote).CompileFast(true);
  if(!t.IsNotNull(compiled))return;
  var tree=compiled();t.IsTrue(ReferenceEquals(tree.Body,tree.Parameters[0]));
  var inner=tree.FromSysExpression().CompileFast(true);if(t.IsNotNull(inner))t.AreEqual(42,inner(42));
  var ignored=E.Lambda<Action>(E.Block(quote,E.Empty())).CompileFast(true);if(t.IsNotNull(ignored))ignored();
 }
 public void Quote_preserves_locally_bound_variables(TestContext t)
 {
  var x=E.Variable(typeof(int));
  var lambda=E.Lambda<Func<int>>(E.Block(new[]{x},E.Assign(x,E.Constant(7)),x));
  var compiled=E.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(E.Quote(lambda)).CompileFast(true);
  if(!t.IsNotNull(compiled))return;
  var inner=compiled().FromSysExpression().CompileFast(true);if(t.IsNotNull(inner))t.AreEqual(7,inner());
  var exception=E.Parameter(typeof(Exception));
  var caught=E.Lambda<Func<Exception>>(E.TryCatch(E.Throw(E.New(typeof(Exception)),typeof(Exception)),E.Catch(exception,exception)));
  var catchQuote=E.Lambda<Func<System.Linq.Expressions.Expression<Func<Exception>>>>(E.Quote(caught)).CompileFast(true);
  if(t.IsNotNull(catchQuote))t.AreEqual(System.Linq.Expressions.ExpressionType.Try,catchQuote().Body.NodeType);
 }
 public void Quote_rejects_external_captures(TestContext t)
 {
  var x=E.Parameter(typeof(string));
  var expression=E.Lambda<Func<string,System.Linq.Expressions.Expression<Func<string>>>>(E.Quote(E.Lambda<Func<string>>(x)),x);
  t.IsTrue(expression.CompileFast(true)==null);
  var error=t.Throws<NotSupportedExpressionException>(()=>expression.CompileFast(true,CompilerFlags.ThrowOnNotSupportedExpression));
  if(t.IsNotNull(error))t.AreEqual(ExpressionCompiler.Result.NotSupported_Quote,error.Reason);
 }
 public void RuntimeVariables_empty_and_nonempty_rejection(TestContext t)
 {
  var empty=S.Lambda<Func<IRuntimeVariables>>(S.RuntimeVariables()).FromSysExpression().CompileFast(true);
  if(!t.IsNotNull(empty))return;
  var vars=empty();t.AreEqual(0,vars.Count);
  t.Throws<IndexOutOfRangeException>(()=>{var unused=vars[0];});
  t.Throws<IndexOutOfRangeException>(()=>vars[0]=null);
  var x=S.Variable(typeof(int));
  var nonempty=S.Lambda<Func<IRuntimeVariables>>(S.Block(new[]{x},S.RuntimeVariables(x))).FromSysExpression();
  t.IsTrue(nonempty.CompileFast(true)==null);
  var error=t.Throws<NotSupportedExpressionException>(()=>nonempty.CompileFast(true,CompilerFlags.ThrowOnNotSupportedExpression));
  if(t.IsNotNull(error))t.AreEqual(ExpressionCompiler.Result.NotSupported_RuntimeVariables,error.Reason);
 }
}
