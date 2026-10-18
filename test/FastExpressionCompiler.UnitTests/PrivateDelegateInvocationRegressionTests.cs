using System;
using System.Reflection;
using System.Reflection.Emit;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct PrivateDelegateInvocationRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Private_invoke_method_can_be_called(t);
        Private_delegate_with_parameters_can_be_called(t);
    }

    private static Type CreateDelegateType(Type[] parameters)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PrivateInvoke" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var builder = assembly.DefineDynamicModule("PrivateInvoke").DefineType("PrivateDelegate",
            TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Sealed, typeof(MulticastDelegate));
        builder.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public,
            CallingConventions.Standard, new[] { typeof(object), typeof(IntPtr) }).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
        builder.DefineMethod("Invoke", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
            typeof(int), parameters).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
        return builder.CreateTypeInfo().AsType();
    }

    public void Private_invoke_method_can_be_called(TestContext t)
    {
        var type = CreateDelegateType(Type.EmptyTypes);
        var expression = SysExpr.Lambda(type, SysExpr.Constant(42));
        var compiled = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(compiled)) return;
        var invoke = SysExpr.Lambda<Func<int>>(SysExpr.Invoke(SysExpr.Constant(compiled, type)));
        var fast = invoke.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        t.AreEqual(42, fast());
        t.AreEqual(invoke.Compile()(), fast());
    }

    public void Private_delegate_with_parameters_can_be_called(TestContext t)
    {
        var type = CreateDelegateType(new[] { typeof(int), typeof(int) });
        var left = SysExpr.Parameter(typeof(int));
        var right = SysExpr.Parameter(typeof(int));
        var expression = SysExpr.Lambda(type, SysExpr.Add(left, right), left, right);
        var compiled = expression.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(compiled)) return;
        var invoke = SysExpr.Lambda<Func<int, int, int>>(SysExpr.Invoke(SysExpr.Constant(compiled, type), left, right), left, right);
        var fast = invoke.FromSysExpression().CompileFast(true);
        if (!t.IsNotNull(fast)) return;
        var expected = invoke.Compile();
        foreach (var value in new[] { int.MinValue, 0, 42, int.MaxValue })
            t.AreEqual(expected(value, 3), fast(value, 3));
    }
}
