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
public struct MemberConstructorExceptionTests : ITestX
{
 public static List<int> Trace=new List<int>();
 public sealed class Model {public int Total;public int Value;public Model(int a,int b){Trace.Add(5);Total=a+b;}}
 public struct ValueModel {public int Total;public int Value;public ValueModel(int a,int b){Trace.Add(5);Total=a+b;Value=0;}}
 public static int Mark(int n){Trace.Add(n);return n;}
 public void Run(TestRun t) { Constructor_arguments_keep_order_before_bindings(t); }
 public void Constructor_arguments_keep_order_before_bindings(TestContext t) {
  foreach(var type in new[]{typeof(Model),typeof(ValueModel)})
  foreach(var ignored in new[]{false,true}) {
   var mark=typeof(MemberConstructorExceptionTests).GetMethod(nameof(Mark));
   var created=E.New(type.GetConstructor(new[]{typeof(int),typeof(int)}),E.Call(mark,E.Constant(1)),E.TryFinally(E.Call(mark,E.Constant(2)),E.Block(E.Call(mark,E.Constant(3)),E.Empty())));
   var initializer=E.MemberInit(created,E.Bind(type.GetField("Value"),E.Call(mark,E.Constant(4))));
   var tree=E.Lambda<Func<object>>(ignored
    ? E.Block(initializer,E.Constant(null,typeof(object)))
    : E.Convert(initializer,typeof(object)));
#if NETFRAMEWORK
   // Framework's System compiler rejects EH inside struct member initializers.
   Func<object> system=type.IsValueType
    ? () => { var value=FrameworkValue();return ignored?null:value; }
    : (Func<object>)tree.CompileSys();
#else
   var system=(Func<object>)tree.CompileSys();
#endif
   Trace.Clear();var expected=system();var effects=string.Join(",",Trace);
   foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo}) {var fast=tree.CompileFast(true,flags);if(!t.IsNotNull(fast))return;Trace.Clear();var actual=fast();if(ignored)t.AreEqual(expected,actual);else{t.AreEqual(type.GetField("Total").GetValue(expected),type.GetField("Total").GetValue(actual));t.AreEqual(type.GetField("Value").GetValue(expected),type.GetField("Value").GetValue(actual));}t.AreEqual(effects,string.Join(",",Trace));}
  }
 }
#if NETFRAMEWORK
 private static object FrameworkValue() {
  var first=Mark(1);int second;
  try{second=Mark(2);}finally{Mark(3);}
  var result=new ValueModel(first,second);result.Value=Mark(4);return result;
 }
#endif

}
