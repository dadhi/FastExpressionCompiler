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
    public class Issue248_Calling_method_with_in_out_parameters_in_expression_lead_to_NullReferenceException_on_calling_site : ITest
    {
        public int Run()
        {
            Test_1();
            Test_2();
            return 2;
        }

        [Test]
        public void Test_1()
        {
            var serializer = Parameter(typeof(ISerializer), "serializer");
            var data       = Parameter(typeof(Test).MakeByRefType(), "data");

            var expr = Lambda<SerializerDelegate>(
                Call(serializer, typeof(ISerializer).GetMethod(nameof(ISerializer.WriteDecimal)), 
                Field(data, typeof(Test).GetField(nameof(Test.Field1)))), 
                serializer, data);

            expr.PrintCSharpString();

            var serialize = expr.CompileFast(true);
            Assert.IsNotNull(serialize);
            serialize.PrintIL();

            serialize.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldflda,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var test = new Test { Field1 = 35m };
            serialize(new MySerializer(), ref test); // does nothing
            Assert.AreEqual(35m, test.Field1);
        }

        [Test]
        public void Test_2()
        {
            var serializer = Parameter(typeof(ISerializer), "serializer");
            var data       = Parameter(typeof(Test).MakeByRefType(), "data");

            var expr = Lambda<SerializerDelegate>(
                Call(serializer, typeof(ISerializer).GetMethod(nameof(ISerializer.WriteDecimal)),
                    Field(
                        Field(data, typeof(Test).GetField(nameof(Test.NestedTest))), 
                        typeof(NestedTest).GetField(nameof(NestedTest.Field1)))),
                serializer, data);

            expr.PrintCSharpString();

            var serialize = expr.CompileFast(true);
            Assert.IsNotNull(serialize);
            serialize.PrintIL();

            serialize.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldflda,
                OpCodes.Ldflda,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var test = new Test { NestedTest = { Field1 = 35m }};
            serialize(new MySerializer(), ref test); // does nothing
            Assert.AreEqual(35m, test.NestedTest.Field1);
        }

        public interface ISerializer
        {
            void WriteDecimal(in decimal value);
        }

        class MySerializer : ISerializer
        {
            public void WriteDecimal(in decimal value) { 
                Assert.AreEqual(35m, value);
            }
        }

        public struct Test
        {
            public decimal Field1;
            public NestedTest NestedTest;
        }

        public struct NestedTest
        {
            public decimal Field1;
        }

        public delegate void SerializerDelegate(ISerializer serializer, ref Test data);
    }
}