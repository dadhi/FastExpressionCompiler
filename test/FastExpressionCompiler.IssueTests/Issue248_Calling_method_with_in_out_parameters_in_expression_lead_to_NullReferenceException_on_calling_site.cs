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
            return 1;
        }

        [Test]
        public void Test_1()
        {
            var serializer = Parameter(typeof(ISerializer), "serializer");
            var method = typeof(ISerializer).GetMethod("WriteDecimal");
            var data = Parameter(typeof(Test).MakeByRefType(), "data");
            var field = Field(data, typeof(Test).GetField("Field1"));
            var call = Call(serializer, method, field);
            var expr = Lambda<SerializerDelegate>(call, serializer, data);

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
    /*
    Expected IL:
            IL_0000: ldarg.1
            IL_0001: ldarg.2
            IL_0002: ldflda valuetype [System.Private.CoreLib]System.Decimal C/Test::Field1
            IL_0007: callvirt instance void C/ISerializer::WriteDecimal(valuetype [System.Private.CoreLib]System.Decimal& modreq([System.Private.CoreLib]System.Runtime.InteropServices.InAttribute))
            IL_000c: ret
    */

            var test = new Test { Field1 = 35m };
            serialize(new MySerializer(), ref test); // does nothing
            Assert.AreEqual(35m, test.Field1);
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
        }

        public delegate void SerializerDelegate(ISerializer serializer, ref Test data);
    }
}