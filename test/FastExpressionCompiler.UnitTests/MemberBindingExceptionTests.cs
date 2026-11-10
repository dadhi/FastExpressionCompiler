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
public struct MemberBindingExceptionTests : ITestX
{
 public static List<int> Trace=new List<int>();
 public sealed class Model {public int Value;public int Number{get;set;}}
 public struct ValueModel {public int Value;public int Number{get;set;}}
 public static int Mark(int n){Trace.Add(n);return n;}
 public void Run(TestRun t) { Binding_values_keep_the_receiver_across_finally(t); }
 public void Binding_values_keep_the_receiver_across_finally(TestContext t) {
  foreach(var type in new[]{typeof(Model),typeof(ValueModel)}) {
   var mark=typeof(MemberBindingExceptionTests).GetMethod(nameof(Mark));
   var init=E.MemberInit(E.New(type),E.Bind(type.GetField("Value"),E.TryFinally(E.Call(mark,E.Constant(1)),E.Block(E.Call(mark,E.Constant(2)),E.Empty()))),E.Bind(type.GetProperty("Number"),E.TryFinally(E.Call(mark,E.Constant(3)),E.Block(E.Call(mark,E.Constant(4)),E.Empty()))));
   var tree=E.Lambda<Func<object>>(E.Convert(init,typeof(object)));
#if NETFRAMEWORK
   // Framework's System compiler rejects EH inside struct member initializers.
   Func<object> system=type.IsValueType?FrameworkValue:(Func<object>)tree.CompileSys();
#else
   var system=(Func<object>)tree.CompileSys();
#endif
   Trace.Clear();var expected=system();var effects=string.Join(",",Trace);
   foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo}) {var fast=tree.CompileFast(true,flags);if(!t.IsNotNull(fast))return;Trace.Clear();var actual=fast();t.AreEqual(type.GetField("Value").GetValue(expected),type.GetField("Value").GetValue(actual));t.AreEqual(type.GetProperty("Number").GetValue(expected),type.GetProperty("Number").GetValue(actual));t.AreEqual(effects,string.Join(",",Trace));}
  }
 }
#if NETFRAMEWORK
 private static object FrameworkValue() {
  var result=new ValueModel();int value;
  try{value=Mark(1);}finally{Mark(2);}result.Value=value;
  try{value=Mark(3);}finally{Mark(4);}result.Number=value;
  return result;
 }
#endif

}
