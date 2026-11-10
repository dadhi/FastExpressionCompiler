using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ArrayIndexExceptionRegressionTests : ITestX
{
 public void Run(TestRun t) => Binary_array_indices_with_exception_regions_keep_order(t);
 public void Binary_array_indices_with_exception_regions_keep_order(TestContext t) {
  foreach(var lower in new[]{-2,0,1}) {
   var array=Array.CreateInstance(typeof(int),new[]{3},new[]{lower});array.SetValue(42,lower);var trace=new List<int>();

   Func<Array> getArray=()=>{trace.Add(0);return array;};Func<int> getIndex=()=>{trace.Add(1);return lower;};Action cleanup=()=>trace.Add(2);
   var access=S.ArrayIndex(S.Convert(S.Invoke(S.Constant(getArray)),array.GetType()),S.TryFinally(S.Invoke(S.Constant(getIndex)),S.Invoke(S.Constant(cleanup))));
   var f=S.Lambda<Func<int>>(access).FromSysExpression().CompileFast(true);if(!t.IsNotNull(f))return;
   t.AreEqual(42,f());t.AreEqual("0,1,2",string.Join(",",trace));
  }
 }
}
