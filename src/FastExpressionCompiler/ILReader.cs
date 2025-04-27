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
        Console.WriteLine($"m_runtimeMethodInfoType: {_runtimeMethodInfoType},\n_runtimeConstructorInfoType: {_runtimeConstructorInfoType} ");
#endif

#if !NET8_0_OR_GREATER
        if (dynamicMethod == null && sourceType == _rtDynamicMethodType)
            dynamicMethod = (DynamicMethod)_fiOwner.GetValue(source);
#if DEBUG_INTERNALS
        Console.WriteLine($"m_rtDynamicMethodType: {_rtDynamicMethodType}, _fiOwner: {_fiOwner}");
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

        s ??= new StringBuilder();

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

    public ILReader(MethodBase method)
        : this(new MethodBaseILProvider(method), new ModuleScopeTokenResolver(method))
    {
    }

    public ILReader(IILProvider ilProvider, ITokenResolver tokenResolver)
    {
        _resolver = tokenResolver;
        _byteArray = ilProvider?.GetByteArray() ?? throw new ArgumentNullException(nameof(ilProvider));
    }

    // todo: @perf implement optimized IEnumerator<OpCode> which does not need to allocate ILInstruction objects
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

        // read first 1 or 2 bytes as opCode
        var code = ReadByte(ref position);
        var opCode = code != 0xFE
            ? _oneByteOpCodes[code]
            : _twoByteOpCodes[ReadByte(ref position)];

        switch (opCode.OperandType)
        {
            case OperandType.InlineNone:
                return new InlineNoneInstruction(offset, opCode);
            // 8-bit integer branch target
            case OperandType.ShortInlineBrTarget:
                return new ShortInlineBrTargetInstruction(offset, opCode, ReadSByte(ref position));
            // 32-bit integer branch target
            case OperandType.InlineBrTarget:
                return new InlineBrTargetInstruction(offset, opCode, ReadInt32(ref position));
            // 8-bit integer: 001F  ldc.i4.s, FE12  unaligned.
            case OperandType.ShortInlineI:
                return new ShortInlineIInstruction(offset, opCode, ReadByte(ref position));
            // 32-bit integer
            case OperandType.InlineI:
                return new InlineIInstruction(offset, opCode, ReadInt32(ref position));
            // 64-bit integer
            case OperandType.InlineI8:
                return new InlineI8Instruction(offset, opCode, ReadInt64(ref position));
            // 32-bit IEEE floating point number
            case OperandType.ShortInlineR:
                return new ShortInlineRInstruction(offset, opCode, ReadSingle(ref position));
            // 64-bit IEEE floating point number
            case OperandType.InlineR:
                return new InlineRInstruction(offset, opCode, ReadDouble(ref position));
            // 8-bit integer containing the ordinal of a local variable or an argument
            case OperandType.ShortInlineVar:
                return new ShortInlineVarInstruction(offset, opCode, ReadByte(ref position));
            // 16-bit integer containing the ordinal of a local variable or an argument
            case OperandType.InlineVar:
                return new InlineVarInstruction(offset, opCode, ReadUInt16(ref position));
            // 32-bit metadata string token
            case OperandType.InlineString:
                return new InlineStringInstruction(offset, opCode, ReadInt32(ref position), _resolver);
            // 32-bit metadata signature token
            case OperandType.InlineSig:
                return new InlineSigInstruction(offset, opCode, ReadInt32(ref position), _resolver);
            // 32-bit metadata token
            case OperandType.InlineMethod:
                return new InlineMethodInstruction(offset, opCode, ReadInt32(ref position), _resolver);
            // 32-bit metadata token
            case OperandType.InlineField:
                return new InlineFieldInstruction(_resolver, offset, opCode, ReadInt32(ref position));
            // 32-bit metadata token
            case OperandType.InlineType:
                return new InlineTypeInstruction(offset, opCode, ReadInt32(ref position), _resolver);
            // FieldRef, MethodRef, or TypeRef token
            case OperandType.InlineTok:
                return new InlineTokInstruction(offset, opCode, ReadInt32(ref position), _resolver);
            // 32-bit integer argument to a switch instruction
            case OperandType.InlineSwitch:
                return new InlineSwitchInstruction(offset, opCode, ReadDeltas(ref position));
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
    public int Delta { get; }
    public int TargetOffset => Offset + Delta + 1 + 4;

    internal InlineBrTargetInstruction(int offset, OpCode opCode, int delta)
        : base(offset, opCode) => Delta = delta;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineBrTargetInstruction(this);
}

