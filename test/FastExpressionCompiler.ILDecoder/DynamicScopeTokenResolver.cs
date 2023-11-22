using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace FastExpressionCompiler.ILDecoder;

internal class DynamicScopeTokenResolver : ITokenResolver
{
    #region Static stuff

    private static readonly PropertyInfo s_indexer;
    private static readonly FieldInfo s_scopeFi;

    private static readonly Type s_genMethodInfoType;
    private static readonly FieldInfo s_genmethFi1;
    private static readonly FieldInfo s_genmethFi2;

    private static readonly Type s_varArgMethodType;
    private static readonly FieldInfo s_varargFi1;

    private static readonly Type s_genFieldInfoType;
    private static readonly FieldInfo s_genfieldFi1;
    private static readonly FieldInfo s_genfieldFi2;

    static DynamicScopeTokenResolver()
    {
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

        s_indexer = Type.GetType("System.Reflection.Emit.DynamicScope").GetProperty("Item", flags);
        s_scopeFi = Type.GetType("System.Reflection.Emit.DynamicILGenerator").GetField("m_scope", flags);

        s_varArgMethodType = Type.GetType("System.Reflection.Emit.VarArgMethod");
        s_varargFi1 = s_varArgMethodType.GetField("m_method", flags);

        s_genMethodInfoType = Type.GetType("System.Reflection.Emit.GenericMethodInfo");
        s_genmethFi1 = s_genMethodInfoType.GetField("m_methodHandle", flags);
        s_genmethFi2 = s_genMethodInfoType.GetField("m_context", flags);

        s_genFieldInfoType = Type.GetType("System.Reflection.Emit.GenericFieldInfo", throwOnError: false);

        s_genfieldFi1 = s_genFieldInfoType?.GetField("m_fieldHandle", flags);
        s_genfieldFi2 = s_genFieldInfoType?.GetField("m_context", flags);
    }

    #endregion

    private readonly object m_scope;

    private object this[int token] => s_indexer.GetValue(m_scope, new object[] {token});

    public DynamicScopeTokenResolver(DynamicMethod dm)
    {
        m_scope = s_scopeFi.GetValue(dm.GetILGenerator());
    }

    public string AsString(int token)
    {
        return this[token] as string;
    }

    public FieldInfo AsField(int token)
    {
        if (this[token] is RuntimeFieldHandle)
            return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)this[token]);

        if (this[token].GetType() == s_genFieldInfoType)
        {
            return FieldInfo.GetFieldFromHandle(
                (RuntimeFieldHandle)s_genfieldFi1.GetValue(this[token]),
                (RuntimeTypeHandle)s_genfieldFi2.GetValue(this[token]));
        }

        Debug.Assert(false, $"unexpected type: {this[token].GetType()}");
        return null;
    }

    public Type AsType(int token)
    {
        return Type.GetTypeFromHandle((RuntimeTypeHandle)this[token]);
    }

    public MethodBase AsMethod(int token)
    {
        var dynamicMethod = this[token] as DynamicMethod;
        if (dynamicMethod != null)
            return dynamicMethod;

        if (this[token] is RuntimeMethodHandle)
            return MethodBase.GetMethodFromHandle((RuntimeMethodHandle)this[token]);

        if (this[token].GetType() == s_genMethodInfoType)
            return MethodBase.GetMethodFromHandle(
                (RuntimeMethodHandle)s_genmethFi1.GetValue(this[token]),
                (RuntimeTypeHandle)s_genmethFi2.GetValue(this[token]));

        if (this[token].GetType() == s_varArgMethodType)
            return (MethodInfo)s_varargFi1.GetValue(this[token]);

        Debug.Assert(false, $"unexpected type: {this[token].GetType()}");
        return null;
    }

    public MemberInfo AsMember(int token)
    {
        if ((token & 0x02000000) == 0x02000000)
            return AsType(token);
        if ((token & 0x06000000) == 0x06000000)
            return AsMethod(token);
        if ((token & 0x04000000) == 0x04000000)
            return AsField(token);

        Debug.Assert(false, $"unexpected token type: {token:x8}");
        return null;
    }

    public byte[] AsSignature(int token)
    {
        return this[token] as byte[];
    }
}