using System.Reflection.Emit;


#pragma warning disable 649
#pragma warning disable 219

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
// ReSharper disable UnusedMember.Global
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue170_Serializer_Person_Ref : ITest
    {
        public int Run()
        {
            InvokeActionConstantIsSupportedSimpleClass_AddAssign();
            InvokeActionConstantIsSupportedSimpleStruct_AddAssign();
            InvokeActionConstantIsSupported();
            InvokeActionConstantIsSupportedSimple();
            InvokeActionConstantIsSupportedSimpleStruct();
            return 5;
        }

        delegate void DeserializeDelegate<T>(byte[] buffer, ref int offset, ref T value);

        class Person
        {
            public string Name;
            public int Health;
            public Person BestFriend;
        }


        public void InvokeActionConstantIsSupported()
        {
            var bufferArg = Parameter(typeof(byte[]), "buffer");
            var refOffsetArg = Parameter(typeof(int).MakeByRefType(), "offset");
            var refValueArg = Parameter(typeof(Person).MakeByRefType(), "value");

            var assignBlock = Block(
                Assign(PropertyOrField(refValueArg, nameof(Person.Health)), Constant(5)),
                Assign(PropertyOrField(refValueArg, nameof(Person.Name)), Constant("test result name"))
               );

            void AssigningRefs(byte[] buffer, ref int offset, ref Person value)
            {
                value.Health = 5;
                value.Name = "test result name";
            }

            var lambda = Lambda<DeserializeDelegate<Person>>(assignBlock, bufferArg, refOffsetArg, refValueArg);


            void LocalAssert(DeserializeDelegate<Person> invoke)
            {
                var person = new Person { Name = "a", Health = 1 };
                int offset = 0;

                invoke(null, ref offset, ref person);
                Asserts.AreEqual(5, person.Health);
                Asserts.AreEqual("test result name", person.Name);
            }

            LocalAssert(AssigningRefs);

            lambda.PrintCSharp();

            var func = lambda.CompileSys();
            func.PrintIL();
            LocalAssert(func);

            var funcFast = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            funcFast.PrintIL();
            funcFast.AssertOpCodes(
                OpCodes.Ldarg_3,
                OpCodes.Ldind_Ref,
                OpCodes.Ldc_I4_5,
                OpCodes.Stfld,
                OpCodes.Ldarg_3,
                OpCodes.Ldind_Ref,
                OpCodes.Ldstr,
                OpCodes.Stfld,
                OpCodes.Ret
            );
            LocalAssert(funcFast);
        }

        delegate void DeserializeDelegateSimple<T>(ref T value);


        class SimplePerson
        {
            public int Health;
        }


        public void InvokeActionConstantIsSupportedSimple()
        {
            var refValueArg = Parameter(typeof(SimplePerson).MakeByRefType(), "value");
            void AssigningRefs(ref SimplePerson value) => value.Health = 5;
            var lambda = Lambda<DeserializeDelegateSimple<SimplePerson>>(
                Assign(PropertyOrField(refValueArg, nameof(SimplePerson.Health)), Constant(5)),
                refValueArg);


            void LocalAssert(DeserializeDelegateSimple<SimplePerson> invoke)
            {
                var person = new SimplePerson { Health = 1 };
                invoke(ref person);
                Asserts.AreEqual(5, person.Health);
            }

            LocalAssert(AssigningRefs);

            var func = lambda.CompileSys();
            LocalAssert(func);


            var funcFast = lambda.CompileFast(true);
            LocalAssert(funcFast);
        }

        struct SimplePersonStruct
        {
            public int Health;
        }


        public void InvokeActionConstantIsSupportedSimpleStruct()
        {
            var refValueArg = Parameter(typeof(SimplePersonStruct).MakeByRefType(), "value");

            void AssigningRefs(ref SimplePersonStruct value) => value.Health = 5;

            var lambda = Lambda<DeserializeDelegateSimple<SimplePersonStruct>>(
                Assign(PropertyOrField(refValueArg, nameof(SimplePersonStruct.Health)), Constant(5)),
                refValueArg);

            void LocalAssert(DeserializeDelegateSimple<SimplePersonStruct> invoke)
            {
                var person = new SimplePersonStruct { Health = 1 };
                invoke(ref person);
                Asserts.AreEqual(5, person.Health);
            }

            LocalAssert(AssigningRefs);

            _ = lambda.CompileSys();

            var funcFast = lambda.CompileFast(true);
            LocalAssert(funcFast);
        }


        public void InvokeActionConstantIsSupportedSimpleStruct_AddAssign()
        {
            var refValueArg = Parameter(typeof(SimplePersonStruct).MakeByRefType(), "value");

            void AssigningRefs(ref SimplePersonStruct value) => value.Health += 5;

            var lambda = Lambda<DeserializeDelegateSimple<SimplePersonStruct>>(
                AddAssign(PropertyOrField(refValueArg, nameof(SimplePersonStruct.Health)), Constant(5)),
                refValueArg);

            void LocalAssert(DeserializeDelegateSimple<SimplePersonStruct> invoke)
            {
                var person = new SimplePersonStruct { Health = 1 };
                invoke(ref person);
                Asserts.AreEqual(6, person.Health);
            }

            LocalAssert(AssigningRefs);

            var s = lambda.CompileSys();
            s.PrintIL();
            // LocalAssert(s); // system thing does not work for the structs, but works for the class

            var f = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldflda,// SimplePersonStruct.Health
                OpCodes.Dup,
                OpCodes.Ldind_I4,
                OpCodes.Ldc_I4_5,
                OpCodes.Add,
                OpCodes.Stind_I4,
                OpCodes.Ret
            );
            LocalAssert(f);
        }

        class SimplePersonClass
        {
            public int Health;
        }


        public void InvokeActionConstantIsSupportedSimpleClass_AddAssign()
        {
            var refValueArg = Parameter(typeof(SimplePersonClass).MakeByRefType(), "value");

            void AssigningRefs(ref SimplePersonClass value) => value.Health += 5;

            var lambda = Lambda<DeserializeDelegateSimple<SimplePersonClass>>(
                AddAssign(PropertyOrField(refValueArg, nameof(SimplePersonClass.Health)), Constant(5)),
                refValueArg);
            lambda.PrintCSharp();

            void LocalAssert(DeserializeDelegateSimple<SimplePersonClass> invoke)
            {
                var person = new SimplePersonClass { Health = 1 };
                invoke(ref person);
                Asserts.AreEqual(6, person.Health);
            }
            LocalAssert(AssigningRefs);

            var s = lambda.CompileSys();
            s.PrintIL();
            LocalAssert(s); // works for class

            var f = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            f.PrintIL();
            f.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_Ref,
                OpCodes.Dup,
                OpCodes.Ldfld,  // SimplePersonClass.Health
                OpCodes.Ldc_I4_5,
                OpCodes.Add,
                OpCodes.Stfld,  // SimplePersonClass.Health
                OpCodes.Ret
            );
            LocalAssert(f);
        }
    }
}
