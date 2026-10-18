using System;
using E=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RefCopybackReceiverReviewTests : ITestX
{
 public void Run(TestRun t) { Struct_method_results_as_owners(t); Struct_constructor_results_as_owners(t); Reference_return_method_preserves_address(t); Boxed_struct_reference_property_keeps_declared_object_storage(t); }
 public struct Inner { public int Value; public int Mutate() => ++Value; }
 public struct Outer { public Inner Field; public Inner Property { get=>Field; set=>Field=value; } public Outer(ref int value) { ++value; Field=new Inner{Value=7}; } }
 public class Owner { public int Field=3; public int Value { get=>Field; set=>Field=value; } public object Box = new Inner{Value=7}; public object Object { get=>Box; set=>Box=value; } }
 public static Outer StructResult(ref int value) { ++value; return new Outer{Field=new Inner{Value=7}}; }
 public static int Static;
 public static ref int ReferenceResult(ref int value) { ++value; Static=8; return ref Static; }
 public delegate ref int RefLambda(Owner owner);
 public static int Add(ref int value,int amount) => value+=amount;
 public static int ReplaceBox(ref object value) { var before=(Inner)value; value=new Inner{Value=before.Value+1}; return ((Inner)value).Value; }
 private static System.Reflection.MethodInfo M(string name)=>typeof(RefCopybackReceiverReviewTests).GetMethod(name);
 private static void Check(TestContext t,System.Linq.Expressions.Expression<Func<Owner,int>> expression) {
  var fast=expression.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return;
  var system=expression.Compile(); var a=new Owner(); var expected=system(a); var b=new Owner(); t.AreEqual(expected,fast(b)); t.AreEqual(a.Field,b.Field); t.AreEqual(((Inner)a.Box).Value,((Inner)b.Box).Value);
 }
 private static void Owners(TestContext t,bool constructor) {
  var p=E.Parameter(typeof(Owner)); var arg=E.Property(p,"Value");
  System.Linq.Expressions.Expression result=constructor?E.New(typeof(Outer).GetConstructor(new[]{typeof(int).MakeByRefType()}),arg):E.Call(M(nameof(StructResult)),arg);
  Check(t,E.Lambda<Func<Owner,int>>(E.Field(E.Field(result,"Field"),"Value"),p));
  Check(t,E.Lambda<Func<Owner,int>>(E.Call(E.Field(result,"Field"),typeof(Inner).GetMethod(nameof(Inner.Mutate))),p));
  Check(t,E.Lambda<Func<Owner,int>>(E.Field(E.Property(result,"Property"),"Value"),p));
 }
 public void Struct_method_results_as_owners(TestContext t)=>Owners(t,false);
 public void Struct_constructor_results_as_owners(TestContext t)=>Owners(t,true);
 public void Reference_return_method_preserves_address(TestContext t) {
  var p=E.Parameter(typeof(Owner)); var result=E.Call(M(nameof(ReferenceResult)),E.Property(p,"Value"));
  var tree=E.Lambda<RefLambda>(result,p); var system=tree.Compile(); var a=new Owner(); ref var expected=ref system(a); expected+=3; t.AreEqual(11,Static); t.AreEqual(4,a.Field);
  var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return; var b=new Owner(); ref var actual=ref fast(b); actual+=3; t.AreEqual(11,Static); t.AreEqual(4,b.Field);
 }
 public void Boxed_struct_reference_property_keeps_declared_object_storage(TestContext t) {
  var p=E.Parameter(typeof(Owner)); Check(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(ReplaceBox)),E.Property(p,"Object")),p));
 }
}
