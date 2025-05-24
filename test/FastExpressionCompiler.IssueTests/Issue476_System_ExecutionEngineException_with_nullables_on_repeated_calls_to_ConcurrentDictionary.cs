using System;
using System.Collections.Concurrent;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue476_System_ExecutionEngineException_with_nullables_on_repeated_calls_to_ConcurrentDictionary : ITestX
{
    public void Run(TestRun t)
    {
        Original_case(t);
    }

    public class Record
    {
        public DateTimeOffset? Timestamp { get; set; }
    }

    public void Original_case(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<Record, bool>> sExpr = record => record.Timestamp != null;
        var expr = sExpr.FromSysExpression();

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var currTime = DateTimeOffset.UtcNow;
        var notNull = new Record() { Timestamp = currTime };
        var aNull = new Record() { Timestamp = null };

        t.IsTrue(fs(notNull));
        t.IsFalse(fs(aNull));

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        t.IsTrue(ff(notNull));
        t.IsFalse(ff(aNull));
    }
}