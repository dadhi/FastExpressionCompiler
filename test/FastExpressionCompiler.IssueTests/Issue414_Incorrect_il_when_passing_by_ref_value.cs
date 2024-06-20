using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using System.Reflection.Emit;


#if LIGHT_EXPRESSION
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue414_Incorrect_il_when_passing_by_ref_value : ITest
    {
        public int Run()
        {
            ReturnRefParameter();
            PassByRefParameter();
#if LIGHT_EXPRESSION
            // PassByRefVariable();
            return 3;
#else
            return 2;
#endif
        }

        public delegate int MyDelegate(ref int x);

        [Test]
        public void ReturnRefParameter()
        {
            var p = Parameter(typeof(int).MakeByRefType());
            var expr = Lambda<MyDelegate>(p, p);

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            ff.PrintIL();

            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Ldind_I4,
                OpCodes.Ret
            );

            var x = 17;
            Assert.AreEqual(17, fs(ref x));

            var y = 18;
            Assert.AreEqual(18, ff(ref y));
        }

        public static void IncRef(ref int x) => ++x;

        [Test]
        public void PassByRefParameter()
        {
            var p = Parameter(typeof(int).MakeByRefType());
            var expr = Lambda<MyDelegate>(
                Block(
                    Call(GetType().GetMethod(nameof(IncRef)), p),
                    p
                ),
                p
            );

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            ff.PrintIL();

            ff.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Call,
                OpCodes.Ldarg_1,
                OpCodes.Ldind_I4,
                OpCodes.Ret
            );

            var x = 17;
            Assert.AreEqual(18, fs(ref x));

            var y = 18;
            Assert.AreEqual(19, ff(ref y));
        }

#if LIGHT_EXPRESSION

        delegate int MyDelegateNoPars();

        [Test]
        public void PassByRefVariable()
        {
            var p = Parameter(typeof(int).MakeByRefType());
            var expr = Lambda<MyDelegateNoPars>(
                Block(
                    new[] { p },
                    Assign(p, Constant(17)),
                    Call(GetType().GetMethod(nameof(IncRef)), p),
                    p
                )
            );

            expr.PrintCSharp();

            var ff = expr.CompileFast(true, CompilerFlags.ThrowOnNotSupportedExpression);
            ff.PrintIL();

            ff.AssertOpCodes(
                OpCodes.Ldc_I4_S,
                OpCodes.Stloc_0,
                OpCodes.Ldloca_S,
                OpCodes.Dup,
                OpCodes.Call,
                OpCodes.Ldind_I4,
                OpCodes.Ret
            );

            Assert.AreEqual(19, ff());
        }
#endif
    }
}