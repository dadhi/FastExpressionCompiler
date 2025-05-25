using System;
using System.Linq;
using System.Collections.Generic;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public class Issue341_Equality_comparison_between_nullable_and_null_inside_Any_produces_incorrect_compiled_expression : ITest
{
    public int Run()
    {
        foreach (var (a, op, b, expected) in Data)
            Nullable_decimal_parameters_comparison_cases(a, op, b, expected);

        foreach (var (a, op, b, expected) in Data)
            Nullable_decimal_parameter_with_decimal_constant_comparison_cases(a, op, b, expected);

        Nullable_decimal_not_equal_to_zero();
        Nullable_decimal_greater_than_zero();
        Nullable_decimal_not_equal_decimal();
        Nullable_decimal_less_then_decimal();
        Nullable_decimal_not_equal_to_null();
        Null_not_equal_to_nullable_decimal();
        Nullable_decimal_equal_to_null();
        Nullable_decimal_member_not_equal_to_null();
        Nullable_decimal_member_not_equal_to_null_inside_predicate();

        return Data.Length * 2 + 9;
    }

    public enum Ops { Equal, NotEqual, Greater, Less, GreaterOrEqual, LessOrEqual }

    public static System.Linq.Expressions.Expression<Func<decimal?, decimal?, bool>>[] sysTwoParamsExpressions =
    {
        (a, b) => a == b,
        (a, b) => a != b,
        (a, b) => a >  b,
        (a, b) => a <  b,
        (a, b) => a >= b,
        (a, b) => a <= b,
    };

    public static Expression<Func<decimal?, decimal?, bool>>[] twoParamsExpressions =
        sysTwoParamsExpressions.Select(e => e.FromSysExpression()).ToArray();

    public static ParameterExpression aParam = Parameter(typeof(decimal?), "a");
    public static Func<decimal?, Expression<Func<decimal?, bool>>>[] oneParamExpressions =
    {
        b => Lambda<Func<decimal?, bool>>(Equal(aParam, Constant(b, typeof(decimal?))), aParam),
        b => Lambda<Func<decimal?, bool>>(NotEqual(aParam, Constant(b, typeof(decimal?))), aParam),
        b => Lambda<Func<decimal?, bool>>(GreaterThan(aParam, Constant(b, typeof(decimal?))), aParam),
        b => Lambda<Func<decimal?, bool>>(LessThan(aParam, Constant(b, typeof(decimal?))), aParam),
        b => Lambda<Func<decimal?, bool>>(GreaterThanOrEqual(aParam, Constant(b, typeof(decimal?))), aParam),
        b => Lambda<Func<decimal?, bool>>(LessThanOrEqual(aParam, Constant(b, typeof(decimal?))), aParam),
    };

    public static readonly (decimal?, Ops, decimal?, bool)[] Data =
    {
        (0M, Ops.Equal, 0M, true),
        (0M, Ops.NotEqual, 0M, false),
        (1.12M, Ops.Greater, 1.11M, true),
        (1.12M, Ops.GreaterOrEqual, 1.11M, true),
        (1.12M, Ops.LessOrEqual, 1.11M, false),
        (1.101M, Ops.Less, 1.11M, true),
        (1.101M, Ops.LessOrEqual, 1.11M, true),
        (1.101M, Ops.Greater, 1.11M, false),
        (1.142M, Ops.NotEqual, null, true),
        (1.142M, Ops.Equal, null, false),
        (null, Ops.NotEqual, 1.366M, true),
        (null, Ops.Equal, 1.366M, false),
        (null, Ops.Equal, null, true),
        (null, Ops.NotEqual, null, false),
    };

    public record struct TestCaseData(decimal? a, Ops op, decimal? b, bool expected);
    public static readonly IEnumerable<TestCaseData> TestCases = Data.Select(x => new TestCaseData(x.Item1, x.Item2, x.Item3, x.Item4));

    public void Nullable_decimal_parameters_comparison_cases(decimal? a, Ops op, decimal? b, bool expected)
    {
        var expression = oneParamExpressions[(int)op](b);
#if DEBUG
        Console.WriteLine($"params: ({(a, op, b, expected)})");
#endif
        expression.PrintCSharp();

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(a);
        Asserts.AreEqual(expected, result);

        result = compiledFast(a);
        Asserts.AreEqual(expected, result);
    }

    public void Nullable_decimal_parameter_with_decimal_constant_comparison_cases(decimal? a, Ops op, decimal? b, bool expected)
    {
        var expression = twoParamsExpressions[(int)op];
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(a, b);
        Asserts.AreEqual(expected, result);

        result = compiledFast(a, b);
        Asserts.AreEqual(expected, result);
    }


    public void Nullable_decimal_not_equal_to_zero()
    {
        System.Linq.Expressions.Expression<Func<decimal?, bool>> sExpression = n => n != 0M;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = default(decimal);

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsFalse(result);

        result = compiledFast(instance);
        Asserts.IsFalse(result);
    }


    public void Nullable_decimal_greater_than_zero()
    {
        System.Linq.Expressions.Expression<Func<decimal?, bool>> sExpression = n => n > 0M;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = default(decimal);

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsFalse(result);

        result = compiledFast(instance);
        Asserts.IsFalse(result);
    }


    public void Nullable_decimal_not_equal_decimal()
    {
        System.Linq.Expressions.Expression<Func<decimal?, bool>> sExpression = n => n != 1.11M;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = 1.111M;

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsTrue(result);

        result = compiledFast(instance);
        Asserts.IsTrue(result);
    }


    public void Nullable_decimal_less_then_decimal()
    {
        System.Linq.Expressions.Expression<Func<decimal?, bool>> sExpression = n => n < 1.11M;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = 1.101M;

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsTrue(result);

        result = compiledFast(instance);
        Asserts.IsTrue(result);
    }


    public void Nullable_decimal_not_equal_to_null()
    {
        System.Linq.Expressions.Expression<Func<decimal?, bool>> sExpression = n => n != null;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = default(decimal);

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsTrue(result);

        result = compiledFast(instance);
        Asserts.IsTrue(result);
    }


    public void Null_not_equal_to_nullable_decimal()
    {
        System.Linq.Expressions.Expression<Func<decimal?, bool>> sExpression = n => null != n;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = default(decimal);

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsTrue(result);

        result = compiledFast(instance);
        Asserts.IsTrue(result);
    }


    public void Nullable_decimal_equal_to_null()
    {
        System.Linq.Expressions.Expression<Func<decimal?, bool>> sExpression = n => n == null;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = default(decimal);

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsFalse(result);

        result = compiledFast(instance);
        Asserts.IsFalse(result);
    }


    public void Nullable_decimal_member_not_equal_to_null()
    {
        // todo: @perf optimize comparison of nullable with null
        System.Linq.Expressions.Expression<Func<Test2, bool>> sExpression = t => t.Value != null;
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        var compiledFast = expression.CompileFast(true);

        var instance = new Test2() { Value = 0 };

        compiledSys.PrintIL("sys");
        compiledFast.PrintIL("fast");

        var result = compiledSys(instance);
        Asserts.IsTrue(result);

        result = compiledFast(instance);
        Asserts.IsTrue(result);
    }


    public void Nullable_decimal_member_not_equal_to_null_inside_predicate()
    {
        System.Linq.Expressions.Expression<Func<Test, bool>> sExpression = t => t.A.Any(e => e.Value != null);
        var expression = sExpression.FromSysExpression();
        expression.PrintCSharp(); // just for debug

        var compiledSys = expression.CompileSys();
        compiledSys.PrintIL("sys");

        var f = expression.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
        Asserts.IsNotNull(f);

        f.PrintIL("fast");

        var dis = f.TryGetDebugInfo();
        foreach (var di in dis.EnumerateNestedLambdas())
        {
            di.PrintIL("predicate");
        }

        var instance = new Test()
        {
            A = new[]
            {
                new Test2() { Value = 0 },
            },
        };

        var result = compiledSys(instance);
        Asserts.IsTrue(result);

        result = f(instance);
        Asserts.IsTrue(result);
    }

    public class Test
    {
        public Test2[] A { get; set; }
    }

    public class Test2
    {
        public decimal? Value { get; set; }
    }
}