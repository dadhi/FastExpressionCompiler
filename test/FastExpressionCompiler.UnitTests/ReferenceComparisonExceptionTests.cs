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
public struct ReferenceComparisonExceptionTests : ITestX
{
 public void Run(TestRun t) { Operands_keep_identity_and_order(t); }
 public void Operands_keep_identity_and_order(TestContext t) {
  var left=S.Parameter(typeof(Func<object>));var right=S.Parameter(typeof(Func<object>));var cleanup=S.Parameter(typeof(Action));
  foreach(var negate in new[]{false,true}) {
   var rhs=S.TryFinally(S.Invoke(right),S.Invoke(cleanup));
   var body=negate?S.ReferenceNotEqual(S.Invoke(left),rhs):S.ReferenceEqual(S.Invoke(left),rhs);
   var tree=S.Lambda<Func<Func<object>,Func<object>,Action,bool>>(body,left,right,cleanup);var system=tree.Compile();
   foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo}) {
    var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
    foreach(var same in new[]{false,true}) {var trace=new List<int>();var value=new object();Func<object> a=()=>{trace.Add(0);return value;};Func<object> b=()=>{trace.Add(1);return same?value:new object();};Action done=()=>trace.Add(2);
     var expected=system(a,b,done);var effects=string.Join(",",trace);trace.Clear();t.AreEqual(expected,fast(a,b,done));t.AreEqual(effects,string.Join(",",trace));}
   }
  }
 }

}
