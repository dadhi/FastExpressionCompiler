using System;
using System.Collections.Generic;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RuntimeSwitchExceptionRegressionTests : ITestX
{
 public void Run(TestRun t) => Exception_regions_in_tests_preserve_order(t);
 public void Exception_regions_in_tests_preserve_order(TestContext t)
 {
  foreach(var nullable in new[]{false,true})
  foreach(var fail in new[]{false,true})
  foreach(var input in nullable?new int?[]{2,null}:new int?[]{2})
  {
   var log=new List<int>();Func<int,int> mark=x=>{log.Add(x);if(fail&&x==2)throw new InvalidOperationException();return x;};
   var type=nullable?typeof(int?):typeof(int);var value=S.Parameter(type);
   Func<int,System.Linq.Expressions.Expression> test=x=>S.Convert(S.Invoke(S.Constant(mark),S.Constant(x)),type);
   var node=S.Switch(value,S.Constant(0),S.SwitchCase(S.Constant(3),test(1)),S.SwitchCase(S.Constant(5),S.TryFinally(test(2),S.Block(test(4),S.Empty()))),S.SwitchCase(S.Constant(7),test(3)));
   var f=S.Lambda<Func<int>>(S.Invoke(S.Lambda(node,value),S.Constant((object)input,type))).FromSysExpression().CompileFast(true);
   if(!t.IsNotNull(f))return;
   if(fail)t.Throws<InvalidOperationException>(()=>f());else t.AreEqual(input.HasValue?5:0,f());
   t.AreEqual(fail||input.HasValue?"1,2,4":"1,2,4,3",string.Join(",",log));
  }
 }
}
