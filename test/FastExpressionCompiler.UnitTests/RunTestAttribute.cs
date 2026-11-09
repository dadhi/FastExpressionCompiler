using System;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression;
#else
namespace FastExpressionCompiler;
#endif

/// <summary>Include this fixture in the compile-time generated runner registration.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class RunTestAttribute : Attribute
{
    public RunTestAttribute(TestRegistrationGroup group = TestRegistrationGroup.Default) => Group = group;
    public TestRegistrationGroup Group { get; }
}

public enum TestRegistrationGroup
{
    Default,
    RuntimeContracts,
    Legacy
}
