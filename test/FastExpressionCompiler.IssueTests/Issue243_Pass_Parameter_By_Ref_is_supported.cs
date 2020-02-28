using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class Issue243_Pass_Parameter_By_Ref_is_supported
    {
        public static string PassByRef(ref string test)
        {
            return test.ToString();
        }

        public static string PassByValue(string test)
        {
            return test.ToString();
        }

        [Test]
        public void Lambda_Parameter_Passed_Into_Ref_Method()
        {
            var parameter = Parameter(typeof(string));
            var call = Call(GetType().GetMethod(nameof(PassByRef)), parameter);

            var lambda = Lambda<Func<string, string>>(call, parameter);

            var fastCompiled = lambda.CompileFast();

            Assert.NotNull(fastCompiled);
            Assert.AreEqual("test", fastCompiled("test"));
        }

        public delegate string RefDelegate(ref string val);

        [Test]
        public void Lambda_Ref_Parameter_Passed_Into_Ref_Method()
        {
            var parameter = Parameter(typeof(string).MakeByRefType());
            var call = Call(GetType().GetMethod(nameof(PassByRef)), parameter);

            var lambda = Lambda<RefDelegate>(call, parameter);

            var fastCompiled = lambda.CompileFast();

            var data = "test";

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(data, fastCompiled(ref data));
        }

        [Test]
        [Ignore("todo: fixme")] // todo: fixme
        public void Lambda_Ref_Parameter_Passed_Into_Value_Method()
        {
            var parameter = Parameter(typeof(string).MakeByRefType());
            var call = Call(GetType().GetMethod(nameof(PassByValue)), parameter);

            var lambda = Lambda<RefDelegate>(call, parameter);

            var fastCompiled = lambda.CompileFast();

            var data = "test";

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(data, fastCompiled(ref data));
        }

        [Test]
        public void Lambda_Parameter_Passed_Into_Ref_Method_Extra_Assignment()
        {
            var parameter = Parameter(typeof(string));

            var variable = Variable(typeof(string));

            var variables = new[]
            {
                variable
            };

            var body = Block(
                variables,
                Assign(variable, parameter),
                Call(GetType().GetMethod(nameof(PassByRef)), variable)
            );

            var lambda = Lambda<Func<string, string>>(body, parameter);

            var fastCompiled = lambda.CompileFast();

            Assert.NotNull(fastCompiled);
            Assert.AreEqual("test", fastCompiled("test"));
        }
    }
}