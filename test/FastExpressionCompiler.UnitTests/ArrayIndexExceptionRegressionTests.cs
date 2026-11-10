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
 public void Run(TestRun t) { Binary_array_indices_with_exception_regions_keep_order(t); Complex_arithmetic_operands_keep_order(t); }
 public void Binary_array_indices_with_exception_regions_keep_order(TestContext t) {
  foreach(var lower in new[]{-2,0,1}) {
   var array=Array.CreateInstance(typeof(int),new[]{3},new[]{lower});array.SetValue(42,lower);var trace=new List<int>();

   Func<Array> getArray=()=>{trace.Add(0);return array;};Func<int> getIndex=()=>{trace.Add(1);return lower;};Action cleanup=()=>trace.Add(2);
   var access=S.ArrayIndex(S.Convert(S.Invoke(S.Constant(getArray)),array.GetType()),S.TryFinally(S.Invoke(S.Constant(getIndex)),S.Invoke(S.Constant(cleanup))));
   var f=S.Lambda<Func<int>>(access).FromSysExpression().CompileFast(true);if(!t.IsNotNull(f))return;
   t.AreEqual(42,f());t.AreEqual("0,1,2",string.Join(",",trace));
  }
 }
 public void Complex_arithmetic_operands_keep_order(TestContext t) {
  foreach (var type in new[] { typeof(int), typeof(long), typeof(decimal), typeof(int?), typeof(long?), typeof(decimal?) })
  foreach (var nullLeft in Nullable.GetUnderlyingType(type) == null ? new[] { false } : new[] { false, true }) {
   var trace = new List<int>();
   Func<int> left = () => { trace.Add(0); return 10; };
   Func<int> right = () => { trace.Add(1); return 3; };
   Action cleanup = () => trace.Add(2);
   var lhs = nullLeft ? S.Block(S.Invoke(S.Constant(left)), S.Default(type))
    : (System.Linq.Expressions.Expression)S.Convert(S.Invoke(S.Constant(left)), type);
   var rhs = S.TryFinally(S.Convert(S.Invoke(S.Constant(right)), type), S.Invoke(S.Constant(cleanup)));
   var tree = S.Lambda<Func<object>>(S.Convert(S.Subtract(lhs, rhs), typeof(object)));
   var expected = tree.Compile()(); var effects = string.Join(",", trace);
   foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining, CompilerFlags.EnableDelegateDebugInfo }) {
    var fast = tree.FromSysExpression().CompileFast(true, flags); if (!t.IsNotNull(fast)) return;
    trace.Clear(); t.AreEqual(expected, fast()); t.AreEqual(effects, string.Join(",", trace));
   }
  }
 }
}
