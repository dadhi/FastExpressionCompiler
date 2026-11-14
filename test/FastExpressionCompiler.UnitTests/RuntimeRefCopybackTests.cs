using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct RuntimeRefCopybackTests : ITestX
{
    public delegate int Update(ref int value,int amount);
    public sealed class Cell { public int Stored=5,Gets,Sets; public int Value { get {++Gets;return Stored;} set {++Sets;Stored=value;} } }
    public static int Add(ref int value,int amount) { value+=amount;if(amount==13)throw new InvalidOperationException();return value; }
    public void Run(TestRun t) { Runtime_ref_properties_copy_back_only_after_success(t); Runtime_ref_aliases_remain_addresses(t); }
    public void Runtime_ref_properties_copy_back_only_after_success(TestContext t)
    {
        var receiver=S.Parameter(typeof(System.Linq.Expressions.Expression<Update>));var cell=S.Parameter(typeof(Cell));var amount=S.Parameter(typeof(int));
        var argument=S.Property(cell,nameof(Cell.Value));
        var p=S.Parameter(typeof(int).MakeByRefType());var delta=S.Parameter(typeof(int));
        var data=S.Lambda<Update>(S.Call(typeof(RuntimeRefCopybackTests).GetMethod(nameof(Add)),p,delta),p,delta);
        foreach(var protectedArgument in new[]{false,true})
        {
            var rhs=protectedArgument?(System.Linq.Expressions.Expression)S.TryFinally(amount,S.Empty()):amount;
            var tree=S.Lambda<Func<System.Linq.Expressions.Expression<Update>,Cell,int,int>>(S.Invoke(receiver,argument,rhs),receiver,cell,amount);
            foreach(var flags in new[]{CompilerFlags.Default,CompilerFlags.NoInvocationLambdaInlining,CompilerFlags.EnableDelegateDebugInfo})
            {
                var fast=tree.FromSysExpression().CompileFast(true,flags);if(!t.IsNotNull(fast))return;
                // System's IL stack spiller loses property copyback when another argument contains try/finally.
                // Use the equivalent explicit temporary/writeback control for that shape.
                Func<System.Linq.Expressions.Expression<Update>,Cell,int,int> control=(expression,model,delta)=>
                {
                    var temporary=model.Value;var result=expression.Compile()(ref temporary,delta);model.Value=temporary;return result;
                };
                foreach(var compiled in new[]{protectedArgument?control:tree.Compile(),fast})
                {
                    var model=new Cell();t.AreEqual(8,compiled(data,model,3));t.AreEqual(8,model.Stored);t.AreEqual(1,model.Gets);t.AreEqual(1,model.Sets);
                    model=new Cell();t.Throws<InvalidOperationException>(()=>compiled(data,model,13));t.AreEqual(5,model.Stored);t.AreEqual(1,model.Gets);t.AreEqual(0,model.Sets);
                }
            }
        }
    }
    public void Runtime_ref_aliases_remain_addresses(TestContext t)
    {
        var receiver=S.Parameter(typeof(System.Linq.Expressions.Expression<Update>));var array=S.Parameter(typeof(int[]));
        var tree=S.Lambda<Func<System.Linq.Expressions.Expression<Update>,int[],int>>(S.Invoke(receiver,S.ArrayAccess(array,S.Constant(0)),S.Constant(2)),receiver,array);
        var p=S.Parameter(typeof(int).MakeByRefType());var amount=S.Parameter(typeof(int));
        var data=S.Lambda<Update>(S.Call(typeof(RuntimeRefCopybackTests).GetMethod(nameof(Add)),p,amount),p,amount);
        var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;var values=new[]{5};t.AreEqual(7,fast(data,values));t.AreEqual(7,values[0]);
    }
}
