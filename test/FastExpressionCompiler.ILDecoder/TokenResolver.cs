using System;
using System.Reflection;

namespace FastExpressionCompiler.ILDecoder;

public interface ITokenResolver
{
    MethodBase AsMethod(int token);
    FieldInfo AsField(int token);
    Type AsType(int token);
    string AsString(int token);
    MemberInfo AsMember(int token);
    byte[] AsSignature(int token);
}

public class ModuleScopeTokenResolver : ITokenResolver
{
    private readonly Module m_module;
    private readonly Type[] m_methodContext;
    private readonly Type[] m_typeContext;

    public ModuleScopeTokenResolver(MethodBase method)
    {
        m_module = method.Module;
        m_methodContext = method is ConstructorInfo ? null : method.GetGenericArguments();
        m_typeContext = method.DeclaringType == null ? null : method.DeclaringType.GetGenericArguments();
    }

    public MethodBase AsMethod(int token) => m_module.ResolveMethod(token, m_typeContext, m_methodContext);
    public FieldInfo AsField(int token) => m_module.ResolveField(token, m_typeContext, m_methodContext);
    public Type AsType(int token) => m_module.ResolveType(token, m_typeContext, m_methodContext);
    public MemberInfo AsMember(int token) => m_module.ResolveMember(token, m_typeContext, m_methodContext);
    public string AsString(int token) => m_module.ResolveString(token);
    public byte[] AsSignature(int token) => m_module.ResolveSignature(token);
}