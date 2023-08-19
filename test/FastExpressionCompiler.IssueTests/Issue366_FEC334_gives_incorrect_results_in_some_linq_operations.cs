#if !LIGHT_EXPRESSION

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.Parser;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests;

[TestFixture]
public class Issue366_FEC334_gives_incorrect_results_in_some_linq_operations : ITest
{
    public int Run()
    {
        Test1();
        return 1;
    }

    [Test]
    public void Test1()
    {
        ParameterExpression x = Expression.Parameter(typeof(double), "threshold");
        ParameterExpression y = Expression.Parameter(typeof(List<double>), "mylist");

        var symbols = new[] { x, y };

        Expression body = new ExpressionParser(symbols, "mylist.Where(c => c > threshold).FirstOrDefault()", null, new ParsingConfig()).Parse(typeof(double));

        LambdaExpression e = Expression.Lambda(body, symbols);
        e.PrintCSharp();

        var fs = e.Compile();
        fs.PrintIL();

        var ff = e.CompileFast();
        ff.PrintIL();

        var result1 = fs.DynamicInvoke(new object[]{ 3.0, new List<double>{1.0,2.0,3.0,4.0,5.0 } });
        Assert.AreEqual(4.0, result1);
        var result1_1 = ((Func<double, List<double>, double>)fs)(3.0, new List<double>{1.0,2.0,3.0,4.0,5.0 });
        Assert.AreEqual(4.0, result1_1);

        var result2 = ff.DynamicInvoke(new object[]{ 3.0, new List<double>{1.0,2.0,3.0,4.0,5.0 } });
        Assert.AreEqual(result1, result2);
        var result2_1 = ((Func<double, List<double>, double>)ff)(3.0, new List<double>{1.0,2.0,3.0,4.0,5.0 });
        Assert.AreEqual(result2, result2_1);
    }
}

#endif
