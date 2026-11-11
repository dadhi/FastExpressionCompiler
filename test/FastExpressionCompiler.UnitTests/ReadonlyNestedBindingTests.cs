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
public struct ReadonlyNestedBindingTests : ITestX
{
 public struct Cell {public int Number;}
 public sealed class Holder {public readonly Cell Readonly=new Cell{Number=2};public Cell Writable;}
 public void Run(TestRun t) { Readonly_value_fields_use_defensive_copies(t); }
 public void Readonly_value_fields_use_defensive_copies(TestContext t) {
  foreach(var name in new[]{"Readonly","Writable"}) {
   var field=typeof(Holder).GetField(name);var tree=E.Lambda<Func<Holder>>(E.MemberInit(E.New(typeof(Holder)),E.MemberBind(field,E.Bind(typeof(Cell).GetField("Number"),E.Constant(7)))));
   var system=(Func<Holder>)tree.CompileSys();var expected=system();var expectedValue=((Cell)field.GetValue(expected)).Number;
   foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo}) {var fast=tree.CompileFast(true,flags);if(t.IsNotNull(fast))t.AreEqual(expectedValue,((Cell)field.GetValue(fast())).Number);}
  }
 }

}
