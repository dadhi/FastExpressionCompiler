using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

#pragma warning disable IDE1006 // Naming Styles for linq2db
#pragma warning disable 649 // Unassigned fields

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
[TestFixture]
    public class Issue147_int_try_parse : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        class MyObject
        {
            public bool a<b>(b i)
            {
                return Equals(i, false);
            }
        }

        [Test]
        public void Test1()
        {
            var intValueParameter = Parameter(typeof(int), "intValue");

            var tryParseMethod = typeof(int)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "TryParse" && m.GetParameters().Length == 2);

            var tryParseCall = Call(
                tryParseMethod,
                Constant("123", typeof(string)),
                intValueParameter);

            var parsedValueOrDefault = Condition(
                tryParseCall,
                intValueParameter,
                Default(typeof(int)));

            var conditionBlock = Block(new[] { intValueParameter }, parsedValueOrDefault);

            var conditionLambda = Lambda<Func<int>>(conditionBlock);

            var conditionFunc = conditionLambda.CompileSys();
            var parsedValue = conditionFunc.Invoke();

            var conditionFuncFast = conditionLambda.CompileFast(true);
            var parsedValueFast = conditionFuncFast.Invoke();

            Assert.AreEqual(parsedValue, parsedValueFast);
        }
    }
}
