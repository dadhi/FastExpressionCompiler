using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace FastExpressionCompiler.ILDecoder;

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
        if (ilProvider == null)
            throw new ArgumentNullException(nameof(ilProvider));

        _resolver = tokenResolver;
        _byteArray = ilProvider.GetByteArray();
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
                    var cases = ReadInt32(ref position);
                    var deltas = new int[cases];
                    for (var i = 0; i < cases; i++)
                        deltas[i] = ReadInt32(ref position);
                    return new InlineSwitchInstruction(offset, opCode, deltas);
                }

            default:
                throw new NotSupportedException($"Unsupported operand type: {opCode.OperandType}");
        }
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

    private uint ReadUInt32(ref int position)
    {
        var value = BitConverter.ToUInt32(_byteArray, position);
        position += 4;
        return value;
    }

    private ulong ReadUInt64(ref int position)
    {
        var value = BitConverter.ToUInt64(_byteArray, position);
        position += 8;
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
