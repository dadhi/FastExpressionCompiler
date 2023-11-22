using System;
using System.IO;

namespace FastExpressionCompiler.ILDecoder;

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