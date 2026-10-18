using System;
using E=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ReadonlyRefFieldRegressionTests : ITestX
{
 public void Run(TestRun t) { Readonly_instance_fields_supply_temporary_references(t); Static_fields_keep_exact_typed_addresses(t); Static_struct_receiver_preserves_readonly_rules(t); }
 public struct Mutable { public int Value; public int Increment()=>++Value; }
 public class Owner { public readonly int Number=3; public readonly string Text="original"; }
 public static int Number;
 public static string Text;
 public static readonly int ReadonlyNumber=3;
 public static Mutable Struct;
 public static readonly Mutable ReadonlyStruct=new Mutable{Value=3};
 public static int Add(ref int value)=>++value;
 public static int Replace(ref string value) { value="replaced"; return value.Length; }
 private static System.Reflection.MethodInfo M(string name)=>typeof(ReadonlyRefFieldRegressionTests).GetMethod(name);
 public void Readonly_instance_fields_supply_temporary_references(TestContext t) {
  foreach(var name in new[]{"Number","Text"}) {
   var p=E.Parameter(typeof(Owner)); var method=name=="Number"?nameof(Add):nameof(Replace);
   var tree=E.Lambda<Func<Owner,int>>(E.Call(M(method),E.Field(p,name)),p); var system=tree.Compile(); var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return;
   var a=new Owner(); var b=new Owner(); t.AreEqual(system(a),fast(b)); t.AreEqual(a.Number,b.Number); t.AreEqual(a.Text,b.Text);
  }
 }
 public void Static_fields_keep_exact_typed_addresses(TestContext t) {
  foreach(var name in new[]{"Number","Text","ReadonlyNumber"}) {
   var method=name=="Text"?nameof(Replace):nameof(Add); var tree=E.Lambda<Func<int>>(E.Call(M(method),E.Field(null,typeof(ReadonlyRefFieldRegressionTests),name)));
   var system=tree.Compile(); var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return;
   Number=3; Text="original"; var expected=system(); var number=Number; var text=Text;
   Number=3; Text="original"; t.AreEqual(expected,fast()); t.AreEqual(number,Number); t.AreEqual(text,Text); t.AreEqual(3,ReadonlyNumber);
  }
 }
 public void Static_struct_receiver_preserves_readonly_rules(TestContext t) {
  foreach(var name in new[]{"Struct","ReadonlyStruct"}) {
   var tree=E.Lambda<Func<int>>(E.Call(E.Field(null,typeof(ReadonlyRefFieldRegressionTests),name),typeof(Mutable).GetMethod(nameof(Mutable.Increment))));
   var system=tree.Compile(); var fast=tree.FromSysExpression().CompileFast(true); if(!t.IsNotNull(fast)) return;
   Struct=new Mutable{Value=3}; var expected=system(); var value=Struct.Value;
   Struct=new Mutable{Value=3}; t.AreEqual(expected,fast()); t.AreEqual(value,Struct.Value); t.AreEqual(3,ReadonlyStruct.Value);
  }
 }
}
