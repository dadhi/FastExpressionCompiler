using System;
using System.Linq;
using System.Reflection.Emit;
using ILDebugging.Decoder;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
class Issue91_Issue95_Tests
    {
        delegate void ActionRef<T>(ref T a1);

        [Test]
        public void RefAssign()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(Assign(objRef, Add(objRef, Constant((double)3.0))), objRef);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleB);
        }

        [Test]
        public void NullComparisonTest()
        {
            var pParam = Parameter(typeof(string), "p");

            var condition = Condition(NotEqual(pParam, Constant(null)),
                Constant(1),
                Constant(0));
            var lambda = Lambda<Func<string, int>>(condition, pParam);
            var convert1 = lambda.CompileFast(true);
            Assert.NotNull(convert1);
            Assert.AreEqual(1, convert1("aaa"));

            // Check TryEmitInvertedNullComparison is used
            var il = ILReaderFactory.Create(convert1.Method).ToList();
            Assert.AreEqual(il[0].OpCode, OpCodes.Ldarg_0);
            Assert.AreEqual(il[1].OpCode, OpCodes.Brfalse);
            Assert.AreEqual(il[2].OpCode, OpCodes.Ldc_I4_1);
            Assert.AreEqual(il[3].OpCode, OpCodes.Br);
            Assert.AreEqual(il[4].OpCode, OpCodes.Ldc_I4_0);
            Assert.AreEqual(il[5].OpCode, OpCodes.Ret);
        }

        [Test]
        public void TestAddAssign()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(AddAssign(objRef, Constant((double)3.0)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<double>>(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Assert.AreEqual(8.0, exampleB);
        }
    }
}
