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
    public void Run(TestRun t) { Computed_and_parameter_receivers(t); Compilation_and_argument_order(t); Ref_expression_receivers_are_values(t); Runtime_byref_arguments_use_writable_storage(t); Ordinary_ref_delegate_property_arguments_keep_copyback(t); }
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
    public void Runtime_byref_arguments_use_writable_storage(TestContext t)
    {
        var receiver=S.Parameter(typeof(System.Linq.Expressions.Expression<Update>));
        var value=S.Parameter(typeof(int));
        var tree=S.Lambda<Func<System.Linq.Expressions.Expression<Update>,int,int>>(S.Invoke(receiver,value),receiver,value);
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var parameter = S.Parameter(typeof(int).MakeByRefType());
        var update = S.Lambda<Update>(S.Call(typeof(RuntimeExpressionInvokeTests).GetMethod(nameof(Increment)), parameter), parameter);
        foreach (var compiled in new[] { tree.Compile(), fast })
            foreach (var initial in new[] { -1, 0, 5 })
                t.AreEqual(initial + 1, compiled(update, initial));
    }
    public sealed class Cell
    {
        public int Stored = 5, Gets, Sets;
        public int Value { get { ++Gets; return Stored; } set { ++Sets; Stored = value; } }
    }
    public static int Increment(ref int value) => ++value;
    public void Ordinary_ref_delegate_property_arguments_keep_copyback(TestContext t)
    {
        var receiver = S.Parameter(typeof(Update));
        var cell = S.Parameter(typeof(Cell));
        var value = S.Property(cell, nameof(Cell.Value));
        var tree = S.Lambda<Func<Update, Cell, int>>(S.Invoke(receiver, value), receiver, cell);
        var fast = tree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        foreach (var compiled in new[] { tree.Compile(), fast })
        {
            var data = new Cell();
            t.AreEqual(6, compiled(Increment, data));
            t.AreEqual(6, data.Stored);
            t.AreEqual(1, data.Gets);
            t.AreEqual(1, data.Sets);
        }
        // Runtime expression receivers also preserve getter/setter copyback.
        var expression = S.Parameter(typeof(System.Linq.Expressions.Expression<Update>));
        var runtimeTree = S.Lambda<Func<System.Linq.Expressions.Expression<Update>, Cell, int>>(
            S.Invoke(expression, value), expression, cell);
        var runtimeFast = runtimeTree.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(runtimeFast)) return;
        var parameter = S.Parameter(typeof(int).MakeByRefType());
        var update = S.Lambda<Update>(S.Call(typeof(RuntimeExpressionInvokeTests).GetMethod(nameof(Increment)), parameter), parameter);
        foreach (var compiled in new[] { runtimeTree.Compile(), runtimeFast })
        {
            var data = new Cell();
            t.AreEqual(6, compiled(update, data));
            t.AreEqual(6, data.Stored);
            t.AreEqual(1, data.Gets);
            t.AreEqual(1, data.Sets);
        }
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
