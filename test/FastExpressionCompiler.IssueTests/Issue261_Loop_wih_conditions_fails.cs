using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using SysExpr = System.Linq.Expressions.Expression;

using NUnit.Framework;
using System.Runtime.InteropServices;

#pragma warning disable CS0164, CS0649

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue261_Loop_wih_conditions_fails : ITest
    {
        public int Run()
        {
#if !NET472
            Test_serialization_of_the_Dictionary(); // passes!
#endif
            Constant_of_Type_value_should_be_of_RuntimeType_because_the_SystemConstant_works_this_way();
            Constant_of_Byte_should_stay_the_Byte_and_not_to_be_changed_to_int();

            Serialize_hard_coded();

            Serialize_the_nullable_decimal_array();
            Serialize_the_nullable_struct_array();

            Serialize_struct_with_the_explicit_layout_and_boxed_on_return();

            Test_unbox_struct_with_the_struct_member_with_the_explicit_layout_and_casted_ref_serialize();

            Test_DictionaryTest_StringDictionary();

            Test_the_big_re_engineering_test_from_the_Apex_Serializer_with_the_simple_mock_arguments();

            Test_assignment_with_the_block_on_the_right_side_with_just_a_constant();
            Test_assignment_with_the_block_on_the_right_side();

            // #265
            Test_class_items_array_index_via_variable_access_then_the_member_access();
            Test_struct_items_array_index_via_variable_access_then_the_member_access();

#if LIGHT_EXPRESSION
            FindMethodOrThrow_in_the_class_hierarchy();

            Test_find_generic_method_with_the_generic_param();

            Can_make_convert_and_compile_binary_equal_expression_of_different_types();

            Test_method_to_expression_code_string();

            Test_nested_generic_type_output();
            Test_triple_nested_non_generic();
            Test_triple_nested_open_generic();
            Test_non_generic_classes();
#else
            Should_throw_for_the_equal_expression_of_different_types();
#endif


#if LIGHT_EXPRESSION && NET472
            return 21;
#elif LIGHT_EXPRESSION
            return 20;
#elif NET472
            return 13;
#else
            return 12;
#endif
        }

        [Test]
        public void Constant_of_Type_value_should_be_of_RuntimeType_because_the_SystemConstant_works_this_way()
        {
            var t = GetType();
            Assert.AreEqual("RuntimeType", Constant(t).Type.Name);
        }

        [Test]
        public void Constant_of_Byte_should_stay_the_Byte_and_not_to_be_changed_to_int()
        {
            Assert.AreEqual(typeof(byte), Constant((byte)0).Type);
        }

        [Test]
        public void Serialize_the_nullable_decimal_array()
        {
            var p = new ParameterExpression[5]; // the parameter expressions 
            var e = new Expression[41]; // the unique expressions 
            var l = new LabelTarget[5]; // the labels 
            var expr = Lambda( // $
              typeof(WriteMethods<Nullable<Decimal>[], BufferedStream, Settings_827720117>.WriteSealed),
              e[0] = Block(
                typeof(void),
                new ParameterExpression[0],
                e[1] = Empty(),
                e[2] = Block(
                  typeof(void),
                  new[] {
                    p[0]=Parameter(typeof(int), "length0")
                  },
                  e[3] = Call(
                    p[1] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                    typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                    e[4] = Constant((int)4)),
                  e[5] = MakeBinary(ExpressionType.Assign,
                    p[0 // (int length0)
                      ],
                    e[6] = Call(
                      p[2] = Parameter(typeof(Nullable<Decimal>[]), "source"),
                      typeof(Array).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetLength" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                      e[7] = Constant((int)0))),
                  e[8] = Call(
                    p[1 // (BufferedStream stream)
                      ],
                    typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                    p[0 // (int length0)
                      ]),
                  e[9] = Condition(
                    e[10] = MakeBinary(ExpressionType.Equal,
                      e[7 // Constant of int
                        ],
                      p[0 // (int length0)
                        ]),
                    e[11] = MakeGoto(GotoExpressionKind.Goto,
                      l[0] = Label(typeof(void), "skipWrite"),
                      null,
                      typeof(void)),
                    e[1 // Default of void
                      ],
                    typeof(void)),
                  e[12] = Block(
                    typeof(void),
                    new[] {
        p[3]=Parameter(typeof(int), "i0")
                    },
                    e[13] = Block(
                      typeof(void),
                      new ParameterExpression[0],
                      e[14] = MakeBinary(ExpressionType.Assign,
                        p[3 // (int i0)
                          ],
                        e[15] = MakeBinary(ExpressionType.Subtract,
                          p[0 // (int length0)
                            ],
                          e[16] = Constant((int)1))),
                      e[17] = Loop(
                        e[18] = Condition(
                          e[19] = MakeBinary(ExpressionType.LessThan,
                            p[3 // (int i0)
                              ],
                            e[7 // Constant of int
                              ]),
                          e[20] = MakeGoto(GotoExpressionKind.Break,
                            l[1] = Label(typeof(void), "break0"),
                            null,
                            typeof(void)),
                          e[21] = Block(
                            typeof(int),
                            new ParameterExpression[0],
                            e[22] = Block(
                              typeof(void),
                              new ParameterExpression[0],
                              e[23] = Call(
                                p[1 // (BufferedStream stream)
                                  ],
                                typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                e[24] = Constant((int)17)),
                              e[25] = Condition(
                                e[26] = Call(
                                  e[27] = MakeBinary(ExpressionType.ArrayIndex,
                                    p[2 // (Nullable<Decimal>[] source)
                                      ],
                                    p[3 // (int i0)
                                      ]),
                                  typeof(Nullable<Decimal>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "get_HasValue" && x.GetParameters().Length == 0)),
                                e[28] = Block(
                                  typeof(void),
                                  new ParameterExpression[0],
                                  e[1 // Default of void
                                    ],
                                  e[29] = Call(
                                    p[1 // (BufferedStream stream)
                                      ],
                                    typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(byte)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(byte) })),
                                    e[30] = Constant((byte)1)),
                                  e[1 // Default of void
                                    ],
                                  e[31] = Call(
                                    p[1 // (BufferedStream stream)
                                      ],
                                    typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Decimal)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Decimal) })),
                                    e[32] = Call(
                                      e[27 // ArrayIndex of Nullable<Decimal>
                                        ],
                                      typeof(Nullable<Decimal>).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "get_Value" && x.GetParameters().Length == 0))),
                                  e[33] = Label(l[2] = Label(typeof(void), "finishWrite"))),
                                e[34] = Call(
                                  p[1 // (BufferedStream stream)
                                    ],
                                  typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(byte)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(byte) })),
                                  e[35] = Constant((byte)0)),
                                typeof(void))),
                            e[36] = Label(l[3] = Label(typeof(void), "continue0")),
                            e[37] = MakeBinary(ExpressionType.Assign,
                              p[3 // (int i0)
                                ],
                              e[38] = Decrement(
                                p[3 // (int i0)
                                  ]))),
                          typeof(void)),
                        l[1]/* break0 */))),
                  e[39] = Label(l[0]/* skipWrite */)),
                e[40] = Label(l[4] = Label(typeof(void), "finishWrite"))),
              p[2 // (Nullable<Decimal>[] source)
                ],
              p[1 // (BufferedStream stream)
                ],
              p[4] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"));

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast();
            f.PrintIL();
        }

        [Test]
        public void Serialize_the_nullable_struct_array()
        {
            var p = new ParameterExpression[6]; // the parameter expressions 
            var e = new Expression[38]; // the unique expressions 
            var l = new LabelTarget[2]; // the labels 
            var expr = Lambda( // $
              typeof(ReadMethods<Nullable<ArrayTests.Test2>[], BufferedStream, Settings_827720117>.ReadSealed),
              e[0] = Block(
                typeof(Nullable<ArrayTests.Test2>[]),
                new[] {
                  p[0]=Parameter(typeof(Nullable<ArrayTests.Test2>[]), "result")
                },
                e[1] = Empty(),
                e[2] = Block(
                  typeof(void),
                  new[] {
                    p[1]=Parameter(typeof(int), "length0")
                  },
                  e[3] = Call(
                    p[2] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                    typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                    e[4] = Constant((int)4)),
                  e[5] = MakeBinary(ExpressionType.Assign,
                    p[1 // (int length0)
                      ],
                    e[6] = Call(
                      p[2 // (BufferedStream stream)
                        ],
                      typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Read" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single())),
                  e[7] = MakeBinary(ExpressionType.Assign,
                    p[0 // (Nullable<ArrayTests.Test2>[] result)
                      ],
                    e[8] = NewArrayBounds(
                      typeof(Nullable<ArrayTests.Test2>),
                      p[1 // (int length0)
                        ])),
                  e[9] = Block(
                    typeof(void),
                    new[] {
        p[3]=Parameter(typeof(int), "index0")
                    },
                    e[10] = Block(
                      typeof(void),
                      new ParameterExpression[0],
                      e[11] = MakeBinary(ExpressionType.Assign,
                        p[3 // (int index0)
                          ],
                        e[12] = MakeBinary(ExpressionType.Subtract,
                          p[1 // (int length0)
                            ],
                          e[13] = Constant((int)1))),
                      e[14] = Loop(
                        e[15] = Condition(
                          e[16] = MakeBinary(ExpressionType.LessThan,
                            p[3 // (int index0)
                              ],
                            e[17] = Constant((int)0)),
                          e[18] = MakeGoto(GotoExpressionKind.Break,
                            l[0] = Label(typeof(void)),
                            null,
                            typeof(void)),
                          e[19] = Block(
                            typeof(int),
                            new ParameterExpression[0],
                            e[20] = Block(
                              typeof(Nullable<ArrayTests.Test2>),
                              new ParameterExpression[0],
                              e[21] = Call(
                                p[2 // (BufferedStream stream)
                                  ],
                                typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                e[22] = Constant((int)5)),
                              e[23] = MakeBinary(ExpressionType.Assign,
                                e[24] = ArrayAccess(
                                  p[0 // (Nullable<ArrayTests.Test2>[] result)
                                    ], new Expression[] {
                      p[3 // (int index0)
                        ]}),
                                e[25] = Block(
                                  typeof(Nullable<ArrayTests.Test2>),
                                  new ParameterExpression[0],
                                  e[1 // Default of void
                                    ],
                                  e[26] = Condition(
                                    e[27] = MakeBinary(ExpressionType.Equal,
                                      e[28] = Call(
                                        p[2 // (BufferedStream stream)
                                          ],
                                        typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Read" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(byte)) : x).Single()),
                                      e[29] = Constant((byte)0)),
                                    e[30] = Default(typeof(Nullable<ArrayTests.Test2>)),
                                    e[31] = Convert(
                                      e[32] = Block(
                                        typeof(ArrayTests.Test2),
                                        new[] {
                            p[4]=Parameter(typeof(ArrayTests.Test2), "tempResult")
                                        },
                                        e[1 // Default of void
                                          ],
                                        e[33] = MakeBinary(ExpressionType.Assign,
                                          p[4 // (ArrayTests.Test2 tempResult)
                                            ],
                                          e[34] = Call(
                                            p[2 // (BufferedStream stream)
                                              ],
                                            typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Read" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(ArrayTests.Test2)) : x).Single())),
                                        p[4 // (ArrayTests.Test2 tempResult)
                                          ]),
                                      typeof(Nullable<ArrayTests.Test2>)),
                                    typeof(Nullable<ArrayTests.Test2>))))),
                            e[35] = Label(l[1] = Label(typeof(void), "continue0")),
                            e[36] = MakeBinary(ExpressionType.Assign,
                              p[3 // (int index0)
                                ],
                              e[37] = Decrement(
                                p[3 // (int index0)
                                  ]))),
                          typeof(void)),
                        l[0]/* void__17068465 */)))),
                p[0 // (Nullable<ArrayTests.Test2>[] result)
                  ]),
              p[2 // (BufferedStream stream)
                ],
              p[5] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"));

            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
        }

        [Test]
        public void Serialize_hard_coded()
        {
            var p = new ParameterExpression[3]; // the parameter expressions 
            var e = new Expression[6]; // the unique expressions 
            var l = new LabelTarget[1]; // the labels 
            var expr = Lambda( // $
              typeof(WriteMethods<FieldInfoModifier.TestReadonly, BufferedStream, Settings_827720117>.WriteSealed),
              e[0] = Block(
                typeof(void),
                new ParameterExpression[0],
                e[1] = Call(
                  p[0] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                  typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[2] = Constant((int)4)),
                e[3] = Call(
                  p[0 // (BufferedStream stream)
                    ],
                  typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[4] = Field(
                    p[1] = Parameter(typeof(FieldInfoModifier.TestReadonly), "source"),
                    typeof(FieldInfoModifier.TestReadonly).GetTypeInfo().GetDeclaredField("Value"))),
                e[5] = Label(l[0] = Label(typeof(void), "finishWrite"))),
              p[1 // (FieldInfoModifier.TestReadonly source)
                ],
              p[0 // (BufferedStream stream)
                ],
              p[2] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"));

            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
        }

        [Test]
        public void Serialize_struct_with_the_explicit_layout_and_boxed_on_return()
        {
            var p = new ParameterExpression[3]; // the parameter expressions 
            var e = new Expression[8]; // the unique expressions 
            var l = new LabelTarget[0]; // the labels 
            var expr = Lambda( // $
              typeof(Binary<BufferedStream, Settings_827720117>.ReadObject),
              e[0] = Block(
                typeof(object),
                new[] {
    p[0]=Parameter(typeof(PrimitiveValue), "result")
                },
                e[1] = MakeBinary(ExpressionType.Assign,
                  p[0 // (PrimitiveValue result)
                    ],
                  e[2] = Default(typeof(PrimitiveValue))),
                e[3] = Call(
                  p[1] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                  typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[4] = Constant((int)16)),
                e[5] = MakeBinary(ExpressionType.Assign,
                  p[0 // (PrimitiveValue result)
                    ],
                  e[6] = Call(
                    p[1 // (BufferedStream stream)
                      ],
                    typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Read" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(PrimitiveValue)) : x).Single())),
                e[7] = Convert(
                  p[0 // (PrimitiveValue result)
                    ],
                  typeof(object))),
              p[1 // (BufferedStream stream)
                ],
              p[2] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"));
            
            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
        }

        [Test]
        public void Test_unbox_struct_with_the_struct_member_with_the_explicit_layout_and_casted_ref_serialize()
        {
            var p = new ParameterExpression[4]; // the parameter expressions 
            var e = new Expression[14]; // the unique expressions 
            var l = new LabelTarget[1]; // the labels 
            var expr = Lambda( // $
              typeof(Binary<BufferedStream, Settings_827720117>.WriteObject),
              e[0] = Block(
                typeof(void),
                new[] {
                  p[0]=Parameter(typeof(CustomProperty), "castedSource")
                },
                e[1] = MakeBinary(ExpressionType.Assign,
                  p[0 // (CustomProperty castedSource)
                    ],
                  e[2] = Unbox(
                    p[1] = Parameter(typeof(object), "source"),
                    typeof(CustomProperty))),
                e[3] = Call(
                  p[2] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                  typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[4] = Constant((int)4)),
                e[5] = Call(
                  p[3] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"),
                  typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(x => !x.IsGenericMethod && x.Name == "WriteTypeRef" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Type), typeof(bool) })),
                  e[6] = Constant(typeof(CustomProperty)),
                  e[7] = Constant(true)),
                e[8] = Call(
                  p[2 // (BufferedStream stream)
                    ],
                  typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Write" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string) })),
                  e[9] = Field(
                    p[0 // (CustomProperty castedSource)
                      ],
                    typeof(CustomProperty).GetTypeInfo().GetDeclaredField("<Key>k__BackingField"))),
                e[10] = Call(
                  p[3 // (Binary<BufferedStream, Settings_827720117> io)
                    ],
                  typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.IsGenericMethod && x.Name == "WriteSealedInternal" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(Value)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(Value), typeof(bool) })),
                  e[11] = Field(
                    p[0 // (CustomProperty castedSource)
                      ],
                    typeof(CustomProperty).GetTypeInfo().GetDeclaredField("<Value>k__BackingField")),
                  e[12] = Constant(false)),
                e[13] = Label(l[0] = Label(typeof(void), "finishWrite"))),
              p[1 // (object source)
                ],
              p[2 // (BufferedStream stream)
                ],
              p[3 // (Binary<BufferedStream, Settings_827720117> io)
                ]);

            var f = expr.CompileFast(true);
            Assert.IsNotNull(f);
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PrimitiveValue
        {
            [FieldOffset(0)] public decimal Number;
            [FieldOffset(0)] public bool Boolean;
            [FieldOffset(0)] public DateTime DateTime;
            [FieldOffset(0)] public Guid Guid;
        }

        public struct CustomProperty
        {
            public CustomProperty(string key, Value value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }
            public Value Value { get; }
        }

        public sealed class Value
        {
            internal PrimitiveValue? _primitive;
            internal string _string;
            internal object _collection;
            internal object _array;
        }

        [Test]
        public void Test_class_items_array_index_via_variable_access_then_the_member_access()
        {

            var elArr = Parameter(typeof(El[]), "elArr");
            var index = Parameter(typeof(int), "index");
            var tempIndex = Parameter(typeof(int), "tempIndex");

            var e = Lambda<Func<El[], int, string>>(
              Block(new[] { tempIndex },
                Assign(tempIndex, index),
                MakeMemberAccess(
                  ArrayIndex(elArr, tempIndex),
                  typeof(El).GetTypeInfo().GetDeclaredField(nameof(El.Message))
                )
              ),
              elArr, index
            );

            var f = e.CompileFast(true);

            f.AssertOpCodes(
                OpCodes.Ldarg_2,
                OpCodes.Stloc_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldelem_Ref,
                OpCodes.Ldfld,
                OpCodes.Ret
            );

            Assert.AreEqual("13", f(new[] { new El() }, 0));
        }

        [Test]
        public void Test_struct_items_array_index_via_variable_access_then_the_member_access()
        {
            var elArr = Parameter(typeof(ElVal[]), "elArr");
            var index = Parameter(typeof(int), "index");
            var tempIndex = Parameter(typeof(int), "tempIndex");

            var e = Lambda<Func<ElVal[], int, string>>(
              Block(new[] { tempIndex },
                Assign(tempIndex, index),
                MakeMemberAccess(
                  ArrayIndex(elArr, tempIndex),
                  typeof(ElVal).GetTypeInfo().GetDeclaredField(nameof(ElVal.Message))
                )
              ),
              elArr, index
            );

            var f = e.CompileFast(true);

            f.AssertOpCodes(
                OpCodes.Ldarg_2,
                OpCodes.Stloc_0,
                OpCodes.Ldarg_1,
                OpCodes.Ldloc_0,
                OpCodes.Ldelema,
                OpCodes.Ldfld,
                OpCodes.Ret
            );

            Assert.AreEqual("14", f(new[] { new ElVal("14", 43) }, 0));
        }

        public class El
        {
            public string Message = "13";
            public int Number = 42;
        }

        public struct ElVal
        {
            public string Message;
            public int Number;
            public ElVal(string m, int n) { Message = m; Number = n; }
        }

#if !NET472
        public static LambdaExpression CreateSerializeDictionaryExpression() 
        {
            var entryType = typeof(System.Collections.Generic.Dictionary<string, string>)
              .GetNestedTypes(BindingFlags.NonPublic).Single(t => t.Name == "Entry")
              .MakeGenericType(typeof(string), typeof(string));

            var entryArrType = entryType.MakeArrayType();

            // FieldInfo GetField<T>(string n) =>
            //   typeof(T).GetTypeInfo().GetDeclaredField(n) ?? throw new InvalidOperationException(n);

            // var _count     = GetField<Dictionary<string, string>>("_count");
            // var _freeCount = GetField<Dictionary<string, string>>("_freeCount");
            // var _freeList  = GetField<Dictionary<string, string>>("_freeList");
            // var _version   = GetField<Dictionary<string, string>>("_version");
            // var _buckets   = GetField<Dictionary<string, string>>("_buckets");
            // var _comparer  = GetField<Dictionary<string, string>>("_comparer");
            // var _entries   = GetField<Dictionary<string, string>>("_entries");

            // var getLength  = typeof(Array).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetLength" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) }));
            // var writeValuesArray1 = typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.IsGenericMethod && x.Name == "WriteValuesArray1" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int[]), typeof(int) }));

            // var hashCode = entryType.GetTypeInfo().GetDeclaredField("hashCode");
            // var value    = entryType.GetTypeInfo().GetDeclaredField("value");
            // var next     = entryType.GetTypeInfo().GetDeclaredField("next");
            // var key      = entryType.GetTypeInfo().GetDeclaredField("key");

            var p = new ParameterExpression[8]; // the parameter expressions 
            var e = new Expression[102]; // the unique expressions 
            var l = new LabelTarget[10]; // the labels 
            var expr = Lambda( // $
              typeof(WriteMethods<Dictionary<string, string>, BufferedStream, Settings_827720117>.WriteSealed),
              e[0] = Block(
                typeof(void),
                new ParameterExpression[0],
                e[1] = Call(
                  p[0] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                  typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[2] = Constant((int)16)),
                e[3] = Call(
                  p[0 // (BufferedStream stream)
                    ],
                  typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[4] = Field(
                    p[1] = Parameter(typeof(Dictionary<string, string>), "source"),
                    typeof(Dictionary<string, string>).GetTypeInfo().GetDeclaredField("_count"))),
                e[5] = Call(
                  p[0 // (BufferedStream stream)
                    ],
                  typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[6] = Field(
                    p[1 // (Dictionary<string, string> source)
                      ],
                    typeof(Dictionary<string, string>).GetTypeInfo().GetDeclaredField("_freeCount"))),
                e[7] = Call(
                  p[0 // (BufferedStream stream)
                    ],
                  typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[8] = Field(
                    p[1 // (Dictionary<string, string> source)
                      ],
                    typeof(Dictionary<string, string>).GetTypeInfo().GetDeclaredField("_freeList"))),
                e[9] = Call(
                  p[0 // (BufferedStream stream)
                    ],
                  typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[10] = Field(
                    p[1 // (Dictionary<string, string> source)
                      ],
                    typeof(Dictionary<string, string>).GetTypeInfo().GetDeclaredField("_version"))),
                e[11] = Block(
                  typeof(void),
                  new[] {
                  p[2]=Parameter(typeof(int[]), "tempResult")
                  },
                  e[12] = MakeBinary(ExpressionType.Assign,
                    p[2 // (int[] tempResult)
                      ],
                    e[13] = Field(
                      p[1 // (Dictionary<string, string> source)
                        ],
                      typeof(Dictionary<string, string>).GetTypeInfo().GetDeclaredField("_buckets"))),
                  e[14] = Block(
                    typeof(void),
                    new ParameterExpression[0],
                    e[15] = Call(
                      p[0 // (BufferedStream stream)
                        ],
                      typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                      e[16] = Constant((int)1)),
                    e[17] = Condition(
                      e[18] = MakeBinary(ExpressionType.Equal,
                        p[2 // (int[] tempResult)
                          ],
                        e[19] = Constant(null)),
                      e[20] = Block(
                        typeof(void),
                        new ParameterExpression[0],
                        e[21] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(byte)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(byte) })),
                          e[22] = Constant((byte)0)),
                        e[23] = MakeGoto(GotoExpressionKind.Continue,
                          l[0] = Label(typeof(void), "afterWrite"),
                          null,
                          typeof(void))),
                      e[24] = Block(
                        typeof(void),
                        new ParameterExpression[0],
                        e[25] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(byte)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(byte) })),
                          e[26] = Constant((byte)1))),
                      typeof(void)),
                    e[27] = Block(
                      typeof(void),
                      new ParameterExpression[0],
                      e[28] = Block(
                        typeof(void),
                        new[] {
                        p[3]=Parameter(typeof(int), "length0")
                        },
                        e[29] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                          e[30] = Constant((int)4)),
                        e[31] = MakeBinary(ExpressionType.Assign,
                          p[3 // (int length0)
                            ],
                          e[32] = Call(
                            p[2 // (int[] tempResult)
                              ],
                            typeof(Array).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetLength" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                            e[33] = Constant((int)0))),
                        e[34] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                          p[3 // (int length0)
                            ]),
                        e[35] = Condition(
                          e[36] = MakeBinary(ExpressionType.Equal,
                            e[33 // Constant of int
                              ],
                            p[3 // (int length0)
                              ]),
                          e[37] = MakeGoto(GotoExpressionKind.Goto,
                            l[1] = Label(typeof(void), "skipWrite"),
                            null,
                            typeof(void)),
                          e[38] = Empty(),
                          typeof(void)),
                        e[39] = Call(
                          p[4] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"),
                          typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(x => x.IsGenericMethod && x.Name == "WriteValuesArray1" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int[]), typeof(int) })),
                          p[2 // (int[] tempResult)
                            ],
                          e[40] = Constant((int)4)),
                        e[41] = Label(l[1]/* skipWrite */))),
                    e[42] = Label(l[0]/* afterWrite */)),
                  e[43] = Label(l[2] = Label(typeof(void), "finishWrite"))),
                e[44] = Call(
                  p[4 // (Binary<BufferedStream, Settings_827720117> io)
                    ],
                  typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(x => !x.IsGenericMethod && x.Name == "WriteInternal" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object) })),
                  e[45] = Field(
                    p[1 // (Dictionary<string, string> source)
                      ],
                    typeof(Dictionary<string, string>).GetTypeInfo().GetDeclaredField("_comparer"))),
                e[46] = Block(
                  typeof(void),
                  new[] {
                  p[5]=Parameter(entryArrType, "tempResult")
                  },
                  e[47] = MakeBinary(ExpressionType.Assign,
                    p[5 // (Dictionary<string, string>.Entry[] tempResult)
                      ],
                    e[48] = Field(
                      p[1 // (Dictionary<string, string> source)
                        ],
                      typeof(Dictionary<string, string>).GetTypeInfo().GetDeclaredField("_entries"))),
                  e[49] = Block(
                    typeof(void),
                    new ParameterExpression[0],
                    e[50] = Call(
                      p[0 // (BufferedStream stream)
                        ],
                      typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                      e[51] = Constant((int)1)),
                    e[52] = Condition(
                      e[53] = MakeBinary(ExpressionType.Equal,
                        p[5 // (Dictionary<string, string>.Entry[] tempResult)
                          ],
                        e[19 // Constant of object
                          ]),
                      e[54] = Block(
                        typeof(void),
                        new ParameterExpression[0],
                        e[55] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(byte)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(byte) })),
                          e[56] = Constant((byte)0)),
                        e[57] = MakeGoto(GotoExpressionKind.Continue,
                          l[3] = Label(typeof(void), "afterWrite"),
                          null,
                          typeof(void))),
                      e[58] = Block(
                        typeof(void),
                        new ParameterExpression[0],
                        e[59] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(byte)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(byte) })),
                          e[60] = Constant((byte)1))),
                      typeof(void)),
                    e[61] = Block(
                      typeof(void),
                      new ParameterExpression[0],
                      e[62] = Block(
                        typeof(void),
                        new[] {
                        p[6]=Parameter(typeof(int), "length0")
                        },
                        e[63] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                          e[64] = Constant((int)4)),
                        e[65] = MakeBinary(ExpressionType.Assign,
                          p[6 // (int length0)
                            ],
                          e[66] = Call(
                            p[5 // (Dictionary<string, string>.Entry[] tempResult)
                              ],
                            typeof(Array).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "GetLength" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                            e[33 // Constant of int
                              ])),
                        e[67] = Call(
                          p[0 // (BufferedStream stream)
                            ],
                          typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                          p[6 // (int length0)
                            ]),
                        e[68] = Condition(
                          e[69] = MakeBinary(ExpressionType.Equal,
                            e[33 // Constant of int
                              ],
                            p[6 // (int length0)
                              ]),
                          e[70] = MakeGoto(GotoExpressionKind.Goto,
                            l[4] = Label(typeof(void), "skipWrite"),
                            null,
                            typeof(void)),
                          e[38 // Default of void
                            ],
                          typeof(void)),
                        e[71] = Block(
                          typeof(void),
                          new[] {
                          p[7]=Parameter(typeof(int), "i0")
                          },
                          e[72] = Block(
                            typeof(void),
                            new ParameterExpression[0],
                            e[73] = MakeBinary(ExpressionType.Assign,
                              p[7 // (int i0)
                                ],
                              e[74] = MakeBinary(ExpressionType.Subtract,
                                p[6 // (int length0)
                                  ],
                                e[75] = Constant((int)1))),
                            e[76] = Loop(
                              e[77] = Condition(
                                e[78] = MakeBinary(ExpressionType.LessThan,
                                  p[7 // (int i0)
                                    ],
                                  e[33 // Constant of int
                                    ]),
                                e[79] = MakeGoto(GotoExpressionKind.Break,
                                  l[5] = Label(typeof(void), "break0"),
                                  null,
                                  typeof(void)),
                                e[80] = Block(
                                  typeof(int),
                                  new ParameterExpression[0],
                                  e[81] = Block(
                                    typeof(void),
                                    new ParameterExpression[0],
                                    e[38 // Default of void
                                      ],
                                    e[82] = Block(
                                      typeof(void),
                                      new ParameterExpression[0],
                                      e[83] = Call(
                                        p[0 // (BufferedStream stream)
                                          ],
                                        typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "ReserveSize" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                        e[84] = Constant((int)8)),
                                      e[85] = Call(
                                        p[0 // (BufferedStream stream)
                                          ],
                                        typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(System.UInt32)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(UInt32) })),
                                        e[86] = Field(
                                          e[87] = MakeBinary(ExpressionType.ArrayIndex,
                                            p[5 // (Dictionary<string, string>.Entry[] tempResult)
                                              ],
                                            p[7 // (int i0)
                                              ]),
                                          entryType.GetTypeInfo().GetDeclaredField("hashCode"))),
                                      e[88] = Call(
                                        p[0 // (BufferedStream stream)
                                          ],
                                        typeof(BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                        e[89] = Field(
                                          e[87 // ArrayIndex of Dictionary<string, string>.Entry
                                            ],
                                          entryType.GetTypeInfo().GetDeclaredField("next"))),
                                      e[90] = Call(
                                        p[0 // (BufferedStream stream)
                                          ],
                                        typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Write" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string) })),
                                        e[91] = Field(
                                          e[87 // ArrayIndex of Dictionary<string, string>.Entry
                                            ],
                                          entryType.GetTypeInfo().GetDeclaredField("key"))),
                                      e[92] = Call(
                                        p[0 // (BufferedStream stream)
                                          ],
                                        typeof(BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == "Write" && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(string) })),
                                        e[93] = Field(
                                          e[87 // ArrayIndex of Dictionary<string, string>.Entry
                                            ],
                                          entryType.GetTypeInfo().GetDeclaredField("value"))),
                                      e[94] = Label(l[6] = Label(typeof(void), "finishWrite")))),
                                  e[95] = Label(l[7] = Label(typeof(void), "continue0")),
                                  e[96] = MakeBinary(ExpressionType.Assign,
                                    p[7 // (int i0)
                                      ],
                                    e[97] = Decrement(
                                      p[7 // (int i0)
                                        ]))),
                                typeof(void)),
                              l[5]/* break0 */))),
                        e[98] = Label(l[4]/* skipWrite */))),
                    e[99] = Label(l[3]/* afterWrite */)),
                  e[100] = Label(l[8] = Label(typeof(void), "finishWrite"))),
                e[101] = Label(l[9] = Label(typeof(void), "finishWrite"))),
              p[1 // (Dictionary<string, string> source)
                ],
              p[0 // (BufferedStream stream)
                ],
              p[4 // (Binary<BufferedStream, Settings_827720117> io)
                ]);

            return expr;
        }

        [Test]
        public void Test_serialization_of_the_Dictionary()
        {
            var expr = CreateSerializeDictionaryExpression();

            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();

            var f = expr.CompileFast(true);
            f.PrintIL();
        }
