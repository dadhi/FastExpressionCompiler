// #define DEBUG_INTERNALS

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace FastExpressionCompiler.ILDecoder;

public static class ILReaderFactory
{
    private static readonly Type _runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
    private static readonly Type _runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

#if !NET8_0_OR_GREATER
    private static readonly Type _rtDynamicMethodType =
        Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod");
    private static readonly FieldInfo _fiOwner =
        _rtDynamicMethodType.GetField("m_owner", BindingFlags.Instance | BindingFlags.NonPublic);
#endif

    public static ILReader Create(object source)
    {
        var sourceType = source.GetType();
        var dynamicMethod = source as DynamicMethod;

#if DEBUG_INTERNALS
        Console.WriteLine($"sourceType: {sourceType}");
        Console.WriteLine($"dynamicMethod >= NET8: {(dynamicMethod?.ToString() ?? "null")}");
        Console.WriteLine($"_runtimeMethodInfoType: {_runtimeMethodInfoType},\n_runtimeConstructorInfoType: {_runtimeConstructorInfoType} ");
#endif

#if !NET8_0_OR_GREATER
        if (dynamicMethod == null && sourceType == _rtDynamicMethodType)
            dynamicMethod = (DynamicMethod)_fiOwner.GetValue(source);
#if DEBUG_INTERNALS
        Console.WriteLine($"_rtDynamicMethodType: {_rtDynamicMethodType}, _fiOwner: {_fiOwner}");
        Console.WriteLine($"dynamicMethod < NET8: {(dynamicMethod?.ToString() ?? "null")}");
#endif
#endif

        if (dynamicMethod != null)
            return new ILReader(new DynamicMethodILProvider(dynamicMethod), new DynamicScopeTokenResolver(dynamicMethod));

        if (sourceType == _runtimeMethodInfoType ||
            sourceType == _runtimeConstructorInfoType)
            return new ILReader((MethodBase)source);

        Debug.WriteLine($"Reading IL from type {sourceType} is currently not supported");
        return null;
    }
}