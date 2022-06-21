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

        class A<X> {}
    }
}