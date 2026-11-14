using System;
using E=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RefCopybackReceiverReviewTests : ITestX
{
 public void Run(TestRun t) { Prepared_results_can_supply_ref_argument_temporaries(t); Wider_formal_reference_with_property_copyback(t); Incoming_ref_delegate_with_property_arguments_is_a_value(t); Struct_method_results_as_owners(t); Struct_constructor_results_as_owners(t); Reference_return_method_preserves_address(t); Boxed_struct_reference_property_keeps_declared_object_storage(t); }
 public delegate int RefCall(ref int value);
 public delegate int RefInvoke(ref RefCall call, RefHolder holder);
 public sealed class RefHolder
 {
  public int Stored=5, Gets, Sets;
  public int Value { get { ++Gets; return Stored; } set { ++Sets; Stored=value; } }
 }
 public static int IncrementArgument(ref int value)=>++value;
 public void Incoming_ref_delegate_with_property_arguments_is_a_value(TestContext t)
 {
  var receiver=E.Parameter(typeof(RefCall).MakeByRefType());var holder=E.Parameter(typeof(RefHolder));
  var tree=E.Lambda<RefInvoke>(E.Invoke(receiver,E.Property(holder,nameof(RefHolder.Value))),receiver,holder);
  var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;
  foreach(var compiled in new[]{tree.Compile(),fast})
  {
   var data=new RefHolder();RefCall call=IncrementArgument;
   t.AreEqual(6,compiled(ref call,data));t.AreEqual(6,data.Stored);t.AreEqual(1,data.Gets);t.AreEqual(1,data.Sets);
   data=new RefHolder();call=null;t.Throws<NullReferenceException>(()=>compiled(ref call,data));
   t.AreEqual(5,data.Stored);t.AreEqual(1,data.Gets);t.AreEqual(0,data.Sets);
  }
 }
 public class ReferenceBase { }
 public sealed class ReferenceDerived : ReferenceBase { }
 public sealed class MixedHolder
 {
  public ReferenceDerived Reference = new ReferenceDerived();
  public int Value { get; set; }
 }
 public static void ReplaceAndIncrement(ref ReferenceBase reference, ref int value)
 {
  reference = new ReferenceBase();
  ++value;
 }
 public void Wider_formal_reference_with_property_copyback(TestContext t)
 {
  var holder = E.Parameter(typeof(MixedHolder));
  var call = E.Call(M(nameof(ReplaceAndIncrement)), E.Field(holder, nameof(MixedHolder.Reference)), E.Property(holder, nameof(MixedHolder.Value)));
  var tree = E.Lambda<Action<MixedHolder>>(call, holder);
  var fast = tree.FromSysExpression().CompileFast(true);
  if (!t.IsNotNull(fast)) return;
  foreach (var compiled in new[] { tree.Compile(), fast })
  {
   var data = new MixedHolder { Value = 5 };
   var original = data.Reference;
   compiled(data);
   t.AreEqual(6, data.Value);
   t.IsTrue(ReferenceEquals(original, data.Reference));
  }
 }
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
 public static int MutateOuter(ref Outer value) => ++value.Field.Value;
 public static object ObjectResult(ref int value) { ++value; return new Inner { Value = 7 }; }
 public void Prepared_results_can_supply_ref_argument_temporaries(TestContext t)
 {
  var p = E.Parameter(typeof(Owner));
  var property = E.Property(p, nameof(Owner.Value));
  var structure = E.Call(M(nameof(StructResult)), property);
  var reference = E.Call(M(nameof(ObjectResult)), property);
  foreach (var discard in new[] { false, true })
  foreach (var call in new[] { E.Call(M(nameof(MutateOuter)), structure), E.Call(M(nameof(ReplaceBox)), reference) })
  {
   var body = discard ? (System.Linq.Expressions.Expression)E.Block(call, E.Constant(8)) : call;
   Check(t, E.Lambda<Func<Owner, int>>(body, p));
  }
 }
 public void Struct_method_results_as_owners(TestContext t)=>Owners(t,false);
 public void Struct_constructor_results_as_owners(TestContext t)=>Owners(t,true);
 public void Reference_return_method_preserves_address(TestContext t) {
  var p=E.Parameter(typeof(Owner)); var result=E.Call(M(nameof(ReferenceResult)),E.Property(p,"Value"));
  var tree=E.Lambda<RefLambda>(result,p);
#if NETFRAMEWORK
  // Framework's DynamicMethod API cannot represent a by-reference return type for either compiler.
  t.Throws<NotSupportedException>(()=>tree.Compile());
  t.Throws<NotSupportedException>(()=>tree.FromSysExpression().CompileFast(true));
#else
  var system=tree.Compile(); var a=new Owner(); ref var expected=ref system(a); expected+=3; t.AreEqual(11,Static); t.AreEqual(4,a.Field);
  var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return; var b=new Owner(); ref var actual=ref fast(b); actual+=3; t.AreEqual(11,Static); t.AreEqual(4,b.Field);
#endif
 }
 public void Boxed_struct_reference_property_keeps_declared_object_storage(TestContext t) {
  var p=E.Parameter(typeof(Owner)); Check(t,E.Lambda<Func<Owner,int>>(E.Call(M(nameof(ReplaceBox)),E.Property(p,"Object")),p));
 }
}
