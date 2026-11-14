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
 public void Run(TestRun t) { Opaque_quote_extensions_are_not_reduced(t); Distinct_quote_nodes_share_their_operand(t); Outer_and_nested_lambdas_share_quotes_in_both_orders(t); Sibling_nested_lambdas_share_first_conversion(t); Quoted_subtrees_share_identity_in_both_orders(t); Structurally_equal_operands_remain_distinct(t); }
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



 private sealed class OpaqueQuoteBody : System.Linq.Expressions.Expression {
  internal int Reductions;
  public override System.Linq.Expressions.ExpressionType NodeType => System.Linq.Expressions.ExpressionType.Extension;
  public override Type Type => typeof(int);
  public override bool CanReduce => true;
  public override System.Linq.Expressions.Expression Reduce() { ++Reductions; return System.Linq.Expressions.Expression.Constant(42); }
 }
 public void Opaque_quote_extensions_are_not_reduced(TestContext t) {
  var body = new OpaqueQuoteBody();
  var expression = System.Linq.Expressions.Expression.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(
   System.Linq.Expressions.Expression.Quote(System.Linq.Expressions.Expression.Lambda<Func<int>>(body)));
#if LIGHT_EXPRESSION
  // Conversion of opaque System nodes is unsupported and must not reduce the data.
  t.Throws<NotSupportedException>(() => expression.FromSysExpression());
#else
  foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo })
   t.IsTrue(expression.CompileFast(true, flags) == null);
#endif
  t.AreEqual(0, body.Reductions);
 }
}
