using System;
using System.Collections.Generic;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct NonVectorArrayRegressionTests : ITestX
{
 public void Run(TestRun t) {
  Reads_use_actual_lower_bounds(t); Writes_and_compounds_use_actual_lower_bounds(t);
  Struct_elements_remain_addressable(t); Ref_arguments_preserve_array_element_storage(t);
  Ignored_reads_still_validate_array_and_bounds(t); Binary_array_indices_with_exception_regions_keep_order(t);
 }
 public struct Cell { public int Value; public int Increment(int n) => Value+=n; }
 public static int Increment(ref int n) => ++n;
 public static void Replace(ref string value) => value="changed";
 public static void ReplaceObject(ref object value) => value=new object();
 static Array Make(Type t, int lower) => Array.CreateInstance(t,new[]{3},new[]{lower});
 public void Reads_use_actual_lower_bounds(TestContext t) {
  Check(t,42); Check(t,"hello"); Check(t,(int?)7); Check(t,(string)null); Check(t,new Cell{Value=9});
 }
 static void Check<T>(TestContext t,T value) {
  foreach(var lower in new[]{-2,1}) {
   var array=Make(typeof(T),lower);array.SetValue(value,lower);
   foreach(var binary in new[]{false,true}) {
    var a=S.Parameter(typeof(Array));var i=S.Parameter(typeof(int));var converted=S.Convert(a,array.GetType());
    var access=binary?(System.Linq.Expressions.Expression)S.ArrayIndex(converted,i):S.ArrayAccess(converted,i);
    var e=S.Lambda<Func<Array,int,T>>(access,a,i);var f=e.FromSysExpression().CompileFast(true);
    if(!t.IsNotNull(f))return;t.AreEqual(value,f(array,lower));
#if !NETFRAMEWORK
    // Framework's System compiler emits unverifiable IL for these non-vector array reads.
    t.AreEqual(e.Compile()(array,lower),f(array,lower));
#endif
   }
  }
 }
 public void Writes_and_compounds_use_actual_lower_bounds(TestContext t) {
  foreach(var lower in new[]{-2,0,1}) {
   var array=Make(typeof(int),lower);var a=S.Parameter(typeof(Array));var i=S.Parameter(typeof(int));
   var access=S.ArrayAccess(S.Convert(a,array.GetType()),i);
   var e=S.Lambda<Func<Array,int,int>>(S.Block(S.Assign(access,S.Constant(5)),S.AddAssign(access,S.Constant(7)),S.PreIncrementAssign(access),S.PostDecrementAssign(access)),a,i);
   var f=e.FromSysExpression().CompileFast(true);if(!t.IsNotNull(f))return;
   t.AreEqual(13,f(array,lower));t.AreEqual(12,array.GetValue(lower));
  }
 }
 public void Struct_elements_remain_addressable(TestContext t) {
  foreach(var lower in new[]{-2,0,1}) {
   var array=Make(typeof(Cell),lower);var a=S.Parameter(typeof(Array));var i=S.Parameter(typeof(int));
   var access=S.ArrayAccess(S.Convert(a,array.GetType()),i);
   var e=S.Lambda<Func<Array,int,int>>(S.Block(S.Assign(S.Field(access,nameof(Cell.Value)),S.Constant(40)),S.Call(access,typeof(Cell).GetMethod(nameof(Cell.Increment)),S.Constant(2))),a,i);
   var f=e.FromSysExpression().CompileFast(true);if(!t.IsNotNull(f))return;
   t.AreEqual(42,f(array,lower));t.AreEqual(42,((Cell)array.GetValue(lower)).Value);
  }
 }
 public void Ref_arguments_preserve_array_element_storage(TestContext t) {
  foreach(var lower in new[]{-2,0,1}) {
   var ints=Make(typeof(int),lower);ints.SetValue(40,lower);var a=S.Parameter(typeof(Array));var i=S.Parameter(typeof(int));
   var access=S.ArrayAccess(S.Convert(a,ints.GetType()),i);
   var f=S.Lambda<Func<Array,int,int>>(S.Call(typeof(NonVectorArrayRegressionTests).GetMethod(nameof(Increment)),access),a,i).FromSysExpression().CompileFast(true);
   if(!t.IsNotNull(f))return;t.AreEqual(41,f(ints,lower));t.AreEqual(41,ints.GetValue(lower));
   var strings=Make(typeof(string),lower);strings.SetValue("old",lower);
   var textAccess=S.ArrayAccess(S.Convert(a,strings.GetType()),i);
   var set=S.Lambda<Action<Array,int>>(S.Call(typeof(NonVectorArrayRegressionTests).GetMethod(nameof(Replace)),textAccess),a,i).FromSysExpression().CompileFast(true);
   if(!t.IsNotNull(set))return;set(strings,lower);t.AreEqual("changed",strings.GetValue(lower));
   var objectArrayType=lower==0?typeof(object[]):typeof(object).MakeArrayType(1);
   var covariantAccess=S.ArrayAccess(S.Convert(a,objectArrayType),i);
   var covariant=S.Lambda<Action<Array,int>>(S.Call(typeof(NonVectorArrayRegressionTests).GetMethod(nameof(ReplaceObject)),covariantAccess),a,i).FromSysExpression().CompileFast(true);
   if(!t.IsNotNull(covariant))return;t.Throws<ArrayTypeMismatchException>(()=>covariant(strings,lower));
  }
 }
 public void Ignored_reads_still_validate_array_and_bounds(TestContext t) {
  foreach(var lower in new[]{-2,0,1}) foreach(var binary in new[]{false,true}) {
   var array=Make(typeof(int),lower);var a=S.Parameter(typeof(Array));var i=S.Parameter(typeof(int));var converted=S.Convert(a,array.GetType());
   var access=binary?(System.Linq.Expressions.Expression)S.ArrayIndex(converted,i):S.ArrayAccess(converted,i);
   var f=S.Lambda<Action<Array,int>>(S.Block(access,S.Empty()),a,i).FromSysExpression().CompileFast(true);
   if(!t.IsNotNull(f))return;f(array,lower);t.Throws<IndexOutOfRangeException>(()=>f(array,lower-1));t.Throws<NullReferenceException>(()=>f(null,lower));
  }
 }
 public void Binary_array_indices_with_exception_regions_keep_order(TestContext t) {
  foreach(var lower in new[]{-2,0,1}) {
   var array=Make(typeof(int),lower);array.SetValue(42,lower);var trace=new List<int>();
   Func<Array> getArray=()=>{trace.Add(0);return array;};Func<int> getIndex=()=>{trace.Add(1);return lower;};Action cleanup=()=>trace.Add(2);
   var access=S.ArrayIndex(S.Convert(S.Invoke(S.Constant(getArray)),array.GetType()),S.TryFinally(S.Invoke(S.Constant(getIndex)),S.Invoke(S.Constant(cleanup))));
   var f=S.Lambda<Func<int>>(access).FromSysExpression().CompileFast(true);if(!t.IsNotNull(f))return;
   t.AreEqual(42,f());t.AreEqual("0,1,2",string.Join(",",trace));
  }
 }
}
