using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct RuntimeExpressionInvokeTests : ITestX
{
    public delegate int RefTree(ref System.Linq.Expressions.Expression<Func<int>> tree);
    public void Run(TestRun t) { Computed_and_parameter_receivers(t); Compilation_and_argument_order(t); Ref_expression_receivers_are_values(t); Unsupported_byref_arguments_are_rejected(t); }
    public void Computed_and_parameter_receivers(TestContext t)
    {
        var tree=S.Parameter(typeof(System.Linq.Expressions.Expression<Func<int,int>>));
        var value=S.Parameter(typeof(int));
        var lambda=S.Lambda<Func<System.Linq.Expressions.Expression<Func<int,int>>,int,int>>(S.Invoke(tree,value),tree,value);
        foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining})
        {
            var fast=lambda.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
            t.AreEqual(7,fast(x=>x+2,5));t.AreEqual(15,fast(x=>x*3,5));
        }
        var calls=0;Func<System.Linq.Expressions.Expression<Func<int>>> factory=()=>{++calls;return ()=>7;};
        var computed=S.Lambda<Func<int>>(S.Invoke(S.Invoke(S.Constant(factory)))).FromSysExpression().CompileFast(true);
        if(t.IsNotNull(computed)){t.AreEqual(7,computed());t.AreEqual(7,computed());t.AreEqual(2,calls);}
    }
    public void Compilation_and_argument_order(TestContext t)
    {
        foreach(var exceptionArgument in new[]{false,true})
        foreach(var nullReceiver in new[]{false,true})
        foreach(var invalidReceiver in new[]{false,true})
        {
            var trace="";
            Func<System.Linq.Expressions.Expression<Func<int,int>>> receiver=()=>
            {
                trace+="receiver;";
                if(nullReceiver)return null;
                var unbound=S.Parameter(typeof(int));
                return invalidReceiver?S.Lambda<Func<int,int>>(unbound,S.Parameter(typeof(int))):x=>x+2;
            };
            Func<int> argument=()=>{trace+="argument;";return 5;};
            Action cleanup=()=>trace+="finally;";
            var arg=(System.Linq.Expressions.Expression)S.Invoke(S.Constant(argument));
            if(exceptionArgument)arg=S.TryFinally(arg,S.Invoke(S.Constant(cleanup)));
            var lambda=S.Lambda<Func<int>>(S.Invoke(S.Invoke(S.Constant(receiver)),arg));
            var system=lambda.Compile();var fast=lambda.FromSysExpression().CompileFast(true);
            if(!t.IsNotNull(fast))return;
            string Observe(Func<int> compiled)
            {
                trace="";string result;
                try{result=compiled().ToString();}catch(Exception e){result=e.GetType().FullName;}
                return trace+result;
            }
            t.AreEqual(Observe(system),Observe(fast));
        }
    }
    public delegate int Update(ref int value);
    public void Unsupported_byref_arguments_are_rejected(TestContext t)
    {
        var receiver=S.Parameter(typeof(System.Linq.Expressions.Expression<Update>));
        var value=S.Parameter(typeof(int));
        var tree=S.Lambda<Func<System.Linq.Expressions.Expression<Update>,int,int>>(S.Invoke(receiver,value),receiver,value);
        t.AreEqual(null,tree.FromSysExpression().CompileFast(true));
    }
    public void Ref_expression_receivers_are_values(TestContext t)
    {
        var receiver=S.Parameter(typeof(System.Linq.Expressions.Expression<Func<int>>).MakeByRefType());
        var lambda=S.Lambda<RefTree>(S.Invoke(receiver),receiver).FromSysExpression().CompileFast(true);
        if(!t.IsNotNull(lambda))return;
        System.Linq.Expressions.Expression<Func<int>> tree=()=>7;
        t.AreEqual(7,lambda(ref tree));t.IsNotNull(tree);
    }
}
