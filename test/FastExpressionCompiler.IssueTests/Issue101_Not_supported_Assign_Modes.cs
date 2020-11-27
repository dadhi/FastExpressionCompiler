using System;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue101_Not_supported_Assign_Modes : ITest
    {
        public int Run()
        {
            PreIncIsSupported();
            PostIncIsSupported();
            PreDecIsSupported();
            PostDecIsSupported();
            PreIncShortIsSupported();
            PreInc3IsSupported();
            ComplexSupported();
            Complex2Supported();
            Complex3Supported();
            return 9;
        }

        [Test]
        public void PreIncIsSupported()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] {eVar},
                    PreIncrementAssign(eVar)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(1, fastCompiled());
        }

        [Test]
        public void PostIncIsSupported()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    PostIncrementAssign(eVar)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(0, fastCompiled());
        }

        [Test]
        public void PreDecIsSupported()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    PreDecrementAssign(eVar)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(-1, fastCompiled());
        }

        [Test]
        public void PostDecIsSupported()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    PostDecrementAssign(eVar)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(0, fastCompiled());
        }

        [Test]
        public void PreIncShortIsSupported()
        {
            var eVar = Variable(typeof(short));
            var blockExpr =
                Block(new[] { eVar },
                    PreIncrementAssign(eVar)
                );

            var lambda = Lambda<Func<short>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(1, fastCompiled());
        }

        [Test]
        public void PreInc3IsSupported()
        {
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] {eVar},
                    PreIncrementAssign(eVar),
                    PreIncrementAssign(eVar),
                    PreIncrementAssign(eVar)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(3, fastCompiled());
        }

        [Test]
        public void ComplexSupported()
        {
            int j = 0;
            Action a = () => { j++; };
            var eVar = Variable(typeof(int));
            var pVar = Parameter(typeof(Action));
            var blockExpr =
                Block(new[] { eVar },
                    Call(Constant(a), a.GetType().GetTypeInfo().GetDeclaredMethod("Invoke")),
                    PreIncrementAssign(eVar)
                    //PreIncrementAssign(eVar),
                    //PreIncrementAssign(eVar)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(1, fastCompiled());
        }

        [Test]
        public void Complex2Supported()
        {
            int j = 0;
            Func<int> a = () => { return j++; };
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    Call(Constant(a), a.GetType().GetTypeInfo().GetDeclaredMethod("Invoke"))
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            j = 88;
            Assert.AreEqual(88, fastCompiled());
        }

        [Test]
        public void Complex3Supported()
        {
            int j = 0;
            Func<int> a = () => { return j++; };
            var eVar = Variable(typeof(int));
            var blockExpr =
                Block(new[] { eVar },
                    Call(Constant(a), a.GetType().GetTypeInfo().GetDeclaredMethod("Invoke")),
                    PreIncrementAssign(eVar)
                );

            var lambda = Lambda<Func<int>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Assert.NotNull(fastCompiled);
            Assert.AreEqual(1, fastCompiled());
        }
    }
}
