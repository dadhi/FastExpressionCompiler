using System;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
#if !LIGHT_EXPRESSION
namespace FastExpressionCompiler.IssueTests
{
    [TestFixture]
    public class EmitSimpleConstructorTest : ITest
    {
        public int Run()
        {
            DynamicMethod_Emit_Newobj();
            return 1;
        }

        [Test]
        public void DynamicMethod_Emit_Newobj()
        {
            var f = Get_DynamicMethod_Emit_Newobj();
            var a = f();
            Assert.IsInstanceOf<A>(a);
        }

        public static Func<A> Get_DynamicMethod_Emit_Newobj()
        {
            var dynMethod = new DynamicMethod(string.Empty,
                typeof(A), new[] { typeof(ExpressionCompiler.ArrayClosure) },
                typeof(ExpressionCompiler), skipVisibility: true);

            var il = dynMethod.GetILGenerator();

            il.Emit(OpCodes.Newobj, _aCtor);
            il.Emit(OpCodes.Ret);

            return (Func<A>)dynMethod.CreateDelegate(typeof(Func<A>), ExpressionCompiler.EmptyArrayClosure);
        }

        public class A
        {
            public A() { }
        }

        private static readonly ConstructorInfo _aCtor = typeof(A).GetConstructor(Type.EmptyTypes);
    }
}
#endif