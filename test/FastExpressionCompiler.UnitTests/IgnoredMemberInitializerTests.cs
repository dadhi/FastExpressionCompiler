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
public struct IgnoredMemberInitializerTests : ITestX
{
 public static string Trace;
 public sealed class Model {public int Value;public int Number{get;set;}public Model(){Trace+="C";}}
 public struct ValueModel {public int Value;public int Number{get;set;}}
 public static int Mark(int n){Trace+=n;return n;}
 public void Run(TestRun t) { Discarded_initializers_keep_all_effects(t); }
 public void Discarded_initializers_keep_all_effects(TestContext t) {
  foreach(var type in new[]{typeof(Model),typeof(ValueModel)}) {
   var mark=typeof(IgnoredMemberInitializerTests).GetMethod(nameof(Mark));
   var init=E.MemberInit(E.New(type),E.Bind(type.GetField("Value"),E.Call(mark,E.Constant(1))),E.Bind(type.GetProperty("Number"),E.Call(mark,E.Constant(2))));
   var tree=E.Lambda<Func<int>>(E.Block(init,E.Constant(17)));var system=(Func<int>)tree.CompileSys();Trace="";var expected=system();var effects=Trace;
   foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo}) {var fast=tree.CompileFast(true,flags);if(!t.IsNotNull(fast))return;Trace="";t.AreEqual(expected,fast());t.AreEqual(effects,Trace);}
  }
 }

}