public class ShortInlineBrTargetInstruction : ILInstruction
{
    public sbyte Delta { get; }
    public int TargetOffset => Offset + Delta + 1 + 1;

    internal ShortInlineBrTargetInstruction(int offset, OpCode opCode, sbyte delta)
        : base(offset, opCode) => Delta = delta;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineBrTargetInstruction(this);
}

public class InlineSwitchInstruction : ILInstruction
{
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

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineSwitchInstruction(this);
}

public class InlineIInstruction : ILInstruction
{
    public int Int32 { get; }

    internal InlineIInstruction(int offset, OpCode opCode, int value)
        : base(offset, opCode) => Int32 = value;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineIInstruction(this);
}

public class InlineI8Instruction : ILInstruction
{
    public long Int64 { get; }

    internal InlineI8Instruction(int offset, OpCode opCode, long value)
        : base(offset, opCode) => Int64 = value;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineI8Instruction(this);
}

public class ShortInlineIInstruction : ILInstruction
{
    public byte Byte { get; }

    internal ShortInlineIInstruction(int offset, OpCode opCode, byte value)
        : base(offset, opCode) => Byte = value;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineIInstruction(this);
}

public class InlineRInstruction : ILInstruction
{
    public double Double { get; }

    internal InlineRInstruction(int offset, OpCode opCode, double value)
        : base(offset, opCode) => Double = value;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineRInstruction(this);
}

public class ShortInlineRInstruction : ILInstruction
{
    public float Single { get; }

    internal ShortInlineRInstruction(int offset, OpCode opCode, float value)
        : base(offset, opCode) => Single = value;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineRInstruction(this);
}

public class InlineFieldInstruction : ILInstruction
{
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

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineFieldInstruction(this);
}

public class InlineMethodInstruction : ILInstruction
{
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

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineMethodInstruction(this);
}

public class InlineTypeInstruction : ILInstruction
{
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

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineTypeInstruction(this);
}

public class InlineSigInstruction : ILInstruction
{
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

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineSigInstruction(this);
}

public class InlineTokInstruction : ILInstruction
{
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

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineTokInstruction(this);
}

public class InlineStringInstruction : ILInstruction
{
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

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineStringInstruction(this);
}

public class InlineVarInstruction : ILInstruction
{
    public ushort Ordinal { get; }

    internal InlineVarInstruction(int offset, OpCode opCode, ushort ordinal)
        : base(offset, opCode) => Ordinal = ordinal;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitInlineVarInstruction(this);
}

public class ShortInlineVarInstruction : ILInstruction
{
    public byte Ordinal { get; }

    internal ShortInlineVarInstruction(int offset, OpCode opCode, byte ordinal)
        : base(offset, opCode) => Ordinal = ordinal;

    public override void Accept(ILInstructionVisitor visitor) => visitor.VisitShortInlineVarInstruction(this);
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

        if (methodType != _runtimeMethodInfoType && methodType != _runtimeConstructorInfoType)
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
    private DefaultFormatProvider() { }

    public static readonly DefaultFormatProvider Instance = new();

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
    protected TextWriter _writer;

    public ReadableILStringToTextWriter(TextWriter writer) => _writer = writer;

    public virtual void Process(ILInstruction ilInstruction, string operandString)
    {
        _writer.WriteLine("IL_{0:x4}: {1,-10} {2}",
            ilInstruction.Offset,
            ilInstruction.OpCode.Name,
            operandString);
    }
}

