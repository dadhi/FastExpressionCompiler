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
            Lambda_Ref_Parameter_Passed_Into_Static_Value_Method();
            Lambda_Ref_Parameter_Passed_Into_Instance_Value_Method();
            Lambda_Ref_Parameter_Passed_Into_Struct_Instance_Value_Method();
            Lambda_Ref_ValueType_Parameter_Passed_Into_Value_Method();
            Lambda_Parameter_Passed_Into_Ref_Method_Extra_Assignment();
            return 7;
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
            Asserts.AreEqual("test", fastCompiled("test"));
        }

        public delegate string RefDelegate(ref string val);

        public delegate string RefInstanceDelegate<T, P>(ref T instance, ref P val);

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
            Asserts.AreEqual(data, fastCompiled(ref data));
        }

        [Test]
        public void Lambda_Ref_Parameter_Passed_Into_Static_Value_Method()
        {
            var parameter = Parameter(typeof(string).MakeByRefType());

            var lambda = Lambda<RefDelegate>(
                Call(GetType().GetMethod(nameof(PassByValue)), parameter),
                parameter);

            lambda.PrintCSharp();

            var systCompiled = lambda.CompileSys();
            systCompiled.PrintIL();
            var a = "test";
            Asserts.AreEqual(a, systCompiled(ref a));

            var fastCompiled = lambda.CompileFast(ifFastFailedReturnNull: true);
            Assert.NotNull(fastCompiled);

            fastCompiled.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_Ref,
                OpCodes.Call,
                OpCodes.Ret);

            var b = "test";
            Asserts.AreEqual(b, fastCompiled(ref b));
        }

        public class PassedByRefClass
        {
            public string PassByRef(ref string test) => test.ToString();

            public string PassByValue(string test) => test.ToString();
        }

        [Test]
        public void Lambda_Ref_Parameter_Passed_Into_Instance_Value_Method()
        {
            var i = Parameter(typeof(PassedByRefClass).MakeByRefType());
            var s = Parameter(typeof(string).MakeByRefType());

            var lambda = Lambda<RefInstanceDelegate<PassedByRefClass, string>>(
                Call(i, typeof(PassedByRefClass).GetMethod(nameof(PassByValue)), s),
                i, s);

            lambda.PrintCSharp();

            var cls = new PassedByRefClass();

            var systCompiled = lambda.CompileSys();
            systCompiled.PrintIL();
            var a = "test";
            Asserts.AreEqual(a, systCompiled(ref cls, ref a));

            var fastCompiled = lambda.CompileFast(ifFastFailedReturnNull: true);
            Assert.NotNull(fastCompiled);

            fastCompiled.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_Ref,
                OpCodes.Ldarg_2,
                OpCodes.Ldind_Ref,
                OpCodes.Call,
                OpCodes.Ret);

            var b = "test";
            Asserts.AreEqual(b, fastCompiled(ref cls, ref b));
        }

        public struct PassedByRefStruct
        {
            public string PassByRef(ref string test) => test.ToString();

            public string PassByValue(int test) => test.ToString();
        }

        [Test]
        public void Lambda_Ref_Parameter_Passed_Into_Struct_Instance_Value_Method()
        {
            var i = Parameter(typeof(PassedByRefStruct).MakeByRefType());
            var s = Parameter(typeof(int).MakeByRefType());

            var lambda = Lambda<RefInstanceDelegate<PassedByRefStruct, int>>(
                Call(i, typeof(PassedByRefStruct).GetMethod(nameof(PassByValue)), s),
                i, s);

            lambda.PrintCSharp();

            var cls = new PassedByRefStruct();

            var systCompiled = lambda.CompileSys();
            systCompiled.PrintIL();
            var a = 1;
            Asserts.AreEqual("1", systCompiled(ref cls, ref a));

            var fastCompiled = lambda.CompileFast(ifFastFailedReturnNull: true);
            Assert.NotNull(fastCompiled);
            fastCompiled.PrintIL();

            fastCompiled.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldind_I4,
                OpCodes.Call,
                OpCodes.Ret);

            var b = 2;
            Asserts.AreEqual("2", fastCompiled(ref cls, ref b));
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
            Asserts.AreEqual(data.ToString(), fastCompiled(ref data));
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
            Asserts.AreEqual("test", fastCompiled("test"));
        }
    }
}