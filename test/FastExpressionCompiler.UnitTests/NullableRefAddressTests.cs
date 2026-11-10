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
public struct NullableRefAddressTests : ITestX
{
 public delegate int Read(ref int? value);
 public delegate bool Has(ref int? value);
 public void Run(TestRun t) { Ref_members_read_the_original_nullable(t); Null_comparisons_keep_the_nullable_address(t); }
 public void Ref_members_read_the_original_nullable(TestContext t) {
  var p=E.Parameter(typeof(int?).MakeByRefType());
  var tree=E.Lambda<Read>(E.Call(p,typeof(int?).GetMethod("GetValueOrDefault",Type.EmptyTypes)),p);
  var has=E.Lambda<Has>(E.Property(p,"HasValue"),p);
  var system=(Read)tree.CompileSys();var systemHas=(Has)has.CompileSys();
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo}) {
   var fast=tree.CompileFast(true,flags);var fastHas=has.CompileFast(true,flags);if(!t.IsNotNull(fast)||!t.IsNotNull(fastHas))return;
   foreach(var input in new int?[]{null,0,7}) {var a=input;var b=input;t.AreEqual(system(ref a),fast(ref b));t.AreEqual(systemHas(ref a),fastHas(ref b));t.AreEqual(a,b);}
  }
 }
 public void Null_comparisons_keep_the_nullable_address(TestContext t) {
  var p=E.Parameter(typeof(int?).MakeByRefType());
  foreach(var body in new[]{E.Equal(p,E.Constant(null)),E.NotEqual(E.Constant(null,typeof(int?)),p)}) {
   var tree=E.Lambda<Has>(body,p);var system=(Has)tree.CompileSys();var fast=tree.CompileFast(true);
   if(!t.IsNotNull(fast))return;foreach(var input in new int?[]{null,0,7}) {var a=input;var b=input;t.AreEqual(system(ref a),fast(ref b));}
  }
 }

}
