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
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue490_Regression_in_compiling_lambdas_with_ref_struct_parameters : ITestX
{
    public void Run(TestRun t)
    {
        Return_true_when_token_is_null();
        Return_false_when_token_is_not_null();
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
        fs.AssertOpCodes(
            OpCodes.Ldarg_1, //        at IL_0000
            OpCodes.Call, // JsonTokenType Utf8JsonReader.get_TokenType() at IL_0001
            OpCodes.Ldc_I4_S, // 11    at IL_0006
            OpCodes.Bne_Un, // IL_0019 at IL_0008
            OpCodes.Ldc_I4_0, //       at IL_0013
            OpCodes.Br, // IL_0025     at IL_0014
            OpCodes.Ldarg_1, //        at IL_0019
            OpCodes.Call, // int Utf8JsonReader.GetInt32() at IL_0020
            OpCodes.Ret  //            at IL_0025
        );
        t.AreEqual(default, a);

        var ff = expr.CompileFast(false);
        ff.PrintIL(format: ILDecoder.ILFormat.AssertOpCodes);
        ff.AssertOpCodes(
            OpCodes.Ldarg_1, //        at IL_0000
            OpCodes.Call, // JsonTokenType Utf8JsonReader.get_TokenType() at IL_0001
            OpCodes.Ldc_I4_S, // 11    at IL_0006
            OpCodes.Ceq, //            at IL_0008
            OpCodes.Brfalse, // IL_0021at IL_0010
            OpCodes.Ldc_I4_0, //       at IL_0015
            OpCodes.Br, // IL_0027     at IL_0016
            OpCodes.Ldarg_1, //        at IL_0021
            OpCodes.Call, // int Utf8JsonReader.GetInt32() at IL_0022
            OpCodes.Ret  //            at IL_0027
        );

        var b = ff(ref reader);
        t.AreEqual(default, b);
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
        lambda.PrintCSharp();

        var fs = lambda.CompileSys();
        fs.PrintIL();

        var reader = new MyJsonReader();
        reader.TokenType = MyJsonTokenType.Null;

        var a = fs(ref reader);
        t.AreEqual(true, a);

        var ff = lambda.CompileFast();
        ff.PrintIL();

        var b = ff(ref reader);
        t.AreEqual(true, b);
    }

    public void Return_false_when_token_is_not_null(TestContext t = default)
    {
        var readerParam = Parameter(typeof(MyJsonReader).MakeByRefType(), "reader");
        var tokenType = Property(readerParam, nameof(MyJsonReader.TokenType));
        var nullToken = Constant(MyJsonTokenType.Null);

        var body = Condition(
            Equal(tokenType, nullToken),
            Constant(true),
            Constant(false));

        var lambda = Lambda<RefStructReaderDelegate>(body, readerParam);
        lambda.PrintCSharp();

        var fs = lambda.CompileSys();
        fs.PrintIL();

        var reader = new MyJsonReader();
        reader.TokenType = MyJsonTokenType.StartObject;

        var a = fs(ref reader);
        t.AreEqual(false, a);

        var ff = lambda.CompileFast();
        ff.PrintIL();

        var b = ff(ref reader);
        t.AreEqual(false, b);
    }
}
#endif