#endif

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

            e.PrintCSharp();

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

            e.PrintCSharp();

            var fSys = e.CompileSys();
            Assert.AreEqual(42, fSys()[0]);

            var f = e.CompileFast(true);
            Assert.AreEqual(42, f()[0]);
        }

        [Test]
        public void Test_DictionaryTest_StringDictionary()
        {
            var p = new ParameterExpression[3]; // the parameter expressions 
            var e = new Expression[6]; // the unique expressions 
            var l = new LabelTarget[1]; // the labels 

            var expr = Lambda(/*$*/
              typeof(WriteMethods<FieldInfoModifier.TestReadonly, BufferedStream, Settings_827720117>.WriteSealed),
              e[0] = Block(
                typeof(void),
                new ParameterExpression[0],
                e[1] = Call(
                  p[0] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                  typeof(BufferedStream).GetMethods().Single(x =>
                  x.Name == "ReserveSize" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[2] = Constant((int)4)),
                e[3] = Call(
                  p[0]/*(BufferedStream stream)*/,
                  typeof(BufferedStream).GetMethods().Where(x =>
                  x.Name == "Write" && x.IsGenericMethod && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x)
                .Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                  e[4] = Field(
                    p[1] = Parameter(typeof(FieldInfoModifier.TestReadonly), "source"),
                    typeof(FieldInfoModifier.TestReadonly).GetTypeInfo().GetDeclaredField("Value"))),
                e[5] = Label(l[0] = Label(typeof(void), "finishWrite"))),
              p[1]/*(FieldInfoModifier.TestReadonly source)*/,
              p[0]/*(BufferedStream stream)*/,
              p[2] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"));

            expr.PrintCSharp();

            var fs = (WriteMethods<FieldInfoModifier.TestReadonly, BufferedStream, Settings_827720117>.WriteSealed)expr.CompileSys();
            fs.PrintIL();

            var f = (WriteMethods<FieldInfoModifier.TestReadonly, BufferedStream, Settings_827720117>.WriteSealed)expr.CompileFast(true);
            f.PrintIL();
        }

        [Test]
        public void Test_the_big_re_engineering_test_from_the_Apex_Serializer_with_the_simple_mock_arguments()
        {
            var p = new ParameterExpression[7]; // the parameter expressions 
            var e = new Expression[56]; // the unique expressions 
            var l = new LabelTarget[3]; // the labels 
            var expr = Lambda(/*$*/
              typeof(ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed),
              e[0] = Block(
                typeof(ConstructorTests.Test[]),
                new[] {
              p[0]=Parameter(typeof(ConstructorTests.Test[]), "result")
                },
                e[1] = Empty(),
                e[2] = Block(
                  typeof(void),
                  new[] {
                p[1]=Parameter(typeof(int), "length0")
                  },
                  e[3] = Call(
                    p[2] = Parameter(typeof(BufferedStream).MakeByRefType(), "stream"),
                    typeof(BufferedStream).GetMethods().Single(x => x.Name == "ReserveSize" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                    e[4] = Constant((int)4)),
                  e[5] = MakeBinary(ExpressionType.Assign,
                    p[1]/*(int length0)*/,
                    e[6] = Call(
                      p[2]/*(BufferedStream stream)*/,
                      typeof(BufferedStream).GetMethods().Single(x => x.Name == "Read" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0).MakeGenericMethod(typeof(int)))),
                  e[7] = MakeBinary(ExpressionType.Assign,
                    p[0]/*(ConstructorTests.Test[] result)*/,
                    e[8] = NewArrayBounds(
                      typeof(ConstructorTests.Test),
                      p[1]/*(int length0)*/)),
                  e[9] = Call(
                    e[10] = Call(
                      p[3] = Parameter(typeof(Binary<BufferedStream, Settings_827720117>), "io"),
                      typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(x => x.Name == "get_LoadedObjectRefs" && !x.IsGenericMethod && x.GetParameters().Length == 0)),
                    typeof(List<object>).GetMethods().Single(x => x.Name == "Add" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object) })),
                    p[0]/*(ConstructorTests.Test[] result)*/),
                  e[11] = Block(
                    typeof(void),
                    new[] {
                  p[4]=Parameter(typeof(int), "index0"),
                  p[5]=Parameter(typeof(ConstructorTests.Test), "tempResult")
                    },
                    e[12] = Block(
                      typeof(void),
                      new ParameterExpression[0],
                      e[13] = MakeBinary(ExpressionType.Assign,
                        p[4]/*(int index0)*/,
                        e[14] = MakeBinary(ExpressionType.Subtract,
                          p[1]/*(int length0)*/,
                          e[15] = Constant((int)1))),
                      e[16] = Loop(
                        e[17] = Condition(
                          e[18] = MakeBinary(ExpressionType.LessThan,
                            p[4]/*(int index0)*/,
                            e[19] = Constant((int)0)),
                          e[20] = MakeGoto(GotoExpressionKind.Break,
                            l[0] = Label(typeof(void)),
                            null,
                            typeof(void)),
                          e[21] = Block(
                            typeof(int),
                            new ParameterExpression[0],
                            e[22] = Block(
                              typeof(ConstructorTests.Test),
                              new ParameterExpression[0],
                              e[1]/*Default*/,
                              e[23] = MakeBinary(ExpressionType.Assign,
                                e[24] = ArrayAccess(
                                  p[0]/*(ConstructorTests.Test[] result)*/, new Expression[] {
                                p[4]/*(int index0)*/}),
                                e[25] = Block(
                                  typeof(ConstructorTests.Test),
                                  new ParameterExpression[0],
                                  e[26] = MakeBinary(ExpressionType.Assign,
                                    p[5]/*(ConstructorTests.Test tempResult)*/,
                                    e[27] = Default(typeof(ConstructorTests.Test))),
                                  e[28] = Call(
                                    p[2]/*(BufferedStream stream)*/,
                                    typeof(BufferedStream).GetMethods().Single(x => x.Name == "ReserveSize" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) })),
                                    e[29] = Constant((int)5)),
                                  e[30] = Condition(
                                    e[31] = MakeBinary(ExpressionType.Equal,
                                      e[32] = Call(
                                        p[2]/*(BufferedStream stream)*/,
                                        typeof(BufferedStream).GetMethods().Single(x => x.Name == "Read" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0).MakeGenericMethod(typeof(byte))),
                                      e[33] = Constant((byte)0)),
                                    e[34] = MakeGoto(GotoExpressionKind.Goto,
                                      l[1] = Label(typeof(void), "skipRead"),
                                      null,
                                      typeof(void)),
                                    e[1]/*Default*/,
                                    typeof(void)),
                                  e[35] = Block(
                                    typeof(void),
                                    new[] {
                                  p[6]=Parameter(typeof(int), "refIndex")
                                    },
                                    e[36] = MakeBinary(ExpressionType.Assign,
                                      p[6]/*(int refIndex)*/,
                                      e[37] = Call(
                                        p[2]/*(BufferedStream stream)*/,
                                        typeof(BufferedStream).GetMethods().Single(x => x.Name == "Read" && x.IsGenericMethod && x.GetGenericArguments().Length == 1 && x.GetParameters().Length == 0).MakeGenericMethod(typeof(int)))),
                                    e[38] = Condition(
                                      e[39] = MakeBinary(ExpressionType.NotEqual,
                                        p[6]/*(int refIndex)*/,
                                        e[40] = Constant((int)-1)),
                                      e[41] = Block(
                                        typeof(void),
                                        new ParameterExpression[0],
                                        e[42] = MakeBinary(ExpressionType.Assign,
                                          p[5]/*(ConstructorTests.Test tempResult)*/,
                                          e[43] = Convert(
                                            e[44] = MakeIndex(
                                              e[45] = Call(
                                                p[3]/*(Binary<BufferedStream, Settings_827720117> io)*/,
                                                typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(x => x.Name == "get_LoadedObjectRefs" && !x.IsGenericMethod && x.GetParameters().Length == 0)),
                                              typeof(List<object>).GetTypeInfo().GetDeclaredProperty("Item"), new Expression[] {
                                            e[46]=Decrement(
                                              p[6]/*(int refIndex)*/)}),
                                            typeof(ConstructorTests.Test))),
                                        e[47] = MakeGoto(GotoExpressionKind.Goto,
                                          l[1]/* skipRead */,
                                          null,
                                          typeof(void))),
                                      e[1]/*Default*/,
                                      typeof(void))),
                                  e[48] = MakeBinary(ExpressionType.Assign,
                                    p[5]/*(ConstructorTests.Test tempResult)*/,
                                    e[49] = New(/*0 args*/
                                      typeof(ConstructorTests.Test).GetTypeInfo().DeclaredConstructors.ToArray()[0], new Expression[0])),
                                  e[50] = Call(
                                    e[51] = Call(
                                      p[3]/*(Binary<BufferedStream, Settings_827720117> io)*/,
                                      typeof(Binary<BufferedStream, Settings_827720117>).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(x => x.Name == "get_LoadedObjectRefs" && !x.IsGenericMethod && x.GetParameters().Length == 0)),
                                    typeof(List<object>).GetMethods().Single(x => x.Name == "Add" && !x.IsGenericMethod && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(object) })),
                                    p[5]/*(ConstructorTests.Test tempResult)*/),
                                  e[52] = Label(l[1]/* skipRead */),
                                  p[5]/*(ConstructorTests.Test tempResult)*/))),
                            e[53] = Label(l[2] = Label(typeof(void), "continue0")),
                            e[54] = MakeBinary(ExpressionType.Assign,
                              p[4]/*(int index0)*/,
                              e[55] = Decrement(
                                p[4]/*(int index0)*/))),
                          typeof(void)),
                        l[0]/* void_60623824 */)))),
                p[0]/*(ConstructorTests.Test[] result)*/),
              p[2]/*(BufferedStream stream)*/,
              p[3]/*(Binary<BufferedStream, Settings_827720117> io)*/);

            var s = string.Empty;
            expr.PrintCSharp(ref s);
            StringAssert.DoesNotContain("return index0", s);

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

        ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed F = (
            ref BufferedStream stream,
            Binary<BufferedStream, Settings_827720117> io) =>
        {
            Issue261_Loop_wih_conditions_fails.ConstructorTests.Test[] result;

            int length0;
            stream.ReserveSize((int)4);
            length0 = stream.Read<int>();
            result = new Issue261_Loop_wih_conditions_fails.ConstructorTests.Test[length0];
            io.LoadedObjectRefs.Add(result);
            int index0;
            Issue261_Loop_wih_conditions_fails.ConstructorTests.Test tempResult;
            index0 = (length0 - 1);

            while (true)
            {
                if (index0 < (int)0)
                {
                    goto void_58225482;
                }
                else
                {

                    // The block result will be assigned to `result[index0]` {
                    tempResult = default(Issue261_Loop_wih_conditions_fails.ConstructorTests.Test);
                    stream.ReserveSize((int)5);

                    if (stream.Read<byte>() == (byte)0)
                    {
                        goto skipRead;
                    }

                    int refIndex;
                    refIndex = stream.Read<int>();

                    if (refIndex != (int)-1)
                    {
                        tempResult = ((Issue261_Loop_wih_conditions_fails.ConstructorTests.Test)io.LoadedObjectRefs[(refIndex - 1)]);
                        goto skipRead;
                    }
                    tempResult = new Issue261_Loop_wih_conditions_fails.ConstructorTests.Test();
                    io.LoadedObjectRefs.Add(tempResult);

                skipRead:
                    result[index0] = tempResult;
                    //} end of block assignment

                    // continue0: // todo: @incomplete - if label is not reference we may safely remove or better comment it in the output
                    index0 = (index0 - 1);
                }
            }
        void_58225482:
            return result;
        };

        internal static class FieldInfoModifier
        {
            internal class TestReadonly
            {
                public TestReadonly()
                {
                }

                public TestReadonly(int v)
                {
                    Value = v;
                }

                public readonly int Value;
            }
        }

        public class ConstructorTests
        {
            public class Test
            {
                public Test()
                { }
            }
        }

        public class ArrayTests
        {
            public struct Test2
            {
                public int Value;
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

        internal static class WriteMethods<T, TStream, TSettingGen>
            where TStream : struct, IBinaryStream
        {
            public delegate void WriteSealed(T obj, ref TStream stream, Binary<TStream, TSettingGen> binary);
            public static WriteSealed Method;
            public static int VersionUniqueId;
        }

        internal sealed partial class Binary<TStream, TSettingGen> : ISerializer, IBinary
            where TStream : struct, IBinaryStream
        {
            public delegate void WriteObject(object obj, ref TStream stream, Binary<TStream, TSettingGen> binary);
            public delegate object ReadObject(ref TStream stream, Binary<TStream, TSettingGen> binary);

            internal List<object> LoadedObjectRefs { get; } = new List<object>();

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public T Read<T>(Stream outputStream)
            {
                return typeof(T) == typeof(int) ? (T)(object)2 : default(T); // todo: @mock
            }

            public void Write<T>(T value, Stream outputStream)
            {
                throw new NotImplementedException();
            }

            //unsafe
            internal void WriteValuesArray1<T>(T[] array, int elementSize)
                where T : unmanaged
            {
                // fixed (void* ptr = array)
                // {
                //     _stream.WriteBytes(ptr, (uint)(array.Length * elementSize));
                // }
            }

            internal void WriteInternal(object value)
            {
                // if (WriteNullByte(value))
                // {
                //     return;
                // }

                // var type = value!.GetType();

                // if (_lastWriteType == type)
                // {
                //     _lastWriteMethod!(value, ref _stream, this);
                //     return;
                // }

                // ref var method = ref VirtualWriteMethods.GetOrAddValueRef(type);

                // if (method == null)
                // {
                //     method = DynamicCode<TStream, Binary<TStream, TSettingGen>>.GenerateWriteMethod<WriteObject>(type, Settings, true, false);
                // }

                // _lastWriteType = type;
                // _lastWriteMethod = method;

                // method(value, ref _stream, this);
            }

            internal bool WriteTypeRef(Type value, bool writeSerializedVersionId)
            {
                // if (_lastRefType == value)
                // {
                //     _stream.Write(_lastRefIndex);
                //     return false;
                // }

                // _lastRefType = value;

                // ref var index = ref _savedTypeLookup.GetOrAddValueRef(value);
                // if (index == 0)
                // {
                //     index = _savedTypeLookup.Count;
                //     _stream.Write(-1);
                //     _stream.WriteTypeId(value);
                //     if (writeSerializedVersionId)
                //     {
                //         _stream.ReserveSize(4);
                //         var id = GetSerializedVersionUniqueId(value);
                //         _stream.Write(id);
                //     }
                //     _lastRefIndex = index;
                //     return true;
                // }

                // _stream.Write(index);
                // _lastRefIndex = index;
                return false;
            }

            internal void WriteSealedInternal<T>(T value, bool useSerializedVersionId)
            {
                // _stream.ReserveSize(5);
                // if (ReferenceEquals(value, null))
                // {
                //     _stream.Write((byte)0);
                //     return;
                // }
                // else
                // {
                //     _stream.Write((byte)1);
                // }

                // if(useSerializedVersionId)
                // {
                //     var id = GetSerializedVersionUniqueId<T>();
                //     _stream.Write(id);
                // }
                // ref var method = ref WriteMethods<T, TStream, TSettingGen>.Method;
                // if (method == null)
                // {
                //     CheckTypes(value!);

                //     method = DynamicCode<TStream, Binary<TStream, TSettingGen>>.GenerateWriteMethod<WriteMethods<T, TStream, TSettingGen>.WriteSealed>(value!.GetType(), Settings, false, false);
                // }

                // method(value, ref _stream, this);
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
                return typeof(T) == typeof(int) ? (T)(object)2 : default(T); // todo: @mock
            }

            private static T Read2<T>() where T : struct
            {
                throw new NotImplementedException();
            }

            public void ReadFrom(Stream stream)
            {
            }

            private int _reservedSize;
            public void ReserveSize(int sizeNeeded)
            {
                _reservedSize += sizeNeeded;
            }

            public void Write(string input)
            {
            }

            public void Write<T>(T value) where T : struct
            {
            }

            public void WriteTo(Stream stream)
            {
            }

            public void WriteTypeId(Type type)
            {
            }
        }

        public static byte GetByte() => 0;

#if !LIGHT_EXPRESSION
        [Test]
        public void Should_throw_for_the_equal_expression_of_different_types()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
              Equal(
                Constant(0),
                Call(GetType().GetMethod(nameof(GetByte)))
              )
            );

            StringAssert.StartsWith("The binary operator Equal is not defined for the types", ex.Message);
        }
