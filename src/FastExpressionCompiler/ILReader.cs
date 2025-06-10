// #define DEBUG_INTERNALS

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Linq;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ImTools;
namespace FastExpressionCompiler.LightExpression.ILDecoder;
#else
using FastExpressionCompiler.ImTools;
namespace FastExpressionCompiler.ILDecoder;
#endif

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uses reflection on internal types and is not trim-compatible.")]
public static class ILReaderFactory
{
    private static readonly Type _runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
    private static readonly Type _runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

#if !NET6_0_OR_GREATER
    private static readonly Type _rtDynamicMethodType =
        Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod");
    private static readonly FieldInfo _fiOwner =
        _rtDynamicMethodType.GetField("m_owner", BindingFlags.Instance | BindingFlags.NonPublic);
#endif

    public static ILReader GetILReaderOrNull(MethodBase source)
    {
        var sourceType = source.GetType();
        var dynamicMethod = source as DynamicMethod;

#if DEBUG_INTERNALS
        Console.WriteLine($"sourceType: {sourceType}");
        Console.WriteLine($"dynamicMethod >= NET8: {(dynamicMethod?.ToString() ?? "null")}");
        Console.WriteLine($"m_runtimeMethodInfoType: {_runtimeMethodInfoType},\n_runtimeConstructorInfoType: {_runtimeConstructorInfoType} ");
#endif

#if !NET6_0_OR_GREATER
        if (dynamicMethod == null & sourceType == _rtDynamicMethodType)
            dynamicMethod = (DynamicMethod)_fiOwner.GetValue(source);
#if DEBUG_INTERNALS
        Console.WriteLine($"m_rtDynamicMethodType: {_rtDynamicMethodType}, _fiOwner: {_fiOwner}");
        Console.WriteLine($"dynamicMethod < NET8: {(dynamicMethod?.ToString() ?? "null")}");
#endif
#endif
        if (dynamicMethod != null && DynamicScopeTokenResolver.IsSupported)
            return new ILReader(new DynamicMethodILProvider(dynamicMethod), new DynamicScopeTokenResolver(dynamicMethod));

        if (sourceType == _runtimeMethodInfoType ||
            sourceType == _runtimeConstructorInfoType)
            return new ILReader(source);

        Debug.WriteLine($"Reading IL from type {sourceType} is currently not supported");
        return null;
    }

    public static StringBuilder ToILString(this MethodInfo method, StringBuilder s = null)
    {
        var il = GetILReaderOrNull(method);
        return il != null
            ? il.ToILString(s)
            : (s ?? new StringBuilder()).AppendLine($"ILReader for {method} is not supported");
    }

    public static ILInstruction[] ReadAllInstructions(this MethodBase source) =>
        GetILReaderOrNull(source)?.ToArray() ?? [];

