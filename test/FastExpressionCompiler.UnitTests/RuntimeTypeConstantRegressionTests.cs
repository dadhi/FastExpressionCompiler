using System;
using System.Reflection;
using System.Reflection.Emit;
#if LIGHT_EXPRESSION
using E = FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using E = System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif
public struct RuntimeTypeConstantRegressionTests : ITestX
{
    public void Run(TestRun t) { Nonruntime_type_constants_preserve_identity(t); Runtime_type_constants_keep_token_semantics(t); }
    public void Nonruntime_type_constants_preserve_identity(TestContext t)
    {
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("FecTypeConstant"), AssemblyBuilderAccess.Run).DefineDynamicModule("M");
        foreach (Type value in new Type[] { new TypeDelegator(typeof(int)), module.DefineType("Unfinished") })
        {
            var expression = E.Lambda<Func<Type>>(E.Constant(value, typeof(Type)));
            var fast = expression.CompileFast(true);
            if (t.IsNotNull(fast)) t.AreEqual(true, ReferenceEquals(value, fast()));
            var repeated = E.Lambda<Func<bool>>(E.ReferenceEqual(E.Constant(value, typeof(Type)), E.Constant(value, typeof(Type)))).CompileFast(true);
            if (t.IsNotNull(repeated)) t.AreEqual(true, repeated());
        }
    }
    public void Runtime_type_constants_keep_token_semantics(TestContext t)
    {
        foreach (var value in new Type[] { null, typeof(int), typeof(Func<>), typeof(Func<>).GetGenericArguments()[0], typeof(int).MakeByRefType(), typeof(int[,]) })
        {
            var fast = E.Lambda<Func<Type>>(E.Constant(value, typeof(Type))).CompileFast(true);
            if (t.IsNotNull(fast)) t.AreEqual(true, ReferenceEquals(value, fast()));
        }
    }
}
