using System;
using System.Linq;
using System.Reflection;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{

    public class AssignTests : ITest
    {
        public int Run()
        {
            Array_multi_dimensional_index_assign_value_type_block();
            Can_assign_to_parameter();
            Can_assign_to_parameter_in_nested_lambda();
            Parameter_test_try_catch_finally_result();
            Local_Variable_test_try_catch_finally_result();
            Member_test_prop();
            Member_test_field();
            Member_test_block_result_should_detect_non_block_variable();
            Member_test_block_result();
            Member_test_try_catch_finally_result();
            Array_index_assign_body_less();
            Array_index_assign_ref_type_body_less();
            Array_index_assign_value_type_block();
            Array_index_assign_ref_type_block();
            Array_multi_dimensional_index_assign_ref_type_block();
            Array_index_assign_custom_indexer();
            Array_index_assign_custom_indexer_with_get();
            return 17;
        }


        public void Can_assign_to_parameter()
        {
            var sParamExpr = Parameter(typeof(string), "s");
            var expr = Lambda<Func<string, string>>(
                Assign(sParamExpr, Constant("aaa")),
                sParamExpr);

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual("aaa", f("ignored"));
        }


        public void Can_assign_to_parameter_in_nested_lambda()
        {
            // s => () => s = "aaa" 
            var sParamExpr = Parameter(typeof(string), "s");
            var e = Lambda<Func<string, Func<string>>>(
                Lambda<Func<string>>(
                    Assign(sParamExpr, Constant("aaa"))),
                sParamExpr);

            e.PrintCSharp();
            var @cs = (Func<string, Func<string>>)((string s) =>
                (Func<string>)(() =>
                        s = "aaa"));
            Asserts.AreEqual("aaa", @cs("ignored").Invoke());

            var fs = e.CompileSys();
            Asserts.AreEqual("aaa", fs("ignored").Invoke());

            var f = e.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual("aaa", f("ignored").Invoke());
        }


        public void Parameter_test_try_catch_finally_result()
        {
            var tryCatchParameter = Variable(typeof(TryCatchTest));

            var e = Lambda<Func<TryCatchTest, TryCatchTest>>(
                Block(
                    Assign(
                        tryCatchParameter,
                        TryCatch(
                            New(tryCatchParameter.Type.GetConstructor(Type.EmptyTypes)),
                            Catch(typeof(Exception), Default(tryCatchParameter.Type)))),
                    tryCatchParameter
                ),
                tryCatchParameter);
            e.PrintCSharp();
            var @cs = (Func<TryCatchTest, TryCatchTest>)((TryCatchTest assigntests_trycatchtest__54708252) =>
            {
                assigntests_trycatchtest__54708252 = ((Func<TryCatchTest>)(() =>
                {
                    try
                    {
                        return new TryCatchTest();
                    }
                    catch (Exception)
                    {
                        return (TryCatchTest)null;
                    }
                }))();
                return assigntests_trycatchtest__54708252;
            });

            var func = e.CompileFast(true);

            Asserts.IsNotNull(func);

            var input = new TryCatchTest();
            var tryCatchResult = func(input);
            Asserts.AreNotSame(input, tryCatchResult);
            Asserts.IsNotNull(tryCatchResult);
        }


        public void Local_Variable_test_try_catch_finally_result()
        {
            var tryCatchVar = Variable(typeof(TryCatchTest));

            var assignExpr = Lambda<Func<TryCatchTest>>(
                Block(
                    new[] { tryCatchVar },
                    Assign(
                        tryCatchVar,
                        TryCatch(
                            New(tryCatchVar.Type.GetConstructor(Type.EmptyTypes)),
                            Catch(typeof(Exception), Default(tryCatchVar.Type)))),
                    tryCatchVar
                ));

            var func = assignExpr.CompileFast(true);

            Asserts.IsNotNull(func);

            var tryCatch = func();
            Asserts.IsNotNull(tryCatch);
        }


        public void Member_test_prop()
        {
            var a = new Test();
            var expr = Lambda<Func<int>>(
               Assign(Property(Constant(a), "Prop"), Constant(5)));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(5, f());
            Asserts.AreEqual(5, a.Prop);
        }


        public void Member_test_field()
        {
            var a = new Test();
            var expr = Lambda<Func<int>>(
                Assign(Field(Constant(a), "Field"),
                Constant(5)));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(5, f());
            Asserts.AreEqual(5, a.Field);
        }


        public void Member_test_block_result_should_detect_non_block_variable()
        {
            var testVar = Variable(typeof(Test));
            var intVar = Variable(typeof(int));

            var assignExpr = Lambda<Func<Test>>(
                Block(
                    Assign(testVar, New(testVar.Type.GetConstructor(Type.EmptyTypes))),
                    Assign(
                        Property(testVar, nameof(Test.Prop)),
                        Block(
                            new[] { intVar },
                            Assign(intVar, Constant(0)),
                            PreIncrementAssign(intVar),
                            PreIncrementAssign(intVar),
                            intVar)),
                    testVar));

            // InvalidOperationException:
            // 'variable '' of type 'FastExpressionCompiler.LightExpression.UnitTests.AssignTests+Test' referenced from scope '', but it is not defined'
            Asserts.Throws<InvalidOperationException>(() => assignExpr.CompileSys());

            var func = assignExpr.CompileFast(true);
            Asserts.IsNull(func);
        }


        public void Member_test_block_result()
        {
            var testVar = Variable(typeof(Test));
            var intVar = Variable(typeof(int));

            var assignExpr = Lambda<Func<Test>>(
                Block(new[] { testVar },
                    Assign(testVar, New(testVar.Type.GetConstructor(Type.EmptyTypes))),
                    Assign(
                        Property(testVar, nameof(Test.Prop)),
                        Block(new[] { intVar },
                            Assign(intVar, Constant(0)),
                            PreIncrementAssign(intVar),
                            PreIncrementAssign(intVar),
                            intVar)),
                    testVar));

            assignExpr.CompileSys();

            var func = assignExpr.CompileFast(true);

            Asserts.IsNotNull(func);
            var test = func();
            Asserts.IsNotNull(test);
            Asserts.AreEqual(2, test.Prop);
        }


        public void Member_test_try_catch_finally_result()
        {
            var tryCatchVar = Variable(typeof(TryCatchTest));
            var tryCatchNestedVar = Variable(typeof(TryCatchNestedTest));

            var assignExpr = Lambda<Func<TryCatchTest>>(
                TryCatch(
                    Block(
                        new[] { tryCatchVar },
                        Assign(tryCatchVar, New(tryCatchVar.Type.GetConstructor(Type.EmptyTypes))),
                        Assign(
                            Property(tryCatchVar, nameof(TryCatchTest.NestedTest)),
                            TryCatch(
                                Block(
                                    new[] { tryCatchNestedVar },
                                    Assign(tryCatchNestedVar, New(tryCatchNestedVar.Type.GetConstructor(Type.EmptyTypes))),
                                    Assign(Property(tryCatchNestedVar, nameof(TryCatchNestedTest.Nested)), Constant("Value")),
                                    tryCatchNestedVar),
                                Catch(typeof(Exception), Default(tryCatchNestedVar.Type)))),
                        tryCatchVar
                    ),
                    Catch(typeof(Exception), Default(tryCatchVar.Type))));

            assignExpr.PrintCSharp();
            // var @cs = (Func<TryCatchTest>)(() =>
            // {
            // try
            // {
            //     TryCatchTest assigntests_trycatchtest__58225482 = default;
            //     assigntests_trycatchtest__58225482 = new TryCatchTest();
            //     assigntests_trycatchtest__58225482.NestedTest = try // todo: @imrpove @printer, is not a valid C#
            //     {
            //         TryCatchNestedTest assigntests_trycatchnestedtest__54267293 = default;
            //         assigntests_trycatchnestedtest__54267293 = new TryCatchNestedTest();
            //         assigntests_trycatchnestedtest__54267293.Nested = "Value";
            //         return assigntests_trycatchnestedtest__54267293;
            //     }
            //     catch (Exception)
            //     {
            //         return default(TryCatchNestedTest);
            //     };
            //     return assigntests_trycatchtest__58225482;
            // }
            // catch (Exception)
            // {
            //     return default(TryCatchTest);
            // }
            // });

            var fs = assignExpr.CompileSys();
            fs.PrintIL();
            var tryCatch = fs();
            Asserts.IsNotNull(tryCatch);
            Asserts.IsNotNull(tryCatch.NestedTest);
            Asserts.AreEqual("Value", tryCatch.NestedTest.Nested);

            var ff = assignExpr.CompileFast(true);
            ff.PrintIL();
            Asserts.IsNotNull(ff);
            tryCatch = ff();
            Asserts.IsNotNull(tryCatch);
            Asserts.IsNotNull(tryCatch.NestedTest);
            Asserts.AreEqual("Value", tryCatch.NestedTest.Nested);
        }
        public class Test
        {
            public int Prop { get; set; }
            public int Field;
        }


        public void Array_index_assign_body_less()
        {
            var expr = Lambda<Func<int>>(
                Assign(ArrayAccess(NewArrayInit(typeof(int), Constant(0), Constant(0)), Constant(1)),
                    Constant(5)));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(5, f());
        }


        public void Array_index_assign_ref_type_body_less()
        {
            var a = new object();
            var expr = Lambda<Func<object>>(
                Assign(ArrayAccess(NewArrayInit(typeof(object), Constant(null), Constant(null)), Constant(1)),
                    Constant(a)));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(a, f());
        }


        public void Array_index_assign_value_type_block()
        {
            var variable = Variable(typeof(int[]));
            var arr = NewArrayInit(typeof(int), Constant(0), Constant(0));
            var expr = Lambda<Func<int>>(
                Block(new[] { variable },
                    Assign(variable, arr),
                    Assign(ArrayAccess(variable, Constant(1)), Constant(5)),
                    ArrayIndex(variable, Constant(1))));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(5, f());
        }


        public void Array_index_assign_ref_type_block()
        {
            var a = new object();
            var variable = Variable(typeof(object[]));
            var arr = NewArrayInit(typeof(object), Constant(null), Constant(null));
            var expr = Lambda<Func<object>>(
                Block(new[] { variable },
                    Assign(variable, arr),
                    Assign(ArrayAccess(variable, Constant(1)), Constant(a)),
                    ArrayIndex(variable, Constant(1))));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(a, f());
        }


        public void Array_multi_dimensional_index_assign_value_type_block()
        {
            var variable = Variable(typeof(int[,]));
            var arr = NewArrayBounds(typeof(int), Constant(2), Constant(1)); // new int[2,1]
            var expr = Lambda<Func<int>>(
                Block(new[] { variable },
                    Assign(variable, arr),
                    Assign(ArrayAccess(variable, Constant(1), Constant(0)), Constant(5)), // a[1,0] = 5
                    ArrayAccess(variable, Constant(1), Constant(0)))); // ret a[1,0]

            expr.PrintCSharp();

            var fs = expr.CompileFast(true);

#if LIGHT_EXPRESSION
            var sysExpr = expr.ToLambdaExpression();
            var restoredExpr = sysExpr.ToLightExpression();
            restoredExpr.PrintCSharp();
            // todo: @wip #431 generates different names for the unnamed variables which is not comparable
            Asserts.AreEqual(expr.ToCSharpString(), restoredExpr.ToCSharpString());
#endif

            Asserts.IsNotNull(fs);
            Asserts.AreEqual(5, fs());
        }


        public void Array_multi_dimensional_index_assign_ref_type_block()
        {
            var a = new object();
            var variable = Variable(typeof(object[,]));
            var arr = NewArrayBounds(typeof(object), Constant(2), Constant(1)); // new object[2,1]
            var expr = Lambda<Func<object>>(
                Block(new[] { variable },
                    Assign(variable, arr),
                    Assign(ArrayAccess(variable, Constant(1), Constant(0)), Constant(a)), // o[1,0] = a
                    ArrayAccess(variable, Constant(1), Constant(0)))); // ret o[1,0]

            var f = expr.CompileFast(true);
            f.PrintIL();

            Asserts.IsNotNull(f);
            Asserts.AreEqual(a, f());
        }


        public void Array_index_assign_custom_indexer()
        {
            var a = new IndexTest();
            var variable = Variable(typeof(IndexTest));
            var prop = typeof(IndexTest).GetTypeInfo().DeclaredProperties.First(p => p.GetIndexParameters().Length > 0);
            var expr = Lambda<Func<int>>(
                Block(new[] { variable },
                    Assign(variable, Constant(a)),
                    Assign(Property(variable, prop, Constant(1)), Constant(5))));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(5, f());
        }


        public void Array_index_assign_custom_indexer_with_get()
        {
            var a = new IndexTest();
            var variable = Variable(typeof(IndexTest));
            var prop = typeof(IndexTest).GetTypeInfo().DeclaredProperties.First(p => p.GetIndexParameters().Length > 0);
            var expr = Lambda<Func<int>>(
                Block(new[] { variable },
                    Assign(variable, Constant(a)),
                    Assign(Property(variable, prop, Constant(1)), Constant(5)),
                    Property(variable, prop, Constant(1))));

            var f = expr.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual(5, f());
        }

        public class IndexTest
        {
            private readonly int[] a = { 0, 0 };

            public int this[int i]
            {
                get => a[i];
                set => a[i] = value;
            }
        }

        public class TryCatchTest
        {
            public TryCatchNestedTest NestedTest { get; set; }
        }

        public class TryCatchNestedTest
        {
            public string Nested { get; set; }
        }
    }
}
