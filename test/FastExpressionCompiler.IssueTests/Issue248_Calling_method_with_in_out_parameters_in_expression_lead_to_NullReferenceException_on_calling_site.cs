using System.Reflection.Emit;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue248_Calling_method_with_in_out_parameters_in_expression_lead_to_NullReferenceException_on_calling_site : ITest
    {
        public int Run()
        {
            Test_1();
            Test_2();
            Test_3();
            Test_4();
            return 4;
        }


        public void Test_1()
        {
            var serializer = Parameter(typeof(ISerializer), "serializer");
            var data = Parameter(typeof(Test).MakeByRefType(), "data");

            var expr = Lambda<SerializerDelegate>(
                Call(serializer, typeof(ISerializer).GetMethod(nameof(ISerializer.WriteDecimal)),
                    Field(data, typeof(Test).GetField(nameof(Test.Field1)))),
                serializer, data);

            expr.PrintCSharp();

            var serialize = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Asserts.IsNotNull(serialize);
            serialize.PrintIL();

            serialize.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldflda,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var test = new Test { Field1 = 35m };
            serialize(new MySerializer(), ref test); // does nothing
            Asserts.AreEqual(35m, test.Field1);
        }


        public void Test_2()
        {
            var serializer = Parameter(typeof(ISerializer), "serializer");
            var data = Parameter(typeof(Test).MakeByRefType(), "data");

            var expr = Lambda<SerializerDelegate>(
                Call(serializer, typeof(ISerializer).GetMethod(nameof(ISerializer.WriteDecimal)),
                    Field(
                        Field(data, typeof(Test).GetField(nameof(Test.NestedTest))),
                        typeof(NestedTest).GetField(nameof(NestedTest.Field1)))),
                serializer, data);

            expr.PrintCSharp();

            var serializeSys = expr.CompileSys();
            serializeSys.PrintIL();
            serializeSys.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldflda,
                OpCodes.Ldflda,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var serialize = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Asserts.IsNotNull(serialize);
            serialize.PrintIL();

            serialize.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldflda,
                OpCodes.Ldflda,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var test = new Test { NestedTest = { Field1 = 35m } };
            serialize(new MySerializer(), ref test); // does nothing
            Asserts.AreEqual(35m, test.NestedTest.Field1);
        }


        public void Test_3()
        {
            var serializer = Parameter(typeof(ISerializer), "serializer");
            var data = Parameter(typeof(Test).MakeByRefType(), "data");

            var expr = Lambda<SerializerDelegate>(
                Call(serializer, typeof(ISerializer).GetMethod(nameof(ISerializer.WriteDecimalByVal)),
                    Field(data, typeof(Test).GetField(nameof(Test.Field1)))),
                serializer, data);

            expr.PrintCSharp();

            var serializeSys = expr.CompileSys();
            serializeSys.PrintIL();
            serializeSys.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldfld,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var serialize = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Asserts.IsNotNull(serialize);
            serialize.PrintIL();

            serialize.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldfld,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var test = new Test { Field1 = 35m };
            serialize(new MySerializer(), ref test); // does nothing
            Asserts.AreEqual(35m, test.Field1);
        }


        public void Test_4()
        {
            var serializer = Parameter(typeof(ISerializer), "serializer");
            var data = Parameter(typeof(Test).MakeByRefType(), "data");

            var expr = Lambda<SerializerDelegate>(
                Call(serializer, typeof(ISerializer).GetMethod(nameof(ISerializer.WriteDecimalTwoArgs)),
                    Field(data, typeof(Test).GetField(nameof(Test.Field1))),
                    Field(New(typeof(Klaz).GetConstructors()[0], Constant(true), Constant(0.3f)),
                        typeof(Klaz).GetField(nameof(Klaz.KlazField)))
                ), serializer, data);

            expr.PrintCSharp();

            var serializeSys = expr.CompileSys();
            serializeSys.PrintIL();
            serializeSys.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldflda,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_R4,
                OpCodes.Newobj,
                OpCodes.Ldfld,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var serialize = expr.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Asserts.IsNotNull(serialize);
            serialize.PrintIL();

            serialize.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldarg_2,
                OpCodes.Ldflda,
                OpCodes.Ldc_I4_1,
                OpCodes.Ldc_R4,
                OpCodes.Newobj,
                OpCodes.Ldfld,
                OpCodes.Callvirt,
                OpCodes.Ret);

            var test = new Test { Field1 = 35m };
            serialize(new MySerializer(), ref test); // does nothing
            Asserts.AreEqual(35m, test.Field1);
        }

        public interface ISerializer
        {
            void WriteDecimal(in decimal value);
            void WriteDecimalByVal(decimal value);
            void WriteDecimalTwoArgs(in decimal value, decimal klaz);
        }

        class MySerializer : ISerializer
        {
            public void WriteDecimal(in decimal value) => Asserts.AreEqual(35m, value);
            public void WriteDecimalByVal(decimal value) => Asserts.AreEqual(35m, value);

            public void WriteDecimalTwoArgs(in decimal value, decimal klaz)
            {
                Asserts.AreEqual(35m, value);
                Asserts.AreEqual(35m, klaz);
            }
        }

        public class Klaz
        {
            public decimal KlazField;
            public Klaz(bool b, float f) { KlazField = 35m; }
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