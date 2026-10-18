using System;
#if LIGHT_EXPRESSION
using E=FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E=System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct QuoteIdentityRegressionTests : ITestX
{
 public void Run(TestRun t) { Distinct_quote_nodes_share_their_operand(t); Outer_and_nested_lambdas_share_quotes_in_both_orders(t); Sibling_nested_lambdas_share_first_conversion(t); Quoted_subtrees_share_identity_in_both_orders(t); Structurally_equal_operands_remain_distinct(t); External_captures_remain_rejected(t); }
 public void Distinct_quote_nodes_share_their_operand(TestContext t) {
  var operand=E.Lambda<Func<int>>(E.Constant(7)); var tree=E.Lambda<Func<bool>>(E.ReferenceEqual(E.Quote(operand),E.Quote(operand)));
  var fast=tree.CompileFast(true); if(t.IsNotNull(fast)) t.IsTrue(fast());
 }
 public void Outer_and_nested_lambdas_share_quotes_in_both_orders(TestContext t) {
  foreach(var nestedFirst in new[]{false,true}) {
   var operand=E.Lambda<Func<int>>(E.Constant(7)); var quote=E.Quote(operand);
   var nested=E.Invoke(E.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(E.Quote(operand)));
   var tree=E.Lambda<Func<bool>>(nestedFirst?E.ReferenceEqual(nested,quote):E.ReferenceEqual(quote,nested));
   var fast=tree.CompileFast(true,CompilerFlags.NoInvocationLambdaInlining); if(t.IsNotNull(fast)) t.IsTrue(fast());
  }
 }
 public void Sibling_nested_lambdas_share_first_conversion(TestContext t) {
  var operand=E.Lambda<Func<int>>(E.Constant(7));
  var first=E.Invoke(E.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(E.Quote(operand)));
  var second=E.Invoke(E.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(E.Quote(operand)));
  var tree=E.Lambda<Func<bool>>(E.ReferenceEqual(first,second));
  var fast=tree.CompileFast(true,CompilerFlags.NoInvocationLambdaInlining); if(t.IsNotNull(fast)) t.IsTrue(fast());
 }
 public void Quoted_subtrees_share_identity_in_both_orders(TestContext t) {
  foreach(var outerFirst in new[]{false,true}) {
   var inner=E.Lambda<Func<int>>(E.Constant(7));
   var outer=E.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(E.Quote(inner));
   var a=E.Convert(E.Quote(inner),typeof(object)); var b=E.Convert(E.Quote(outer),typeof(object));
   var tree=E.Lambda<Func<object[]>>(E.NewArrayInit(typeof(object),outerFirst?b:a,outerFirst?a:b));
   var fast=tree.CompileFast(true); if(!t.IsNotNull(fast)) return;
   var result=fast(); var convertedInner=(System.Linq.Expressions.Expression<Func<int>>)result[outerFirst?1:0];
   var convertedOuter=(System.Linq.Expressions.Expression<Func<System.Linq.Expressions.Expression<Func<int>>>>)result[outerFirst?0:1];
   t.IsTrue(ReferenceEquals(convertedInner,((System.Linq.Expressions.UnaryExpression)convertedOuter.Body).Operand));
  }
 }
 public void Structurally_equal_operands_remain_distinct(TestContext t) {
  var first=E.Lambda<Func<int>>(E.Constant(7)); var second=E.Lambda<Func<int>>(E.Constant(7));
  var fast=E.Lambda<Func<bool>>(E.ReferenceEqual(E.Quote(first),E.Quote(second))).CompileFast(true);
  if(t.IsNotNull(fast)) t.IsTrue(!fast());
 }
 public void External_captures_remain_rejected(TestContext t) {
  var external=E.Parameter(typeof(int)); var quoted=E.Quote(E.Lambda<Func<int>>(external));
  var tree=E.Lambda<Func<int,System.Linq.Expressions.Expression<Func<int>>>>(quoted,external);
  t.IsTrue(tree.CompileFast(true)==null);
 }
}
