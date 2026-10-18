using System;
using System.Collections.Generic;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RuntimeSwitchRegressionTests : ITestX
{
 public void Run(TestRun t){Computed_string_tests_are_collected(t);Runtime_numeric_tests_preserve_order(t);Explicit_void_switch_ignores_branch_values(t);Exception_regions_in_tests_preserve_order(t);}
 public void Computed_string_tests_are_collected(TestContext t)
 {
  System.Linq.Expressions.Expression<Func<string>> value=()=>new string('q',5);
  System.Linq.Expressions.Expression<Func<string>> body=()=>new string('a',5);
  var x=S.Parameter(typeof(string));var node=S.Switch(x,S.Constant("default"),S.SwitchCase(S.Invoke(body),S.Invoke(value)),S.SwitchCase(S.Constant("bye"),S.Constant("later")));
  var f=S.Lambda<Func<string,string>>(node,x).FromSysExpression().CompileFast(true);if(!t.IsNotNull(f))return;
  t.AreEqual("aaaaa",f("qqqqq"));t.AreEqual("bye",f("later"));t.AreEqual("default",f(null));
 }
 public void Runtime_numeric_tests_preserve_order(TestContext t)
 {
  foreach(var constantValue in new[]{false,true})
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.DisableInterpreter})
  {
   var log=new List<int>();Func<int,int> mark=x=>{log.Add(x);return x;};
   var x=S.Parameter(typeof(int));var dynamicTest=S.Invoke(S.Constant(mark),S.Constant(1));
   var node=S.Switch(constantValue?S.Constant(2):(System.Linq.Expressions.Expression)x,S.Constant(9),S.SwitchCase(S.Constant(3),dynamicTest),S.SwitchCase(S.Constant(5),S.Constant(2)),S.SwitchCase(S.Constant(7),S.Constant(3)));
   var f=S.Lambda<Func<int,int>>(node,x).FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(f))return;
   t.AreEqual(5,f(2));t.AreEqual("1",string.Join(",",log));
  }
 }
 public void Explicit_void_switch_ignores_branch_values(TestContext t)
 {
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.DisableInterpreter})
  {
   var x=S.Parameter(typeof(DateTime));
   var node=S.Switch(typeof(void),x,S.Constant(1),null,S.SwitchCase(S.Constant("value"),S.Constant(DateTime.MinValue)),S.SwitchCase(S.Constant(DateTime.MinValue),S.Constant(DateTime.MaxValue)));
   var f=S.Lambda<Action<DateTime>>(node,x).FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(f))return;
   f(DateTime.MinValue);f(DateTime.MaxValue);f(new DateTime(2000,1,1));
   var selected=S.Lambda<Action>(S.Switch(typeof(void),S.Constant(1),S.Constant(DateTime.MinValue),null,S.SwitchCase(S.Constant("value"),S.Constant(1)))).FromSysExpression().CompileFast(true,flags);
   if(t.IsNotNull(selected))selected();
  }
 }
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
