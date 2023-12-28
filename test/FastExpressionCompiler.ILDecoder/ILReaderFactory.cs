using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FastExpressionCompiler.ILDecoder;

public static class ILReaderFactory
{
    // todo: @wip Support reading IL on the NET_FRAMEWORK and on NET_7_0_OR_LESS, #385
    public static ILReader Create(object obj)
    {
        var type = obj.GetType();

        // var isRTDynamicMethod = type == s_rtDynamicMethodType;

        if (type == s_dynamicMethodType)// | isRTDynamicMethod)
        {
            DynamicMethod dm;
            // if (type == s_rtDynamicMethodType)
            // {
            //     //
            //     // if the target is RTDynamicMethod, get the value of
            //     // RTDynamicMethod.m_owner instead
            //     //
            //     dm = (DynamicMethod)s_fiOwner.GetValue(obj);
            // }
            // else
            // {
                dm = obj as DynamicMethod;
            // }

            return new ILReader(new DynamicMethodILProvider(dm), new DynamicScopeTokenResolver(dm));
        }

        if (type == s_runtimeMethodInfoType || type == s_runtimeConstructorInfoType)
        {
            var method = obj as MethodBase;
            return new ILReader(method);
        }

        // todo: @wip return the exception back when supported
        // throw new NotSupportedException($"Reading IL from type {type} is currently not supported");
        return null;
    }

    private static readonly Type s_dynamicMethodType = Type.GetType("System.Reflection.Emit.DynamicMethod");
    private static readonly Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
    private static readonly Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

#if !NET8_0_OR_GREATER
    // private static readonly Type s_rtDynamicMethodType = Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod");
    // private static readonly FieldInfo s_fiOwner = s_rtDynamicMethodType.GetField("m_owner", BindingFlags.Instance | BindingFlags.NonPublic);
#endif
}