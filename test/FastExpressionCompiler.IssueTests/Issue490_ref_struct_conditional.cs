using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue490_ref_struct_conditional : ITest, ITestX
{
    public int Run()
    {
        Return_true_when_token_is_null();
        Return_false_when_token_isnot_null();
        return 1;
    }

    public void Run(TestRun t)
    {
        Return_true_when_token_is_null(t);
        Return_false_when_token_isnot_null(t);
    }

    public delegate bool RefStructReaderDelegate(ref MyJsonReader reader);

    public enum MyJsonTokenType : byte
    {
        None = 0,
        StartObject = 1,
        Null = 11
    }

    public ref struct MyJsonReader
    {
        public MyJsonTokenType TokenType { get; set; }
    }

    public void Return_true_when_token_is_null(TestContext t = default)
    {
        var readerParam = Parameter(typeof(MyJsonReader).MakeByRefType(), "reader");
        var tokenType = Property(readerParam, nameof(MyJsonReader.TokenType));
        var nullToken = Constant(MyJsonTokenType.Null);

        var body = Condition(
            Equal(tokenType, nullToken),
            Constant(true),
            Constant(false));

        var lambda = Lambda<RefStructReaderDelegate>(body, readerParam);
        var func = lambda.CompileFast();

        var reader = new MyJsonReader();
        reader.TokenType = MyJsonTokenType.Null;

        var result = func(ref reader);
        t.AreEqual(true, result);
    }

    public void Return_false_when_token_isnot_null(TestContext t = default)
    {
        var readerParam = Parameter(typeof(MyJsonReader).MakeByRefType(), "reader");
        var tokenType = Property(readerParam, nameof(MyJsonReader.TokenType));
        var nullToken = Constant(MyJsonTokenType.Null);

        var body = Condition(
            Equal(tokenType, nullToken),
            Constant(true),
            Constant(false));

        var lambda = Lambda<RefStructReaderDelegate>(body, readerParam);
        var func = lambda.CompileFast();

        var reader = new MyJsonReader();
        reader.TokenType = MyJsonTokenType.StartObject;

        var result = func(ref reader);
        t.AreEqual(false, result);
    }
}