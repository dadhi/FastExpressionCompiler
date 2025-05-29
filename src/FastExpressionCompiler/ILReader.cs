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
namespace FastExpressionCompiler.LightExpression.ILDecoder;
#else
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
                s.Append($"{il.Offset,-4}{il.OpCode}");
                switch (il.OperandType)
                {
                    case OperandType.InlineField:
                        var f = (InlineFieldInstruction)il;
                        s.Append(' ')
                            .AppendTypeName(f.Field.FieldType).Append(' ')
                            .AppendTypeName(f.Field.DeclaringType).Append('.')
                            .Append(f.Field.Name);
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
                    case OperandType.InlineType:
                        var t = (InlineTypeInstruction)il;
                        s.Append(' ').AppendTypeName(t.Type);
                        break;
                    case OperandType.InlineTok:
                        var tok = (InlineTokInstruction)il;
                        s.Append(' ').Append(tok.Member.Name);
                        break;
                    case OperandType.InlineBrTarget:
                        var br = (InlineBrTargetInstruction)il;
                        s.Append(' ').Append(br.TargetOffset);
                        break;
                    case OperandType.InlineSwitch:
                        var sw = (InlineSwitchInstruction)il;
                        s.Append(' ');
                        foreach (var offset in sw.TargetOffsets)
                            s.Append(offset).Append(',');
                        break;
                    case OperandType.ShortInlineBrTarget:
                        var sbr = (ShortInlineBrTargetInstruction)il;
                        s.Append(' ').Append(sbr.TargetOffset);
                        break;
                    case OperandType.InlineString:
                        var si = (InlineStringInstruction)il;
                        s.Append(" \"").Append(si.String).Append('"');
                        break;
                    case OperandType.ShortInlineI:
                        var sii = (ShortInlineIInstruction)il;
                        s.Append(' ').Append(sii.Byte);
                        break;
                    case OperandType.InlineI:
                        var ii = (InlineIInstruction)il;
                        s.Append(' ').Append(ii.Int32);
                        break;
                    case OperandType.InlineI8:
                        var i8 = (InlineI8Instruction)il;
                        s.Append(' ').Append(i8.Int64);
                        break;
                    case OperandType.ShortInlineR:
                        var sir = (ShortInlineRInstruction)il;
                        s.Append(' ').Append(sir.Single);
                        break;
                    case OperandType.InlineR:
                        var ir = (InlineRInstruction)il;
                        s.Append(' ').Append(ir.Double);
                        break;
                    case OperandType.InlineVar:
                        var iv = (InlineVarInstruction)il;
                        s.Append(' ').Append(iv.Ordinal);
                        break;
                    case OperandType.ShortInlineVar:
                        var siv = (ShortInlineVarInstruction)il;
                        s.Append(' ').Append(siv.Ordinal);
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

        return opCode.OperandType switch
        {
            OperandType.InlineNone => new InlineNoneInstruction(offset, opCode),
            // 8-bit integer branch target
            OperandType.ShortInlineBrTarget => new ShortInlineBrTargetInstruction(offset, opCode, ReadSByte(ref position)),
            // 32-bit integer branch target
            OperandType.InlineBrTarget => new InlineBrTargetInstruction(offset, opCode, ReadInt32(ref position)),
            // 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
            OperandType.ShortInlineI => new ShortInlineIInstruction(offset, opCode, ReadByte(ref position)),
            // 32-bit integer
            OperandType.InlineI => new InlineIInstruction(offset, opCode, ReadInt32(ref position)),
            // 64-bit integer
            OperandType.InlineI8 => new InlineI8Instruction(offset, opCode, ReadInt64(ref position)),
            // 32-bit IEEE floating point number
            OperandType.ShortInlineR => new ShortInlineRInstruction(offset, opCode, ReadSingle(ref position)),
            // 64-bit IEEE floating point number
            OperandType.InlineR => new InlineRInstruction(offset, opCode, ReadDouble(ref position)),
            // 8-bit integer containing the ordinal of a local variable or an argument
            OperandType.ShortInlineVar => new ShortInlineVarInstruction(offset, opCode, ReadByte(ref position)),
            // 16-bit integer containing the ordinal of a local variable or an argument
            OperandType.InlineVar => new InlineVarInstruction(offset, opCode, ReadUInt16(ref position)),
            // 32-bit metadata string token
            OperandType.InlineString => new InlineStringInstruction(offset, opCode, ReadInt32(ref position), _resolver),
            // 32-bit metadata signature token
            OperandType.InlineSig => new InlineSigInstruction(offset, opCode, ReadInt32(ref position), _resolver),
            // 32-bit metadata token
            OperandType.InlineMethod => new InlineMethodInstruction(offset, opCode, ReadInt32(ref position), _resolver),
            // 32-bit metadata token
            OperandType.InlineField => new InlineFieldInstruction(_resolver, offset, opCode, ReadInt32(ref position)),
            // 32-bit metadata token
            OperandType.InlineType => new InlineTypeInstruction(offset, opCode, ReadInt32(ref position), _resolver),
            // FieldRef, MethodRef, or TypeRef token
            OperandType.InlineTok => new InlineTokInstruction(offset, opCode, ReadInt32(ref position), _resolver),
            // 32-bit integer argument to a switch instruction
            OperandType.InlineSwitch => new InlineSwitchInstruction(offset, opCode, ReadDeltas(ref position)),
            _ => throw new NotSupportedException($"Unsupported operand type: {opCode.OperandType}"),
        };
    }

    private int[] ReadDeltas(ref int position)
    {
        var cases = ReadInt32(ref position);
        var deltas = new int[cases];
        for (var i = 0; i < cases; i++)
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

public sealed class InlineNoneInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineNone;

    internal InlineNoneInstruction(int offset, OpCode opCode)
        : base(offset, opCode) { }
}

