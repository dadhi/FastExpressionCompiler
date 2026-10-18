using System;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ScopedLabelBindingRegressionTests : ITestX
{
 public void Run(TestRun t){Sibling_labels_keep_distinct_values(t);Nested_redefinition_is_rejected_without_jump(t);Unique_void_child_entry_remains_valid(t);Outer_jump_to_siblings_is_ambiguous(t);Sibling_backward_targets_keep_local_values(t);Shared_targets_in_nested_lambdas_keep_inlining_context(t);}
 static System.Linq.Expressions.Expression Branch(System.Linq.Expressions.LabelTarget target,int value,int fallback,System.Linq.Expressions.GotoExpressionKind kind)=>S.Block(S.MakeGoto(kind,target,S.Constant(value),typeof(void)),S.Throw(S.New(typeof(Exception))),S.Label(target,S.Constant(fallback)));
 public void Sibling_labels_keep_distinct_values(TestContext t)
 {
  foreach(var kind in new[]{System.Linq.Expressions.GotoExpressionKind.Goto,System.Linq.Expressions.GotoExpressionKind.Return,System.Linq.Expressions.GotoExpressionKind.Break,System.Linq.Expressions.GotoExpressionKind.Continue})
  {
   var target=S.Label(typeof(int),"reused");var body=S.Add(S.Add(Branch(target,6,101,kind),Branch(target,4,202,kind)),Branch(target,5,303,kind));
   var source=S.Lambda<Func<int>>(body);t.AreEqual(15,source.Compile()());var fast=source.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.AreEqual(15,fast());
  }
  var label=S.Label(typeof(int));var defaults=S.Lambda<Func<int>>(S.Add(S.Block(S.Label(label,S.Constant(2))),S.Block(S.Label(label,S.Constant(8))))).FromSysExpression().CompileFast(true);
  if(t.IsNotNull(defaults))t.AreEqual(10,defaults());
 }
 public void Nested_redefinition_is_rejected_without_jump(TestContext t)
 {
  var target=S.Label("nested-overlap");var source=S.Lambda<Action>(S.Block(S.Label(target),S.Block(S.Label(target))));
  t.Throws<InvalidOperationException>(()=>source.Compile());t.Throws<InvalidOperationException>(()=>source.FromSysExpression().CompileFast(true, CompilerFlags.ValidateLabels));
  var valueTarget=S.Label(typeof(int),"nested-default");var valueSource=S.Lambda<Func<int>>(S.Label(valueTarget,S.Block(S.Label(valueTarget,S.Constant(3)))));t.Throws<InvalidOperationException>(()=>valueSource.Compile());t.Throws<InvalidOperationException>(()=>valueSource.FromSysExpression().CompileFast(true, CompilerFlags.ValidateLabels));
 }
 public void Unique_void_child_entry_remains_valid(TestContext t)
 {
  var target=S.Label("unique-child");var count=0;Action mark=()=>++count;
  var source=S.Lambda<Action>(S.Block(S.Goto(target),S.Block(S.Label(target)),S.Invoke(S.Constant(mark))));
  var fast=source.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast)){fast();t.AreEqual(1,count);}
 }
 public void Outer_jump_to_siblings_is_ambiguous(TestContext t)
 {
  var target=S.Label("ambiguous-child");var source=S.Lambda<Action>(S.Block(S.Goto(target),S.Block(S.Label(target)),S.Block(S.Label(target))));
  t.Throws<InvalidOperationException>(()=>source.Compile());t.Throws<InvalidOperationException>(()=>source.FromSysExpression().CompileFast(true, CompilerFlags.ValidateLabels));
 }
 static System.Linq.Expressions.Expression Counter(System.Linq.Expressions.LabelTarget target,int count)
 {
  var value=S.Variable(typeof(int));return S.Block(new[]{value},S.Assign(value,S.Constant(0)),S.Label(target,S.Constant(-1)),S.IfThen(S.LessThan(S.PreIncrementAssign(value),S.Constant(count)),S.Goto(target,S.Constant(47))),value);
 }
 public void Sibling_backward_targets_keep_local_values(TestContext t)
 {
  var target=S.Label(typeof(int),"backward");var source=S.Lambda<Func<int>>(S.Add(Counter(target,3),Counter(target,4)));
  t.AreEqual(7,source.Compile()());var fast=source.FromSysExpression().CompileFast(true);if(t.IsNotNull(fast))t.AreEqual(7,fast());
 }
 public void Shared_targets_in_nested_lambdas_keep_inlining_context(TestContext t)
 {
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining})
  {
   var target=S.Label(typeof(int),"shared-lambda-target");var a=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Constant(1)),S.Label(target,S.Constant(11))));var b=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Constant(2)),S.Label(target,S.Constant(22))));
   var source=S.Lambda<Func<int>>(S.Add(S.Invoke(a),S.Invoke(b)));t.AreEqual(3,source.Compile()());var fast=source.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(fast))t.AreEqual(3,fast());
   var sequence=S.Lambda<Func<int>>(S.Block(S.Invoke(a),S.Invoke(b)));t.AreEqual(2,sequence.Compile()());var sequential=sequence.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(sequential))t.AreEqual(2,sequential());
   var shadow=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Add(S.Invoke(a),S.Constant(10))),S.Label(target,S.Constant(101))));t.AreEqual(11,shadow.Compile()());var shadowed=shadow.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(shadowed))t.AreEqual(11,shadowed());
   var invalidInner=S.Lambda<Func<int>>(S.Goto(target,S.Constant(9),typeof(int)));var cross=S.Lambda<Func<int>>(S.Block(S.Invoke(invalidInner),S.Label(target,S.Constant(0))));t.Throws<InvalidOperationException>(()=>cross.Compile());t.Throws<InvalidOperationException>(()=>cross.FromSysExpression().CompileFast(true, flags | CompilerFlags.ValidateLabels));
   System.Linq.Expressions.Expression<Func<int>> chain=a;for(var i=0;i<4;i++)chain=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Add(S.Invoke(chain),S.Constant(1))),S.Label(target,S.Constant(111))));var growth=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Invoke(chain)),S.Label(target,S.Constant(999))));t.AreEqual(5,growth.Compile()());var grown=growth.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(grown))t.AreEqual(5,grown());
   var defaultGrowth=S.Lambda<Func<int>>(S.Label(target,S.Invoke(chain)));t.AreEqual(5,defaultGrowth.Compile()());var defaultGrown=defaultGrowth.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(defaultGrown))t.AreEqual(5,defaultGrown());
   var parameter=S.Parameter(typeof(int));
   // Both inlined and separately compiled invocation arguments keep caller jump semantics.
   {
    var callerTarget=S.Label(typeof(int),"caller-argument-target");var identity=S.Lambda<Func<int,int>>(parameter,parameter);var callerGoto=S.Lambda<Func<int>>(S.Block(S.Invoke(identity,S.Goto(callerTarget,S.Constant(7),typeof(int))),S.Label(callerTarget,S.Constant(8))));t.AreEqual(7,callerGoto.Compile()());var callerFast=callerGoto.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(callerFast))t.AreEqual(7,callerFast());
   }
   var callerInner=S.Label("caller-argument-overlap");var overlap=S.Lambda<Action>(S.Block(S.Label(callerInner),S.Invoke(S.Lambda<Action<int>>(S.Empty(),parameter),S.Block(S.Label(callerInner),S.Constant(1)))));t.Throws<InvalidOperationException>(()=>overlap.Compile());t.Throws<InvalidOperationException>(()=>overlap.FromSysExpression().CompileFast(true, flags | CompilerFlags.ValidateLabels));
   var sharedParameter=S.Parameter(typeof(int),"shared-input");var reuseSource=S.Lambda<Func<int,int>>(S.Invoke(S.Lambda<Func<int,int>>(S.Add(sharedParameter,S.Constant(1)),sharedParameter),sharedParameter),sharedParameter);t.AreEqual(10,reuseSource.Compile()(9));var reused=reuseSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(reused))t.AreEqual(10,reused(9));
   var stop=S.Label(typeof(int),"inline-loop-stop");var simpleLoop=S.Lambda<Func<int>>(S.Loop(S.Break(stop,S.Constant(13)),stop));var loopSource=S.Lambda<Func<int>>(S.Invoke(simpleLoop));t.AreEqual(13,loopSource.Compile()());var loopFast=loopSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(loopFast))t.AreEqual(13,loopFast());
   var next=S.Label("inline-loop-next");var counter=S.Variable(typeof(int));var finiteLoop=S.Lambda<Func<int>>(S.Block(new[]{counter},S.Assign(counter,S.Constant(0)),S.Loop(S.Block(S.PreIncrementAssign(counter),S.IfThen(S.LessThan(counter,S.Constant(2)),S.Continue(next)),S.Break(stop,counter)),stop,next)));var finiteSource=S.Lambda<Func<int>>(S.Invoke(finiteLoop));t.AreEqual(2,finiteSource.Compile()());var finiteFast=finiteSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(finiteFast))t.AreEqual(2,finiteFast());
   var invocation=S.Invoke(a);var same=S.Lambda<Func<int>>(S.Add(invocation,invocation));t.AreEqual(2,same.Compile()());var again=same.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(again))t.AreEqual(2,again());
  }
 }
}
