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
        Original_case();
        Struct_without_ctor_case();
        return 2;
    }

    public struct SampleType
    {
        public int? Value { get; set; }

        public SampleType() { }
    }

    public struct SampleType_NoCtor
    {
        public int? Value { get; set; }
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
        Asserts.AreEqual(666, sr.Value.Value);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(666, sr.Value.Value);
    }

    [Test]
    public void Struct_without_ctor_case()
    {
        var valueProp = typeof(SampleType_NoCtor).GetProperty(nameof(SampleType_NoCtor.Value));

        var expr = Lambda<Func<SampleType_NoCtor>>(
            MemberInit(
                New(typeof(SampleType_NoCtor)),
                Bind(valueProp, Constant(666, typeof(int?)))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Asserts.AreEqual(666, sr.Value.Value);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Asserts.AreEqual(666, sr.Value.Value);
    }
}