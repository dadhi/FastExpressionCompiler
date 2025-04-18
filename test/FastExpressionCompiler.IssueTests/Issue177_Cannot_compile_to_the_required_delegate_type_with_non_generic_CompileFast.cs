﻿using System.Reflection;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue177_Cannot_compile_to_the_required_delegate_type_with_non_generic_CompileFast : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }


        public void Test()
        {
            var delegateType = typeof(MyDelegate<>).MakeGenericType(typeof(string));
            var xExpr = Parameter(typeof(string), "x");
            var lambda = Lambda(delegateType,
                Call(null, GetType().GetTypeInfo().GetDeclaredMethod("Out"), xExpr),
                xExpr);

            var sysLambdaType = lambda.CompileSys().GetType();
            Asserts.AreSame(delegateType, sysLambdaType);

            var fastLambdaType = lambda.CompileFast(true).GetType();
            Asserts.AreSame(delegateType, fastLambdaType);
        }

        public delegate void MyDelegate<T>(T x);

        public static void Out(string x) { }
    }
}
