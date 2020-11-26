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
    public delegate bool DeserializerDlg<in T>(ref ReadOnlySequence<byte> seq, T value, out long bytesRead);

    [TestFixture]
    public class Issue237_Trying_to_implement_For_Foreach_loop_but_getting_an_InvalidProgramException_thrown : ITest
    {
        private static readonly MethodInfo _tryRead = typeof(ReaderExtensions).GetMethod(nameof(ReaderExtensions.TryReadValue));
        private static readonly MethodInfo _tryDeserialize = typeof(Serializer).GetMethod(nameof(Serializer.TryDeserializeValues));

        public int Run()
        {
            Conditional_with_Equal_true_should_shortcircuit_to_Brtrue_or_Brfalse();
            Conditional_with_Equal_0_should_shortcircuit_to_Brtrue_or_Brfalse();
            Conditional_with_Equal_false_should_shortcircuit_to_Brtrue_or_Brfalse();
            Conditional_with_NotEqual_true_should_shortcircuit_to_Brtrue_or_Brfalse();
            Conditional_with_NotEqual_false_should_shortcircuit_to_Brtrue_or_Brfalse();

            Try_compare_strings();

            Should_Deserialize_Simple();
            Should_Deserialize_Simple_via_manual_CSharp_code();

            return 8;
        }

        [Test]
        public void Should_Deserialize_Simple_via_manual_CSharp_code()
        {
            DeserializerDlg<Word> dlgWord = 
            /*DeserializerDlg<Word>*/(ref ReadOnlySequence<Byte> input, Word value, out Int64 bytesRead) => 
            {
                SequenceReader<Byte> reader;
                String wordValue;

                reader = new SequenceReader<Byte>(input);
                if (ReaderExtensions.TryReadValue<String>(
                    ref reader,
                    out wordValue) == false)
                {
                    bytesRead = reader.Consumed;
                    return false;
                }

                value.Value = wordValue;
                bytesRead = reader.Consumed;
                return true;
            };

            DeserializerDlg<Simple> dlgSimple = 
            /*DeserializerDlg<Simple>*/(ref ReadOnlySequence<Byte> input, Simple value, out Int64 bytesRead) => 
            {
                SequenceReader<Byte> reader;
                Int32 identifier;
                Word[] content;
                Byte contentLength;

                reader = new SequenceReader<Byte>(input);
                if (ReaderExtensions.TryReadValue<Int32>(
                    ref reader,
                    out identifier) == false)
                {
                    bytesRead = reader.Consumed;
                    return false;
                }

                if (ReaderExtensions.TryReadValue<Byte>(
                    ref reader,
                    out contentLength) == false)
                {
                    bytesRead = reader.Consumed;
                    return false;
                }

                if (Serializer.TryDeserializeValues<Word>(
                    ref reader,
                    (Int32)contentLength,
                    out content) == false)
                {
                    bytesRead = reader.Consumed;
                    return false;
                }

                value.Identifier = identifier;
                value.Sentence = content;
                bytesRead = reader.Consumed;
                return true;
            }; 

            Serializer.Setup(dlgWord);
            Serializer.Setup(dlgSimple);

            RunDeserializer();
        }

// This is for benchmark
#if !LIGHT_EXPRESSION
        public static void CreateExpression_and_CompileSys(
            out DeserializerDlg<Word> desWord, 
            out DeserializerDlg<Simple> desSimple) 
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

            desWord = expr0.Compile();

            var valueSimple = Parameter(typeof(Simple), "value");
            var identifierVar = Variable(typeof(int), "identifier");
            var contentVar = Variable(typeof(Word[]), "content");
            var contentLenVar = Variable(typeof(byte), "contentLength");

            var expr1 = Lambda<DeserializerDlg<Simple>>(
                    Block(new[] { reader, identifierVar, contentVar, contentLenVar },
                        createReader,
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(int)), reader, identifierVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(byte)), reader, contentLenVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryDeserialize.MakeGenericMethod(typeof(Word)), reader, Convert(contentLenVar, typeof(int)), contentVar), Constant(true)),
                            returnFalse),
                        Assign(Property(valueSimple, nameof(Simple.Identifier)), identifierVar),
                        Assign(Property(valueSimple, nameof(Simple.Sentence)), contentVar),
                    returnTrue),
                input, valueSimple, bytesRead);

            desSimple = expr1.Compile();
        }
