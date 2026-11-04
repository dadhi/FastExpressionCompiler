using System;
using S=System.Linq.Expressions.Expression;
using E=System.Linq.Expressions;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif
public struct TryResultValueRegressionTests : ITestX
{
    static readonly CompilerFlags[] Flags={CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo};
    public struct Counter { public int Value; public Counter() { Value=9; } public int Increment()=>++Value; }
    public struct Outer { public Counter Inner; }
    public static int Finalized;
    public static void Finally()=>++Finalized;
    public delegate ref int RefGetter();
    public delegate int RefCounterReader(ref Counter value);
    public static int RefValue;
    public static ref int GetRef()=>ref RefValue;
    public void Run(TestRun t)
    {
        Finally_result_receiver_uses_value_copy(t);
        Catch_result_receiver_uses_value_copy(t);
        Field_and_array_results_use_value_copy(t);
        Byref_argument_result_uses_temporary(t);
        Reference_result_identity_is_preserved(t);
        Ref_parameter_try_result_is_loaded_as_value(t);
        Supported_byref_return_keeps_reference(t);
    }
    static E.Expression Wrap(E.Expression body,bool catches,bool realFinally)
    {
        E.Expression final=realFinally?S.Call(typeof(TryResultValueRegressionTests).GetMethod(nameof(Finally))):S.Empty();
        return catches?S.TryCatch(S.Throw(S.New(typeof(Exception)),body.Type),S.Catch(typeof(Exception),body)):
            S.TryFinally(body,final);
    }
    static E.Expression<Func<int>> Build(string shape,bool catches,bool realFinally)
    {
        var value=S.Parameter(typeof(Counter),"value");var outer=S.Parameter(typeof(Outer),"outer");
        E.Expression receiver=shape=="default"?S.Default(typeof(Counter)):shape=="new"?S.New(typeof(Counter).GetConstructor(Type.EmptyTypes)):shape=="nested"?S.Field(outer,nameof(Outer.Inner)):value;
        var call=S.Call(Wrap(receiver,catches,realFinally),typeof(Counter).GetMethod(nameof(Counter.Increment)));
        if(shape=="default"||shape=="new")return S.Lambda<Func<int>>(call);
        var result=S.Add(S.Multiply(call,S.Constant(100)),S.Field(receiver,nameof(Counter.Value)));
        return shape=="nested"?S.Lambda<Func<int>>(S.Block(new[]{outer},S.Assign(outer,S.Constant(new Outer{Inner=new Counter{Value=5}})),result)):
            S.Lambda<Func<int>>(S.Block(new[]{value},S.Assign(value,S.Constant(new Counter{Value=5})),result));
    }
    static void Check(TestContext t,E.Expression<Func<int>> tree,int expected,int finalized=0)
    {
        Finalized=0;t.AreEqual(expected,tree.Compile()());t.AreEqual(finalized,Finalized);
        foreach(var flags in Flags){var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))continue;
            Finalized=0;t.AreEqual(expected,fast());t.AreEqual(finalized,Finalized);}
    }
    public void Finally_result_receiver_uses_value_copy(TestContext t)
    {
        foreach(var shape in new[]{"variable","nested","default","new"})foreach(var real in new[]{false,true})
            Check(t,Build(shape,false,real),shape=="default"?1:shape=="new"?10:605,real?1:0);
    }
    public void Catch_result_receiver_uses_value_copy(TestContext t)
    {
        foreach(var shape in new[]{"variable","nested","default","new"})Check(t,Build(shape,true,false),shape=="default"?1:shape=="new"?10:605);
    }
    public void Field_and_array_results_use_value_copy(TestContext t)
    {
        var value=S.Parameter(typeof(Counter));var arr=S.Parameter(typeof(Counter[]));
        foreach(var catches in new[]{false,true})
        {
            Check(t,S.Lambda<Func<int>>(S.Block(new[]{value},S.Assign(value,S.Constant(new Counter{Value=7})),S.Field(Wrap(value,catches,true),nameof(Counter.Value)))),7,catches?0:1);
            var element=S.ArrayIndex(arr,S.Constant(0));
            Check(t,S.Lambda<Func<int>>(S.Block(new[]{arr},S.Assign(arr,S.NewArrayInit(typeof(Counter),S.Constant(new Counter{Value=5}))),
                S.Call(Wrap(element,catches,true),typeof(Counter).GetMethod(nameof(Counter.Increment))),S.Field(element,nameof(Counter.Value)))),5,catches?0:1);
        }
    }
    public static int IncrementRef(ref int x)=>++x;
    public void Byref_argument_result_uses_temporary(TestContext t)
    {
        var x=S.Parameter(typeof(int));foreach(var catches in new[]{false,true})
            Check(t,S.Lambda<Func<int>>(S.Block(new[]{x},S.Assign(x,S.Constant(4)),S.Call(typeof(TryResultValueRegressionTests).GetMethod(nameof(IncrementRef)),Wrap(x,catches,false)),x)),4);
    }
    public void Reference_result_identity_is_preserved(TestContext t)
    {
        var obj=new object();foreach(var catches in new[]{false,true})
        {
            var tree=S.Lambda<Func<object>>(Wrap(S.Constant(obj),catches,false));t.IsTrue(ReferenceEquals(obj,tree.Compile()()));
            foreach(var flags in Flags)t.IsTrue(ReferenceEquals(obj,tree.FromSysExpression().CompileFast(true,flags)()));
        }
    }
    public void Supported_byref_return_keeps_reference(TestContext t)
    {
        // Some expression runtimes reject a byref Try type at construction.
        // Record that API boundary without asserting an FEC-only extension.
        E.Expression<RefGetter> tree;
        try { tree=S.Lambda<RefGetter>(S.TryFinally(S.Call(typeof(TryResultValueRegressionTests).GetMethod(nameof(GetRef))),S.Empty())); }
        catch(ArgumentException) { return; }
        RefGetter system;
        try { system=tree.Compile(); }
#if NETFRAMEWORK
        catch(NotSupportedException) { return; }
#endif
        catch { throw; }
        RefValue=4;system()=7;t.AreEqual(7,RefValue);
        foreach(var flags in Flags){var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))continue;RefValue=4;fast()=8;t.AreEqual(8,RefValue);}
    }
    public void Ref_parameter_try_result_is_loaded_as_value(TestContext t)
    {
        var input=S.Parameter(typeof(Counter).MakeByRefType(),"input");
        foreach(var catches in new[]{false,true})foreach(var real in new[]{false,true})
        {
            var call=S.Call(Wrap(input,catches,real),typeof(Counter).GetMethod(nameof(Counter.Increment)));
            var tree=S.Lambda<RefCounterReader>(S.Add(S.Multiply(call,S.Constant(100)),S.Field(input,nameof(Counter.Value))),input);
            var system=tree.Compile();var a=new Counter{Value=5};t.AreEqual(605,system(ref a));t.AreEqual(5,a.Value);
            foreach(var flags in Flags){var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))continue;var b=new Counter{Value=5};t.AreEqual(605,fast(ref b));t.AreEqual(5,b.Value);}
        }
    }
}
