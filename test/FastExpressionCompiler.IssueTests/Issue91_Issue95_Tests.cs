﻿using System;
using System.Linq;
using System.Reflection.Emit;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue91_Issue95_Tests : ITest
    {
        public int Run()
        {
            RefAssign();
            NullComparisonTest();
            TestAddAssign();
            return 3;
        }

        delegate void ActionRef<T>(ref T a1);


        public void RefAssign()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(Assign(objRef, Add(objRef, Constant((double)3.0))), objRef);

            var compiledB = lambda.CompileFast(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Asserts.AreEqual(8.0, exampleB);
        }


        public void NullComparisonTest()
        {
            var pParam = Parameter(typeof(string), "p");

            var condition = Condition(
                NotEqual(pParam, Constant(null)),
                Constant(1),
                Constant(0));

            var lambda = Lambda<Func<string, int>>(condition, pParam);
            var convert1 = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            Asserts.IsNotNull(convert1);
            Asserts.AreEqual(1, convert1("aaa"));

            convert1.AssertOpCodes(
                OpCodes.Ldarg_1,
                OpCodes.Brfalse,
                OpCodes.Ldc_I4_1,
                OpCodes.Br,
                OpCodes.Ldc_I4_0,
                OpCodes.Ret
            );
        }


        public void TestAddAssign()
        {
            var objRef = Parameter(typeof(double).MakeByRefType());
            var lambda = Lambda<ActionRef<double>>(AddAssign(objRef, Constant((double)3.0)), objRef);

            var compiledB = lambda.CompileFast<ActionRef<double>>(true);
            var exampleB = 5.0;
            compiledB(ref exampleB);
            Asserts.AreEqual(8.0, exampleB);
        }
    }
}
