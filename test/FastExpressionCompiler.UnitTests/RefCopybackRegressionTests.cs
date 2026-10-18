using System;
using System.Collections.Generic;
using E = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RefCopybackRegressionTests : ITestX
{
 public void Run(TestRun t) { Property_and_indexer_copyback(t); Nested_struct_owner_copyback(t); True_addresses_keep_aliases(t); Copyback_orders_setters_and_stops_after_throw(t); Throwing_call_does_not_copy_back(t); Throwing_argument_does_not_copy_back(t); Byref_lambda_invocation_preserves_alias_and_exception_stack(t); Delegate_and_constructor_copy_back_properties(t); Address_aliases_survive_prepared_call(t); Struct_parameter_property_and_indexer_write_back(t); Temporary_and_readonly_struct_owners_stay_temporary(t); }
 public static List<string> Trace = new List<string>();
 public struct Inner { public int Field; public int Value { get { Trace.Add("inner.get"); return Field; } set { Trace.Add("inner.set"); Field=value; } } public int this[int i] { get { return Field+i; } set { Field=value-i; } } }
 public delegate int RefInner(ref Inner value);
 public struct Outer { public Inner Field; public Inner Value { get { Trace.Add("outer.get"); return Field; } set { Trace.Add("outer.set"); Field=value; } } }
 public class Owner {
  public Outer Data; public readonly Inner Readonly = new Inner{Field=7}; public int Field=3; public bool ThrowSetter;
  public int Value { get { Trace.Add("get"); return Field; } set { Trace.Add("set:"+value); if(ThrowSetter) throw new InvalidOperationException(); Field=value; } }
  public int this[int i] { get { Trace.Add("get:"+i); return Field+i; } set { Trace.Add("set:"+i+":"+value); Field=value-i; } }
  public Outer Nested { get { Trace.Add("nested.get"); return Data; } set { Trace.Add("nested.set"); Data=value; } }
 }
 public static Owner GetOwner(Owner value) { Trace.Add("owner"); return value; }
 public static int Index() { Trace.Add("index"); return 2; }
 public static int Add(ref int value,int amount) { Trace.Add("call"); value+=amount; return value; }
 public static int Alias(ref int a,ref int b) { a+=10; b+=20; return a+b; }
 public static int ThrowCall(ref int value) { value=90; Trace.Add("call.throw"); throw new InvalidOperationException(); }
 public static int ThrowArg() { Trace.Add("arg.throw"); throw new InvalidOperationException(); }
 public static int AliasThree(ref int a,ref int b,ref int c) { a+=10; b+=20; c+=30; return a+b+c; }
 public static int AssignTwo(ref int a,ref int b) { a=20; b=30; return 40; }
 private static System.Reflection.MethodInfo M(string name) => typeof(RefCopybackRegressionTests).GetMethod(name);
 private static System.Linq.Expressions.Expression Receiver(System.Linq.Expressions.ParameterExpression owner) => E.Call(M(nameof(GetOwner)),owner);
 private static void Compare(TestContext t,System.Linq.Expressions.Expression<Func<Owner,int>> tree,Func<Owner> create)
 {
  var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return; var system=tree.Compile();
  var a=create(); Trace.Clear(); int expected=0; Type exception=null; try { expected=system(a); } catch(Exception e) { exception=e.GetType(); } var expectedTrace=string.Join(",",Trace);
  var b=create(); Trace.Clear(); int actual=0; Type actualException=null; try { actual=fast(b); } catch(Exception e) { actualException=e.GetType(); }
  t.AreEqual(expected,actual); t.AreEqual(exception,actualException); t.AreEqual(a.Field,b.Field); t.AreEqual(a.Data.Field.Field,b.Data.Field.Field); t.AreEqual(expectedTrace,string.Join(",",Trace));
 }
 public void Property_and_indexer_copyback(TestContext t) {
  foreach(var index in new[]{false,true}) { var p=E.Parameter(typeof(Owner)); var receiver=Receiver(p); var arg=index?(System.Linq.Expressions.Expression)E.MakeIndex(receiver,typeof(Owner).GetProperty("Item"),new[]{E.Call(M(nameof(Index)))}):E.Property(receiver,"Value");
  var call=E.Call(M(nameof(Add)),arg,E.Constant(4)); Compare(t,E.Lambda<Func<Owner,int>>(call,p),()=>new Owner()); }
 }
 public void Nested_struct_owner_copyback(TestContext t) {
  foreach(var field in new[]{false,true}) { var p=E.Parameter(typeof(Owner)); var nested=E.Property(Receiver(p),"Nested"); var inner=E.Property(nested,"Value"); var arg=field?(System.Linq.Expressions.Expression)E.Field(inner,"Field"):E.Property(inner,"Value");
  Compare(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(Add)),arg,E.Constant(5)),p),()=>new Owner()); }
 }
 public void True_addresses_keep_aliases(TestContext t) {
  var p=E.Parameter(typeof(Owner)); var field=E.Field(p,"Field");
  // The property forces prepared-call emission while the two field arguments must retain one address.
  var call=E.Call(M(nameof(Alias)),field,field); var first=E.Call(M(nameof(Add)),E.Property(p,"Value"),E.Constant(0));
  Compare(t,E.Lambda<Func<Owner,int>>(E.Block(first,call),p),()=>new Owner());
  Compare(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(Alias)),E.Property(p,"Value"),E.Property(p,"Value")),p),()=>new Owner());
 }
 public void Copyback_orders_setters_and_stops_after_throw(TestContext t) {
  var p=E.Parameter(typeof(Owner)); var call=E.Call(M(nameof(AssignTwo)),E.Property(Receiver(p),"Value"),E.Property(Receiver(p),"Value"));
  Compare(t,E.Lambda<Func<Owner,int>>(call,p),()=>new Owner()); Compare(t,E.Lambda<Func<Owner,int>>(call,p),()=>new Owner{ThrowSetter=true});
 }
 public void Throwing_call_does_not_copy_back(TestContext t) { var p=E.Parameter(typeof(Owner)); Compare(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(ThrowCall)),E.Property(Receiver(p),"Value")),p),()=>new Owner()); }
 public void Throwing_argument_does_not_copy_back(TestContext t) { var p=E.Parameter(typeof(Owner)); Compare(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(Add)),E.Property(Receiver(p),"Value"),E.Call(M(nameof(ThrowArg)))),p),()=>new Owner()); }
 public delegate int ByRefs(ref int a,ref int b,int value);
 public void Byref_lambda_invocation_preserves_alias_and_exception_stack(TestContext t) {
  var a=E.Parameter(typeof(int).MakeByRefType()); var b=E.Parameter(typeof(int).MakeByRefType()); var v=E.Parameter(typeof(int));
  var lambda=E.Lambda<ByRefs>(E.Block(E.Assign(a,v),E.AddAssign(b,E.Constant(2)),E.Add(a,b)),a,b,v); var p=E.Parameter(typeof(Owner)); var f=E.Field(p,"Field");
  var tree=E.Lambda<Func<Owner,int>>(E.Invoke(lambda,f,f,E.TryFinally(E.Constant(40),E.Empty())),p);
#if NETFRAMEWORK
  // Framework's System compiler rejects EH argument spilling with managed references.
  var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return;
  var owner=new Owner(); t.AreEqual(84,fast(owner)); t.AreEqual(42,owner.Field);
#else
  Compare(t,tree,()=>new Owner());
#endif
 }
 public delegate int RefAmount(ref int value,int amount);
 public class Constructed { public int Value; public Constructed(ref int value,int amount) { value+=amount; Value=value; } }
 public void Delegate_and_constructor_copy_back_properties(TestContext t) {
  var p=E.Parameter(typeof(Owner)); var property=E.Property(Receiver(p),"Value");
  var del=E.Constant((RefAmount)Add);
  Compare(t,E.Lambda<Func<Owner,int>>(E.Invoke(del,property,E.Constant(4)),p),()=>new Owner());
  var ctor=typeof(Constructed).GetConstructor(new[]{typeof(int).MakeByRefType(),typeof(int)});
  Compare(t,E.Lambda<Func<Owner,int>>(E.Field(E.New(ctor,property,E.Constant(4)),"Value"),p),()=>new Owner());
 }
 public void Address_aliases_survive_prepared_call(TestContext t) {
  var p=E.Parameter(typeof(Owner)); var field=E.Field(p,"Field");
  Compare(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(AliasThree)),field,field,E.Property(p,"Value")),p),()=>new Owner());
 }
 public void Struct_parameter_property_and_indexer_write_back(TestContext t) {
  foreach(var byref in new[]{false,true}) foreach(var indexed in new[]{false,true}) {
   var p=E.Parameter(byref?typeof(Inner).MakeByRefType():typeof(Inner));
   var property=indexed?(System.Linq.Expressions.Expression)E.MakeIndex(p,typeof(Inner).GetProperty("Item"),new[]{E.Constant(2)}):E.Property(p,"Value");
   var body=E.Block(E.Call(M(nameof(Add)),property,E.Constant(1)),E.Field(p,"Field"));
   if(byref) { var tree=E.Lambda<RefInner>(body,p); var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return; var value=new Inner{Field=41}; t.AreEqual(42,fast(ref value)); t.AreEqual(42,value.Field); }
   else { var tree=E.Lambda<Func<Inner,int>>(body,p); var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return; t.AreEqual(42,fast(new Inner{Field=41})); }
  }
 }
 public void Temporary_and_readonly_struct_owners_stay_temporary(TestContext t) {
  var p=E.Parameter(typeof(Owner));
  foreach(var receiver in new System.Linq.Expressions.Expression[]{E.New(typeof(Inner)),E.Field(p,"Readonly"),E.Default(typeof(Inner)),E.Block(E.New(typeof(Inner)))}) {
   Compare(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(Add)),E.Property(receiver,"Value"),E.Constant(3)),p),()=>new Owner());
  }
 }
}
