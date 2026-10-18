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
 public void Run(TestRun t) { Distinct_quote_nodes_share_their_operand(t); Outer_and_nested_lambdas_share_quotes_in_both_orders(t); Sibling_nested_lambdas_share_first_conversion(t); Quoted_subtrees_share_identity_in_both_orders(t); Structurally_equal_operands_remain_distinct(t); Repeated_captured_operands_share_live_storage_across_nested_lambdas(t); }
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
 public void Repeated_captured_operands_share_live_storage_across_nested_lambdas(TestContext t) {
  var external=E.Parameter(typeof(int));var amount=E.Parameter(typeof(int));
  var operand=E.Lambda<Func<int>>(external);var quoted=E.Quote(operand);
  var nested=E.Invoke(E.Lambda<Func<System.Linq.Expressions.Expression<Func<int>>>>(quoted));
  var increment=E.Quote(E.Lambda<Func<int,int>>(E.Assign(external,E.Add(external,amount)),amount));
  var saved=E.Variable(typeof(object[]));
  var tree=E.Lambda<Func<int,object[]>>(E.Block(new[]{saved},
   E.Assign(saved,E.NewArrayInit(typeof(object),E.Convert(quoted,typeof(object)),
    E.Convert(nested,typeof(object)),E.Convert(increment,typeof(object)),E.Convert(E.Lambda<Func<int>>(external),typeof(object)))),
   E.Assign(external,E.Add(external,E.Constant(1))),saved),external);
#if LIGHT_EXPRESSION
  var system=(Func<int,object[]>)tree.ToLambdaExpression().Compile();
#else
  var system=tree.Compile();
#endif
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,
   CompilerFlags.EnableDelegateDebugInfo|CompilerFlags.ThrowOnNotSupportedExpression}) {
   var fast=tree.CompileFast(true,flags);if(!t.IsNotNull(fast))return;
   foreach(var outer in new[]{system,fast}) {
    var result=outer(41);
    var first=((System.Linq.Expressions.Expression<Func<int>>)result[0]).Compile();
    var second=((System.Linq.Expressions.Expression<Func<int>>)result[1]).FromSysExpression().CompileFast(true,flags);
    var writeTree=(System.Linq.Expressions.Expression<Func<int,int>>)result[2];
    var writeFast=writeTree.FromSysExpression().CompileFast(true,flags);
    if(!t.IsNotNull(second)||!t.IsNotNull(writeFast))return;
    var sibling=(Func<int>)result[3];t.AreEqual(42,first());t.AreEqual(42,second());
    t.AreEqual(43,writeTree.Compile()(1));t.AreEqual(43,first());t.AreEqual(43,second());
    t.AreEqual(53,writeFast(10));t.AreEqual(53,first());t.AreEqual(53,second());t.AreEqual(53,sibling());
    var independent=outer(10);t.AreEqual(11,((System.Linq.Expressions.Expression<Func<int>>)independent[0]).Compile()());
    t.AreEqual(53,first());
   }
  }
 }

}
