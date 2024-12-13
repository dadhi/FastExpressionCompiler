using System;
using System.Linq.Expressions;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

[TestFixture]
public class Issue439_Support_unused_Field_access_in_Block : ITest
{
    public int Run()
    {
        Original_case();
        return 1;
    }

    public class TestClass
    {
        public int Result0 = 42;
        public int Result1;
    }

    [Test]
    public void Original_case()
    {
        var variable = Variable(typeof(TestClass), "testClass");

        var block = Block(
            new[] { variable },
            Assign(
                variable,
                New(typeof(TestClass))
            ),
            Block(
                Field(variable, nameof(TestClass.Result0)), // Unused
                Assign(
                    Field(variable, nameof(TestClass.Result1)),
                    Field(variable, nameof(TestClass.Result0))
                )
            ),
            Field(variable, nameof(TestClass.Result1))
        );

        var expr = Lambda<Func<int>>(block);
        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();

        var sr = fs();
        Assert.AreEqual(42, sr);

        var ff = expr.CompileFast(false);
        ff.PrintIL();

        var fr = ff();
        Assert.AreEqual(42, fr);
    }
}