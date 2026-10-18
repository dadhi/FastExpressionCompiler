using System;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct FormalByRefArgumentRegressionTests : ITestX
{
 public void Run(TestRun t) { Method_arguments_use_formal_reference_temporaries(t); Constructor_arguments_use_formal_reference_temporaries(t); Member_init_constructor_arguments_use_formal_reference_temporaries(t); Delegate_arguments_use_formal_reference_temporaries(t); Incoming_byref_string_is_not_reinterpreted(t); Exact_covariant_array_addresses_keep_runtime_checks(t); Exact_mutable_array_and_unbox_arguments_keep_aliases(t); Exact_typed_rvalues_get_value_temporaries(t); }
 public class Owner { public string Field="original"; public object Box="original"; public int Gets,Sets,ObjectSets,Receivers,Indexes; public string Property { get {++Gets;return Field;} set {++Sets;Field=value;} } public string this[int i] { get {++Gets;return Field;} set {++Sets;Field=value;} } public object Object {get=>Box;set {++ObjectSets;Box=value;}} }
 public static Owner Receiver(Owner owner) {++owner.Receivers;return owner;}
 public static int Index(Owner owner) {++owner.Indexes;return 0;}
 public static object Observed;
 public static int Change(ref object value) {Observed=value;if(value is string text && text!="original") throw new InvalidOperationException();value=new object();return 17;}
 public static int ChangeTwo(ref object first,ref object value) {Observed=value;if(value is string text && text!="original") throw new InvalidOperationException();first=new object();value=new object();return 17;}
 public delegate int One(ref object value); public delegate int Two(ref object first,ref object value); public delegate int RefString(Owner owner,ref string value);
 public class Created {public int Value; public int Other;public Created(ref object value) {Value=Change(ref value);} public Created(ref object first,ref object value) {Value=ChangeTwo(ref first,ref value);} }
 public struct Mutable {public int Value;}
 public static int Increment(ref Mutable value)=>++value.Value;
 private static System.Reflection.MethodInfo M(string name)=>typeof(FormalByRefArgumentRegressionTests).GetMethod(name);
 private static System.Linq.Expressions.Expression Build(string kind,System.Linq.Expressions.ParameterExpression owner,System.Linq.Expressions.ParameterExpression text,string[] vector,string[,] matrix) {
  var receiver=S.Call(M(nameof(Receiver)),owner);
  switch(kind) {
   case "property": return S.Property(receiver,"Property");
   case "indexer": return S.MakeIndex(receiver,typeof(Owner).GetProperty("Item"),new[]{S.Call(M(nameof(Index)),owner)});
   case "field": return S.Field(receiver,"Field");
   case "vector": return S.ArrayIndex(S.Constant(vector),S.Constant(0));
   case "matrix": return S.ArrayIndex(S.Constant(matrix),S.Constant(0),S.Constant(0));
   case "parameter": return text;
   default: return S.Constant("original");
  }
 }
 private static System.Linq.Expressions.Expression Call(int form,bool prepared,System.Linq.Expressions.ParameterExpression owner,System.Linq.Expressions.Expression argument) {
  var args=prepared?new[]{S.Property(owner,"Object"),(System.Linq.Expressions.Expression)argument}:new[]{argument};
  if(form==0)return S.Call(M(prepared?nameof(ChangeTwo):nameof(Change)),args);
  if(form==1 || form==3) {var signature=prepared?new[]{typeof(object).MakeByRefType(),typeof(object).MakeByRefType()}:new[]{typeof(object).MakeByRefType()};var created=S.New(typeof(Created).GetConstructor(signature),args); return S.Field(form==3?S.MemberInit(created,S.Bind(typeof(Created).GetField("Other"),S.Constant(1))):(System.Linq.Expressions.Expression)created,"Value");}
  return S.Invoke(S.Constant(prepared?(Delegate)(Two)ChangeTwo:(One)Change),args);
 }
 private static void Check(TestContext t,int form) {
  foreach(var prepared in new[]{false,true}) foreach(var kind in new[]{"property","indexer","field","vector","matrix","parameter","constant"}) {
   var owner=S.Parameter(typeof(Owner)); var text=S.Parameter(typeof(string));var b=new Owner();var vb=new[]{"original"};var mb=new[,]{{"original"}};
#if !NETFRAMEWORK
   var a=new Owner();var va=new[]{"original"};var ma=new[,]{{"original"}};
   var system=S.Lambda<Func<Owner,string,int>>(Call(form,prepared,owner,Build(kind,owner,text,va,ma)),owner,text).Compile();
   t.AreEqual(17,system(a,"original"));
#endif
   // Framework's System compiler emits unverifiable IL for covariant byref argument temporaries.
   // Every target still executes strict FEC and checks concrete storage and evaluation effects.
   var fast=S.Lambda<Func<Owner,string,int>>(Call(form,prepared,owner,Build(kind,owner,text,vb,mb)),owner,text).FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;
   t.AreEqual(17,fast(b,"original"));t.AreEqual("original",Observed);
   t.AreEqual("original",b.Field);t.AreEqual("original",vb[0]);t.AreEqual("original",mb[0,0]);t.AreEqual(0,b.Sets);
   t.AreEqual(kind=="property"||kind=="indexer"?1:0,b.Gets);t.AreEqual(kind=="property"||kind=="indexer"||kind=="field"?1:0,b.Receivers);t.AreEqual(kind=="indexer"?1:0,b.Indexes);
   t.AreEqual(prepared?1:0,b.ObjectSets);t.AreEqual(prepared?typeof(object):typeof(string),b.Box.GetType());
#if !NETFRAMEWORK
   t.AreEqual(a.Gets,b.Gets);t.AreEqual(a.Receivers,b.Receivers);t.AreEqual(a.Indexes,b.Indexes);t.AreEqual(a.ObjectSets,b.ObjectSets);
#endif
  }
 }
 public void Method_arguments_use_formal_reference_temporaries(TestContext t)=>Check(t,0);
 public void Constructor_arguments_use_formal_reference_temporaries(TestContext t)=>Check(t,1);
 public void Member_init_constructor_arguments_use_formal_reference_temporaries(TestContext t)=>Check(t,3);
 public void Delegate_arguments_use_formal_reference_temporaries(TestContext t)=>Check(t,2);
 public void Incoming_byref_string_is_not_reinterpreted(TestContext t) {
  foreach(var form in new[]{0,1,2}) foreach(var prepared in new[]{false,true}) {
   var owner=S.Parameter(typeof(Owner));var text=S.Parameter(typeof(string).MakeByRefType());var tree=S.Lambda<RefString>(Call(form,prepared,owner,text),owner,text);
#if !NETFRAMEWORK
   var system=tree.Compile();var a="original";t.AreEqual(17,system(new Owner(),ref a));t.AreEqual("original",a);
#endif
   var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;var b="original";t.AreEqual(17,fast(new Owner(),ref b));t.AreEqual("original",b);t.AreEqual("original",Observed);
  }
 }
 public void Exact_covariant_array_addresses_keep_runtime_checks(TestContext t) {
  foreach(var multidimensional in new[]{false,true}) foreach(var prepared in new[]{false,true}) {
   var owner=S.Parameter(typeof(Owner));object[] vector=new string[]{"original"};object[,] matrix=new string[,]{{"original"}};
   var arg=multidimensional?S.ArrayIndex(S.Constant(matrix,typeof(object[,])),S.Constant(0),S.Constant(0)):(System.Linq.Expressions.Expression)S.ArrayIndex(S.Constant(vector,typeof(object[])),S.Constant(0));
   var tree=S.Lambda<Func<Owner,int>>(Call(0,prepared,owner,arg),owner);
#if !NETFRAMEWORK
   var system=tree.Compile();t.Throws<ArrayTypeMismatchException>(()=>system(new Owner()));
#endif
   var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;var value=new Owner();t.Throws<ArrayTypeMismatchException>(()=>fast(value));t.AreEqual(0,value.ObjectSets);t.AreEqual("original",vector[0]);t.AreEqual("original",matrix[0,0]);
  }
 }
 public void Exact_mutable_array_and_unbox_arguments_keep_aliases(TestContext t) {
  var matrix=new Mutable[1,1];object box=new Mutable();
  foreach(var unbox in new[]{false,true}) {var arg=unbox?S.Unbox(S.Constant(box,typeof(object)),typeof(Mutable)):(System.Linq.Expressions.Expression)S.ArrayIndex(S.Constant(matrix),S.Constant(0),S.Constant(0));var tree=S.Lambda<Func<int>>(S.Call(M(nameof(Increment)),arg));var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;t.AreEqual(1,fast());t.AreEqual(1,unbox?((Mutable)box).Value:matrix[0,0].Value);}
 }
 public void Exact_typed_rvalues_get_value_temporaries(TestContext t) {
  foreach(var form in new[]{0,1,2}) {var owner=S.Parameter(typeof(Owner));foreach(var arg in new System.Linq.Expressions.Expression[]{S.Convert(S.Constant("original"),typeof(object)),S.New(typeof(object)),S.Block(S.Constant("original",typeof(object)))}) {var fast=S.Lambda<Func<Owner,int>>(Call(form,false,owner,arg),owner).FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.AreEqual(17,fast(new Owner()));}}
#if LIGHT_EXPRESSION
  var intrinsic=Expression.TryConvertIntrinsic<object>(Expression.Constant("original"));var direct=Expression.Lambda<Func<int>>(Expression.Call(M(nameof(Change)),intrinsic)).CompileFast(true);if(t.IsNotNull(direct))t.AreEqual(17,direct());
#endif
 }
}
