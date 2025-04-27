// #define DEBUG_INTERNALS

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
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

        var line = 0;
        foreach (var il in ilReader)
        {
            try
            {
                s = line++ > 0 ? s.AppendLine() : s;

                s.Append($"{il.Offset,-4}{il.OpCode}");

                if (il is InlineFieldInstruction f)
                {
                    s.Append(' ')
                        .AppendTypeName(f.Field.FieldType).Append(' ')
                        .AppendTypeName(f.Field.DeclaringType).Append('.')
                        .Append(f.Field.Name);
                }
                else if (il is InlineMethodInstruction m)
                {
                    var sig = m.Method.ToString();
                    var paramStart = sig.IndexOf('(');
                    var paramList = paramStart == -1 ? "()" : sig.Substring(paramStart);

                    if (m.Method is MethodInfo met)
                    {
                        s.Append(' ')
                            .AppendTypeName(met.ReturnType).Append(' ')
                            .AppendTypeName(met.DeclaringType).Append('.')
                            .Append(met.Name)
                            .Append(paramList);
                    }
                    else if (m.Method is ConstructorInfo con)
                        s.Append(' ').AppendTypeName(con.DeclaringType).Append(paramList);
                    else
                        s.Append(' ').AppendTypeName(m.Method.DeclaringType).Append('.').Append(sig);
                }
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

    public static StringBuilder AppendTypeName(this StringBuilder sb, Type type) =>
        type == null ? sb : sb.Append(type.TypeToCode(true));

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

public sealed class ILReader : IEnumerable<ILInstruction>
{
    private static readonly OpCode[] _oneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] _twoByteOpCodes = new OpCode[0x100];

    static ILReader()
    {
        foreach (var fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var opCode = (OpCode)fi.GetValue(null);
            var value = (ushort)opCode.Value;

            if (value < 0x100)
                _oneByteOpCodes[value] = opCode;
            else if ((value & 0xff00) == 0xfe00)
                _twoByteOpCodes[value & 0xff] = opCode;
        }
    }

    private readonly ITokenResolver _resolver;
    private readonly byte[] _byteArray;

    public ILReader(MethodBase method)
        : this(new MethodBaseILProvider(method), new ModuleScopeTokenResolver(method))
    {
    }

    public ILReader(IILProvider ilProvider, ITokenResolver tokenResolver)
    {
        _resolver = tokenResolver;
        _byteArray = ilProvider?.GetByteArray() ?? throw new ArgumentNullException(nameof(ilProvider));
    }

    public IEnumerator<ILInstruction> GetEnumerator()
    {
        var position = 0;
        while (position < _byteArray.Length)
            yield return Next(ref position);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private ILInstruction Next(ref int position)
    {
        var offset = position;
        OpCode opCode;

        // read first 1 or 2 bytes as opCode
        var code = ReadByte(ref position);
        if (code != 0xFE)
        {
            opCode = _oneByteOpCodes[code];
        }
        else
        {
            code = ReadByte(ref position);
            opCode = _twoByteOpCodes[code];
        }

        switch (opCode.OperandType)
        {
            case OperandType.InlineNone:
                {
                    return new InlineNoneInstruction(offset, opCode);
                }
            case OperandType.ShortInlineBrTarget:
                {
                    // 8-bit integer branch target
                    return new ShortInlineBrTargetInstruction(offset, opCode, ReadSByte(ref position));
                }
            case OperandType.InlineBrTarget:
                {
                    // 32-bit integer branch target
                    return new InlineBrTargetInstruction(offset, opCode, ReadInt32(ref position));
                }
            case OperandType.ShortInlineI:
                {
                    // 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
                    return new ShortInlineIInstruction(offset, opCode, ReadByte(ref position));
                }
            case OperandType.InlineI:
                {
                    // 32-bit integer
                    return new InlineIInstruction(offset, opCode, ReadInt32(ref position));
                }
            case OperandType.InlineI8:
                {
                    // 64-bit integer
                    return new InlineI8Instruction(offset, opCode, ReadInt64(ref position));
                }
            case OperandType.ShortInlineR:
                {
                    // 32-bit IEEE floating point number
                    return new ShortInlineRInstruction(offset, opCode, ReadSingle(ref position));
                }
            case OperandType.InlineR:
                {
                    // 64-bit IEEE floating point number
                    return new InlineRInstruction(offset, opCode, ReadDouble(ref position));
                }
            case OperandType.ShortInlineVar:
                {
                    // 8-bit integer containing the ordinal of a local variable or an argument
                    return new ShortInlineVarInstruction(offset, opCode, ReadByte(ref position));
                }
            case OperandType.InlineVar:
                {
                    // 16-bit integer containing the ordinal of a local variable or an argument
                    return new InlineVarInstruction(offset, opCode, ReadUInt16(ref position));
                }
            case OperandType.InlineString:
                {
                    // 32-bit metadata string token
                    return new InlineStringInstruction(offset, opCode, ReadInt32(ref position), _resolver);
                }
            case OperandType.InlineSig:
                {
                    // 32-bit metadata signature token
                    return new InlineSigInstruction(offset, opCode, ReadInt32(ref position), _resolver);
                }
            case OperandType.InlineMethod:
                {
                    // 32-bit metadata token
                    return new InlineMethodInstruction(offset, opCode, ReadInt32(ref position), _resolver);
                }
            case OperandType.InlineField:
                {
                    // 32-bit metadata token
                    return new InlineFieldInstruction(_resolver, offset, opCode, ReadInt32(ref position));
                }
            case OperandType.InlineType:
                {
                    // 32-bit metadata token
                    return new InlineTypeInstruction(offset, opCode, ReadInt32(ref position), _resolver);
                }
            case OperandType.InlineTok:
                {
                    // FieldRef, MethodRef, or TypeRef token
                    return new InlineTokInstruction(offset, opCode, ReadInt32(ref position), _resolver);
                }
            case OperandType.InlineSwitch:
                {
                    // 32-bit integer argument to a switch instruction
                    int[] deltas = ReadDeltas(ref position);
                    return new InlineSwitchInstruction(offset, opCode, deltas);
                }

            default:
                throw new NotSupportedException($"Unsupported operand type: {opCode.OperandType}");
        }
    }

    private int[] ReadDeltas(ref int position)
    {
        var cases = ReadInt32(ref position);
        var deltas = new int[cases];
        for (var i = 0; i < cases; i++)
            deltas[i] = ReadInt32(ref position);
        return deltas;
    }

    public void Accept(ILInstructionVisitor visitor)
    {
        if (visitor == null)
            throw new ArgumentNullException(nameof(visitor));

        foreach (var instruction in this)
            instruction.Accept(visitor);
    }

    private byte ReadByte(ref int position) => _byteArray[position++];

    private sbyte ReadSByte(ref int position) => (sbyte)ReadByte(ref position);

    private ushort ReadUInt16(ref int position)
    {
        var value = BitConverter.ToUInt16(_byteArray, position);
        position += 2;
        return value;
    }

    private int ReadInt32(ref int position)
    {
        var value = BitConverter.ToInt32(_byteArray, position);
        position += 4;
        return value;
    }

    private long ReadInt64(ref int position)
    {
        var value = BitConverter.ToInt64(_byteArray, position);
        position += 8;
        return value;
    }

    private float ReadSingle(ref int position)
    {
        var value = BitConverter.ToSingle(_byteArray, position);
        position += 4;
        return value;
    }

    private double ReadDouble(ref int position)
    {
        var value = BitConverter.ToDouble(_byteArray, position);
        position += 8;
        return value;
    }
}


public abstract class ILInstruction
{
    public readonly int Offset;
    public readonly OpCode OpCode;
    internal ILInstruction(int offset, OpCode opCode)
    {
        Offset = offset;
        OpCode = opCode;
    }

    public abstract void Accept(ILInstructionVisitor visitor);
}

public class InlineNoneInstruction : ILInstruction
{
    internal InlineNoneInstruction(int offset, OpCode opCode)
        : base(offset, opCode)
    {
    }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineNoneInstruction(this);
}

public class InlineBrTargetInstruction : ILInstruction
{
    internal InlineBrTargetInstruction(int offset, OpCode opCode, int delta)
        : base(offset, opCode) => Delta = delta;

    public int Delta { get; }

    public int TargetOffset => Offset + Delta + 1 + 4;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineBrTargetInstruction(this);
}

public class ShortInlineBrTargetInstruction : ILInstruction
{
    internal ShortInlineBrTargetInstruction(int offset, OpCode opCode, sbyte delta)
        : base(offset, opCode)
    {
        Delta = delta;
    }

    public sbyte Delta { get; }

    public int TargetOffset => Offset + Delta + 1 + 1;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineBrTargetInstruction(this);
}

public class InlineSwitchInstruction : ILInstruction
{
    private readonly int[] m_deltas;
    private int[] m_targetOffsets;

    internal InlineSwitchInstruction(int offset, OpCode opCode, int[] deltas)
        : base(offset, opCode) => m_deltas = deltas;

    public int[] Deltas => (int[])m_deltas.Clone();

    public int[] TargetOffsets
    {
        get
        {
            if (m_targetOffsets == null)
            {
                var cases = m_deltas.Length;
                var itself = 1 + 4 + 4 * cases;
                m_targetOffsets = new int[cases];
                for (var i = 0; i < cases; i++)
                    m_targetOffsets[i] = Offset + m_deltas[i] + itself;
            }
            return m_targetOffsets;
        }
    }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineSwitchInstruction(this);
}

public class InlineIInstruction : ILInstruction
{
    internal InlineIInstruction(int offset, OpCode opCode, int value)
        : base(offset, opCode) => Int32 = value;

    public int Int32 { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineIInstruction(this);
}

public class InlineI8Instruction : ILInstruction
{
    internal InlineI8Instruction(int offset, OpCode opCode, long value)
        : base(offset, opCode) => Int64 = value;

    public long Int64 { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineI8Instruction(this);
}

public class ShortInlineIInstruction : ILInstruction
{
    internal ShortInlineIInstruction(int offset, OpCode opCode, byte value)
        : base(offset, opCode) => Byte = value;

    public byte Byte { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineIInstruction(this);
}

public class InlineRInstruction : ILInstruction
{
    internal InlineRInstruction(int offset, OpCode opCode, double value)
        : base(offset, opCode) => Double = value;

    public double Double { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineRInstruction(this);
}

public class ShortInlineRInstruction : ILInstruction
{
    internal ShortInlineRInstruction(int offset, OpCode opCode, float value)
        : base(offset, opCode) => Single = value;

    public float Single { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineRInstruction(this);
}

public class InlineFieldInstruction : ILInstruction
{
    private readonly ITokenResolver m_resolver;
    private FieldInfo m_field;

    internal InlineFieldInstruction(ITokenResolver resolver, int offset, OpCode opCode, int token)
        : base(offset, opCode)
    {
        m_resolver = resolver;
        Token = token;
    }

    public FieldInfo Field => m_field ?? (m_field = m_resolver.AsField(Token));

    public int Token { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineFieldInstruction(this);
}

public class InlineMethodInstruction : ILInstruction
{
    private readonly ITokenResolver m_resolver;
    private MethodBase m_method;

    internal InlineMethodInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        m_resolver = resolver;
        Token = token;
    }

    public MethodBase Method => m_method ?? (m_method = m_resolver.AsMethod(Token));

    public int Token { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineMethodInstruction(this);
}

public class InlineTypeInstruction : ILInstruction
{
    private readonly ITokenResolver m_resolver;
    private Type m_type;

    internal InlineTypeInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        m_resolver = resolver;
        Token = token;
    }

    public Type Type => m_type ?? (m_type = m_resolver.AsType(Token));

    public int Token { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineTypeInstruction(this);
}

public class InlineSigInstruction : ILInstruction
{
    private readonly ITokenResolver m_resolver;
    private byte[] m_signature;

    internal InlineSigInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        m_resolver = resolver;
        Token = token;
    }

    public byte[] Signature => m_signature ?? (m_signature = m_resolver.AsSignature(Token));

    public int Token { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineSigInstruction(this);
}

public class InlineTokInstruction : ILInstruction
{
    private readonly ITokenResolver m_resolver;
    private MemberInfo m_member;

    internal InlineTokInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        m_resolver = resolver;
        Token = token;
    }

    public MemberInfo Member => m_member ?? (m_member = m_resolver.AsMember(Token));

    public int Token { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineTokInstruction(this);
}

public class InlineStringInstruction : ILInstruction
{
    private readonly ITokenResolver m_resolver;
    private string m_string;

    internal InlineStringInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        m_resolver = resolver;
        Token = token;
    }

    public string String => m_string ?? (m_string = m_resolver.AsString(Token));

    public int Token { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineStringInstruction(this);
}

public class InlineVarInstruction : ILInstruction
{
    internal InlineVarInstruction(int offset, OpCode opCode, ushort ordinal)
        : base(offset, opCode) => Ordinal = ordinal;

    public ushort Ordinal { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineVarInstruction(this);
}

public class ShortInlineVarInstruction : ILInstruction
{
    internal ShortInlineVarInstruction(int offset, OpCode opCode, byte ordinal)
        : base(offset, opCode) => Ordinal = ordinal;

    public byte Ordinal { get; }

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineVarInstruction(this);
}

public interface IILProvider
{
    byte[] GetByteArray();
}

public class MethodBaseILProvider : IILProvider
{
    private static readonly Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
    private static readonly Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

    private readonly MethodBase m_method;
    private byte[] m_byteArray;

    public MethodBaseILProvider(MethodBase method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        var methodType = method.GetType();

        if (methodType != s_runtimeMethodInfoType && methodType != s_runtimeConstructorInfoType)
            throw new ArgumentException("Must have type RuntimeMethodInfo or RuntimeConstructorInfo.", nameof(method));

        m_method = method;
    }

    public byte[] GetByteArray()
    {
        return m_byteArray ?? (m_byteArray = m_method.GetMethodBody()?.GetILAsByteArray() ?? new byte[0]);
    }
}

public class DynamicMethodILProvider : IILProvider
{
#if !NET8_0_OR_GREATER
    private static readonly Type _runtimeILGeneratorType = typeof(ILGenerator);
#else
    private static readonly Type _runtimeILGeneratorType = Type.GetType("System.Reflection.Emit.RuntimeILGenerator");
#endif

    private static readonly FieldInfo _fiLen =
        _runtimeILGeneratorType.GetField("m_length", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo _fiStream =
        _runtimeILGeneratorType.GetField("m_ILStream", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly MethodInfo _miBakeByteArray =
        _runtimeILGeneratorType.GetMethod("BakeByteArray", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly DynamicMethod m_method;
    private byte[] m_byteArray;

    public DynamicMethodILProvider(DynamicMethod method)
    {
        m_method = method;
    }

    public byte[] GetByteArray()
    {
        if (m_byteArray == null)
        {
            var ilgen = m_method.GetILGenerator();
            try
            {
                m_byteArray = (byte[])_miBakeByteArray.Invoke(ilgen, null) ?? new byte[0];
            }
            catch (TargetInvocationException)
            {
                var length = (int)_fiLen.GetValue(ilgen);
                m_byteArray = new byte[length];
                Array.Copy((byte[])_fiStream.GetValue(ilgen), m_byteArray, length);
            }
        }
        return m_byteArray;
    }
}

public interface IFormatProvider
{
    string Int32ToHex(int int32);
    string Int16ToHex(int int16);
    string Int8ToHex(int int8);
    string Argument(int ordinal);
    string EscapedString(string str);
    string Label(int offset);
    string MultipleLabels(int[] offsets);
    string SigByteArrayToString(byte[] sig);
}

public class DefaultFormatProvider : IFormatProvider
{
    private DefaultFormatProvider()
    {
    }

    public static DefaultFormatProvider Instance { get; } = new DefaultFormatProvider();

    public virtual string Int32ToHex(int int32) => int32.ToString("X8");
    public virtual string Int16ToHex(int int16) => int16.ToString("X4");
    public virtual string Int8ToHex(int int8) => int8.ToString("X2");
    public virtual string Argument(int ordinal) => $"V_{ordinal}";
    public virtual string Label(int offset) => $"IL_{offset:x4}";

    public virtual string MultipleLabels(int[] offsets)
    {
        var sb = new StringBuilder();
        var length = offsets.Length;
        for (var i = 0; i < length; i++)
        {
            sb.AppendFormat(i == 0 ? "(" : ", ");
            sb.Append(Label(offsets[i]));
        }
        sb.AppendFormat(")");
        return sb.ToString();
    }

    public virtual string EscapedString(string str)
    {
        var length = str.Length;
        var sb = new StringBuilder(length * 2);

        sb.Append('"');
        for (var i = 0; i < length; i++)
        {
            var ch = str[i];
            if (ch == '\t')
                sb.Append("\\t");
            else if (ch == '\n')
                sb.Append("\\n");
            else if (ch == '\r')
                sb.Append("\\r");
            else if (ch == '\"')
                sb.Append("\\\"");
            else if (ch == '\\')
                sb.Append("\\");
            else if (ch < 0x20 || ch >= 0x7f)
                sb.AppendFormat("\\u{0:x4}", (int)ch);
            else
                sb.Append(ch);
        }
        sb.Append('"');

        return sb.ToString();
    }

    public virtual string SigByteArrayToString(byte[] sig)
    {
        var sb = new StringBuilder();
        var length = sig.Length;
        for (var i = 0; i < length; i++)
        {
            sb.AppendFormat(i == 0 ? "SIG [" : " ");
            sb.Append(Int8ToHex(sig[i]));
        }
        sb.AppendFormat("]");
        return sb.ToString();
    }
}

public interface IILStringCollector
{
    void Process(ILInstruction ilInstruction, string operandString);
}

public class ReadableILStringToTextWriter : IILStringCollector
{
    protected TextWriter writer;

    public ReadableILStringToTextWriter(TextWriter writer)
    {
        this.writer = writer;
    }

    public virtual void Process(ILInstruction ilInstruction, string operandString)
    {
        writer.WriteLine("IL_{0:x4}: {1,-10} {2}",
            ilInstruction.Offset,
            ilInstruction.OpCode.Name,
            operandString);
    }
}

public class RawILStringToTextWriter : ReadableILStringToTextWriter
{
    public RawILStringToTextWriter(TextWriter writer)
        : base(writer)
    {
    }

    public override void Process(ILInstruction ilInstruction, string operandString)
    {
        writer.WriteLine("IL_{0:x4}: {1,-4:x2}| {2, -8}",
            ilInstruction.Offset,
            ilInstruction.OpCode.Value,
            operandString);
    }
}

public abstract class ILInstructionVisitor
{
    public virtual void VisitInlineBrTargetInstruction(InlineBrTargetInstruction inlineBrTargetInstruction)
    {
    }

    public virtual void VisitInlineFieldInstruction(InlineFieldInstruction inlineFieldInstruction)
    {
    }

    public virtual void VisitInlineIInstruction(InlineIInstruction inlineIInstruction)
    {
    }

    public virtual void VisitInlineI8Instruction(InlineI8Instruction inlineI8Instruction)
    {
    }

    public virtual void VisitInlineMethodInstruction(InlineMethodInstruction inlineMethodInstruction)
    {
    }

    public virtual void VisitInlineNoneInstruction(InlineNoneInstruction inlineNoneInstruction)
    {
    }

    public virtual void VisitInlineRInstruction(InlineRInstruction inlineRInstruction)
    {
    }

    public virtual void VisitInlineSigInstruction(InlineSigInstruction inlineSigInstruction)
    {
    }

    public virtual void VisitInlineStringInstruction(InlineStringInstruction inlineStringInstruction)
    {
    }

    public virtual void VisitInlineSwitchInstruction(InlineSwitchInstruction inlineSwitchInstruction)
    {
    }

    public virtual void VisitInlineTokInstruction(InlineTokInstruction inlineTokInstruction)
    {
    }

    public virtual void VisitInlineTypeInstruction(InlineTypeInstruction inlineTypeInstruction)
    {
    }

    public virtual void VisitInlineVarInstruction(InlineVarInstruction inlineVarInstruction)
    {
    }

    public virtual void VisitShortInlineBrTargetInstruction(ShortInlineBrTargetInstruction shortInlineBrTargetInstruction)
    {
    }

    public virtual void VisitShortInlineIInstruction(ShortInlineIInstruction shortInlineIInstruction)
    {
    }

    public virtual void VisitShortInlineRInstruction(ShortInlineRInstruction shortInlineRInstruction)
    {
    }

    public virtual void VisitShortInlineVarInstruction(ShortInlineVarInstruction shortInlineVarInstruction)
    {
    }
}

public class ReadableILStringVisitor : ILInstructionVisitor
{
    protected IFormatProvider formatProvider;
    protected IILStringCollector collector;

    public ReadableILStringVisitor(IILStringCollector collector)
        : this(collector, DefaultFormatProvider.Instance)
    {
    }

    public ReadableILStringVisitor(IILStringCollector collector, IFormatProvider formatProvider)
    {
        this.formatProvider = formatProvider;
        this.collector = collector;
    }

    public override void VisitInlineBrTargetInstruction(InlineBrTargetInstruction inlineBrTargetInstruction)
    {
        collector.Process(inlineBrTargetInstruction, formatProvider.Label(inlineBrTargetInstruction.TargetOffset));
    }

    public override void VisitInlineFieldInstruction(InlineFieldInstruction inlineFieldInstruction)
    {
        string field;
        try
        {
            field = inlineFieldInstruction.Field + "/" + inlineFieldInstruction.Field.DeclaringType;
        }
        catch (Exception ex)
        {
            field = "!" + ex.Message + "!";
        }
        collector.Process(inlineFieldInstruction, field);
    }

    public override void VisitInlineIInstruction(InlineIInstruction inlineIInstruction)
    {
        collector.Process(inlineIInstruction, inlineIInstruction.Int32.ToString());
    }

    public override void VisitInlineI8Instruction(InlineI8Instruction inlineI8Instruction)
    {
        collector.Process(inlineI8Instruction, inlineI8Instruction.Int64.ToString());
    }

    public override void VisitInlineMethodInstruction(InlineMethodInstruction inlineMethodInstruction)
    {
        string method;
        try
        {
            method = inlineMethodInstruction.Method + "/" + inlineMethodInstruction.Method.DeclaringType;
        }
        catch (Exception ex)
        {
            method = "!" + ex.Message + "!";
        }
        collector.Process(inlineMethodInstruction, method);
    }

    public override void VisitInlineNoneInstruction(InlineNoneInstruction inlineNoneInstruction)
    {
        collector.Process(inlineNoneInstruction, string.Empty);
    }

    public override void VisitInlineRInstruction(InlineRInstruction inlineRInstruction)
    {
        collector.Process(inlineRInstruction, inlineRInstruction.Double.ToString());
    }

    public override void VisitInlineSigInstruction(InlineSigInstruction inlineSigInstruction)
    {
        collector.Process(inlineSigInstruction, formatProvider.SigByteArrayToString(inlineSigInstruction.Signature));
    }

    public override void VisitInlineStringInstruction(InlineStringInstruction inlineStringInstruction)
    {
        collector.Process(inlineStringInstruction, formatProvider.EscapedString(inlineStringInstruction.String));
    }

    public override void VisitInlineSwitchInstruction(InlineSwitchInstruction inlineSwitchInstruction)
    {
        collector.Process(inlineSwitchInstruction, formatProvider.MultipleLabels(inlineSwitchInstruction.TargetOffsets));
    }

    public override void VisitInlineTokInstruction(InlineTokInstruction inlineTokInstruction)
    {
        string member;
        try
        {
            member = inlineTokInstruction.Member + "/" + inlineTokInstruction.Member.DeclaringType;
        }
        catch (Exception ex)
        {
            member = "!" + ex.Message + "!";
        }
        collector.Process(inlineTokInstruction, member);
    }

    public override void VisitInlineTypeInstruction(InlineTypeInstruction inlineTypeInstruction)
    {
        string type;
        try
        {
            type = inlineTypeInstruction.Type.ToString();
        }
        catch (Exception ex)
        {
            type = "!" + ex.Message + "!";
        }
        collector.Process(inlineTypeInstruction, type);
    }

    public override void VisitInlineVarInstruction(InlineVarInstruction inlineVarInstruction)
    {
        collector.Process(inlineVarInstruction, formatProvider.Argument(inlineVarInstruction.Ordinal));
    }

    public override void VisitShortInlineBrTargetInstruction(ShortInlineBrTargetInstruction shortInlineBrTargetInstruction)
    {
        collector.Process(shortInlineBrTargetInstruction, formatProvider.Label(shortInlineBrTargetInstruction.TargetOffset));
    }

    public override void VisitShortInlineIInstruction(ShortInlineIInstruction shortInlineIInstruction)
    {
        collector.Process(shortInlineIInstruction, shortInlineIInstruction.Byte.ToString());
    }

    public override void VisitShortInlineRInstruction(ShortInlineRInstruction shortInlineRInstruction)
    {
        collector.Process(shortInlineRInstruction, shortInlineRInstruction.Single.ToString());
    }

    public override void VisitShortInlineVarInstruction(ShortInlineVarInstruction shortInlineVarInstruction)
    {
        collector.Process(shortInlineVarInstruction, formatProvider.Argument(shortInlineVarInstruction.Ordinal));
    }
}

public class RawILStringVisitor : ReadableILStringVisitor
{
    public RawILStringVisitor(IILStringCollector collector)
        : this(collector, DefaultFormatProvider.Instance)
    {
    }

    public RawILStringVisitor(IILStringCollector collector, IFormatProvider formatProvider)
        : base(collector, formatProvider)
    {
    }

    public override void VisitInlineBrTargetInstruction(InlineBrTargetInstruction inlineBrTargetInstruction)
    {
        collector.Process(inlineBrTargetInstruction, formatProvider.Int32ToHex(inlineBrTargetInstruction.Delta));
    }

    public override void VisitInlineFieldInstruction(InlineFieldInstruction inlineFieldInstruction)
    {
        collector.Process(inlineFieldInstruction, formatProvider.Int32ToHex(inlineFieldInstruction.Token));
    }

    public override void VisitInlineMethodInstruction(InlineMethodInstruction inlineMethodInstruction)
    {
        collector.Process(inlineMethodInstruction, formatProvider.Int32ToHex(inlineMethodInstruction.Token));
    }

    public override void VisitInlineSigInstruction(InlineSigInstruction inlineSigInstruction)
    {
        collector.Process(inlineSigInstruction, formatProvider.Int32ToHex(inlineSigInstruction.Token));
    }

    public override void VisitInlineStringInstruction(InlineStringInstruction inlineStringInstruction)
    {
        collector.Process(inlineStringInstruction, formatProvider.Int32ToHex(inlineStringInstruction.Token));
    }

    public override void VisitInlineSwitchInstruction(InlineSwitchInstruction inlineSwitchInstruction)
    {
        collector.Process(inlineSwitchInstruction, "...");
    }

    public override void VisitInlineTokInstruction(InlineTokInstruction inlineTokInstruction)
    {
        collector.Process(inlineTokInstruction, formatProvider.Int32ToHex(inlineTokInstruction.Token));
    }

    public override void VisitInlineTypeInstruction(InlineTypeInstruction inlineTypeInstruction)
    {
        collector.Process(inlineTypeInstruction, formatProvider.Int32ToHex(inlineTypeInstruction.Token));
    }

    public override void VisitInlineVarInstruction(InlineVarInstruction inlineVarInstruction)
    {
        collector.Process(inlineVarInstruction, formatProvider.Int16ToHex(inlineVarInstruction.Ordinal));
    }

    public override void VisitShortInlineBrTargetInstruction(ShortInlineBrTargetInstruction shortInlineBrTargetInstruction)
    {
        collector.Process(shortInlineBrTargetInstruction, formatProvider.Int8ToHex(shortInlineBrTargetInstruction.Delta));
    }

    public override void VisitShortInlineVarInstruction(ShortInlineVarInstruction shortInlineVarInstruction)
    {
        collector.Process(shortInlineVarInstruction, formatProvider.Int8ToHex(shortInlineVarInstruction.Ordinal));
    }
}

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

internal class DynamicScopeTokenResolver : ITokenResolver
{
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

    private readonly object m_scope;

    private object this[int token] => s_indexer.GetValue(m_scope, new object[] { token });

    public DynamicScopeTokenResolver(DynamicMethod dm)
    {
        m_scope = s_scopeFi.GetValue(dm.GetILGenerator());
    }

    public string AsString(int token) => this[token] as string;

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

    public byte[] AsSignature(int token) => this[token] as byte[];
}