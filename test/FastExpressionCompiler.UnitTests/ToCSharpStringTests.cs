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
        public void Outputs_default_reference_type_is_just_null()
        {
            var cs = Default(typeof(string)).ToCSharpString();
            Assert.AreEqual("null;", cs);

            cs = Default(typeof(System.Collections.Generic.List<string>)).ToCSharpString();
            Assert.AreEqual("null;", cs);
        }

        [Test]
        public void Somehow_handles_block_in_expression()
        {
            // it's probably not possible to output compilable C# for expressions like this, but at least it can be easy to read
            var variable = Parameter(typeof(int), "variable");
            var cs = Add(Constant(1), Block(new [] { variable },
                Assign(variable, Constant(2)),
                variable
            )).ToCSharpString();
            Assert.AreEqual("""
                    (1 + {
                        int variable;
                        variable = 2;
                        variable;
                    });
                    """, cs);
        }


        class A<X> {}
    }
}
