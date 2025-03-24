using System;
using System.Linq;
using System.Reflection;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue322_NullableIntArgumentWithDefaultIntValue : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }


        public void Test()
        {
            var e = new Expression[3]; // the unique expressions
            var expr = Lambda<Func<MyService_NullableIntArgWithIntValue>>( //$
              e[0] = New( // 2 args
                typeof(MyService_NullableIntArgWithIntValue).GetTypeInfo().DeclaredConstructors.ToArray()[0],
                e[1] = New( // 0 args
                  typeof(MyOtherDependency).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0]),
                e[2] = Constant(15, typeof(int?))));

            var fSys = expr.CompileSys();
            fSys.PrintIL("sys");
            var x = fSys();
            Asserts.AreEqual(15, x.OptionalArgument);

            var fFast = expr.CompileFast();
            fFast.PrintIL("fast");
            var y = fFast();
            Asserts.AreEqual(15, y.OptionalArgument);
        }

        interface IOtherDependency { }
        class MyOtherDependency : IOtherDependency { }

        class MyService_NullableIntArgWithIntValue
        {
            public IOtherDependency OtherDependency;
            public int OptionalArgument;
            public MyService_NullableIntArgWithIntValue(IOtherDependency otherDependency, int? optionalArgument = 15)
            {
                OtherDependency = otherDependency;
                OptionalArgument = optionalArgument ?? 42;
            }
        }
    }
}