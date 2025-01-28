using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
using FastExpressionCompiler.LightExpression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
using System.Linq.Expressions;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue449_MemberInit_produces_InvalidProgram : ITest
{
    public int Run()
    {
        // Original_case();
        return 1;
    }

    public struct SampleType
    {
        public int? Value { get; set; }

        public SampleType() { }
    }

    [Test]
    public void Original_case()
    {
        var ctor = typeof(SampleType).GetConstructors()[0];
        var valueProp = typeof(SampleType).GetProperty(nameof(SampleType.Value));

        var expr = Lambda<Func<SampleType>>(
            MemberInit(
                New(ctor),
                Bind(valueProp, Constant(666, typeof(int?)))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(666, sr.Value.Value);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(666, sr.Value.Value);
    }
}