public class RawILStringToTextWriter : ReadableILStringToTextWriter
{
    public RawILStringToTextWriter(TextWriter writer) : base(writer) { }

    public override void Process(ILInstruction ilInstruction, string operandString)
    {
        _writer.WriteLine("IL_{0:x4}: {1,-4:x2}| {2, -8}",
            ilInstruction.Offset,
            ilInstruction.OpCode.Value,
            operandString);
    }
}

public abstract class ILInstructionVisitor
{
    public virtual void VisitInlineBrTargetInstruction(InlineBrTargetInstruction inlineBrTargetInstruction) { }

    public virtual void VisitInlineFieldInstruction(InlineFieldInstruction inlineFieldInstruction) { }

    public virtual void VisitInlineIInstruction(InlineIInstruction inlineIInstruction) { }

    public virtual void VisitInlineI8Instruction(InlineI8Instruction inlineI8Instruction) { }

    public virtual void VisitInlineMethodInstruction(InlineMethodInstruction inlineMethodInstruction) { }

    public virtual void VisitInlineNoneInstruction(InlineNoneInstruction inlineNoneInstruction) { }

    public virtual void VisitInlineRInstruction(InlineRInstruction inlineRInstruction) { }

    public virtual void VisitInlineSigInstruction(InlineSigInstruction inlineSigInstruction) { }

    public virtual void VisitInlineStringInstruction(InlineStringInstruction inlineStringInstruction) { }

    public virtual void VisitInlineSwitchInstruction(InlineSwitchInstruction inlineSwitchInstruction) { }

    public virtual void VisitInlineTokInstruction(InlineTokInstruction inlineTokInstruction) { }

    public virtual void VisitInlineTypeInstruction(InlineTypeInstruction inlineTypeInstruction) { }

    public virtual void VisitInlineVarInstruction(InlineVarInstruction inlineVarInstruction) { }

    public virtual void VisitShortInlineBrTargetInstruction(ShortInlineBrTargetInstruction shortInlineBrTargetInstruction) { }

    public virtual void VisitShortInlineIInstruction(ShortInlineIInstruction shortInlineIInstruction) { }

    public virtual void VisitShortInlineRInstruction(ShortInlineRInstruction shortInlineRInstruction) { }

    public virtual void VisitShortInlineVarInstruction(ShortInlineVarInstruction shortInlineVarInstruction) { }
}

public class ReadableILStringVisitor : ILInstructionVisitor
{
    protected IFormatProvider _formatProvider;
    protected IILStringCollector _collector;

    public ReadableILStringVisitor(IILStringCollector collector)
        : this(collector, DefaultFormatProvider.Instance) { }

    public ReadableILStringVisitor(IILStringCollector collector, IFormatProvider formatProvider)
    {
        _formatProvider = formatProvider;
        _collector = collector;
    }

    public override void VisitInlineBrTargetInstruction(InlineBrTargetInstruction inlineBrTargetInstruction)
    {
        _collector.Process(inlineBrTargetInstruction, _formatProvider.Label(inlineBrTargetInstruction.TargetOffset));
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
        _collector.Process(inlineFieldInstruction, field);
    }

    public override void VisitInlineIInstruction(InlineIInstruction inlineIInstruction)
    {
        _collector.Process(inlineIInstruction, inlineIInstruction.Int32.ToString());
    }

    public override void VisitInlineI8Instruction(InlineI8Instruction inlineI8Instruction)
    {
        _collector.Process(inlineI8Instruction, inlineI8Instruction.Int64.ToString());
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
        _collector.Process(inlineMethodInstruction, method);
    }

    public override void VisitInlineNoneInstruction(InlineNoneInstruction inlineNoneInstruction)
    {
        _collector.Process(inlineNoneInstruction, string.Empty);
    }

    public override void VisitInlineRInstruction(InlineRInstruction inlineRInstruction)
    {
        _collector.Process(inlineRInstruction, inlineRInstruction.Double.ToString());
    }

