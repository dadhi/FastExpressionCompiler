using System;
using System.Linq;
using System.Linq.Expressions;


#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif


public class Issue420_Nullable_DateTime_comparison_differs_from_Expression_Compile : ITest
{
    public int Run()
    {
        Original_case();
        return 1;
    }

    public class HasDateTime
    {
        public DateTime? T { get; }
        public HasDateTime(DateTime? t) => T = t;
    }


    public void Original_case()
    {
        var time = DateTime.UtcNow;

        var p = new ParameterExpression[1]; // the parameter expressions
        var e = new Expression[3]; // the unique expressions
        var expr = Lambda<Func<HasDateTime, bool>>(
            e[0] = MakeBinary(ExpressionType.Equal,
                e[1] = Property(
                    p[0] = Parameter(typeof(HasDateTime), nameof(HasDateTime)),
                    typeof(HasDateTime).GetProperty(nameof(HasDateTime.T))),
                e[2] = Constant(time, typeof(DateTime?)),
                liftToNull: false,
                typeof(DateTime).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "op_Equality" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(System.DateTime), typeof(System.DateTime) }))),
            p[0 // (HasDateTime)
            ]);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
        ff.PrintIL();

        var hasDT = new HasDateTime(time);
        Asserts.IsTrue(fs(hasDT));
        Asserts.IsTrue(ff(hasDT));
    }
}
