using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using NUnit.Framework;
using System.Text;
using SysExpr = System.Linq.Expressions.Expression;

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

            // Test_assignment_with_the_block_on_the_right_side_with_just_a_constant();
            // Test_assignment_with_the_block_on_the_right_side();

#if LIGHT_EXPRESSION
            // Can_make_convert_and_compile_binary_equal_expression_of_different_types();

            // Test_method_to_expression_code_string();

            // Test_nested_generic_type_output();
            // Test_triple_nested_non_generic();
            // Test_triple_nested_open_generic();
            // Test_non_generic_classes();
#endif
            return 4;
        }

        [Test]
        public void Test_assignment_with_the_block_on_the_right_side_with_just_a_constant()
        {
            var result = Parameter(typeof(int), "result");
            var temp = Parameter(typeof(int), "temp");
            var e = Lambda<Func<int>>(
                Block(
                    new ParameterExpression[] { result },
                    Assign(result, Block(
                        new ParameterExpression[] { temp },
                        Assign(temp, Constant(42)),
                        temp
                    )),
                    result
                )
            );

            e.PrintCSharpString();

            var fSys = e.CompileSys();
            Assert.AreEqual(42, fSys());

            var f = e.CompileFast(true);
            Assert.AreEqual(42, f());
        }

        [Test]
        public void Test_assignment_with_the_block_on_the_right_side()
        {
            var result = Parameter(typeof(int[]), "result");
            var temp = Parameter(typeof(int), "temp");
            var e = Lambda<Func<int[]>>(
                Block(
                    new ParameterExpression[] { result },
                    Assign(result, NewArrayBounds(typeof(int), Constant(1))),
                    Assign(
                        ArrayAccess(result, Constant(0)),
                        Block(
                            new ParameterExpression[] { temp },
                            Assign(temp, Constant(42)),
                            temp
                    )),
                    result
                )
            );

            e.PrintCSharpString();

            var fSys = e.CompileSys();
            Assert.AreEqual(42, fSys()[0]);

            var f = e.CompileFast(true);
            Assert.AreEqual(42, f()[0]);
        }

        /*
        ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed F = (
            ref BufferedStream stream, 
            Binary<BufferedStream, Settings_827720117> io) =>
        {
            ConstructorTests.Test[] result;

            int length0;
            stream.ReserveSize(4);
            length0 = stream.Read<int>();
            result = new ConstructorTests.Test[length0];
            io.get_LoadedObjectRefs().Add(result);
            int index0;
            ConstructorTests.Test tempResult;
            index0 = (length0 - 1);

            while (true)
            {
                if ((index0 < 0))
                {
                    goto void_11487847;
                }
                else
                {
                    tempResult = default(ConstructorTests.Test);
                    stream.ReserveSize(5);

                    if (stream.Read<byte>() == 0)
                    {
                        goto skipRead;
                    }

                    int refIndex;
                    refIndex = stream.Read<int>();

                    if (refIndex != -1)
                    {
                        tempResult = ((ConstructorTests.Test)io.get_LoadedObjectRefs().Item[Decrement(refIndex)]);
                        goto skipRead;
                    }
                    tempResult = new ConstructorTests.Test();
                    io.get_LoadedObjectRefs().Add(tempResult);

                    skipRead:
                    result[index0] = tempResult;

                    continue0:
                    return index0 = Decrement(index0);
                }
            }
            void_11487847: 
            return result;
        };
        */

        [Test]
        public void Test1()
        {
          var p = new ParameterExpression[7]; // the parameter expressions 
          var e = new Expression[56]; // the unique expressions 
          var l = new LabelTarget[3]; // the labels 
          var expr = Lambda(/*$*/
            typeof(ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed),
            e[0]=Block(
              typeof(ConstructorTests.Test[]),
              new[] {
              p[0]=Parameter(typeof(ConstructorTests.Test[]), "result")
              },
              e[1]=Empty(),
              e[2]=Block(
                typeof(void),
                new[] {
                p[1]=Parameter(typeof(int), "length0")
                },
                e[3]=Call(
                  p[2]=Parameter(typeof(BufferedStream).MakeByRefType(), "stream"), 
                  typeof(BufferedStream).GetMethods().Single(x => x.Name == "ReserveSize" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[4]=Constant(4)),
                e[5]=MakeBinary(ExpressionType.Assign,
                  p[1]/*(int length0)*/,
                  e[6]=Call(
                    p[2]/*(BufferedStream stream)*/, 
                    typeof(BufferedStream).GetMethods().Single(x => x.Name == "Read" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0).MakeGenericMethod(typeof(int)))),
                e[7]=MakeBinary(ExpressionType.Assign,
                  p[0]/*(ConstructorTests.Test[] result)*/,
                  e[8]=NewArrayBounds(
                    typeof(ConstructorTests.Test), 
                    p[1]/*(int length0)*/)),
                e[9]=Call(
                  e[10]=Call(
                    p[3]=Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"), 
                    typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).Single(x => x.Name == "get_LoadedObjectRefs" && !x.IsGenericMethod && x.GetParameters().Length == 0)), 
                  typeof(List<object>).GetMethods().Single(x => x.Name == "Add" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object) })),
                  p[0]/*(ConstructorTests.Test[] result)*/),
                e[11]=Block(
                  typeof(void),
                  new[] {
                  p[4]=Parameter(typeof(int), "index0"),
                  p[5]=Parameter(typeof(ConstructorTests.Test), "tempResult")
                  },
                  e[12]=Block(
                    typeof(void),
                    new ParameterExpression[0],
                    e[13]=MakeBinary(ExpressionType.Assign,
                      p[4]/*(int index0)*/,
                      e[14]=MakeBinary(ExpressionType.Subtract,
                        p[1]/*(int length0)*/,
                        e[15]=Constant(1))),
                    e[16]=Loop(
                      e[17]=Condition(
                        e[18]=MakeBinary(ExpressionType.LessThan,
                          p[4]/*(int index0)*/,
                          e[19]=Constant(0)),
                        e[20]=MakeGoto(GotoExpressionKind.Break,
                          l[0]=Label(typeof(void)),
                          null,
                          typeof(void)),
                        e[21]=Block(
                          typeof(int),
                          new ParameterExpression[0],
                          e[22]=Block(
                            typeof(ConstructorTests.Test),
                            new ParameterExpression[0],
                            e[1]/*Default*/,
                            e[23]=MakeBinary(ExpressionType.Assign,
                              e[24]=ArrayAccess(
                                p[0]/*(ConstructorTests.Test[] result)*/, new Expression[] {
                                p[4]/*(int index0)*/}),
                              e[25]=Block(
                                typeof(ConstructorTests.Test),
                                new ParameterExpression[0],
                                e[26]=MakeBinary(ExpressionType.Assign,
                                  p[5]/*(ConstructorTests.Test tempResult)*/,
                                  e[27]=Default(typeof(ConstructorTests.Test))),
                                e[28]=Call(
                                  p[2]/*(BufferedStream stream)*/, 
                                  typeof(BufferedStream).GetMethods().Single(x => x.Name == "ReserveSize" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                  e[29]=Constant(5)),
                                e[30]=Condition(
                                  e[31]=MakeBinary(ExpressionType.Equal,
                                    e[32]=Call(
                                      p[2]/*(BufferedStream stream)*/, 
                                      typeof(BufferedStream).GetMethods().Single(x => x.Name == "Read" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0).MakeGenericMethod(typeof(byte))),
                                    e[33]=Constant(0)),
                                  e[34]=MakeGoto(GotoExpressionKind.Goto,
                                    l[1]=Label(typeof(void), "skipRead"),
                                    null,
                                    typeof(void)),
                                  e[1]/*Default*/,
                                  typeof(void)),
                                e[35]=Block(
                                  typeof(void),
                                  new[] {
                                  p[6]=Parameter(typeof(int), "refIndex")
                                  },
                                  e[36]=MakeBinary(ExpressionType.Assign,
                                    p[6]/*(int refIndex)*/,
                                    e[37]=Call(
                                      p[2]/*(BufferedStream stream)*/, 
                                      typeof(BufferedStream).GetMethods().Single(x => x.Name == "Read" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0).MakeGenericMethod(typeof(int)))),
                                  e[38]=Condition(
                                    e[39]=MakeBinary(ExpressionType.NotEqual,
                                      p[6]/*(int refIndex)*/,
                                      e[40]=Constant(-1)),
                                    e[41]=Block(
                                      typeof(void),
                                      new ParameterExpression[0],
                                      e[42]=MakeBinary(ExpressionType.Assign,
                                        p[5]/*(ConstructorTests.Test tempResult)*/,
                                        e[43]=Convert(
                                          e[44]=MakeIndex(
                                            e[45]=Call(
                                              p[3]/*(Binary<BufferedStream, Settings_827720117> io)*/, 
                                              typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).Single(x => x.Name == "get_LoadedObjectRefs" && !x.IsGenericMethod && x.GetParameters().Length == 0)), 
                                            typeof(List<object>).GetTypeInfo().GetDeclaredProperty("Item"), new Expression[] {
                                            e[46]=Decrement(
                                              p[6]/*(int refIndex)*/)}),
                                          typeof(ConstructorTests.Test))),
                                      e[47]=MakeGoto(GotoExpressionKind.Goto,
                                        l[1]/* skipRead */,
                                        null,
                                        typeof(void))),
                                    e[1]/*Default*/,
                                    typeof(void))),
                                e[48]=MakeBinary(ExpressionType.Assign,
                                  p[5]/*(ConstructorTests.Test tempResult)*/,
                                  e[49]=New(/*0 args*/
                                    typeof(ConstructorTests.Test).GetTypeInfo().DeclaredConstructors.ToArray()[0],new Expression[0])),
                                e[50]=Call(
                                  e[51]=Call(
                                    p[3]/*(Binary<BufferedStream, Settings_827720117> io)*/, 
                                    typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic|BindingFlags.Instance).Single(x => x.Name == "get_LoadedObjectRefs" && !x.IsGenericMethod && x.GetParameters().Length == 0)), 
                                  typeof(List<object>).GetMethods().Single(x => x.Name == "Add" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object) })),
                                  p[5]/*(ConstructorTests.Test tempResult)*/),
                                e[52]=Label(l[1]/* skipRead */),
                                p[5]/*(ConstructorTests.Test tempResult)*/))),
                          e[53]=Label(l[2]=Label(typeof(void), "continue0")),
                          e[54]=MakeBinary(ExpressionType.Assign,
                            p[4]/*(int index0)*/,
                            e[55]=Decrement(
                              p[4]/*(int index0)*/))),
                        typeof(void)),
                      l[0]/* void_8087743 */)))),
              p[0]/*(ConstructorTests.Test[] result)*/),
            p[2]/*(BufferedStream stream)*/,
            p[3]/*(Binary<BufferedStream, Settings_827720117> io)*/);

            expr.PrintCSharpString();

            var fs = (ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed)expr.CompileSys();
            fs.PrintIL();

            var stream = new BufferedStream();
            var binary = new Binary<BufferedStream, Settings_827720117>();
            var x = fs(ref stream, binary);
            Assert.IsNotNull(x);

            var f = (ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed)expr.CompileFast();
            f.PrintIL();
            var y = f(ref stream, binary);
            Assert.IsNotNull(y);
        }

        public class ConstructorTests
        {
            public class Test
            {
                public Test()
                { }
            }
        }

        internal class Settings_827720117 { }

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
            internal List<object> LoadedObjectRefs { get; } = new List<object>();

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public T Read<T>(Stream outputStream)
            {
                return typeof(T) == typeof(int) ? (T)(object)2: default(T); // todo: @mock
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
                return typeof(T) == typeof(int) ? (T)(object)2: default(T); // todo: @mock
            }

            private static T Read2<T>() where T : struct
            {
                throw new NotImplementedException();
            }

            public void ReadFrom(Stream stream)
            {
                throw new NotImplementedException();
            }

            private int _reservedSize;
            public void ReserveSize(int sizeNeeded)
            {
                _reservedSize += sizeNeeded;
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


        #if LIGHT_EXPRESSION
        [Test]
        public void Can_make_convert_and_compile_binary_equal_expression_of_different_types() 
        {
            var e = Lambda<Func<bool>>(
              MakeBinary(ExpressionType.Equal, 
              Call(GetType().GetMethod(nameof(GetByte))),
              Constant(0))
            );

            e.PrintCSharpString();

            var f = e.CompileFast(true);
            f.PrintIL("FEC IL:");
            Assert.IsTrue(f());

            var fs = e.CompileSys();
            fs.PrintIL("System IL:");
            Assert.IsTrue(fs());
        }

        public static byte GetByte() => 0;

        [Test]
        public void Test_method_to_expression_code_string() 
        {
            var m = typeof(BufferedStream).GetMethods().Single(x => 
              x.Name == "Read" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0);
            Assert.AreEqual("Read", m.Name);

            var s = new StringBuilder().AppendMethod(m, true, null).ToString();
            Assert.AreEqual("typeof(IssueXXX_Loop_wih_conditions_fails.BufferedStream).GetMethods().Single(x => x.Name == \"Read\" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0)", s);

            m = typeof(BufferedStream).GetMethods().Single(x =>
              x.Name == "Read" && !x.IsGenericMethod && x.GetParameters().Length == 0);
            Assert.AreEqual("Read", m.Name);

            s = new StringBuilder().AppendMethod(m, true, null).ToString();
            Assert.AreEqual("typeof(IssueXXX_Loop_wih_conditions_fails.BufferedStream).GetMethods().Single(x => x.Name == \"Read\" && !x.IsGenericMethod && x.GetParameters().Length == 0)", s);

            m = typeof(BufferedStream).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(x =>
              x.Name == "Read2" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0);
            Assert.AreEqual("Read2", m.Name);

            s = new StringBuilder().AppendMethod(m, true, null).ToString();
            Assert.AreEqual("typeof(IssueXXX_Loop_wih_conditions_fails.BufferedStream).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Single(x => x.Name == \"Read2\" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0)", s);
        }

        [Test]
        public void Test_nested_generic_type_output() 
        {
            var s = typeof(ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed)
                .ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""));

            Assert.AreEqual("ReadMethods<Test[], BufferedStream, Settings_1449479367_b6fc048b>.ReadSealed", s);
        }

        [Test]
        public void Test_triple_nested_non_generic() 
        {
            var s = typeof(A<int>.B<string>.Z).ToCode(true);
            Assert.AreEqual("IssueXXX_Loop_wih_conditions_fails.A<int>.B<string>.Z", s);

            s = typeof(A<int>.B<string>.Z).ToCode();
            Assert.AreEqual("FastExpressionCompiler.LightExpression.IssueTests.IssueXXX_Loop_wih_conditions_fails.A<int>.B<string>.Z", s);

            s = typeof(A<int>.B<string>.Z[]).ToCode(true);
            Assert.AreEqual("IssueXXX_Loop_wih_conditions_fails.A<int>.B<string>.Z[]", s);
            
            s = typeof(A<int>.B<string>.Z[]).ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<int>.B<string>.Z[]", s);
        }

        [Test]
        public void Test_triple_nested_open_generic() 
        {
            var s = typeof(A<>).ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<>", s);
            
            s = typeof(A<>).ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""), true);
            Assert.AreEqual("A<X>", s);

            s = typeof(A<>.B<>).ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<>.B<>", s);

            s = typeof(A<>.B<>.Z).ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<>.B<>.Z", s);

            s = typeof(A<>.B<>.Z).ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""), true);
            Assert.AreEqual("A<X>.B<Y>.Z", s);
        }

        [Test]
        public void Test_non_generic_classes() 
        {
            var s = typeof(A.B.C).ToCode(true, (_, x) => x.Replace("IssueXXX_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A.B.C", s);
        }

        class A
        {
            public class B
            {
                public class C {}
            }
        }

        class A<X> 
        {
            public class B<Y> 
            {
                public class Z {}
            }
        }

#endif
    }
}