    public override void VisitInlineSigInstruction(InlineSigInstruction inlineSigInstruction)
    {
        _collector.Process(inlineSigInstruction, _formatProvider.SigByteArrayToString(inlineSigInstruction.Signature));
    }

    public override void VisitInlineStringInstruction(InlineStringInstruction inlineStringInstruction)
    {
        _collector.Process(inlineStringInstruction, _formatProvider.EscapedString(inlineStringInstruction.String));
    }

    public override void VisitInlineSwitchInstruction(InlineSwitchInstruction inlineSwitchInstruction)
    {
        _collector.Process(inlineSwitchInstruction, _formatProvider.MultipleLabels(inlineSwitchInstruction.TargetOffsets));
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
        _collector.Process(inlineTokInstruction, member);
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
        _collector.Process(inlineTypeInstruction, type);
    }

    public override void VisitInlineVarInstruction(InlineVarInstruction inlineVarInstruction)
    {
        _collector.Process(inlineVarInstruction, _formatProvider.Argument(inlineVarInstruction.Ordinal));
    }

    public override void VisitShortInlineBrTargetInstruction(ShortInlineBrTargetInstruction shortInlineBrTargetInstruction)
    {
        _collector.Process(shortInlineBrTargetInstruction, _formatProvider.Label(shortInlineBrTargetInstruction.TargetOffset));
    }

    public override void VisitShortInlineIInstruction(ShortInlineIInstruction shortInlineIInstruction)
    {
        _collector.Process(shortInlineIInstruction, shortInlineIInstruction.Byte.ToString());
    }

    public override void VisitShortInlineRInstruction(ShortInlineRInstruction shortInlineRInstruction)
    {
        _collector.Process(shortInlineRInstruction, shortInlineRInstruction.Single.ToString());
    }

    public override void VisitShortInlineVarInstruction(ShortInlineVarInstruction shortInlineVarInstruction)
    {
        _collector.Process(shortInlineVarInstruction, _formatProvider.Argument(shortInlineVarInstruction.Ordinal));
    }
}

public class RawILStringVisitor : ReadableILStringVisitor
{
    public RawILStringVisitor(IILStringCollector collector)
        : this(collector, DefaultFormatProvider.Instance) { }

    public RawILStringVisitor(IILStringCollector collector, IFormatProvider formatProvider)
        : base(collector, formatProvider) { }

    public override void VisitInlineBrTargetInstruction(InlineBrTargetInstruction inlineBrTargetInstruction)
    {
        _collector.Process(inlineBrTargetInstruction, _formatProvider.Int32ToHex(inlineBrTargetInstruction.Delta));
    }

    public override void VisitInlineFieldInstruction(InlineFieldInstruction inlineFieldInstruction)
    {
        _collector.Process(inlineFieldInstruction, _formatProvider.Int32ToHex(inlineFieldInstruction.Token));
    }

    public override void VisitInlineMethodInstruction(InlineMethodInstruction inlineMethodInstruction)
    {
        _collector.Process(inlineMethodInstruction, _formatProvider.Int32ToHex(inlineMethodInstruction.Token));
    }

    public override void VisitInlineSigInstruction(InlineSigInstruction inlineSigInstruction)
    {
        _collector.Process(inlineSigInstruction, _formatProvider.Int32ToHex(inlineSigInstruction.Token));
    }

    public override void VisitInlineStringInstruction(InlineStringInstruction inlineStringInstruction)
    {
        _collector.Process(inlineStringInstruction, _formatProvider.Int32ToHex(inlineStringInstruction.Token));
    }

    public override void VisitInlineSwitchInstruction(InlineSwitchInstruction inlineSwitchInstruction)
    {
        _collector.Process(inlineSwitchInstruction, "...");
    }

    public override void VisitInlineTokInstruction(InlineTokInstruction inlineTokInstruction)
    {
        _collector.Process(inlineTokInstruction, _formatProvider.Int32ToHex(inlineTokInstruction.Token));
    }