public sealed class InlineBrTargetInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineBrTarget;
    public int Delta { get; }
    public int TargetOffset => Offset + Delta + 1 + 4;

    internal InlineBrTargetInstruction(int offset, OpCode opCode, int delta)
        : base(offset, opCode) => Delta = delta;
}

public sealed class ShortInlineBrTargetInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineBrTarget;
    public sbyte Delta { get; }
    public int TargetOffset => Offset + Delta + 1 + 1;
    internal ShortInlineBrTargetInstruction(int offset, OpCode opCode, sbyte delta)
        : base(offset, opCode) => Delta = delta;
}

public sealed class InlineSwitchInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineSwitch;
    private readonly int[] _deltas;
    private int[] _targetOffsets;

    internal InlineSwitchInstruction(int offset, OpCode opCode, int[] deltas)
        : base(offset, opCode) => _deltas = deltas;

    public int[] Deltas => (int[])_deltas.Clone();

    public int[] TargetOffsets
    {
        get
        {
            if (_targetOffsets == null)
            {
                var cases = _deltas.Length;
                var itself = 1 + 4 + 4 * cases;
                _targetOffsets = new int[cases];
                for (var i = 0; i < cases; i++)
                    _targetOffsets[i] = Offset + _deltas[i] + itself;
            }
            return _targetOffsets;
        }
    }
}

public sealed class InlineIInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineI;
    public int Int32 { get; }
    internal InlineIInstruction(int offset, OpCode opCode, int value)
        : base(offset, opCode) => Int32 = value;
}

public sealed class InlineI8Instruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineI8;
    public long Int64 { get; }

    internal InlineI8Instruction(int offset, OpCode opCode, long value)
        : base(offset, opCode) => Int64 = value;
}

public sealed class ShortInlineIInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineI;
    public byte Byte { get; }

    internal ShortInlineIInstruction(int offset, OpCode opCode, byte value)
        : base(offset, opCode) => Byte = value;
}

public class InlineRInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineR;
    public double Double { get; }

    internal InlineRInstruction(int offset, OpCode opCode, double value)
        : base(offset, opCode) => Double = value;
}

public sealed class ShortInlineRInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineR;
    public float Single { get; }

    internal ShortInlineRInstruction(int offset, OpCode opCode, float value)
        : base(offset, opCode) => Single = value;
}

public sealed class InlineFieldInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineField;
    private readonly ITokenResolver _resolver;
    public int Token { get; }
    private FieldInfo _field;
    public FieldInfo Field => _field ??= _resolver.AsField(Token);
    internal InlineFieldInstruction(ITokenResolver resolver, int offset, OpCode opCode, int token)
        : base(offset, opCode)
    {
        _resolver = resolver;
        Token = token;
    }
}

public sealed class InlineMethodInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineMethod;
    private readonly ITokenResolver _resolver;
    public int Token { get; }
    private MethodBase _method;
    public MethodBase Method => _method ??= _resolver.AsMethod(Token);

    internal InlineMethodInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        _resolver = resolver;
        Token = token;
    }
}

public sealed class InlineTypeInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineType;
    private readonly ITokenResolver _resolver;
    public int Token { get; }
    private Type _type;
    public Type Type => _type ??= _resolver.AsType(Token);

    internal InlineTypeInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        _resolver = resolver;
        Token = token;
    }
}

public sealed class InlineSigInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineSig;
    private readonly ITokenResolver _resolver;
    public int Token { get; }
    private byte[] _signature;
    public byte[] Signature => _signature ??= _resolver.AsSignature(Token);

    internal InlineSigInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        _resolver = resolver;
        Token = token;
    }
}

