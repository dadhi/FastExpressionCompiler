using System;
using S=System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct ScopedLabelBindingRegressionTests : ITestX
{
 public void Run(TestRun t){Shared_targets_in_nested_lambdas_keep_inlining_context(t);}
 static System.Linq.Expressions.Expression Branch(System.Linq.Expressions.LabelTarget target,int value,int fallback,System.Linq.Expressions.GotoExpressionKind kind)=>S.Block(S.MakeGoto(kind,target,S.Constant(value),typeof(void)),S.Throw(S.New(typeof(Exception))),S.Label(target,S.Constant(fallback)));

 static System.Linq.Expressions.Expression Counter(System.Linq.Expressions.LabelTarget target,int count)
 {
  var value=S.Variable(typeof(int));return S.Block(new[]{value},S.Assign(value,S.Constant(0)),S.Label(target,S.Constant(-1)),S.IfThen(S.LessThan(S.PreIncrementAssign(value),S.Constant(count)),S.Goto(target,S.Constant(47))),value);
 }

 public void Shared_targets_in_nested_lambdas_keep_inlining_context(TestContext t)
 {
  foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining})
  {
   var target=S.Label(typeof(int),"shared-lambda-target");var a=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Constant(1)),S.Label(target,S.Constant(11))));var b=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Constant(2)),S.Label(target,S.Constant(22))));
   var source=S.Lambda<Func<int>>(S.Add(S.Invoke(a),S.Invoke(b)));t.AreEqual(3,source.Compile()());var fast=source.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(fast))t.AreEqual(3,fast());
   var sequence=S.Lambda<Func<int>>(S.Block(S.Invoke(a),S.Invoke(b)));t.AreEqual(2,sequence.Compile()());var sequential=sequence.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(sequential))t.AreEqual(2,sequential());
   var shadow=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Add(S.Invoke(a),S.Constant(10))),S.Label(target,S.Constant(101))));t.AreEqual(11,shadow.Compile()());var shadowed=shadow.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(shadowed))t.AreEqual(11,shadowed());
   var invalidInner=S.Lambda<Func<int>>(S.Goto(target,S.Constant(9),typeof(int)));var cross=S.Lambda<Func<int>>(S.Block(S.Invoke(invalidInner),S.Label(target,S.Constant(0))));t.Throws<InvalidOperationException>(()=>cross.Compile());t.Throws<InvalidOperationException>(()=>cross.FromSysExpression().CompileFast(true, flags));
   System.Linq.Expressions.Expression<Func<int>> chain=a;for(var i=0;i<4;i++)chain=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Add(S.Invoke(chain),S.Constant(1))),S.Label(target,S.Constant(111))));var growth=S.Lambda<Func<int>>(S.Block(S.Return(target,S.Invoke(chain)),S.Label(target,S.Constant(999))));t.AreEqual(5,growth.Compile()());var grown=growth.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(grown))t.AreEqual(5,grown());
   var defaultGrowth=S.Lambda<Func<int>>(S.Label(target,S.Invoke(chain)));t.AreEqual(5,defaultGrowth.Compile()());var defaultGrown=defaultGrowth.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(defaultGrown))t.AreEqual(5,defaultGrown());
   var parameter=S.Parameter(typeof(int));
   // Both inlined and separately compiled invocation arguments keep caller jump semantics.
   {
    var callerTarget=S.Label(typeof(int),"caller-argument-target");var identity=S.Lambda<Func<int,int>>(parameter,parameter);var callerGoto=S.Lambda<Func<int>>(S.Block(S.Invoke(identity,S.Goto(callerTarget,S.Constant(7),typeof(int))),S.Label(callerTarget,S.Constant(8))));t.AreEqual(7,callerGoto.Compile()());var callerFast=callerGoto.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(callerFast))t.AreEqual(7,callerFast());
   }
   var localReturn=S.Label(typeof(int),"caller-local-return");var localArgument=S.Block(S.Return(localReturn,S.Constant(5)),S.Label(localReturn,S.Constant(0)));var localSource=S.Lambda<Func<int>>(S.Invoke(S.Lambda<Func<int,int>>(parameter,parameter),localArgument));t.AreEqual(5,localSource.Compile()());var localFast=localSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(localFast))t.AreEqual(5,localFast());
   var nestedLocalSource=S.Lambda<Func<int>>(S.Invoke(S.Lambda<Func<int>>(S.Add(S.Invoke(S.Lambda<Func<int,int>>(parameter,parameter),localArgument),S.Constant(10)))));t.AreEqual(15,nestedLocalSource.Compile()());var nestedLocalFast=nestedLocalSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(nestedLocalFast))t.AreEqual(15,nestedLocalFast());
   var callerInner=S.Label("caller-argument-overlap");var overlap=S.Lambda<Action>(S.Block(S.Label(callerInner),S.Invoke(S.Lambda<Action<int>>(S.Empty(),parameter),S.Block(S.Label(callerInner),S.Constant(1)))));t.Throws<InvalidOperationException>(()=>overlap.Compile());t.Throws<InvalidOperationException>(()=>overlap.FromSysExpression().CompileFast(true, flags));
   var sharedParameter=S.Parameter(typeof(int),"shared-input");var reuseSource=S.Lambda<Func<int,int>>(S.Invoke(S.Lambda<Func<int,int>>(S.Add(sharedParameter,S.Constant(1)),sharedParameter),sharedParameter),sharedParameter);t.AreEqual(10,reuseSource.Compile()(9));var reused=reuseSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(reused))t.AreEqual(10,reused(9));
   var stop=S.Label(typeof(int),"inline-loop-stop");var simpleLoop=S.Lambda<Func<int>>(S.Loop(S.Break(stop,S.Constant(13)),stop));var loopSource=S.Lambda<Func<int>>(S.Invoke(simpleLoop));t.AreEqual(13,loopSource.Compile()());var loopFast=loopSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(loopFast))t.AreEqual(13,loopFast());
   var next=S.Label("inline-loop-next");var counter=S.Variable(typeof(int));var finiteLoop=S.Lambda<Func<int>>(S.Block(new[]{counter},S.Assign(counter,S.Constant(0)),S.Loop(S.Block(S.PreIncrementAssign(counter),S.IfThen(S.LessThan(counter,S.Constant(2)),S.Continue(next)),S.Break(stop,counter)),stop,next)));var finiteSource=S.Lambda<Func<int>>(S.Invoke(finiteLoop));t.AreEqual(2,finiteSource.Compile()());var finiteFast=finiteSource.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(finiteFast))t.AreEqual(2,finiteFast());
   var invocation=S.Invoke(a);var same=S.Lambda<Func<int>>(S.Add(invocation,invocation));t.AreEqual(2,same.Compile()());var again=same.FromSysExpression().CompileFast(true,flags);if(t.IsNotNull(again))t.AreEqual(2,again());
  }
 }
}
