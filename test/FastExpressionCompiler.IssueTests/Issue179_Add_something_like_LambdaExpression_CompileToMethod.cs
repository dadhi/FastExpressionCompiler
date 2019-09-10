#if !LIGHT_EXPRESSION

using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class Issue179_Add_something_like_LambdaExpression_CompileToMethod
    {
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

            var paramA = Expression.Parameter(typeof(int));
            var paramB = Expression.Parameter(typeof(int));

            var funcExpr = Expression.Lambda<Func<int, int, int>>(
                Expression.Add(paramA, paramB), 
                paramA, paramB);

            funcExpr.CompileFastToIL(methodBuilder.GetILGenerator());

            var dynamicType = typeBuilder.CreateType();
            var myAddMethod = dynamicType.GetTypeInfo().GetDeclaredMethod("MyAdd");
            var func = (Func<int, int, int>)Delegate.CreateDelegate(funcExpr.Type, myAddMethod);

            Assert.AreEqual(42, func(39, 3));
        }
    }
}

#endif
