#if NETCOREAPP3_1
using System;
using System.Linq;
using System.Text;
using System.Buffers;
using System.Reflection;
using System.Buffers.Binary;

using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    internal delegate bool DeserializerDlg<in T>(ref ReadOnlySequence<byte> seq, T value, out long bytesRead);

    [TestFixture, Ignore("todo: fix me")]
    public class Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown : ITest
    {
        private static readonly MethodInfo _tryRead =  typeof(ReaderExtensions).GetMethod(nameof(ReaderExtensions.TryReadValue));
        private static readonly MethodInfo _tryDeserialize = typeof(Serializer).GetMethod(nameof(Serializer.TryDeserializeValues));

        public int Run()
        {
            Setup_ShouldCompileExpressions();
            TryDeserialize_ShouldParseSimple();

            return 1;
        }

        [SetUp]
        public void Setup_ShouldCompileExpressions()
        {
            var reader = Variable(typeof(SequenceReader<byte>), "reader");
            var bytesRead = Parameter(typeof(long).MakeByRefType(), "bytesRead");
            var input = Parameter(typeof(ReadOnlySequence<byte>).MakeByRefType(), "input");

            var createReader = Assign(reader,
                New(typeof(SequenceReader<byte>).GetConstructor(new[] { typeof(ReadOnlySequence<byte>) }), input));

            var returnTarget = Label(typeof(bool));
            var returnLabel = Label(returnTarget, Constant(default(bool)));
            var returnFalse =
                Block(
                    Assign(bytesRead,
                        Property(reader,
                            typeof(SequenceReader<byte>).GetProperty(nameof(SequenceReader<byte>.Consumed)))),
                    Block(Return(returnTarget, Constant(false), typeof(bool)), returnLabel));

            var returnTrue =
                Block(
                    Assign(bytesRead,
                        Property(reader,
                            typeof(SequenceReader<byte>).GetProperty(nameof(SequenceReader<byte>.Consumed)))),
                    Block(Return(returnTarget, Constant(true), typeof(bool)), returnLabel));

            var valueWord = Parameter(typeof(Word), "value");
            var wordValueVar = Variable(typeof(string), "wordValue");
            var expr0 = Lambda<DeserializerDlg<Word>>(  
                Block(new[] { reader, wordValueVar },
                    createReader,
                    IfThen(
                        NotEqual(Call(_tryRead.MakeGenericMethod(typeof(string)), reader, wordValueVar), Constant(true)),
                        returnFalse),
                    Assign(Property(valueWord, nameof(Word.Value)), wordValueVar),
                    returnTrue), 
                input, valueWord, bytesRead);
            
            expr0.PrintCSharpString();

            // sanity check
            var f0sys = expr0.CompileSys();
            Assert.IsNotNull(f0sys);
            // Console.WriteLine("System Expression IL:");
            // f0sys.PrintIL();

            var f0 = expr0.CompileFast(true);
            f0.PrintIL();

            Serializer.Setup(f0);

            var valueSimple   = Parameter(typeof(Simple), "value");
            var identifierVar = Variable(typeof(int),     "identifier");
            var contentVar    = Variable(typeof(Word[]),  "content");
            var contentLenVar = Variable(typeof(int),     "contentLength");

            var expr1 = Lambda<DeserializerDlg<Simple>>(
                    Block(new[] { reader, identifierVar, contentVar, contentLenVar },
                        createReader,
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(int)), reader, identifierVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(int)), reader, contentLenVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryDeserialize.MakeGenericMethod(typeof(Word)), reader, contentLenVar, contentVar), Constant(true)),
                            returnFalse),
                        Assign(Property(valueSimple, nameof(Simple.Identifier)), identifierVar),
                        Assign(Property(valueSimple, nameof(Simple.Sentence)), contentVar),
                    returnTrue), 
                input, valueSimple, bytesRead);
            
            expr1.PrintCSharpString();

            var f1 = expr1.CompileFast(true);
            f1.PrintIL();

            Serializer.Setup(f1);
        }

        [Test]
        public void TryDeserialize_ShouldParseSimple()
        {
            var expected = new Simple { Identifier = 150, Sentence = new[] { new Word { Value = "hello" }, new Word { Value = "there" } } };
            Memory<byte> buffer = new byte[20];
            BinaryPrimitives.WriteInt32BigEndian(buffer.Span, expected.Identifier);
            buffer.Span.Slice(4)[0] = 2;
            BinaryPrimitives.WriteInt16BigEndian(buffer.Span.Slice(5), 5);
            Encoding.UTF8.GetBytes(expected.Sentence[0].Value, buffer.Span.Slice(7));
            BinaryPrimitives.WriteInt16BigEndian(buffer.Span.Slice(13), 5);
            Encoding.UTF8.GetBytes(expected.Sentence[01].Value, buffer.Span.Slice(15));

            var deserialized = new Simple();
            var input = new ReadOnlySequence<byte>(buffer);

            Assert.True(Serializer.TryDeserialize(ref input, deserialized, out var bytesRead));
            Assert.AreEqual(buffer.Length, bytesRead);
            Assert.True(expected.Equals(deserialized));
        }
    }