    public static StringBuilder ToILString(this IEnumerable<ILInstruction> ilInstructions, StringBuilder s = null)
    {
        s ??= new StringBuilder();
        var line = 0;
        foreach (var il in ilInstructions)
        {
            try
            {
                s = line++ > 0 ? s.AppendLine() : s;
                ILFormatter.Label(s, il.Offset).Append(": ").Append(il.OpCode);
                switch (il.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        ILFormatter.Label(s.Append(' '), ((InlineBrTargetInstruction)il).TargetOffset);
                        break;
                    case OperandType.InlineI:
                        s.Append(' ').Append(((InlineIInstruction)il).Int32);
                        break;
                    case OperandType.InlineField:
                        var f = (InlineFieldInstruction)il;
                        s.Append(' ')
                            .AppendTypeName(f.Field.FieldType).Append(' ')
                            .AppendTypeName(f.Field.DeclaringType).Append('.')
                            .Append(f.Field.Name);
                        break;
                    case OperandType.InlineI8:
                        s.Append(' ').Append(((InlineI8Instruction)il).Int64);
                        break;
                    case OperandType.InlineMethod:
                        var m = (InlineMethodInstruction)il;
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
                        break;
                    case OperandType.InlineNone:
                        break;
                    case OperandType.InlineR:
                        s.Append(' ').Append(((InlineRInstruction)il).Double);
                        break;
                    case OperandType.InlineSig:
                        ILFormatter.SigByteArrayToString(s.Append(' '), ((InlineSigInstruction)il).Signature);
                        break;
                    case OperandType.InlineString:
                        ILFormatter.EscapedString(s.Append(' '), ((InlineStringInstruction)il).String);
                        break;
                    case OperandType.InlineSwitch:
                        ILFormatter.MultipleLabels(s.Append(" switch "), ((InlineSwitchInstruction)il).TargetOffsets);
                        break;
                    case OperandType.InlineTok:
                        s.Append(' ').Append(((InlineTokInstruction)il).Member.Name);
                        break;
                    case OperandType.InlineType:
                        s.Append(' ').AppendTypeName(((InlineTypeInstruction)il).Type);
                        break;
                    case OperandType.InlineVar:
                        ILFormatter.Argument(s.Append(' '), ((InlineVarInstruction)il).Ordinal);
                        break;
                    case OperandType.ShortInlineBrTarget:
                        s.Append(' ').Append(((ShortInlineBrTargetInstruction)il).TargetOffset);
                        break;
                    case OperandType.ShortInlineI:
                        s.Append(' ').Append(((ShortInlineIInstruction)il).Byte);
                        break;
                    case OperandType.ShortInlineR:
                        s.Append(' ').Append(((ShortInlineRInstruction)il).Single);
                        break;
                    case OperandType.ShortInlineVar:
                        ILFormatter.Argument(s.Append(' '), ((ShortInlineVarInstruction)il).Ordinal);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                s.AppendLine().AppendLine("EXCEPTION_IN_IL_PRINT: " + ex.Message).AppendLine();
            }
        }
        return s;
    }

    public static StringBuilder AppendTypeName(this StringBuilder sb, Type type) =>
        sb.Append(type.ToCode(stripNamespace: true));
}

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uses reflection on internal types and is not trim-compatible.")]
public sealed class ILReader : IEnumerable<ILInstruction>
{
    private static readonly OpCode[] _oneByteOpCodes = new OpCode[0x100];
    private static readonly OpCode[] _twoByteOpCodes = new OpCode[0x100];

    static ILReader()
    {
        // Populate the one-byte and two-byte OpCode arrays
        foreach (var fi in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var opCode = (OpCode)fi.GetValue(null);
            var value = (ushort)opCode.Value;

            if (value < 0x100) // 0x100 - 256, 0b0000_0000_0000_0000
                _oneByteOpCodes[value] = opCode;
            else if ((value & 0xff00) == 0xfe00) // 0xFF00 - 0b1111_1111_0000_0000, 0xFE00 - 0b1111_1110_0000_0000
                _twoByteOpCodes[value & 0xff] = opCode;
        }
    }

    private readonly ITokenResolver _resolver;
    private readonly byte[] _byteArray;

    public ILReader(IILProvider ilProvider, ITokenResolver tokenResolver)
    {
        _resolver = tokenResolver;
        _byteArray = ilProvider?.GetByteArray() ?? throw new ArgumentNullException(nameof(ilProvider));
    }

    public ILReader(MethodBase method)
        : this(new MethodBaseILProvider(method), new ModuleScopeTokenResolver(method)) { }

    // todo: @perf implement optimized IEnumerator<OpCode> which does not need to allocate ILInstruction objects, let's try a data-oriented modeling! 
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

        // Read first 1 or 2 bytes as opCode
        var code = ReadByte(ref position);
        var opCode = code != 0xFE
            ? _oneByteOpCodes[code]
            : _twoByteOpCodes[ReadByte(ref position)];

