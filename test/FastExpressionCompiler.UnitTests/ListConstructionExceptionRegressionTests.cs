using System;
using System.Collections;
using System.Collections.Generic;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ListConstructionExceptionRegressionTests : ITestX
{
 public void Run(TestRun t) { Reference_collection_arguments_keep_order(t); Value_collection_arguments_keep_order(t); Throwing_argument_prevents_Add_and_later_elements(t); Constructor_arguments_support_exception_regions(t); }
 public class Items : IEnumerable { public int Total; public Items() {} public Items(int a,int b){Total=a+b;} public int Add(int a,int b){Total+=a+b;return Total;} public IEnumerator GetEnumerator()=>throw new NotImplementedException(); }
 public struct ValueItems : IEnumerable { public int Total; public int Add(int a,int b){Total+=a+b;return Total;} public IEnumerator GetEnumerator()=>throw new NotImplementedException(); }
 public void Reference_collection_arguments_keep_order(TestContext t) { Check(t,typeof(Items)); }
 public void Value_collection_arguments_keep_order(TestContext t) { Check(t,typeof(ValueItems)); }
 static void Check(TestContext t,Type type) {
  foreach(var ignored in new[]{false,true}) {
   var log=new List<int>(); Func<int,int> mark=x=>{log.Add(x);return x;};
   Func<int,E> arg=x=>E.Invoke(E.Constant(mark),E.Constant(x));
   var init=E.ListInit(E.New(type),E.ElementInit(type.GetMethod("Add"),arg(1),E.TryFinally(arg(2),E.Block(arg(3),E.Empty()))),E.ElementInit(type.GetMethod("Add"),E.TryFinally(arg(4),E.Empty()),arg(5)));
   E body=ignored ? E.Block(init,E.Constant(12)) : E.Field(init,"Total");
   var f=E.Lambda<Func<int>>(body).CompileFast(true);
   if(!t.IsNotNull(f))return;t.AreEqual(12,f());t.AreEqual("1,2,3,4,5",string.Join(",",log));
  }
 }
 public void Throwing_argument_prevents_Add_and_later_elements(TestContext t) {
  var log=new List<int>(); Func<int,int> mark=x=>{log.Add(x);if(x==2)throw new InvalidOperationException();return x;};
  Func<int,E> arg=x=>E.Invoke(E.Constant(mark),E.Constant(x));
  var init=E.ListInit(E.New(typeof(Items)),E.ElementInit(typeof(Items).GetMethod("Add"),arg(1),E.TryFinally(arg(2),E.Block(arg(3),E.Empty()))),E.ElementInit(typeof(Items).GetMethod("Add"),arg(4),arg(5)));
  var f=E.Lambda<Func<Items>>(init).CompileFast(true);if(!t.IsNotNull(f))return;t.Throws<InvalidOperationException>(()=>f());t.AreEqual("1,2,3",string.Join(",",log));
 }
 public void Constructor_arguments_support_exception_regions(TestContext t) {
  foreach(var ignored in new[]{false,true}) {
  var log=new List<int>(); Func<int,int> mark=x=>{log.Add(x);return x;};
  Func<int,E> arg=x=>E.Invoke(E.Constant(mark),E.Constant(x));
  var init=E.ListInit(E.New(typeof(Items).GetConstructor(new[]{typeof(int),typeof(int)}),arg(7),E.TryFinally(arg(9),E.Empty())),E.ElementInit(typeof(Items).GetMethod("Add"),arg(2),E.TryFinally(arg(3),E.Empty())));
  E body=ignored?E.Block(init,E.Constant(21)):E.Field(init,"Total");
  var f=E.Lambda<Func<int>>(body).CompileFast(true);if(t.IsNotNull(f))t.AreEqual(21,f());t.AreEqual("7,9,2,3",string.Join(",",log));
  }
 }

}
