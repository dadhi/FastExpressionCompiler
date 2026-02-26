using System;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue480_CLR_detected_an_invalid_program_exception : ITestX
{
    public void Run(TestRun t)
    {
        Original_case(t);
    }

    public void Original_case(TestContext t)
    {
        var p1 = Condition(Equal(Constant(true), Constant(true)), Constant(null, typeof(bool?)), Constant(null, typeof(bool?)));
        var p2 = Condition(Equal(Constant(true), Constant(true)), Constant(null, typeof(bool?)), Constant(null, typeof(bool?)));
        var exp = Convert(OrElse(p1, p2), typeof(object));
        var expr = Lambda<Func<object>>(exp);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);

        // the compiled function should return default(int), yet it calls reader.GetInt32 instead
        var a = fs();

        var ff = expr.CompileFast(false);
        ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        var b = ff();
    }
}