        int token;
        return opCode.OperandType switch
        {
            // 32-bit integer branch target
            OperandType.InlineBrTarget => new InlineBrTargetInstruction(offset, opCode, ReadInt32(ref position)),
            // 32-bit metadata token
            OperandType.InlineField => new InlineFieldInstruction(offset, opCode, token = ReadInt32(ref position), _resolver.AsField(token)),
            // 32-bit integer
            OperandType.InlineI => new InlineIInstruction(offset, opCode, ReadInt32(ref position)),
            // 64-bit integer
            OperandType.InlineI8 => new InlineI8Instruction(offset, opCode, ReadInt64(ref position)),
            // 32-bit metadata token
            OperandType.InlineMethod => new InlineMethodInstruction(offset, opCode, token = ReadInt32(ref position), _resolver.AsMethod(token)),
            OperandType.InlineNone => new InlineNoneInstruction(offset, opCode),
            // 64-bit IEEE floating point number
            OperandType.InlineR => new InlineRInstruction(offset, opCode, ReadDouble(ref position)),
            // 32-bit metadata signature token
            OperandType.InlineSig => new InlineSigInstruction(offset, opCode, token = ReadInt32(ref position), _resolver.AsSignature(token)),
            // 32-bit metadata string token
            OperandType.InlineString => new InlineStringInstruction(offset, opCode, token = ReadInt32(ref position), _resolver.AsString(token)),
            // 32-bit integer argument to a switch instruction
            OperandType.InlineSwitch => new InlineSwitchInstruction(offset, opCode, ReadDeltas(ref position)),
            // FieldRef, MethodRef, or TypeRef token
            OperandType.InlineTok => new InlineTokInstruction(offset, opCode, token = ReadInt32(ref position), _resolver.AsMember(token)),
            // 32-bit metadata token
            OperandType.InlineType => new InlineTypeInstruction(offset, opCode, token = ReadInt32(ref position), _resolver.AsType(token)),
            // 16-bit integer containing the ordinal of a local variable or an argument
            OperandType.InlineVar => new InlineVarInstruction(offset, opCode, ReadUInt16(ref position)),
            // 8-bit integer branch target
            OperandType.ShortInlineBrTarget => new ShortInlineBrTargetInstruction(offset, opCode, ReadSByte(ref position)),
            // 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
            OperandType.ShortInlineI => new ShortInlineIInstruction(offset, opCode, ReadByte(ref position)),
            // 32-bit IEEE floating point number
            OperandType.ShortInlineR => new ShortInlineRInstruction(offset, opCode, ReadSingle(ref position)),
            // 8-bit integer containing the ordinal of a local variable or an argument
            OperandType.ShortInlineVar => new ShortInlineVarInstruction(offset, opCode, ReadByte(ref position)),
            _ => throw new NotSupportedException($"Unsupported operand type: {opCode.OperandType}"),
        };
    }

    private int[] ReadDeltas(ref int position)
    {
        var caseCount = ReadInt32(ref position);
        var deltas = new int[caseCount];
        for (var i = 0; i < caseCount; i++)
            deltas[i] = ReadInt32(ref position);
        return deltas;
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
    public abstract OperandType OperandType { get; }
    public readonly int Offset;
    public readonly OpCode OpCode;
    internal ILInstruction(int offset, OpCode opCode)
    {
        Offset = offset;
        OpCode = opCode;
    }
}

//todo: @wip APL/DOA like modeling of IL instructions
// internal struct BaseIL
// {
//     public OperandType OperandType;
//     public int Offset;
//     public OpCode OpCode;

//     // List of possible extras:
//     // - InlineNoneInstruction does not have an extra
//     //
//     // - Stores `int` for 
//     // OperandType.InlineBrTarget->Delta, 
//     // OperandType.ShortInlineBrTarget->Delta, 
//     // OperandType.InlineI->Int32
//     // todo: may be store delta inline as ExtraOpArrayIndex itself
//     public const int ExtraDeltasArrayIndex = 1;

//     // This is for OperandType.InlineSwitch
//     public const int ExtraSwitchesArrayIndex = 2;

//     public int ExtraOpArrayIndex;
//     public int ExtraOpItemIndex;
// }

///<summary>Data-oriented structure SOA of IL instructions.</summary>
// internal struct ILs
// {
//     public SmallList<BaseIL, Stack16<BaseIL>> BaseILs;
//     public SmallList<int, Stack16<int>> Deltas;
//     public SmallList<(int[] Deltas, int[] TargetOffsets), Stack2<(int[] Deltas, int[] TargetOffsets)>> Switches;
// }

public sealed class InlineNoneInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineNone;
    internal InlineNoneInstruction(int offset, OpCode opCode)
        : base(offset, opCode) { }
}

public sealed class InlineBrTargetInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineBrTarget;
    public readonly int Delta;
    public int TargetOffset => Offset + Delta + 1 + 4;
    internal InlineBrTargetInstruction(int offset, OpCode opCode, int delta)
        : base(offset, opCode) => Delta = delta;
}

