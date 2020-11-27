using NUnit.Framework;

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
    [TestFixture]
    public class Issue170_Serializer_Person_Ref : ITest
    {
        public int Run()
        {
            InvokeActionConstantIsSupported();
            InvokeActionConstantIsSupportedSimple();
            InvokeActionConstantIsSupportedSimpleStruct();
            return 3;
        }

        delegate void DeserializeDelegate<T>(byte[] buffer, ref int offset, ref T value);

        class Person
        {
            public string Name;
            public int Health;
            public Person BestFriend;
        }

        [Test]
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
                Assert.AreEqual(5, person.Health);
                Assert.AreEqual("test result name", person.Name);
            }

            LocalAssert(AssigningRefs);

            var func = lambda.CompileSys();
            LocalAssert(func);

            var funcFast = lambda.CompileFast(true);
            LocalAssert(funcFast);
        }

        delegate void DeserializeDelegateSimple<T>(ref T value);


        class SimplePerson
        {
            public int Health;
        }

        [Test]
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
                Assert.AreEqual(5, person.Health);
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

        [Test]
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
                Assert.AreEqual(5, person.Health);
            }

            LocalAssert(AssigningRefs);

            Assert.DoesNotThrow(() => lambda.CompileSys());

            var funcFast = lambda.CompileFast(true);
            LocalAssert(funcFast);
        }
    }
}
