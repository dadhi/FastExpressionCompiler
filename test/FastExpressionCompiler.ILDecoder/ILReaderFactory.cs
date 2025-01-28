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
                    s.Append(' ').AppendTypeName(f.Field.DeclaringType).Append('.').Append(f.Field.Name);
                else if (il is InlineMethodInstruction m)
                    s.Append(' ').AppendTypeName(m.Method.DeclaringType).Append('.').Append(m.Method.Name);
                else if (il is InlineTypeInstruction t)
                    s.Append(' ').AppendTypeName(t.Type);
                else if (il is InlineTokInstruction tok)
                    s.Append(' ').Append(tok.Member.Name);
                else if (il is InlineBrTargetInstruction br)
                    s.Append(' ').Append(br.TargetOffset);
                else if (il is InlineSwitchInstruction sw)
                {
                    s.Append(' ');
                    foreach (var offset in sw.TargetOffsets)
                        s.Append(offset).Append(',');
                }
                else if (il is ShortInlineBrTargetInstruction sbr)
                    s.Append(' ').Append(sbr.TargetOffset);
                else if (il is InlineStringInstruction si)
                    s.Append(" \"").Append(si.String).Append('"');
                else if (il is ShortInlineIInstruction sii)
                    s.Append(' ').Append(sii.Byte);
                else if (il is InlineIInstruction ii)
                    s.Append(' ').Append(ii.Int32);
                else if (il is InlineI8Instruction i8)
                    s.Append(' ').Append(i8.Int64);
                else if (il is ShortInlineRInstruction sir)
                    s.Append(' ').Append(sir.Single);
                else if (il is InlineRInstruction ir)
                    s.Append(' ').Append(ir.Double);
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

    public static StringBuilder AppendTypeName(this StringBuilder sb, Type type, bool stripNamespace = false) =>
        type == null ? sb : sb.Append(type.TypeToCode(stripNamespace));

    public static string TypeToCode(this Type type,
        bool stripNamespace = false, Func<Type, string, string> printType = null, bool printGenericTypeArgs = true)
    {
        if (type.IsGenericParameter)
            return !printGenericTypeArgs ? string.Empty : (printType?.Invoke(type, type.Name) ?? type.Name);

        if (Nullable.GetUnderlyingType(type) is Type nullableElementType && !type.IsGenericTypeDefinition)
        {
            var result = nullableElementType.TypeToCode(stripNamespace, printType, printGenericTypeArgs) + "?";
            return printType?.Invoke(type, result) ?? result;
        }

        Type arrayType = null;
        if (type.IsArray)
        {
            // store the original type for the later and process its element type further here
            arrayType = type;
            type = type.GetElementType();
        }

        // the default handling of the built-in types
        string buildInTypeString = null;
        if (type == typeof(void))
            buildInTypeString = "void";
        else if (type == typeof(object))
            buildInTypeString = "object";
        else if (type == typeof(bool))
            buildInTypeString = "bool";
        else if (type == typeof(int))
            buildInTypeString = "int";
        else if (type == typeof(short))
            buildInTypeString = "short";
        else if (type == typeof(byte))
            buildInTypeString = "byte";
        else if (type == typeof(double))
            buildInTypeString = "double";
        else if (type == typeof(float))
            buildInTypeString = "float";
        else if (type == typeof(char))
            buildInTypeString = "char";
        else if (type == typeof(string))
            buildInTypeString = "string";

        if (buildInTypeString != null)
        {
            if (arrayType != null)
                buildInTypeString += "[]";
            return printType?.Invoke(arrayType ?? type, buildInTypeString) ?? buildInTypeString;
        }

        var parentCount = 0;
        for (var ti = type.GetTypeInfo(); ti.IsNested; ti = ti.DeclaringType.GetTypeInfo())
            ++parentCount;

        Type[] parentTypes = null;
        if (parentCount > 0)
        {
            parentTypes = new Type[parentCount];
            var pt = type.DeclaringType;
            for (var i = 0; i < parentTypes.Length; i++, pt = pt.DeclaringType)
                parentTypes[i] = pt;
        }

        var typeInfo = type.GetTypeInfo();
        Type[] typeArgs = null;
        var isTypeClosedGeneric = false;
        if (type.IsGenericType)
        {
            isTypeClosedGeneric = !typeInfo.IsGenericTypeDefinition;
            typeArgs = isTypeClosedGeneric ? typeInfo.GenericTypeArguments : typeInfo.GenericTypeParameters;
        }

        var typeArgsConsumedByParentsCount = 0;
        var s = new StringBuilder();
        if (!stripNamespace && !string.IsNullOrEmpty(type.Namespace)) // for the auto-generated classes Namespace may be empty and in general it may be empty
            s.Append(type.Namespace).Append('.');

        if (parentTypes != null)
        {
            for (var p = parentTypes.Length - 1; p >= 0; --p)
            {
                var parentType = parentTypes[p];
                if (!parentType.IsGenericType)
                {
                    s.Append(parentType.Name).Append('.');
                }
                else
                {
                    var parentTypeInfo = parentType.GetTypeInfo();
                    Type[] parentTypeArgs = null;
                    if (parentTypeInfo.IsGenericTypeDefinition)
                    {
                        parentTypeArgs = parentTypeInfo.GenericTypeParameters;

                        // replace the open parent args with the closed child args,
                        // and close the parent
                        if (isTypeClosedGeneric)
                            for (var t = 0; t < parentTypeArgs.Length; ++t)
                                parentTypeArgs[t] = typeArgs[t];

                        var parentTypeArgCount = parentTypeArgs.Length;
                        if (typeArgsConsumedByParentsCount > 0)
                        {
                            int ownArgCount = parentTypeArgCount - typeArgsConsumedByParentsCount;
                            if (ownArgCount == 0)
                                parentTypeArgs = null;
                            else
                            {
                                var ownArgs = new Type[ownArgCount];
                                for (var a = 0; a < ownArgs.Length; ++a)
                                    ownArgs[a] = parentTypeArgs[a + typeArgsConsumedByParentsCount];
                                parentTypeArgs = ownArgs;
                            }
                        }
                        typeArgsConsumedByParentsCount = parentTypeArgCount;
                    }
                    else
                    {
                        parentTypeArgs = parentTypeInfo.GenericTypeArguments;
                    }

                    var parentTickIndex = parentType.Name.IndexOf('`');
                    s.Append(parentType.Name.Substring(0, parentTickIndex));

                    // The owned parentTypeArgs maybe empty because all args are defined in the parent's parents
                    if (parentTypeArgs?.Length > 0)
                    {
                        s.Append('<');
                        for (var t = 0; t < parentTypeArgs.Length; ++t)
                            (t == 0 ? s : s.Append(", ")).Append(parentTypeArgs[t].TypeToCode(stripNamespace, printType, printGenericTypeArgs));
                        s.Append('>');
                    }
                    s.Append('.');
                }
            }
        }
        var name = type.Name.TrimStart('<', '>').TrimEnd('&');

        if (typeArgs != null && typeArgsConsumedByParentsCount < typeArgs.Length)
        {
            var tickIndex = name.IndexOf('`');
            s.Append(name.Substring(0, tickIndex)).Append('<');
            for (var i = 0; i < typeArgs.Length - typeArgsConsumedByParentsCount; ++i)
                (i == 0 ? s : s.Append(", ")).Append(typeArgs[i + typeArgsConsumedByParentsCount].TypeToCode(stripNamespace, printType, printGenericTypeArgs));
            s.Append('>');
        }
        else
        {
            s.Append(name);
        }

        if (arrayType != null)
            s.Append("[]");

        return printType?.Invoke(arrayType ?? type, s.ToString()) ?? s.ToString();
    }
}