public sealed class ShortInlineBrTargetInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineBrTarget;
    public readonly sbyte Delta;
    public int TargetOffset => Offset + Delta + 1 + 1;
    internal ShortInlineBrTargetInstruction(int offset, OpCode opCode, sbyte delta)
        : base(offset, opCode) => Delta = delta;
}

public sealed class InlineSwitchInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineSwitch;
    public readonly int[] Deltas;
    public readonly int[] TargetOffsets;
    internal InlineSwitchInstruction(int offset, OpCode opCode, int[] deltas)
        : base(offset, opCode)
    {
        Deltas = deltas;

        var caseCount = deltas.Length;
        var itself = 1 + 4 + 4 * caseCount;
        var targetOffsets = new int[caseCount];
        for (var i = 0; i < caseCount; i++)
            targetOffsets[i] = Offset + deltas[i] + itself;

        TargetOffsets = targetOffsets;
    }
}

public sealed class InlineIInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineI;
    public readonly int Int32;
    internal InlineIInstruction(int offset, OpCode opCode, int value)
        : base(offset, opCode) => Int32 = value;
}

public sealed class InlineI8Instruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineI8;
    public readonly long Int64;
    internal InlineI8Instruction(int offset, OpCode opCode, long value)
        : base(offset, opCode) => Int64 = value;
}

public sealed class ShortInlineIInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineI;
    public readonly byte Byte;
    internal ShortInlineIInstruction(int offset, OpCode opCode, byte value)
        : base(offset, opCode) => Byte = value;
}

public class InlineRInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineR;
    public readonly double Double;
    internal InlineRInstruction(int offset, OpCode opCode, double value)
        : base(offset, opCode) => Double = value;
}

public sealed class ShortInlineRInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineR;
    public readonly float Single;
    internal ShortInlineRInstruction(int offset, OpCode opCode, float value)
        : base(offset, opCode) => Single = value;
}

public sealed class InlineFieldInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineField;
    public readonly int Token;
    public readonly FieldInfo Field;
    internal InlineFieldInstruction(int offset, OpCode opCode, int token, FieldInfo field)
        : base(offset, opCode)
    {
        Token = token;
        Field = field;
    }
}

public sealed class InlineMethodInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineMethod;
    public readonly int Token;
    public readonly MethodBase Method;
    internal InlineMethodInstruction(int offset, OpCode opCode, int token, MethodBase method)
        : base(offset, opCode)
    {
        Token = token;
        Method = method;
    }
}

public sealed class InlineTypeInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineType;
    public readonly int Token;
    public readonly Type Type;
    internal InlineTypeInstruction(int offset, OpCode opCode, int token, Type type)
        : base(offset, opCode)
    {
        Token = token;
        Type = type;
    }
}

public sealed class InlineSigInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineSig;
    public readonly int Token;
    public readonly byte[] Signature;
    internal InlineSigInstruction(int offset, OpCode opCode, int token, byte[] signature)
        : base(offset, opCode)
    {
        Signature = signature;
        Token = token;
    }
}

public sealed class InlineTokInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineTok;
    public readonly int Token;
    public readonly MemberInfo Member;
    internal InlineTokInstruction(int offset, OpCode opCode, int token, MemberInfo member)
        : base(offset, opCode)
    {
        Token = token;
        Member = member;
    }
}

public sealed class InlineStringInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineString;
    public readonly int Token;
    public readonly string String;
    internal InlineStringInstruction(int offset, OpCode opCode, int token, string s)
        : base(offset, opCode)
    {
        String = s;
        Token = token;
    }
}

public sealed class InlineVarInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineVar;
    public readonly ushort Ordinal;
    internal InlineVarInstruction(int offset, OpCode opCode, ushort ordinal)
        : base(offset, opCode) => Ordinal = ordinal;
}

public sealed class ShortInlineVarInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineVar;
    public readonly byte Ordinal;
    internal ShortInlineVarInstruction(int offset, OpCode opCode, byte ordinal)
        : base(offset, opCode) => Ordinal = ordinal;
}

