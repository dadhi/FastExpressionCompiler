using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue421_Date_difference_is_giving_wrong_negative_value : ITest
{
    public int Run()
    {
        Original_case_2();
        Original_case_1();
        return 2;
    }

    public class Contract
    {
        public readonly DateTime StartDate = new DateTime(2024, 1, 1);
    }

    [Test]
    public void Original_case_1()
    {
        var contract = new Contract();
        System.Linq.Expressions.Expression<Func<double>> se = () => (DateTime.Now - contract.StartDate).TotalDays;
        var e = se.FromSysExpression();

        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();

        var ff = e.CompileFast(true);
        ff.PrintIL();

        Assert.GreaterOrEqual(fs(), 250);
        Assert.GreaterOrEqual(ff(), 250);
    }

    [Test]
    public void Original_case_2()
    {
        var contract = new Contract();
        System.Linq.Expressions.Expression<Func<double>> se = () => (DateTime.Now.Date - contract.StartDate.Date).TotalDays;
        var e = se.FromSysExpression();

        e.PrintCSharp();

        var fs = e.CompileSys();
        fs.PrintIL();

        var ff = e.CompileFast(true);
        ff.PrintIL();

        Assert.GreaterOrEqual(fs(), 250);
        Assert.GreaterOrEqual(ff(), 250);
    }
}
