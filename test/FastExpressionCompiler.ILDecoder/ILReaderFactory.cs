// #define DEBUG_INTERNALS

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

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

    public static StringBuilder ToILString(this MethodInfo method, StringBuilder s = null)
    {
        if (method is null) throw new ArgumentNullException(nameof(method));

        s = s ?? new StringBuilder();

        var ilReader = Create(method);

        var secondLine = false;
        foreach (var il in ilReader)
        {
            try 
            {
                if (secondLine) 
                    s.AppendLine();
                else 
                    secondLine = true;
                s.Append(il.Offset.ToString().PadRight(4, ' ')).Append(' ').Append(il.OpCode);
                if (il is InlineFieldInstruction f)
                    s.Append(' ').Append(f.Field.DeclaringType.Name).Append('.').Append(f.Field.Name);
                else if (il is InlineMethodInstruction m)
                    s.Append(' ').Append(m.Method.DeclaringType.Name).Append('.').Append(m.Method.Name);
                else if (il is InlineTypeInstruction t)
                    s.Append(' ').Append(t.Type?.Name);
                else if (il is InlineTokInstruction tok)
                    s.Append(' ').Append(tok.Member.Name);
                else if (il is InlineBrTargetInstruction br)
                    s.Append(' ').Append(br.TargetOffset);
                else if (il is ShortInlineBrTargetInstruction sbr)
                    s.Append(' ').Append(sbr.TargetOffset);
                else if (il is InlineStringInstruction si)
                    s.Append(" \"").Append(si.String).Append('"');
                else if (il is InlineIInstruction ii)
                    s.Append(' ').Append(ii.Int32);
                else if (il is ShortInlineIInstruction sii)
                    s.Append(' ').Append(sii.Byte);
                else if (il is InlineVarInstruction iv)
                    s.Append(' ').Append(iv.Ordinal);
                else if (il is ShortInlineVarInstruction siv)
                    s.Append(' ').Append(siv.Ordinal);
            }
            catch (Exception ex)
            {
                s.AppendLine().AppendLine("EXCEPTION_IN_IL_PRINT: " + ex.Message).AppendLine();
            }
        }
        return s;
    }

}