#pragma warning disable CS0659
    internal class Simple
#pragma warning restore CS0659
    {
        public int Identifier { get; set; }
        public Word[] Sentence { get; set; }

#pragma warning disable 659
        public override bool Equals(object obj)
#pragma warning restore 659
        {
            return obj is Simple value
                   && value.Identifier == Identifier
                   && value.Sentence.SequenceEqual(Sentence);
        }
    }

    internal class Word
    {
        public string Value { get; set; }
    }

    internal struct VarShort
    {
        public short Value { get; set; }

        public VarShort(short value) => Value = value;
    }

    internal static class Serializer
    {
        public static bool TryDeserialize<T>(ref ReadOnlySequence<byte> input, T value, out long byteRead) =>
            SerializerStorage<T>.TryDeserialize(ref input, value, out byteRead);

        public static bool TryDeserializeValues<T>(ref SequenceReader<byte> input, int length, out T[] values) where T : new()
        {
            if (length == 0)
            {
                values = Array.Empty<T>();
                return true;
            }

            values = new T[length];
            for (var i = 0; i < length; i++)
            {
                var current = new T();
                var b = input.GetRemainingSequence();
                if (!TryDeserialize(ref b, current, out var bytesRead))
                {
                    values = Array.Empty<T>();
                    return false;
                }

                values[i] = current;
                input.Advance(bytesRead);
            }

            return true;
        }

        public static void Setup<T>(DeserializerDlg<T> des) => 
            SerializerStorage<T>.TryDeserialize = des;

        private static class SerializerStorage<T>
        {
            public static DeserializerDlg<T> TryDeserialize { get; set; }
        }
    }

    internal static class ReaderExtensions
    {
        public static bool TryReadValue<T>(this ref SequenceReader<byte> reader, out T value) =>
            ReaderStorage<T>.TryRead(ref reader, out value);

        static ReaderExtensions()
        {
            ReaderStorage<int>.TryRead = (ref SequenceReader<byte> reader, out int value) =>
                reader.TryReadBigEndian(out value);

            ReaderStorage<string>.TryRead = (ref SequenceReader<byte> reader, out string value) =>
            {
                value = default;
                if (!reader.TryReadBigEndian(out short len)) return false;

                var strLen = unchecked((ushort)len);
                if (reader.Remaining < strLen) return false;

                var strSequence = reader.Sequence.Slice(reader.Position, strLen);
                value = strSequence.AsString();
                reader.Advance(strLen);
                return true;
            };

            ReaderStorage<VarShort>.TryRead = (ref SequenceReader<byte> reader, out VarShort value) =>
            {
                var result = 0;
                for (var offset = 0; offset < 16; offset += 7)
                {
                    if (!reader.TryRead(out var readByte))
                    {
                        value = default;
                        return false;
                    }

                    var hasNext = (readByte & 128) == 128;

                    if (offset > 0) result += (readByte & sbyte.MaxValue) << offset;
                    else result += (readByte & sbyte.MaxValue);

                    if (result > short.MaxValue) result -= 65536;
                    if (!hasNext) break;
                }

                value = new VarShort((short)result);
                return true;
            };
        }

        private static class ReaderStorage<T>
        {
            public delegate bool TryReadValue(ref SequenceReader<byte> reader, out T value);
            public static TryReadValue TryRead { get; set; }
        }
    }

    internal static class SequenceExtensions
    {
        public static string AsString(this ref ReadOnlySequence<byte> buffer, Encoding useEncoding = default)
        {
            var encoding = useEncoding ?? Encoding.UTF8;
            if (buffer.IsSingleSegment)
                return encoding.GetString(buffer.First.Span);

            return string.Create((int)buffer.Length, buffer, (span, sequence) =>
            {
                foreach (var segment in sequence)
                {
                    encoding.GetChars(segment.Span, span);
                    span = span.Slice(segment.Length);
                }
            });
        }

        public static ReadOnlySequence<byte> GetRemainingSequence(this ref SequenceReader<byte> r) =>
            r.Sequence.Slice(r.Position);
    }
}
#endif