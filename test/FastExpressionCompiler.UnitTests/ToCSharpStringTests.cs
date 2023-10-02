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
        public void Outputs_default_null_for_reference_types()
        {
            Assert.AreEqual("(string)null;", Constant(null, typeof(string)).ToCSharpString());
            Assert.AreEqual("(string)null;", Default(typeof(string)).ToCSharpString());
            Assert.AreEqual("(List<string>)null;", Constant(null, typeof(System.Collections.Generic.List<string>)).ToCSharpString());
            Assert.AreEqual("(List<string>)null;", Default(typeof(System.Collections.Generic.List<string>)).ToCSharpString());
            Assert.AreEqual("(int?)null;", Constant(null, typeof(int?)).ToCSharpString());
            Assert.AreEqual("(int?)null;", Default(typeof(int?)).ToCSharpString());

            Assert.AreEqual("default(int);", Default(typeof(int)).ToCSharpString());

            var block = Block(
                new[] { Variable(typeof(int), "integer"), Variable(typeof(int?), "maybe_integer"), Variable(typeof(string), "str") },
                Empty()
            );
            Assert.AreEqual("""

                int integer = default;
                int? maybe_integer = null;
                string str = null;;
                """, block.ToCSharpString());
        }

        class A<X> {}
    }
}
