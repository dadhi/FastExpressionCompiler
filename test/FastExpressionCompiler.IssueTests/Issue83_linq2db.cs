using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue83_linq2db
    {
        [Test]
        public void String_to_number_conversion_using_convert_with_method()
        {
            var from = typeof(string);
            var to = typeof(int);

            var p = Expression.Parameter(from, "p");

            var body = Expression.Condition(
                Expression.NotEqual(p, Expression.Constant(null, from)),
                Expression.Convert(p, to, to.GetMethod(nameof(int.Parse), new[] { from })),
                Expression.Constant(0));

            var expr = Expression.Lambda<Func<string, int>>(body, p);

            var compiled = expr.CompileFast();

            Assert.AreEqual(10, compiled("10"));
        }

    }
}
