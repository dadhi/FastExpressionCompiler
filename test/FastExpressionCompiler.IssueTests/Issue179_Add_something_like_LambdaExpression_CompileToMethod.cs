using System;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue179_Add_something_like_LambdaExpression_CompileToMethod : ITest
    {
        public int Run()
        {
            Test();
            return 1;
        }

        [Test]
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

            funcExpr.CompileFastToIL(methodBuilder.GetILGenerator(), true);

            var dynamicType = typeBuilder.CreateType();
            var myAddMethod = dynamicType.GetTypeInfo().GetDeclaredMethod("MyAdd");
            var func = (Func<int, int, int>)Delegate.CreateDelegate(funcExpr.Type, myAddMethod);

            Assert.AreEqual(42, func(39, 3));
        }
    }
}
