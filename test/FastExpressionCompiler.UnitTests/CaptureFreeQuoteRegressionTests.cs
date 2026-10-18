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
  Quote_captures_share_outer_and_inner_writes(t);
  RuntimeVariables_preserve_empty_behavior_and_live_nonempty_aliases(t);
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
 public void Quote_captures_share_outer_and_inner_writes(TestContext t)
 {
  var x=E.Parameter(typeof(string));
  var saved=E.Variable(typeof(System.Linq.Expressions.Expression<Func<string>>));
  var concat=typeof(string).GetMethod(nameof(string.Concat),new[]{typeof(string),typeof(string)});
  var quoted=E.Quote(E.Lambda<Func<string>>(E.Assign(x,E.Call(concat,x,E.Constant("/inner")))));
  var expression=E.Lambda<Func<string,System.Linq.Expressions.Expression<Func<string>>>>(
   E.Block(new[]{saved},E.Assign(saved,quoted),E.Assign(x,E.Call(concat,x,E.Constant("/outer"))),saved),x);
#if LIGHT_EXPRESSION
  var system=(Func<string,System.Linq.Expressions.Expression<Func<string>>>)expression.ToLambdaExpression().Compile();
#else
  var system=expression.Compile();
#endif
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,
   CompilerFlags.EnableDelegateDebugInfo|CompilerFlags.ThrowOnNotSupportedExpression})
  {
   var fast=expression.CompileFast(true,flags);if(!t.IsNotNull(fast))return;
   foreach(var outer in new[]{system,fast})
   {
    var tree=outer("seed");var systemInner=tree.Compile();
    var fastInner=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fastInner))return;
    t.AreEqual("seed/outer/inner",systemInner());
    t.AreEqual("seed/outer/inner/inner",fastInner());
    var independent=outer("other").Compile();t.AreEqual("other/outer/inner",independent());
    t.AreEqual("seed/outer/inner/inner/inner",systemInner());
   }
  }
 }
 public void RuntimeVariables_preserve_empty_behavior_and_live_nonempty_aliases(TestContext t)
 {
  var empty=S.Lambda<Func<IRuntimeVariables>>(S.RuntimeVariables()).FromSysExpression().CompileFast(true);
  if(!t.IsNotNull(empty))return;
  var vars=empty();t.AreEqual(0,vars.Count);
  t.Throws<IndexOutOfRangeException>(()=>{var unused=vars[0];});
  t.Throws<IndexOutOfRangeException>(()=>vars[0]=null);
  var x=S.Variable(typeof(int));var view=S.Variable(typeof(IRuntimeVariables));
  var nonempty=S.Lambda<Func<IRuntimeVariables>>(S.Block(new[]{x,view},
   S.Assign(view,S.RuntimeVariables(x,x)),S.Assign(x,S.Constant(7)),view)).FromSysExpression();
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,
   CompilerFlags.EnableDelegateDebugInfo|CompilerFlags.ThrowOnNotSupportedExpression})
  {
   var fast=nonempty.CompileFast(true,flags);if(!t.IsNotNull(fast))return;
   var first=fast();var second=fast();t.AreEqual(2,first.Count);t.AreEqual(7,first[0]);
   first[0]=42;t.AreEqual(42,first[1]);t.AreEqual(7,second[0]);
   second[1]=99;t.AreEqual(99,second[0]);t.AreEqual(42,first[0]);
  }
 }

}