public sealed class InlineTokInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineTok;
    private readonly ITokenResolver _resolver;
    public int Token { get; }
    private MemberInfo _member;
    public MemberInfo Member => _member ??= _resolver.AsMember(Token);

    internal InlineTokInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        _resolver = resolver;
        Token = token;
    }
}

public sealed class InlineStringInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineString;
    private readonly ITokenResolver _resolver;
    public int Token { get; }
    private string _string;
    public string String => _string ??= _resolver.AsString(Token);

    internal InlineStringInstruction(int offset, OpCode opCode, int token, ITokenResolver resolver)
        : base(offset, opCode)
    {
        _resolver = resolver;
        Token = token;
    }
}

public sealed class InlineVarInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.InlineVar;
    public ushort Ordinal { get; }
    internal InlineVarInstruction(int offset, OpCode opCode, ushort ordinal)
        : base(offset, opCode) => Ordinal = ordinal;
}

public sealed class ShortInlineVarInstruction : ILInstruction
{
    public override OperandType OperandType => OperandType.ShortInlineVar;
    public byte Ordinal { get; }

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

    private readonly MethodBase _method;
    private byte[] _byteArray;

    public MethodBaseILProvider(MethodBase method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        var methodType = method.GetType();
        if (methodType != _runtimeMethodInfoType & methodType != _runtimeConstructorInfoType)
            throw new ArgumentException("Must have type RuntimeMethodInfo or RuntimeConstructorInfo.", nameof(method));

        _method = method;
    }

    public byte[] GetByteArray()
    {
        return _byteArray ??= _method.GetMethodBody()?.GetILAsByteArray() ?? [];
    }
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

    private readonly DynamicMethod _method;
    private byte[] _byteArray;

    public DynamicMethodILProvider(DynamicMethod method) => _method = method;

    public byte[] GetByteArray()
    {
        if (_byteArray == null)
        {
            var ilgen = _method.GetILGenerator();
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
        return _byteArray;
    }
}

public interface IFormatter
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

public struct DefaultFormatter : IFormatter
{
    public string Int32ToHex(int int32) => int32.ToString("X8");
    public string Int16ToHex(int int16) => int16.ToString("X4");
    public string Int8ToHex(int int8) => int8.ToString("X2");
    public string Argument(int ordinal) => $"V_{ordinal}";
    public string Label(int offset) => $"IL_{offset:x4}";

    public string MultipleLabels(int[] offsets)
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

    public string EscapedString(string str)
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

    public string SigByteArrayToString(byte[] sig)
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

// todo: @feat waiting for C# support of the default/optional generic parameters, e.g. for `ReadableILStringProcessor<TFormatter = DefaultFormatter>`
public sealed class ReadableILStringProcessor<TFormatter> where TFormatter : struct, IFormatter
{
    private static readonly TFormatter _formatProvider = default;
    readonly TextWriter _writer;

    public ReadableILStringProcessor(TextWriter writer) => _writer = writer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Write(ILInstruction i, string operandString) =>
        _writer.WriteLine("IL_{0:x4}: {1,-10} {2}", i.Offset, i.OpCode.Name, operandString);

