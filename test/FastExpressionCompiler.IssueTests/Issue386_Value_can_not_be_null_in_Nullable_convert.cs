using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue386_Value_can_not_be_null_in_Nullable_convert : ITest
{
    public int Run()
    {
        Test_non_nullable_UserType_nullable_type();
        Test_non_nullable_UserType_non_nullable_type();
        Test_nullable_type();
        Test_non_nullable_type();
        return 4;
    }

    public enum UserType { Default, Foo, Bar }

    public class Message
    {
        public UserType? UserType { get; set; }
    }

    public class MessageSpec
    {
        public UserType type;
    }

    [Test]
    public void Test_non_nullable_type()
    {
        var spec = new MessageSpec { type = UserType.Foo };

        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[6]; // the unique expressions
        var expr = Lambda<Func<Message, bool>>(
        e[0] = MakeBinary(ExpressionType.NotEqual,
            e[1] = Convert(
            e[2] = Property(
                p[0] = Parameter(typeof(Message), "x"),
                typeof(Message).GetProperty("UserType")),
            typeof(int?)),
            e[3] = Convert(
            e[4] = Field(
                e[5] = Constant(spec),
                typeof(MessageSpec).GetField("type")),
            typeof(int?))),
        p[0 // (Message x)
            ]);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        var r = fs(new Message { UserType = UserType.Foo });
        Assert.IsFalse(r);

        var ff = expr.CompileFast(true);
        ff.PrintIL();
        r = ff(new Message { UserType = UserType.Foo });
        Assert.IsFalse(r);
    }

    public class MessageSpec_NullableType
    {
        public UserType? type;
    }

    [Test]
    public void Test_nullable_type()
    {
        var spec = new MessageSpec_NullableType { type = null };

        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[6]; // the unique expressions
        var expr = Lambda<Func<Message, bool>>(
        e[0] = MakeBinary(ExpressionType.NotEqual,
            e[1] = Convert(
            e[2] = Property(
                p[0] = Parameter(typeof(Message), "x"),
                typeof(Message).GetProperty("UserType")),
            typeof(int?)),
            e[3] = Convert(
            e[4] = Field(
                e[5] = Constant(spec),
                typeof(MessageSpec_NullableType).GetField("type")),
            typeof(int?))),
        p[0 // (Message x)
            ]);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        var r = fs(new Message { UserType = UserType.Foo });
        Assert.IsTrue(r);

        var ff = expr.CompileFast(true);
        ff.PrintIL();
        r = ff(new Message { UserType = UserType.Foo });
        Assert.IsTrue(r);
    }

    public class Message_non_nullable
    {
        public UserType UserType { get; set; }
    }

    [Test]
    public void Test_non_nullable_UserType_non_nullable_type()
    {
        var spec = new MessageSpec { type = UserType.Foo };

        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[6]; // the unique expressions
        var expr = Lambda<Func<Message_non_nullable, bool>>(
        e[0] = MakeBinary(ExpressionType.NotEqual,
            e[1] = Convert(
            e[2] = Property(
                p[0] = Parameter(typeof(Message_non_nullable), "x"),
                typeof(Message_non_nullable).GetProperty("UserType")),
            typeof(int?)),
            e[3] = Convert(
            e[4] = Field(
                e[5] = Constant(spec),
                typeof(MessageSpec).GetField("type")),
            typeof(int?))),
        p[0 // (Message x)
            ]);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        var r = fs(new Message_non_nullable { UserType = UserType.Foo });
        Assert.IsFalse(r);

        var ff = expr.CompileFast(true);
        ff.PrintIL();
        r = ff(new Message_non_nullable { UserType = UserType.Foo });
        Assert.IsFalse(r);
    }

    [Test]
    public void Test_non_nullable_UserType_nullable_type()
    {
        var spec = new MessageSpec_NullableType { type = null };

        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[6]; // the unique expressions
        var expr = Lambda<Func<Message_non_nullable, bool>>(
        e[0] = MakeBinary(ExpressionType.NotEqual,
            e[1] = Convert(
            e[2] = Property(
                p[0] = Parameter(typeof(Message_non_nullable), "x"),
                typeof(Message_non_nullable).GetProperty("UserType")),
            typeof(int?)),
            e[3] = Convert(
            e[4] = Field(
                e[5] = Constant(spec),
                typeof(MessageSpec_NullableType).GetField("type")),
            typeof(int?))),
        p[0 // (Message x)
            ]);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        var r = fs(new Message_non_nullable { UserType = UserType.Foo });
        Assert.IsTrue(r);

        var ff = expr.CompileFast(true);
        ff.PrintIL();
        r = ff(new Message_non_nullable { UserType = UserType.Foo });
        Assert.IsTrue(r);
    }
}