#else
        public static void CreateLightExpression_and_CompileFast(
            out DeserializerDlg<Word> desWord, 
            out DeserializerDlg<Simple> desSimple) 
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

            desWord = expr0.CompileFast();

            var valueSimple = Parameter(typeof(Simple), "value");
            var identifierVar = Variable(typeof(int), "identifier");
            var contentVar = Variable(typeof(Word[]), "content");
            var contentLenVar = Variable(typeof(byte), "contentLength");

            var expr1 = Lambda<DeserializerDlg<Simple>>(
                    Block(new[] { reader, identifierVar, contentVar, contentLenVar },
                        createReader,
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(int)), reader, identifierVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(byte)), reader, contentLenVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryDeserialize.MakeGenericMethod(typeof(Word)), reader, Convert(contentLenVar, typeof(int)), contentVar), Constant(true)),
                            returnFalse),
                        Assign(Property(valueSimple, nameof(Simple.Identifier)), identifierVar),
                        Assign(Property(valueSimple, nameof(Simple.Sentence)), contentVar),
                    returnTrue),
                input, valueSimple, bytesRead);

            desSimple = expr1.CompileFast();
        }
#endif

        public static bool RunDeserializer(DeserializerDlg<Word> desWord, DeserializerDlg<Simple> desSimple)
        {
            Serializer.Setup(desWord);
            Serializer.Setup(desSimple);

            var expected = new Simple
            { 
                Identifier = 150, 
                Sentence = new[] { new Word { Value = "hello" }, new Word { Value = "there" } } 
            };
            
            Memory<byte> buffer = new byte[19];
            // 4 bytes
            BinaryPrimitives.WriteInt32BigEndian(buffer.Span, expected.Identifier);
            // 4+=1 byte for word count
            buffer.Span.Slice(4)[0] = 2;
            // 5+=2 bytes for the 1st word length
            BinaryPrimitives.WriteInt16BigEndian(buffer.Span.Slice(5), 5);
            // 7+=5 bytes for the 1st word
            Encoding.UTF8.GetBytes(expected.Sentence[0].Value, buffer.Span.Slice(7));
            // 12+=2 bytes for the 2nd word length
            BinaryPrimitives.WriteInt16BigEndian(buffer.Span.Slice(12), 5);
            // 14+=5 bytes for the 2nd word
            Encoding.UTF8.GetBytes(expected.Sentence[1].Value, buffer.Span.Slice(14));

            var deserialized = new Simple();
            var input = new ReadOnlySequence<byte>(buffer);
            return Serializer.TryDeserialize(ref input, deserialized, out var bytesRead);
        }

        [Test]
        public void Should_Deserialize_Simple()
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

            expr0.PrintCSharp();

            // sanity check
            var f0sys = expr0.CompileSys();
            f0sys.PrintIL("system compiled il");

            var f0 = expr0.CompileFast(true);
            f0.PrintIL();

            Serializer.Setup(f0);

            var valueSimple = Parameter(typeof(Simple), "value");
            var identifierVar = Variable(typeof(int), "identifier");
            var contentVar = Variable(typeof(Word[]), "content");
            var contentLenVar = Variable(typeof(byte), "contentLength");

            var expr1 = Lambda<DeserializerDlg<Simple>>(
                    Block(new[] { reader, identifierVar, contentVar, contentLenVar },
                        createReader,
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(int)), reader, identifierVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryRead.MakeGenericMethod(typeof(byte)), reader, contentLenVar), Constant(true)),
                            returnFalse),
                        IfThen(
                            NotEqual(Call(_tryDeserialize.MakeGenericMethod(typeof(Word)), reader, Convert(contentLenVar, typeof(int)), contentVar), Constant(true)),
                            returnFalse),
                        Assign(Property(valueSimple, nameof(Simple.Identifier)), identifierVar),
                        Assign(Property(valueSimple, nameof(Simple.Sentence)), contentVar),
                    returnTrue),
                input, valueSimple, bytesRead);

            expr1.PrintCSharp();

            var f1sys = expr1.CompileSys();
            f1sys.PrintIL("system compiled il");

            var f1 = expr1.CompileFast(true);
            f1.PrintIL();

            Serializer.Setup(f1);

            RunDeserializer();
        }

        public void RunDeserializer()
        {
            var expected = new Simple
            { 
                Identifier = 150, 
                Sentence = new[] { new Word { Value = "hello" }, new Word { Value = "there" } } 
            };
            
            Memory<byte> buffer = new byte[19];
            // 4 bytes
            BinaryPrimitives.WriteInt32BigEndian(buffer.Span, expected.Identifier);
            // 4+=1 byte for word count
            buffer.Span.Slice(4)[0] = 2;
            // 5+=2 bytes for the 1st word length
            BinaryPrimitives.WriteInt16BigEndian(buffer.Span.Slice(5), 5);
            // 7+=5 bytes for the 1st word
            Encoding.UTF8.GetBytes(expected.Sentence[0].Value, buffer.Span.Slice(7));
            // 12+=2 bytes for the 2nd word length
            BinaryPrimitives.WriteInt16BigEndian(buffer.Span.Slice(12), 5);
            // 14+=5 bytes for the 2nd word
            Encoding.UTF8.GetBytes(expected.Sentence[1].Value, buffer.Span.Slice(14));

            var deserialized = new Simple();
            var input = new ReadOnlySequence<byte>(buffer);
            var isDeserialized = Serializer.TryDeserialize(ref input, deserialized, out var bytesRead);
            
            Assert.True(isDeserialized);
            Assert.AreEqual(buffer.Length, bytesRead);
            Assert.AreEqual(expected, deserialized);
        }

        [Test]
        public void Conditional_with_Equal_true_should_shortcircuit_to_Brtrue_or_Brfalse()
        {
            var returnTarget = Label(typeof(string));
            var returnLabel = Label(returnTarget, Constant(null, typeof(string)));
            var returnFalse = Block(Return(returnTarget, Constant("false"), typeof(string)), returnLabel);
            var returnTrue = Block(Return(returnTarget, Constant("true"), typeof(string)), returnLabel);

            var boolParam = Parameter(typeof(bool), "b");

            var expr = Lambda<Func<bool, string>>(
                Block(
                    IfThen(Equal(boolParam, Constant(true)),
                        returnTrue),
                    returnFalse),
                boolParam);

            var fs = expr.CompileSys();
            fs.PrintIL("system compiled il");
            
            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            f.PrintIL();

            Assert.AreEqual("true",  f(true));
            Assert.AreEqual("false", f(false));
        }

        [Test]
        public void Conditional_with_Equal_0_should_shortcircuit_to_Brtrue_or_Brfalse()
        {
            var returnTarget = Label(typeof(string));
            var returnLabel = Label(returnTarget, Constant(null, typeof(string)));
            var returnFalse = Block(Return(returnTarget, Constant("false"), typeof(string)), returnLabel);
            var returnTrue = Block(Return(returnTarget, Constant("true"), typeof(string)), returnLabel);

            var intParam = Parameter(typeof(int), "n");

            var expr = Lambda<Func<int, string>>(
                Block(
                    IfThen(Equal(intParam, Constant(0)),
                        returnTrue),
                    returnFalse),
                intParam);

            var fs = expr.CompileSys();
            fs.PrintIL("system compiled il");
            
            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            f.PrintIL();

            Assert.AreEqual("true",  f(0));
            Assert.AreEqual("false", f(42));
        }

        [Test]
        public void Conditional_with_Equal_false_should_shortcircuit_to_Brtrue_or_Brfalse()
        {
            var returnTarget = Label(typeof(string));
            var returnLabel = Label(returnTarget, Constant(null, typeof(string)));
            var returnFalse = Block(Return(returnTarget, Constant("false"), typeof(string)), returnLabel);
            var returnTrue = Block(Return(returnTarget, Constant("true"), typeof(string)), returnLabel);

            var boolParam = Parameter(typeof(bool), "b");

            var expr = Lambda<Func<bool, string>>(
                Block(
                    IfThen(Equal(boolParam, Constant(false)),
                        returnFalse),
                    returnTrue),
                boolParam);
            
            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            f.PrintIL();

            Assert.AreEqual("true",  f(true));
            Assert.AreEqual("false", f(false));
        }

        [Test]
        public void Conditional_with_NotEqual_true_should_shortcircuit_to_Brtrue_or_Brfalse()
        {
            var returnTarget = Label(typeof(string));
            var returnLabel = Label(returnTarget, Constant(null, typeof(string)));
            var returnFalse = Block(Return(returnTarget, Constant("false"), typeof(string)), returnLabel);
            var returnTrue = Block(Return(returnTarget, Constant("true"), typeof(string)), returnLabel);

            var boolParam = Parameter(typeof(bool), "b");

            var expr = Lambda<Func<bool, string>>(
                Block(
                    IfThen(NotEqual(boolParam, Constant(true)),
                        returnFalse),
                    returnTrue),
                boolParam);
            
            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            f.PrintIL();

            Assert.AreEqual("true",  f(true));
            Assert.AreEqual("false", f(false));
        }

        [Test]
        public void Conditional_with_NotEqual_false_should_shortcircuit_to_Brtrue_or_Brfalse()
        {
            var returnTarget = Label(typeof(string));
            var returnLabel = Label(returnTarget, Constant(null, typeof(string)));
            var returnFalse = Block(Return(returnTarget, Constant("false"), typeof(string)), returnLabel);
            var returnTrue = Block(Return(returnTarget, Constant("true"), typeof(string)), returnLabel);

            var boolParam = Parameter(typeof(bool), "b");

            var expr = Lambda<Func<bool, string>>(
                Block(
                    IfThen(NotEqual(boolParam, Constant(false)),
                        returnTrue),
                    returnFalse),
                boolParam);
            
            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            f.PrintIL();

            Assert.AreEqual("true",  f(true));
            Assert.AreEqual("false", f(false));
        }

        public void Try_compare_strings()
        {
            var returnTarget = Label(typeof(string));
            var returnLabel = Label(returnTarget, Constant(null, typeof(string)));
            var returnFalse = Block(Return(returnTarget, Constant("false"), typeof(string)), returnLabel);
            var returnTrue = Block(Return(returnTarget, Constant("true"), typeof(string)), returnLabel);

            var strParam = Parameter(typeof(string), "s");

            var expr = Lambda<Func<string, string>>(
                Block(
                    IfThen(Equal(strParam, Constant("42")),
                        returnTrue),
                    returnFalse
                ),
                strParam);

            var fs = expr.CompileSys();
            fs.PrintIL("system compiled il");
            
            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
            f.PrintIL();

            Assert.AreEqual("true",  f("42"));
            Assert.AreEqual("false", f("35"));
        }
    }

    public class Simple
    {
        public int Identifier { get; set; }
        public Word[] Sentence { get; set; }

        public override bool Equals(object obj) =>
            obj is Simple value
                && value.Identifier == Identifier
                && value.Sentence.SequenceEqual(Sentence);
    }

    public class Word
    {
        public string Value { get; set; }
        public override bool Equals(object obj) => 
            obj is Word w && w.Value == Value; 
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

            ReaderStorage<byte>.TryRead = (ref SequenceReader<byte> reader, out byte value) =>
                reader.TryRead(out value);

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

                    if (offset > 0) 
                        result += (readByte & sbyte.MaxValue) << offset;
                    else 
                        result += (readByte & sbyte.MaxValue);

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