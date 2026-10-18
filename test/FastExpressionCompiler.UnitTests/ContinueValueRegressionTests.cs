using System;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
using E=FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E=System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ContinueValueRegressionTests : ITestX
{
 public void Run(TestRun t){Continue_value_survives_conversion(t);Continue_value_factory_preserves_payload(t);}
 public void Continue_value_survives_conversion(TestContext t)
 {
  var effects=0;Func<int> value=()=>{++effects;return 7;};var target=S.Label(typeof(int));
  var jump=S.MakeGoto(System.Linq.Expressions.GotoExpressionKind.Continue,target,S.Invoke(S.Constant(value)),typeof(void));
  var source=S.Lambda<Func<int>>(S.Block(jump,S.Label(target,S.Constant(99))));
  var fast=source.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast)){t.AreEqual(7,fast());t.AreEqual(1,effects);}
 }
 public void Continue_value_factory_preserves_payload(TestContext t)
 {
  var target=E.Label(typeof(int));var value=E.Constant(9);
  var jump=E.MakeGoto(System.Linq.Expressions.GotoExpressionKind.Continue,target,value,typeof(void));
  t.IsTrue(ReferenceEquals(value,jump.Value));t.AreEqual(typeof(void),jump.Type);
  var fast=E.Lambda<Func<int>>(E.Block(jump,E.Label(target,E.Constant(101)))).CompileFast(true);
  if(t.IsNotNull(fast))t.AreEqual(9,fast());
 }
}
