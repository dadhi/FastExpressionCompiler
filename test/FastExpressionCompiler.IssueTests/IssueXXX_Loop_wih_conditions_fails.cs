using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class IssueXXX_Loop_wih_conditions_fails : ITest
    {
        public int Run()
        {
            Test1();
            return 1;
        }

        [Test]
        public void Test1()
        {
    //         var e = new Expression[63]; // unique expressions
    //         var expr = Lambda(/*$*/
    //           typeof(ReadMethods<Test[], BufferedStream, Settings_1449479367_b6fc048b>.ReadSealed),
    //           e[0] = Block(
    //             typeof(Test[]),
    //             new[]{
    //   e[1]=Parameter(typeof(Test[]), "result")
    //             },
    //             e[2] = Empty(),
    //             e[3] = Block(
    //               typeof(void),
    //               new[]{
    //     e[4]=Parameter(typeof(int), "length0")
    //               },
    //               e[5] = Call(
    //                 e[6] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
    //                 typeof(BufferedStream)/*.ReserveSize*/.GetMethods()[0],
    //                 e[7] = Constant(4)),
    //               e[8] = MakeBinary(ExpressionType.Assign,
    //                 e[4]/*Parameter*/,
    //                 e[9] = Call(
    //                   e[6]/*Parameter*/,
    //                   typeof(BufferedStream)/*.Read*/.GetMethods()[10].MakeGenericMethod(typeof(int)))),
    //               e[10] = MakeBinary(ExpressionType.Assign,
    //                 e[1]/*Parameter*/,
    //                 e[11] = NewArrayBounds(
    //                   typeof(Test)


    //                   e[4]/*Parameter*/)),
    //               e[12] = Call(
    //                 e[13] = Call(
    //                   e[14] = Parameter(typeof(Binary<BufferedStream, Settings_1449479367_b6fc048b>), "io"),
    //                   typeof(Binary<BufferedStream, Settings_1449479367_b6fc048b>)/*.get_LoadedObjectRefs*/.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)[0]),
    //                 typeof(List<object>)/*.Add*/.GetMethods()[0],
    //                 e[1]/*Parameter*/),
    //               e[15] = Block(
    //                 typeof(void),
    //                 new[]{
    //       e[16]=Parameter(typeof(int), "index0"),
    //       e[17]=Parameter(typeof(Test), "tempResult")
    //                 },
    //                 e[18] = Block(
    //                   typeof(void),
    //                   new ParameterExpression[0],
    //                   e[19] = MakeBinary(ExpressionType.Assign,
    //                     e[16]/*Parameter*/,
    //                     e[20] = MakeBinary(ExpressionType.Subtract,
    //                       e[4]/*Parameter*/,
    //                       e[21] = Constant(1))),
    //                   e[22] = Loop(
    //                     e[23] = Condition(
    //                       e[24] = MakeBinary(ExpressionType.LessThan,
    //                         e[16]/*Parameter*/,
    //                         e[25] = Constant(0)),
    //                       e[26] = MakeGoto(GotoExpressionKind.Break,
    //                         Label(typeof(void)),
    //                         null,
    //                         typeof(void)),
    //                       e[27] = Block(
    //                         typeof(int),
    //                         new ParameterExpression[0],
    //                         e[28] = Block(
    //                           typeof(Test),
    //                           new ParameterExpression[0],
    //                           e[2]/*Default*/,
    //                           e[29] = MakeBinary(ExpressionType.Assign,
    //                             e[30] = ArrayAccess(
    //                               e[1]/*Parameter*/,
    //                               e[16]/*Parameter*/),
    //                             e[31] = Block(
    //                               typeof(Test),
    //                               new ParameterExpression[0],
    //                               e[32] = MakeBinary(ExpressionType.Assign,
    //                                 e[17]/*Parameter*/,
    //                                 e[33] = Default(typeof(Test))),
    //                               e[34] = Call(
    //                                 e[6]/*Parameter*/,
    //                                 typeof(BufferedStream)/*.ReserveSize*/.GetMethods()[0],
    //                                 e[35] = Constant(5)),
    //                               e[36] = Condition(
    //                                 e[37] = MakeBinary(ExpressionType.Equal,
    //                                   e[38] = Call(
    //                                     e[6]/*Parameter*/,
    //                                     typeof(BufferedStream)/*.Read*/.GetMethods()[10].MakeGenericMethod(typeof(byte))),
    //                                   e[39] = Constant(0)),
    //                                 e[40] = MakeGoto(GotoExpressionKind.Goto,
    //                                   Label(typeof(void), "skipRead"),
    //                                   null,
    //                                   typeof(void)),
    //                                 e[2]/*Default*/,
    //                                 typeof(void)),
    //                               e[41] = Block(
    //                                 typeof(void),
    //                                 new[]{
    //                       e[42]=Parameter(typeof(int), "refIndex")
    //                                 },
    //                                 e[43] = MakeBinary(ExpressionType.Assign,
    //                                   e[42]/*Parameter*/,
    //                                   e[44] = Call(
    //                                     e[6]/*Parameter*/,
    //                                     typeof(BufferedStream)/*.Read*/.GetMethods()[10].MakeGenericMethod(typeof(int)))),
    //                                 e[45] = Condition(
    //                                   e[46] = MakeBinary(ExpressionType.NotEqual,
    //                                     e[42]/*Parameter*/,
    //                                     e[47] = Constant(-1)),
    //                                   e[48] = Block(
    //                                     typeof(void),
    //                                     new ParameterExpression[0],
    //                                     e[49] = MakeBinary(ExpressionType.Assign,
    //                                       e[17]/*Parameter*/,
    //                                       e[50] = Convert(
    //                                         e[51] = MakeIndex(
    //                                           e[52] = Call(
    //                                             e[14]/*Parameter*/,
    //                                             typeof(Binary<BufferedStream, Settings_1449479367_b6fc048b>)/*.get_LoadedObjectRefs*/.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)[0]),
    //                                           typeof(List<object>).GetTypeInfo().GetDeclaredProperty("Item"),
    //                                           e[53] = Decrement(
    //                                             e[42]/*Parameter*/)),
    //                                         typeof(Test))),
    //                                     e[54] = MakeGoto(GotoExpressionKind.Goto,
    //                                       Label(typeof(void), "skipRead"),
    //                                       null,
    //                                       typeof(void))),
    //                                   e[2]/*Default*/,
    //                                   typeof(void))),
    //                               e[55] = MakeBinary(ExpressionType.Assign,
    //                                 e[17]/*Parameter*/,
    //                                 e[56] = New(/*0 args*/
    //                                   typeof(Test).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])),
    //                               e[57] = Call(
    //                                 e[58] = Call(
    //                                   e[14]/*Parameter*/,
    //                                   typeof(Binary<BufferedStream, Settings_1449479367_b6fc048b>)/*.get_LoadedObjectRefs*/.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)[0]),
    //                                 typeof(List<object>)/*.Add*/.GetMethods()[0],
    //                                 e[17]/*Parameter*/),
    //                               e[59] = Label(Label(typeof(void), "skipRead")),
    //                               e[17]/*Parameter*/))),
    //                         e[60] = Label(Label(typeof(void), "continue0")),
    //                         e[61] = MakeBinary(ExpressionType.Assign,
    //                           e[16]/*Parameter*/,
    //                           e[62] = Decrement(
    //                             e[16]/*Parameter*/))),
    //                       typeof(void)),
    //                     Label(typeof(void)))))),
    //             e[1]/*Parameter*/),
    //         e[6]/*Parameter*/,
    //         e[14]/*Parameter*/);
        }

        public class Test
        {
            public Test()
            { }
        }

        internal class Settings_1449479367_b6fc048b {}

        internal static class ReadMethods<T, TStream, TSettingGen>
            where TStream : struct, IBinaryStream
        {
            public delegate T ReadSealed(ref TStream stream, Binary<TStream, TSettingGen> binary);
        }

        internal interface ISerializer
        {
            void Write<T>(T value, Stream outputStream);
            T Read<T>(Stream outputStream);
        }

        internal sealed partial class Binary<TStream, TSettingGen> : ISerializer, IBinary
            where TStream : struct, IBinaryStream
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public T Read<T>(Stream outputStream)
            {
                throw new NotImplementedException();
            }

            public void Write<T>(T value, Stream outputStream)
            {
                throw new NotImplementedException();
            }
        }

        public interface IBinary : IDisposable
        {
        }

        internal interface IBinaryStream : IDisposable
        {
            void ReadFrom(Stream stream);
            void WriteTo(Stream stream);

            void ReserveSize(int sizeNeeded);
            bool Flush();
            void Write(string input);
            void WriteTypeId(Type type);
            void Write<T>(T value) where T : struct;

            string Read();
            T Read<T>() where T : struct;
        }

        internal struct BufferedStream : IBinaryStream, IDisposable
        {
            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public bool Flush()
            {
                throw new NotImplementedException();
            }

            public string Read()
            {
                throw new NotImplementedException();
            }

            public T Read<T>() where T : struct
            {
                throw new NotImplementedException();
            }

            public void ReadFrom(Stream stream)
            {
                throw new NotImplementedException();
            }

            public void ReserveSize(int sizeNeeded)
            {
                throw new NotImplementedException();
            }

            public void Write(string input)
            {
                throw new NotImplementedException();
            }

            public void Write<T>(T value) where T : struct
            {
                throw new NotImplementedException();
            }

            public void WriteTo(Stream stream)
            {
                throw new NotImplementedException();
            }

            public void WriteTypeId(Type type)
            {
                throw new NotImplementedException();
            }
        }
    }
}