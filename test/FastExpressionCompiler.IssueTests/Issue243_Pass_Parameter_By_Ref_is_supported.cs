using System;
using System.Reflection.Emit;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue243_Pass_Parameter_By_Ref_is_supported : ITest
    {
        public int Run()
        {
            Lambda_Parameter_Passed_Into_Ref_Method();
            Lambda_Ref_Parameter_Passed_Into_Ref_Method();
            Lambda_Ref_Parameter_Passed_Into_Value_Method();
            Lambda_Ref_ValueType_Parameter_Passed_Into_Value_Method();
            Lambda_Parameter_Passed_Into_Ref_Method_Extra_Assignment();
            return 5;
        }

        public static string PassByRef(ref string test)
        {
            return test.ToString();
        }

        public static string PassByValue(string test)
        {
            return test.ToString();
        }

        public static string PassValueTypeByValue(int test)
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

        public delegate string RefValueTypeDelegate(ref int val);

        [Test]
        public void Lambda_Ref_Parameter_Passed_Into_Ref_Method()
        {
            var parameter = Parameter(typeof(string).MakeByRefType());
            var call = Call(GetType().GetMethod(nameof(PassByRef)), parameter);

            var lambda = Lambda<RefDelegate>(call, parameter);

            var fastCompiled = lambda.CompileFast(ifFastFailedReturnNull: true);

            var data = "test";

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(data, fastCompiled(ref data));
        }

        [Test]
        public void Lambda_Ref_Parameter_Passed_Into_Value_Method()
        {
            var parameter = Parameter(typeof(string).MakeByRefType());
            var call = Call(GetType().GetMethod(nameof(PassByValue)), parameter);

            var lambda = Lambda<RefDelegate>(call, parameter);

            var fastCompiled = lambda.CompileFast(ifFastFailedReturnNull: true);
            Assert.NotNull(fastCompiled);

            fastCompiled.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_Ref,
                OpCodes.Call,
                OpCodes.Ret);

            var data = "test";
            Assert.AreEqual(data, fastCompiled(ref data));
        }

        [Test]
        public void Lambda_Ref_ValueType_Parameter_Passed_Into_Value_Method()
        {
            var parameter = Parameter(typeof(int).MakeByRefType());
            var call = Call(GetType().GetMethod(nameof(PassValueTypeByValue)), parameter);

            var lambda = Lambda<RefValueTypeDelegate>(call, parameter);

            var fastCompiled = lambda.CompileFast(ifFastFailedReturnNull: true);
            Assert.NotNull(fastCompiled);

            fastCompiled.Method.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_I4,
                OpCodes.Call,
                OpCodes.Ret);

            var data = 42;
            Assert.AreEqual(data.ToString(), fastCompiled(ref data));
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

            var fastCompiled = lambda.CompileFast(ifFastFailedReturnNull: true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual("test", fastCompiled("test"));
        }
    }
}