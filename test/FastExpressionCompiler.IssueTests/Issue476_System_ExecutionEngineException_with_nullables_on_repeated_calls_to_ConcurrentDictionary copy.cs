using System;
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

public struct Issue490_Regression_in_compiling_lambdas_with_ref_struct_parameters : ITestX
{
    public void Run(TestRun t)
    {
        Original_case(t);
    }

    private delegate int TestDelegate(ref  Utf8JsonReader reader);

    public void Original_case(TestContext t)
    {
        var param = Parameter(typeof(Utf8JsonReader).MakeByRefType());
        var body = Condition(
            Equal(Property(param, "TokenType"), Constant(JsonTokenType.Null)),
            Default(typeof(int)),
            Call(param, "GetInt32", Type.EmptyTypes)
        );
        var expr = Lambda<TestDelegate>(body, param);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);

        // make reader and advance to the Null token
        var reader = new Utf8JsonReader("null"u8);
        reader.Read();
        t.AreEqual(JsonTokenType.Null, reader.TokenType);

        // the compiled function should return default(int), yet it calls reader.GetInt32 instead
        var a = fs(ref reader);
        t.AreEqual(default, a);

        var ff = expr.CompileFast(false);
        ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);

        var b = ff(ref reader);
        t.AreEqual(default, b);
    }
}