    public void ProcessInstruction(ILInstruction i)
    {
        switch (i.OperandType)
        {
            case OperandType.InlineBrTarget:
                Write(i, _formatProvider.Label(((InlineBrTargetInstruction)i).TargetOffset));
                break;
            case OperandType.InlineField:
                var inlineField = (InlineFieldInstruction)i;
                string field;
                try
                {
                    field = inlineField.Field + "/" + inlineField.Field.DeclaringType;
                }
                catch (Exception ex)
                {
                    field = "!" + ex.Message + "!";
                }
                Write(i, field);
                break;
            case OperandType.InlineI:
                Write(i, ((InlineIInstruction)i).Int32.ToString());
                break;
            case OperandType.InlineI8:
                Write(i, ((InlineI8Instruction)i).Int64.ToString());
                break;
            case OperandType.InlineMethod:
                var inlineMethod = (InlineMethodInstruction)i;
                string method;
                try
                {
                    method = inlineMethod.Method + "/" + inlineMethod.Method.DeclaringType;
                }
                catch (Exception ex)
                {
                    method = "!" + ex.Message + "!";
                }
                Write(i, method);
                break;
            case OperandType.InlineNone:
                Write(i, string.Empty);
                break;
            case OperandType.InlineR:
                Write(i, ((InlineRInstruction)i).Double.ToString());
                break;
            case OperandType.InlineSig:
                Write(i, _formatProvider.SigByteArrayToString(((InlineSigInstruction)i).Signature));
                break;
            case OperandType.InlineString:
                Write(i, _formatProvider.EscapedString(((InlineStringInstruction)i).String));
                break;
            case OperandType.InlineSwitch:
                var inlineSwitch = (InlineSwitchInstruction)i;
                Write(i, _formatProvider.MultipleLabels(inlineSwitch.TargetOffsets));
                break;
            case OperandType.InlineTok:
                var inlineTok = (InlineTokInstruction)i;
                string member;
                try
                {
                    member = inlineTok.Member + "/" + inlineTok.Member.DeclaringType;
                }
                catch (Exception ex)
                {
                    member = "!" + ex.Message + "!";
                }
                Write(i, member);
                break;
            case OperandType.InlineType:
                var inlineType = (InlineTypeInstruction)i;
                string type;
                try
                {
                    type = inlineType.Type.ToString();
                }
                catch (Exception ex)
                {
                    type = "!" + ex.Message + "!";
                }
                Write(i, type);
                break;
            case OperandType.InlineVar:
                var inlineVar = (InlineVarInstruction)i;
                Write(i, _formatProvider.Argument(inlineVar.Ordinal));
                break;
            case OperandType.ShortInlineBrTarget:
                var shortInlineBrTarget = (ShortInlineBrTargetInstruction)i;
                Write(i, _formatProvider.Label(shortInlineBrTarget.TargetOffset));
                break;
            case OperandType.ShortInlineI:
                Write(i, ((ShortInlineIInstruction)i).Byte.ToString());
                break;
            case OperandType.ShortInlineR:
                Write(i, ((ShortInlineRInstruction)i).Single.ToString());
                break;
            case OperandType.ShortInlineVar:
                var shortInlineVar = (ShortInlineVarInstruction)i;
                Write(i, _formatProvider.Argument(shortInlineVar.Ordinal));
                break;
            default:
                Debug.Fail("all cases are covered above, so it is not expected to reach here");
                break;
        }
    }
}

public sealed class RawILStringProcessor<TFormatter> where TFormatter : struct, IFormatter
{
    static readonly TFormatter _formatter = default;
    readonly ReadableILStringProcessor<TFormatter> _fallbackProcessor;
    readonly TextWriter _writer;

    public RawILStringProcessor(TextWriter writer)
    {
        _fallbackProcessor = new ReadableILStringProcessor<TFormatter>(writer);
        _writer = writer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Write(ILInstruction i, string operandString) =>
        _writer.WriteLine("IL_{0:x4}: {1,-4:x2}| {2, -8}", i.Offset, i.OpCode.Value, operandString);

    public void ProcessInstruction(ILInstruction i)
    {
        switch (i.OperandType)
        {
            case OperandType.InlineBrTarget:
                Write(i, _formatter.Int32ToHex(((InlineBrTargetInstruction)i).TargetOffset));
                break;
            case OperandType.InlineField:
                Write(i, _formatter.Int32ToHex(((InlineFieldInstruction)i).Token));
                break;
            case OperandType.InlineI:
            case OperandType.InlineI8:
                _fallbackProcessor.ProcessInstruction(i);
                break;
            case OperandType.InlineMethod:
                Write(i, _formatter.Int32ToHex(((InlineMethodInstruction)i).Token));
                break;
            case OperandType.InlineNone:
            case OperandType.InlineR:
                _fallbackProcessor.ProcessInstruction(i);
                break;
            case OperandType.InlineSig:
                Write(i, _formatter.Int32ToHex(((InlineSigInstruction)i).Token));
                break;
            case OperandType.InlineString:
                Write(i, _formatter.Int32ToHex(((InlineStringInstruction)i).Token));
                break;
            case OperandType.InlineSwitch:
                Write(i, "...");
                break;
            case OperandType.InlineTok:
                Write(i, _formatter.Int32ToHex(((InlineTokInstruction)i).Token));
                break;
            case OperandType.InlineType:
                Write(i, _formatter.Int32ToHex(((InlineTypeInstruction)i).Token));
                break;
            case OperandType.InlineVar:
                Write(i, _formatter.Int16ToHex(((InlineVarInstruction)i).Ordinal));
                break;
            case OperandType.ShortInlineBrTarget:
                Write(i, _formatter.Int8ToHex(((ShortInlineBrTargetInstruction)i).Delta));
                break;
            case OperandType.ShortInlineI:
                Write(i, _formatter.Int8ToHex(((ShortInlineIInstruction)i).Byte));
                break;
            case OperandType.ShortInlineR:
                _fallbackProcessor.ProcessInstruction(i);
                break;
            case OperandType.ShortInlineVar:
                Write(i, _formatter.Int8ToHex(((ShortInlineVarInstruction)i).Ordinal));
                break;
            default:
                Debug.Fail("all cases are covered above, so it is not expected to reach here");
                break;
        }
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