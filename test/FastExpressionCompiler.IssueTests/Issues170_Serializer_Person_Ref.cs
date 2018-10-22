using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using NUnit.Framework;

#pragma warning disable 649
#pragma warning disable 219

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
    public class Issues170_Serializer_Person_Ref
    {
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

            void AssingRefs(byte[] buffer, ref int offset, ref Person value)
            {
                value.Health = 5;
                value.Name = "test result name";
            }

            var lambda = Lambda<DeserializeDelegate<Person>>(assignBlock, bufferArg, refOffsetArg, refValueArg);


            void LocalAssert(DeserializeDelegate<Person> invoke)
            {
                var person = new Person { Name = "a", Health = 1 };
                byte[] buffer = new byte[100];
                int offset = 0;

                invoke(null, ref offset, ref person);
                Assert.AreEqual(5, person.Health);
                Assert.AreEqual("test result name", person.Name);
            }

            LocalAssert(AssingRefs);

#if !LIGHT_EXPRESSION
            {
                var func = lambda.Compile();
                LocalAssert(func);
            }
#endif

            var funcFast = lambda.CompileFast();
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
            void AssingRefs(ref SimplePerson value) => value.Health = 5;
            var lambda = Lambda<DeserializeDelegateSimple<SimplePerson>>(
                Assign(PropertyOrField(refValueArg, nameof(SimplePerson.Health)), Constant(5)), 
                refValueArg);


            void LocalAssert(DeserializeDelegateSimple<SimplePerson> invoke)
            {
                var person = new SimplePerson { Health = 1 };
                invoke(ref person);
                Assert.AreEqual(5, person.Health);
            }

            LocalAssert(AssingRefs);
 
#if !LIGHT_EXPRESSION
            {
                var func = lambda.Compile();
                LocalAssert(func);
            }
#endif


            var funcFast = lambda.CompileFast();
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

            void AssingRefs(ref SimplePersonStruct value) => value.Health = 5;
            

            var lambda = Lambda<DeserializeDelegateSimple<SimplePersonStruct>>(
                Assign(PropertyOrField(refValueArg, nameof(SimplePersonStruct.Health)), Constant(5)), 
                refValueArg);

            void LocalAssert(DeserializeDelegateSimple<SimplePersonStruct> invoke)
            {
                var person = new SimplePersonStruct { Health = 1 };
                invoke(ref person);
                Assert.AreEqual(5, person.Health);
            }

            LocalAssert(AssingRefs);

#if !LIGHT_EXPRESSION
            {
                var func = lambda.Compile();                
                LocalAssert(func);
            }
#endif

            var funcFast = lambda.CompileFast();
            LocalAssert(funcFast);            
        }
    }
}
