using System.Text;

namespace FastExpressionCompiler.ILDecoder;

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
        var sb = new StringBuilder(length*2);

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