#endif

#if LIGHT_EXPRESSION
        [Test]
        public void Can_make_convert_and_compile_binary_equal_expression_of_different_types() 
        {
            var e = Lambda<Func<bool>>(
              MakeBinary(ExpressionType.Equal, 
              Call(GetType().GetMethod(nameof(GetByte))),
              Constant((byte)0))
            );

            var s = e.ToExpressionString();
            StringAssert.Contains("Constant((byte)0)", s);

            e.PrintCSharp();

            var f = e.CompileFast(true);
            f.PrintIL("FEC IL:");
            Assert.IsTrue(f());

            var fs = e.CompileSys();
            fs.PrintIL("System IL:");
            Assert.IsTrue(fs());
        }

        [Test]
        public void Test_find_generic_method_with_the_generic_param() 
        {
            var m = typeof(BufferedStream).GetMethods()
                .Where(x  => x.IsGenericMethod && x.Name == "Write" && x.GetGenericArguments().Length == 1)
                .Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x)
                .Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) }));

            Assert.IsNotNull(m);

            var s = new StringBuilder().AppendMethod(m, true, null).ToString();
            Assert.AreEqual("typeof(Issue261_Loop_wih_conditions_fails.BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == \"Write\" && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single(x => x.GetParameters().Select(y => y.ParameterType).SequenceEqual(new[] { typeof(int) }))", s);
        }

        [Test]
        public void Test_method_to_expression_code_string() 
        {
            var m = typeof(BufferedStream).GetMethods()
                .Where(x  => x.IsGenericMethod && x.Name == "Read" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1)
                .Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x)
                .Single();

            Assert.AreEqual("Read", m.Name);

            var s = new StringBuilder().AppendMethod(m, true, null).ToString();
            Assert.AreEqual("typeof(Issue261_Loop_wih_conditions_fails.BufferedStream).GetMethods().Where(x => x.IsGenericMethod && x.Name == \"Read\" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single()", s);

            m = typeof(BufferedStream).GetMethods()
              .Single(x => !x.IsGenericMethod && x.Name == "Read" && x.GetParameters().Length == 0);
            Assert.AreEqual("Read", m.Name);

            s = new StringBuilder().AppendMethod(m, true, null).ToString();
            Assert.AreEqual("typeof(Issue261_Loop_wih_conditions_fails.BufferedStream).GetMethods().Single(x => !x.IsGenericMethod && x.Name == \"Read\" && x.GetParameters().Length == 0)", s);

            m = typeof(BufferedStream).GetMethods(BindingFlags.NonPublic|BindingFlags.Static)
                .Where(x  => x.IsGenericMethod && x.Name == "Read2" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1)
                .Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x)
                .Single();
            Assert.AreEqual("Read2", m.Name);

            s = new StringBuilder().AppendMethod(m, true, null).ToString();
            Assert.AreEqual("typeof(Issue261_Loop_wih_conditions_fails.BufferedStream).GetMethods(BindingFlags.NonPublic|BindingFlags.Static).Where(x => x.IsGenericMethod && x.Name == \"Read2\" && x.GetParameters().Length == 0 && x.GetGenericArguments().Length == 1).Select(x => x.IsGenericMethodDefinition ? x.MakeGenericMethod(typeof(int)) : x).Single()", s);
        }

        [Test]
        public void Test_nested_generic_type_output() 
        {
            var s = typeof(ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed)
                .ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""));

            Assert.AreEqual("ReadMethods<ConstructorTests.Test[], BufferedStream, Settings_827720117>.ReadSealed", s);
        }

        [Test]
        public void Test_triple_nested_non_generic() 
        {
            var s = typeof(A<int>.B<string>.Z).ToCode(true);
            Assert.AreEqual("Issue261_Loop_wih_conditions_fails.A<int>.B<string>.Z", s);

            s = typeof(A<int>.B<string>.Z).ToCode();
            Assert.AreEqual("FastExpressionCompiler.LightExpression.IssueTests.Issue261_Loop_wih_conditions_fails.A<int>.B<string>.Z", s);

            s = typeof(A<int>.B<string>.Z[]).ToCode(true);
            Assert.AreEqual("Issue261_Loop_wih_conditions_fails.A<int>.B<string>.Z[]", s);
            
            s = typeof(A<int>.B<string>.Z[]).ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<int>.B<string>.Z[]", s);
        }

        [Test]
        public void Test_triple_nested_open_generic() 
        {
            var s = typeof(A<>).ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<>", s);
            
            s = typeof(A<>).ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""), true);
            Assert.AreEqual("A<X>", s);

            s = typeof(A<>.B<>).ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<>.B<>", s);

            s = typeof(A<>.B<>.Z).ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""));
            Assert.AreEqual("A<>.B<>.Z", s);

            s = typeof(A<>.B<>.Z).ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""), true);
            Assert.AreEqual("A<X>.B<Y>.Z", s);
        }

        [Test]
        public void Test_non_generic_classes() 
        {
            var s = typeof(A.B.C).ToCode(true, (_, x) => x.Replace("Issue261_Loop_wih_conditions_fails.", ""));
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

        public interface IFoo
        {
            void Nah(int i);
        }

        public class Foo : IFoo
        {
            public int MethodIndex = -1;

            void IFoo.Nah(int i)             { MethodIndex = 0; }
            public void Nah(int i)           { MethodIndex = 1; }
            public void Nah(ref int d)       { MethodIndex = 2; }
            public void Nah<T>(ref int d)    { MethodIndex = 3; }
            public void Nah(ref object d)    { MethodIndex = 4; }
        }

        public class Bar : Foo
        {
            public void Nah(double d)        { MethodIndex = 5; }
            public void Nah(ref double d)    { MethodIndex = 6; }
            public void Nah<T>(ref double d) { MethodIndex = 7; }
            public void Nah<T>(ref T d)      { MethodIndex = 8; }
        }

        [Test]
        public void FindMethodOrThrow_in_the_class_hierarchy()
        {
            var bar = new Bar();

            var ex = Assert.Throws<InvalidOperationException>(() =>
              Lambda<Action>(Call(Constant(bar), "Nah", new Type[] { typeof(int) }, Constant(5))));
            StringAssert.StartsWith("More than one", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() =>
              SysExpr.Lambda<Action>(SysExpr.Call(SysExpr.Constant(bar), "Nah", new Type[] { typeof(int) }, SysExpr.Constant(5))));
            StringAssert.StartsWith("More than one", ex.Message);

            var e = Lambda<Action>(Call(Constant(bar), "Nah", new Type[] { typeof(string) }, Constant("x")));
            e.CompileFast(true)();
            Assert.AreEqual(8, bar.MethodIndex);

            var es = SysExpr.Lambda<Action>(SysExpr.Call(SysExpr.Constant(bar), "Nah", new Type[] { typeof(string) }, SysExpr.Constant("y")));
            es.Compile()();
            Assert.AreEqual(8, bar.MethodIndex);

            ex = Assert.Throws<InvalidOperationException>(() =>
              Lambda<Action>(Call(Constant(bar), "Nah", new Type[] { typeof(double) }, Constant((double)42.3))));
            StringAssert.StartsWith("More than one", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() =>
              SysExpr.Lambda<Action>(SysExpr.Call(SysExpr.Constant(bar), "Nah", new Type[] { typeof(double) }, SysExpr.Constant((double)42.3))));
            StringAssert.StartsWith("More than one", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() =>
              Lambda<Action>(Call(Constant(bar), "Nah", Type.EmptyTypes, Constant((double)42.3))));
            StringAssert.StartsWith("More than one", ex.Message);

            ex = Assert.Throws<InvalidOperationException>(() =>
              SysExpr.Lambda<Action>(SysExpr.Call(SysExpr.Constant(bar), "Nah", Type.EmptyTypes, SysExpr.Constant((double)42.3))));
            StringAssert.StartsWith("More than one", ex.Message);

            e = Lambda<Action>(Call(Constant(bar), "Nah", Type.EmptyTypes, Constant(null)));
            e.CompileFast(true)();
            Assert.AreEqual(4, bar.MethodIndex);

            es = SysExpr.Lambda<Action>(SysExpr.Call(SysExpr.Constant(bar), "Nah", Type.EmptyTypes, SysExpr.Constant(null)));
            es.Compile()();
            Assert.AreEqual(4, bar.MethodIndex);
        }
#endif
    }
}