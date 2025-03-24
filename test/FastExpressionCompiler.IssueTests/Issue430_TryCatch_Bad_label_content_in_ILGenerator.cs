using System;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif


public class Issue430_TryCatch_Bad_label_content_in_ILGenerator : ITest
{
    public int Run()
    {
        Original_case();
        return 1;
    }

    public static void DoSome() { }


    public void Original_case()
    {
        var returnLabelTarget = Label(typeof(int), "ReturnLabel");

        var method = GetType().GetMethod("DoSome");

        var expr = Lambda<Func<int>>(
            Block(
                TryCatch(
                    Block(Call(method), Return(returnLabelTarget, Constant(1))),
                    Catch(typeof(Exception), Return(returnLabelTarget, Constant(10)))),
                Call(method),
                Return(returnLabelTarget, Constant(5)),
                Label(returnLabelTarget, Constant(0))));

        expr.PrintCSharp();

#if LIGHT_EXPRESSION
        var sysExpr = expr.ToLambdaExpression();
        var restoredExpr = sysExpr.ToLightExpression();
        restoredExpr.PrintCSharp();
        Asserts.AreEqual(expr.ToCSharpString(), restoredExpr.ToCSharpString());
#endif

        var fs = expr.CompileSys();
        fs.PrintIL();
        Asserts.AreEqual(1, fs());

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();
        Asserts.AreEqual(1, ff());
    }
}