public interface IILProvider
{
    byte[] GetByteArray();
}

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uses reflection on internal types and is not trim-compatible.")]
public class MethodBaseILProvider : IILProvider
{
    private static readonly Type _runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo");
    private static readonly Type _runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo");

    private readonly byte[] _byteArray;

    public MethodBaseILProvider(MethodBase method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        var methodType = method.GetType();
        if (methodType != _runtimeMethodInfoType & methodType != _runtimeConstructorInfoType)
            throw new ArgumentException("Must have type RuntimeMethodInfo or RuntimeConstructorInfo.", nameof(method));

        _byteArray = method.GetMethodBody()?.GetILAsByteArray() ?? [];
    }

    public byte[] GetByteArray() => _byteArray;
}

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uses reflection on internal types and is not trim-compatible.")]
[UnconditionalSuppressMessage("Trimming", "IL2070:Method expects parameter 'type' to be dynamically accessible. The type obtained via 'Type.GetType' is not known statically.", Justification = "Uses reflection on internal types and is not trim-compatible.")]
[UnconditionalSuppressMessage("Trimming", "IL2080:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'target method'. The field/type does not have matching annotations.", Justification = "Uses reflection on internal types and is not trim-compatible.")]
[UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' requirements.", Justification = "Uses reflection on internal types and is not trim-compatible.")]
[UnconditionalSuppressMessage("Trimming", "IL2069:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' requirements.", Justification = "Uses reflection on internal types and is not trim-compatible.")]
public class DynamicMethodILProvider : IILProvider
{
#if !NET6_0_OR_GREATER
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

    private readonly byte[] _byteArray;

    public DynamicMethodILProvider(DynamicMethod method)
    {
        var ilgen = method.GetILGenerator();
        try
        {
            _byteArray = (byte[])_miBakeByteArray.Invoke(ilgen, null) ?? [];
        }
        catch (TargetInvocationException)
        {
            var length = (int)_fiLen.GetValue(ilgen);
            _byteArray = new byte[length];
            Array.Copy((byte[])_fiStream.GetValue(ilgen), _byteArray, length);
        }
    }

    public byte[] GetByteArray() => _byteArray;
}

public static class ILFormatter
{
    public static StringBuilder Int32ToHex(StringBuilder sb, int int32) => sb.Append(int32.ToString("X8"));
    public static StringBuilder Int16ToHex(StringBuilder sb, int int16) => sb.Append(int16.ToString("X4"));
    public static StringBuilder Int8ToHex(StringBuilder sb, int int8) => sb.Append(int8.ToString("X2"));
    public static StringBuilder Argument(StringBuilder sb, int ordinal) => sb.Append($"V_{ordinal}");
    public static StringBuilder Label(StringBuilder sb, int offset) => sb.Append($"IL_{offset:D4}");

    public static StringBuilder MultipleLabels(StringBuilder sb, int[] offsets)
    {
        var length = offsets.Length;
        for (var i = 0; i < length; i++)
        {
            sb.AppendFormat(i == 0 ? "(" : ", ");
            sb.Append(Label(sb, offsets[i]));
        }
        sb.AppendFormat(")");
        return sb;
    }

    public static StringBuilder EscapedString(StringBuilder sb, string str)
    {
        var length = str.Length;

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
        return sb;
    }

    public static StringBuilder SigByteArrayToString(StringBuilder sb, byte[] sig)
    {
        var length = sig.Length;
        for (var i = 0; i < length; i++)
        {
            sb.AppendFormat(i == 0 ? "SIG [" : " ");
            sb.Append(Int8ToHex(sb, sig[i]));
        }
        sb.AppendFormat("]");
        return sb;
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

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uses reflection on internal types and is not trim-compatible.")]
public sealed class ModuleScopeTokenResolver : ITokenResolver
{
    private readonly Module _module;
    private readonly Type[] _methodContext;
    private readonly Type[] _typeContext;

    public ModuleScopeTokenResolver(MethodBase method)
    {
        _module = method.Module;
        _methodContext = method is ConstructorInfo ? null : method.GetGenericArguments();
        _typeContext = method.DeclaringType?.GetGenericArguments();
    }

    public MethodBase AsMethod(int token) => _module.ResolveMethod(token, _typeContext, _methodContext);
    public FieldInfo AsField(int token) => _module.ResolveField(token, _typeContext, _methodContext);
    public Type AsType(int token) => _module.ResolveType(token, _typeContext, _methodContext);
    public MemberInfo AsMember(int token) => _module.ResolveMember(token, _typeContext, _methodContext);
    public string AsString(int token) => _module.ResolveString(token);
    public byte[] AsSignature(int token) => _module.ResolveSignature(token);
}

[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uses reflection on internal types and is not trim-compatible.")]
[UnconditionalSuppressMessage("Trimming", "IL2080:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'target method'. The field/type does not have matching annotations.", Justification = "Uses reflection on internal types and is not trim-compatible.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Reflection on internal types, not trim-compatible.")]
internal sealed class DynamicScopeTokenResolver : ITokenResolver
{
    static readonly FieldInfo ILGeneratorField;
    private static readonly PropertyInfo _ilGeneratorScopeIndexer;
    private static readonly FieldInfo _ilGeneratorScope;

    private static readonly Type _genMethodInfoType;
    private static readonly FieldInfo _genmethFi1;
    private static readonly FieldInfo _genmethFi2;

    private static readonly Type _varArgMethodType;
    private static readonly FieldInfo _varargFi1;

    private static readonly Type _genFieldInfoType;
    private static readonly FieldInfo _genfieldFi1;
    private static readonly FieldInfo _genfieldFi2;

    public static bool IsSupported => ILGeneratorField != null;

    static DynamicScopeTokenResolver()
    {
        const BindingFlags instanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

        ILGeneratorField = typeof(DynamicMethod).GetField("_ilGenerator", instanceNonPublic);

        // Stop at this moment if the DynamicILGenerator type is not found.
        if (ILGeneratorField == null)
            return;

        var dynamicIlGeneratorType = ILGeneratorField.FieldType;
        _ilGeneratorScope = dynamicIlGeneratorType.GetField("m_scope", instanceNonPublic)
            ?? throw new InvalidOperationException("DynamicILGenerator._scope field is not found");

        var dynamicScopeType = _ilGeneratorScope.FieldType;
        _ilGeneratorScopeIndexer = dynamicScopeType.GetProperty("Item", instanceNonPublic)
            ?? throw new InvalidOperationException("DynamicScope.Item property is not found");

        _varArgMethodType = Type.GetType("System.Reflection.Emit.VarArgMethod");
        _varargFi1 = _varArgMethodType.GetField("m_method", instanceNonPublic);

        _genMethodInfoType = Type.GetType("System.Reflection.Emit.GenericMethodInfo");
        _genmethFi1 = _genMethodInfoType.GetField("m_methodHandle", instanceNonPublic);
        _genmethFi2 = _genMethodInfoType.GetField("m_context", instanceNonPublic);

        _genFieldInfoType = Type.GetType("System.Reflection.Emit.GenericFieldInfo", throwOnError: false);

        _genfieldFi1 = _genFieldInfoType?.GetField("m_fieldHandle", instanceNonPublic);
        _genfieldFi2 = _genFieldInfoType?.GetField("m_context", instanceNonPublic);
    }

    private readonly object _scope;

    private object this[int token] => _ilGeneratorScopeIndexer.GetValue(_scope, [token]);

    public DynamicScopeTokenResolver(DynamicMethod dm) =>
        _scope = _ilGeneratorScope.GetValue(dm.GetILGenerator());

    public string AsString(int token) => this[token] as string;

    public FieldInfo AsField(int token)
    {
        if (this[token] is RuntimeFieldHandle)
            return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)this[token]);

        if (this[token].GetType() == _genFieldInfoType)
            return FieldInfo.GetFieldFromHandle(
                (RuntimeFieldHandle)_genfieldFi1.GetValue(this[token]),
                (RuntimeTypeHandle)_genfieldFi2.GetValue(this[token]));

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

        if (this[token].GetType() == _genMethodInfoType)
            return MethodBase.GetMethodFromHandle(
                (RuntimeMethodHandle)_genmethFi1.GetValue(this[token]),
                (RuntimeTypeHandle)_genmethFi2.GetValue(this[token]));

        if (this[token].GetType() == _varArgMethodType)
            return (MethodInfo)_varargFi1.GetValue(this[token]);

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

#pragma warning restore CS1591