    public override void VisitInlineTypeInstruction(InlineTypeInstruction inlineTypeInstruction)
    {
        _collector.Process(inlineTypeInstruction, _formatProvider.Int32ToHex(inlineTypeInstruction.Token));
    }

    public override void VisitInlineVarInstruction(InlineVarInstruction inlineVarInstruction)
    {
        _collector.Process(inlineVarInstruction, _formatProvider.Int16ToHex(inlineVarInstruction.Ordinal));
    }

    public override void VisitShortInlineBrTargetInstruction(ShortInlineBrTargetInstruction shortInlineBrTargetInstruction)
    {
        _collector.Process(shortInlineBrTargetInstruction, _formatProvider.Int8ToHex(shortInlineBrTargetInstruction.Delta));
    }

    public override void VisitShortInlineVarInstruction(ShortInlineVarInstruction shortInlineVarInstruction)
    {
        _collector.Process(shortInlineVarInstruction, _formatProvider.Int8ToHex(shortInlineVarInstruction.Ordinal));
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
public class ModuleScopeTokenResolver : ITokenResolver
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
internal class DynamicScopeTokenResolver : ITokenResolver
{
    private static readonly PropertyInfo _indexer;
    private static readonly FieldInfo _scopeFi;

    private static readonly Type _genMethodInfoType;
    private static readonly FieldInfo _genmethFi1;
    private static readonly FieldInfo _genmethFi2;

    private static readonly Type _varArgMethodType;
    private static readonly FieldInfo _varargFi1;

    private static readonly Type _genFieldInfoType;
    private static readonly FieldInfo _genfieldFi1;
    private static readonly FieldInfo _genfieldFi2;

    static DynamicScopeTokenResolver()
    {
        const BindingFlags memberFlags = BindingFlags.NonPublic | BindingFlags.Instance;

        var dynamicScopeType = Type.GetType("System.Reflection.Emit.DynamicScope") ?? throw new InvalidOperationException("DynamicScope type is not found");
        _indexer = dynamicScopeType.GetProperty("Item", memberFlags) ?? throw new InvalidOperationException("DynamicScope.Item property is not found");

        var dynamicIlGeneratorType = Type.GetType("System.Reflection.Emit.DynamicILGenerator") ?? throw new InvalidOperationException("DynamicILGenerator type is not found");
        _scopeFi = dynamicIlGeneratorType.GetField("m_scope", memberFlags) ?? throw new InvalidOperationException("DynamicILGenerator._scope field is not found");

        _varArgMethodType = Type.GetType("System.Reflection.Emit.VarArgMethod");
        _varargFi1 = _varArgMethodType.GetField("m_method", memberFlags);

        _genMethodInfoType = Type.GetType("System.Reflection.Emit.GenericMethodInfo");
        _genmethFi1 = _genMethodInfoType.GetField("m_methodHandle", memberFlags);
        _genmethFi2 = _genMethodInfoType.GetField("m_context", memberFlags);

        _genFieldInfoType = Type.GetType("System.Reflection.Emit.GenericFieldInfo", throwOnError: false);

        _genfieldFi1 = _genFieldInfoType?.GetField("m_fieldHandle", memberFlags);
        _genfieldFi2 = _genFieldInfoType?.GetField("m_context", memberFlags);
    }

    private readonly object _scope;

    private object this[int token] => _indexer.GetValue(_scope, [token]);

    public DynamicScopeTokenResolver(DynamicMethod dm)
    {
        _scope = _scopeFi.GetValue(dm.GetILGenerator());
    }

    public string AsString(int token) => this[token] as string;

    public FieldInfo AsField(int token)
    {
        if (this[token] is RuntimeFieldHandle)
            return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)this[token]);

        if (this[token].GetType() == _genFieldInfoType)
        {
            return FieldInfo.GetFieldFromHandle(
                (RuntimeFieldHandle)_genfieldFi1.GetValue(this[token]),
                (RuntimeTypeHandle)_genfieldFi2.GetValue(this[token]));
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