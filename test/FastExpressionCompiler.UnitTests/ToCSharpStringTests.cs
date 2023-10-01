using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class ToCSharpStringTests : ITest
    {
        public int Run()
        {
            Outputs_closed_generic_type_constant_correctly();
            return 1;
        }

        [Test]
        public void Outputs_closed_generic_type_constant_correctly()
        {
            var e = Lambda<Func<Type>>(Constant(typeof(A<string>)));

            var cs = e.ToCSharpString();

            StringAssert.Contains("A<string>", cs);

            var f = e.CompileFast(true);
            Assert.AreEqual(typeof(A<string>), f());
        }

        [Test]
        public void Outputs_type_equals()
        {
            var p = Parameter(typeof(object), "p");
            var eSealed = TypeEqual(p, typeof(string));
            var eStruct = TypeEqual(p, typeof(int));
            var eArray = TypeEqual(p, typeof(object[]));
            var eOpen = TypeEqual(p, typeof(System.Collections.Generic.List<string>));

            Assert.AreEqual("(p is string);", eSealed.ToCSharpString());
            Assert.AreEqual("(p is int);", eStruct.ToCSharpString());
            Assert.AreEqual("(p.GetType() == typeof(object[]));", eArray.ToCSharpString());
            Assert.AreEqual("(p.GetType() == typeof(List<string>));", eOpen.ToCSharpString());
        }

        class A<X> {}
    }
}
