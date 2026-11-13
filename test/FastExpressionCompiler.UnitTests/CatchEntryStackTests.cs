using System;
using S = System.Linq.Expressions.Expression;
#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct CatchEntryStackTests : ITestX
{
    public sealed class Model { public int Value=7; }
    public static int Finalized;
    public static void Finally()=>++Finalized;
    public void Run(TestRun t) { Rethrow_preserves_exception_identity(t); }
    public void Rethrow_preserves_exception_identity(TestContext t)
    {
        var original=new InvalidOperationException();var caught=S.Parameter(typeof(Exception));
        var inner=S.TryCatch(S.Throw(S.Constant(original),typeof(bool)),S.Catch(typeof(Exception),
            S.TryFinally(S.Rethrow(typeof(bool)),S.Call(typeof(CatchEntryStackTests).GetMethod(nameof(Finally))))));
        var tree=S.Lambda<Func<bool>>(S.TryCatch(inner,S.Catch(caught,S.ReferenceEqual(caught,S.Constant(original)))));
        Finalized=0;t.IsTrue(tree.Compile()());t.AreEqual(1,Finalized);
        var fast=tree.FromSysExpression().CompileFast(true);if(!t.IsNotNull(fast))return;
        Finalized=0;t.IsTrue(fast());t.AreEqual(1,Finalized);
    }
}
