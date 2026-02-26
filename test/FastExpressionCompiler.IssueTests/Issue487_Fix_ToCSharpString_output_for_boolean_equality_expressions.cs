#if NET8_0_OR_GREATER

using System;
using System.Reflection.Emit;
using System.Text.Json;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using FastExpressionCompiler.ImTools;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue487_Fix_ToCSharpString_output_for_boolean_equality_expressions : ITestX
{
    public void Run(TestRun t)
    {
        Original_case(t);
    }

    public class TestClass 
    {
        public required bool MyTestBool { get; set; }
    }

    public void Original_case(TestContext t)
    {
        var input = new TestClass() { MyTestBool = true };
        var parameter = Expression.Parameter(input.GetType(), "x");
        var property = Expression.Property(parameter, nameof(TestClass.MyTestBool));
        var expr = Expression.Equal(property, Expression.Constant(true));

        var str = expr.ToString(); // => (x.MyTestBool == True)
        var cs = expr.ToCSharpString(); // => (x.MyTestBool;

        expr.PrintCSharp();

        // var fs = expr.CompileSys();
        // fs.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);

        // var ff = expr.CompileFast(false);
        // ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
    }
}
#endif