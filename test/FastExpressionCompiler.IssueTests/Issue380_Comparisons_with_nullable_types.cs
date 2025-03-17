using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue380_Comparisons_with_nullable_types : ITest
{
    public int Run()
    {
        Test_left_decimal_Nullable_constant();
        Test_left_decimal_constant();
        Test_right_decimal_constant();
        return 3;
    }

    public class DecimalTest
    {
        public decimal? D1 { get; set; }
    }

    [Test]
    public void Test_left_decimal_Nullable_constant()
    {
#if LIGHT_EXPRESSION
        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[5]; // the unique expressions
        var expr = Lambda<Func<DecimalTest, bool>>(
        e[0] = MakeBinary(ExpressionType.GreaterThan,
            e[3] = Constant(20m, typeof(decimal?)),
            e[4] = Property(
                p[0] = Parameter(typeof(DecimalTest), "t"),
                typeof(DecimalTest).GetTypeInfo().GetDeclaredProperty("D1")),
            liftToNull: false,
            typeof(Decimal).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_GreaterThan" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Decimal), typeof(Decimal) }))),
        p[0 // (DecimalTest t)
            ]);
#else
        Expression<Func<DecimalTest, bool>> expr = t => 20m > t.D1;
#endif

        expr.PrintCSharp();
        expr.PrintExpression();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var d = new DecimalTest { D1 = null };
        var r = fs(d);
        Asserts.IsFalse(r);

        var ff = expr.CompileFast(true);
        ff.PrintIL();

        r = ff(d);
        Asserts.IsFalse(r);

        r = ff(new DecimalTest { D1 = 19m });
        Asserts.IsTrue(r);
    }

    [Test]
    public void Test_left_decimal_constant()
    {
#if LIGHT_EXPRESSION
        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[5]; // the unique expressions
        var expr = Lambda<Func<DecimalTest, bool>>(
        e[0] = MakeBinary(ExpressionType.GreaterThan,
            e[1] = Convert(
                e[2] = Convert(
                    e[3] = Constant(20),
                    typeof(Decimal),
                    typeof(Decimal).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_Implicit" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) }))),
                typeof(Decimal?)),
            e[4] = Property(
                p[0] = Parameter(typeof(DecimalTest), "t"),
                typeof(DecimalTest).GetTypeInfo().GetDeclaredProperty("D1")),
            liftToNull: false,
            typeof(Decimal).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_GreaterThan" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Decimal), typeof(Decimal) }))),
        p[0 // (DecimalTest t)
            ]);
#else
        Expression<Func<DecimalTest, bool>> expr = t => 20 > t.D1;
#endif

        expr.PrintCSharp();
        expr.PrintExpression();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var d = new DecimalTest { D1 = null };
        var r = fs(d);
        Asserts.IsFalse(r);

        var ff = expr.CompileFast(true);
        ff.PrintIL();

        r = ff(d);
        Asserts.IsFalse(r);

        r = ff(new DecimalTest { D1 = 19m });
        Asserts.IsTrue(r);
    }

    [Test]
    public void Test_right_decimal_constant()
    {
#if LIGHT_EXPRESSION
        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[5]; // the unique expressions
        var expr = Lambda<Func<DecimalTest, bool>>(
        e[0] = MakeBinary(ExpressionType.LessThan,
            e[1] = Property(
                p[0] = Parameter(typeof(DecimalTest), "t"),
                typeof(DecimalTest).GetTypeInfo().GetDeclaredProperty("D1")),
            e[2] = Convert(
                e[3] = Convert(
                    e[4] = Constant(20),
                    typeof(Decimal),
                    typeof(Decimal).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_Implicit" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) }))),
                typeof(Decimal?)),
            liftToNull: false,
            typeof(Decimal).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_LessThan" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Decimal), typeof(Decimal) }))),
        p[0 // (DecimalTest t)
            ]);
#else
        Expression<Func<DecimalTest, bool>> expr = t => t.D1 < 20;
#endif

        expr.PrintCSharp();
        expr.PrintExpression();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var d = new DecimalTest { D1 = null };
        var r = fs(d);
        Asserts.IsFalse(r);

        var ff = expr.CompileFast(true);
        ff.PrintIL();

        r = ff(d);
        Asserts.IsFalse(r);

        r = ff(new DecimalTest { D1 = 19m });
        Asserts.IsTrue(r);
    }
}
