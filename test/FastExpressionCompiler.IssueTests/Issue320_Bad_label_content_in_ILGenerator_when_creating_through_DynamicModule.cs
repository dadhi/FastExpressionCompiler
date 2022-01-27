using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using System;
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
    public class Issue320_Bad_label_content_in_ILGenerator_when_creating_through_DynamicModule : ITest
    {
        public int Run()
        {
            Test_instance_call_without_ifthen();
            Test_instance_call();
            return 2;
        }

        //[Test] // todo: @fix
        public void Test_instance_call()
        {
            var expr = CreateExpression();

            expr.PrintCSharp(s => s.Replace(GetType().Name + ".", ""));
            var cs = (Func<int>)(() => //$
            {
                int ret;

                if (true)
                {
                    ret = default(TestMethods)/* (!) Please provide the non-default value for the constant */.InstanceMethod();
                }

            int__34138141:
                return ret;
            });

            var s = expr.CompileSys();
            s.PrintIL("sys");
            Assert.AreEqual(314, s());

            var f = expr.CompileFast(true);
            f.PrintIL("fec");
            Assert.IsNotNull(f);
            Assert.AreEqual(314, f());

            GenerateAssemblyManually(expr);
        }

        //[Test]
        public void Test_instance_call_without_ifthen()
        {
            var expr = CreateNonIfThenExpression();


            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);

            Assert.AreEqual(8675309, f());

            GenerateAssemblyManually(expr);
        }

        private static void GenerateAssemblyManually(Expression<Func<int>> expr)
        {
            var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("temp"), AssemblyBuilderAccess.Run);
            var dynamicModule = dynamicAssembly.DefineDynamicModule("temp_module");
            var dynamicType = dynamicModule.DefineType("temp_type");
            // create a dynamic method
            var dynamicMethod = dynamicType.DefineMethod("temp_method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), null);
            // get the IL generator and put the code there
            var il = dynamicMethod.GetILGenerator();

            expr.CompileFastToIL(il);

            dynamicType.CreateType();
        }

        class TestMethods
        {
            public int count = 0;
            public TestMethods(int initialcount) => count = initialcount;
            public int InstanceMethod() => count;
        }

        private Expression<Func<int>> CreateExpression()
        {
            var instance = new TestMethods(314);
            var call = Expression.Call(Expression.Constant(instance), typeof(TestMethods).GetMethod("InstanceMethod")!);

            var localint = Expression.Variable(typeof(int), "ret");
            var setlocaltocall = Expression.Assign(localint, call);
            var program = Expression.Block(
                new[] { localint },
                Expression.IfThen(Expression.Constant(true), setlocaltocall),
                Label(Label(typeof(int)), localint)
            );

            var fe = Lambda<Func<int>>(program);

            return fe;
        }

        private Expression<Func<int>> CreateNonIfThenExpression()
        {
            var instance = new TestMethods(8675309);
            var call = Expression.Call(Expression.Constant(instance), typeof(TestMethods).GetMethod("InstanceMethod")!);

            var localint = Expression.Variable(typeof(int), "ret");
            var setlocaltocall = Expression.Assign(localint, call);
            var program = Expression.Block(
                new[] { localint },
                setlocaltocall,
                Label(Label(typeof(int)), localint)
            );

            var fe = Lambda<Func<int>>(program);

            return fe;
        }

    }
}