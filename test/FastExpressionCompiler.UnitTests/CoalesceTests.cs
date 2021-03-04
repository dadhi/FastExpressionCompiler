using NUnit.Framework;
using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class CoalesceTests : ITest
    {
        public int Run()
        {
            Coalesce_bodyless_left_null();
            Coalesce_bodyless_left_not_null();
            Coalesce_block_variable_assign_constant();
            Coalesce_block_variable_assign_constant_right_box();
            Coalesce_block_variable_assign_constant_right_cast();
            Coalesce_block_variable_assign_with_param();
            return 6;
        }

        [Test]
        public void Coalesce_bodyless_left_null()
        {
            var a = new object();
            var dlgt = Lambda<Func<object>>(Coalesce(Constant(null), Constant(a))).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreSame(a, dlgt());
        }

        [Test]
        public void Coalesce_bodyless_left_not_null()
        {
            var a = new object();
            var dlgt = Lambda<Func<object>>(Coalesce(Constant(a), Constant(new object()))).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreSame(a, dlgt());
        }

        [Test]
        public void Coalesce_block_variable_assign_constant()
        {
            var a = new object();
            var variable = Variable(typeof(object));
            var block = Block(new[] { variable },
                Assign(variable, Coalesce(variable, Constant(a))),
                variable);

            var dlgt = Lambda<Func<object>>(block).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreSame(a, dlgt());
        }

        [Test]
        public void Coalesce_block_variable_assign_constant_right_box()
        {
            var variable = Variable(typeof(object));
            var block = Block(new[] { variable },
                Assign(variable, Coalesce(variable, Constant(5))),
                variable);

            var dlgt = Lambda<Func<object>>(block).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreEqual(5, dlgt());
        }

        [Test]
        public void Coalesce_block_variable_assign_constant_right_cast()
        {
            var a = new A();
            var variable = Variable(typeof(object));
            var block = Block(new[] { variable },
                Assign(variable, Coalesce(variable, Constant(a))),
                variable);

            var dlgt = Lambda<Func<object>>(block).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreEqual(a, dlgt());
        }

        [Test]
        public void Coalesce_block_variable_assign_with_param()
        {
            var a = new object();
            var variable = Variable(typeof(object));
            var param = Variable(typeof(object));
            var param2 = Variable(typeof(object));
            var block = Block(new[] { variable },
                Assign(variable, Coalesce(param, param2)),
                variable);

            var dlgt = Lambda<Func<object, object, object>>(block, param, param2).CompileFast(true);

            Assert.IsNotNull(dlgt);
            Assert.AreSame(a, dlgt(null, a));
            Assert.AreSame(a, dlgt(a, new object()));
        }

        private class A { }
    }
}
