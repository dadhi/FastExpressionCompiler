using System;
using System.Reflection;
using System.Reflection.Emit;

namespace FastExpressionCompiler.ILDecoder;

public abstract class ILInstruction
{
    internal ILInstruction(int offset, OpCode opCode)
    {
        Offset = offset;
        OpCode = opCode;
    }

    public int Offset { get; }
    public OpCode OpCode { get; }

    public abstract void Accept(ILInstructionVisitor visitor);
}

public class InlineNoneInstruction : ILInstruction
{
    internal InlineNoneInstruction(int offset, OpCode opCode)
        : base(offset, opCode)
    {
    }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineNoneInstruction(this);
    }
}

public class InlineBrTargetInstruction : ILInstruction
{
    internal InlineBrTargetInstruction(int offset, OpCode opCode, int delta)
        : base(offset, opCode)
    {
        Delta = delta;
    }

    public int Delta { get; }

    public int TargetOffset => Offset + Delta + 1 + 4;

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineBrTargetInstruction(this);
    }
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

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitShortInlineBrTargetInstruction(this);
    }
}

public class InlineSwitchInstruction : ILInstruction
{
    private readonly int[] m_deltas;
    private int[] m_targetOffsets;

    internal InlineSwitchInstruction(int offset, OpCode opCode, int[] deltas)
        : base(offset, opCode)
    {
        m_deltas = deltas;
    }

    public int[] Deltas => (int[])m_deltas.Clone();

    public int[] TargetOffsets
    {
        get
        {
            if (m_targetOffsets == null)
            {
                var cases = m_deltas.Length;
                var itself = 1 + 4 + 4*cases;
                m_targetOffsets = new int[cases];
                for (var i = 0; i < cases; i++)
                    m_targetOffsets[i] = Offset + m_deltas[i] + itself;
            }
            return m_targetOffsets;
        }
    }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineSwitchInstruction(this);
    }
}

public class InlineIInstruction : ILInstruction
{
    internal InlineIInstruction(int offset, OpCode opCode, int value)
        : base(offset, opCode)
    {
        Int32 = value;
    }

    public int Int32 { get; }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineIInstruction(this);
    }
}

public class InlineI8Instruction : ILInstruction
{
    internal InlineI8Instruction(int offset, OpCode opCode, long value)
        : base(offset, opCode)
    {
        Int64 = value;
    }

    public long Int64 { get; }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineI8Instruction(this);
    }
}

public class ShortInlineIInstruction : ILInstruction
{
    internal ShortInlineIInstruction(int offset, OpCode opCode, byte value)
        : base(offset, opCode)
    {
        Byte = value;
    }

    public byte Byte { get; }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitShortInlineIInstruction(this);
    }
}

public class InlineRInstruction : ILInstruction
{
    internal InlineRInstruction(int offset, OpCode opCode, double value)
        : base(offset, opCode)
    {
        Double = value;
    }

    public double Double { get; }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineRInstruction(this);
    }
}

public class ShortInlineRInstruction : ILInstruction
{
    internal ShortInlineRInstruction(int offset, OpCode opCode, float value)
        : base(offset, opCode)
    {
        Single = value;
    }

    public float Single { get; }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitShortInlineRInstruction(this);
    }
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

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineFieldInstruction(this);
    }
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

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineMethodInstruction(this);
    }
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

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineTypeInstruction(this);
    }
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

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineSigInstruction(this);
    }
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

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineTokInstruction(this);
    }
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

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineStringInstruction(this);
    }
}

public class InlineVarInstruction : ILInstruction
{
    internal InlineVarInstruction(int offset, OpCode opCode, ushort ordinal)
        : base(offset, opCode)
    {
        Ordinal = ordinal;
    }

    public ushort Ordinal { get; }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitInlineVarInstruction(this);
    }
}

public class ShortInlineVarInstruction : ILInstruction
{
    internal ShortInlineVarInstruction(int offset, OpCode opCode, byte ordinal)
        : base(offset, opCode)
    {
        Ordinal = ordinal;
    }

    public byte Ordinal { get; }

    public override void Accept(ILInstructionVisitor visitor)
    {
        visitor.VisitShortInlineVarInstruction(this);
    }
}