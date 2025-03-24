using System;
using System.Reflection;
using System.Reflection.Emit;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue179_Add_something_like_LambdaExpression_CompileToMethod : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }


        public void Test()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("MyDynamicAssembly"), AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule("MyDynamicModule");

            var typeBuilder = moduleBuilder.DefineType("MyType", TypeAttributes.Public);

            var methodBuilder = typeBuilder.DefineMethod("MyAdd",
                MethodAttributes.Public | MethodAttributes.Static, // static is required to be able to compile to method
                CallingConventions.Standard,
                typeof(int), new[] { typeof(int), typeof(int) });

            var paramA = Parameter(typeof(int));
            var paramB = Parameter(typeof(int));

            var funcExpr = Lambda<Func<int, int, int>>(Add(paramA, paramB), paramA, paramB);

            var success = funcExpr.CompileFastToIL(methodBuilder.GetILGenerator());
            Asserts.IsTrue(success);

            var dynamicType = typeBuilder.CreateType();
            var myAddMethod = dynamicType.GetTypeInfo().GetDeclaredMethod("MyAdd");
            var func = (Func<int, int, int>)Delegate.CreateDelegate(funcExpr.Type, myAddMethod);

            Asserts.AreEqual(42, func(39, 3));
        }
    }
}
