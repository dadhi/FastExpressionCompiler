using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class AssignTests : ITest
    {
        public int Run()
        {
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
            Array_multi_dimensional_index_assign_value_type_block();
            Array_multi_dimensional_index_assign_ref_type_block();
            Array_index_assign_custom_indexer();
            Array_index_assign_custom_indexer_with_get();
            return 17;
        }

        [Test]
        public void Can_assign_to_parameter()
        {
            var sParamExpr = Parameter(typeof(string), "s");
            var expr = Lambda<Func<string, string>>(
                Assign(sParamExpr, Constant("aaa")),
                sParamExpr);

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual("aaa", f("ignored"));
        }

        [Test]
        public void Can_assign_to_parameter_in_nested_lambda()
        {
            // s => () => s = "aaa" 
            var sParamExpr = Parameter(typeof(string), "s");
            var expr = Lambda<Func<string, Func<string>>>(
                Lambda<Func<string>>(
                    Assign(sParamExpr, Constant("aaa"))),
                sParamExpr);

            var fs = expr.CompileSys();
            Assert.AreEqual("aaa", fs("ignored").Invoke());

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual("aaa", f("ignored").Invoke());
        }

        [Test]
        public void Parameter_test_try_catch_finally_result()
        {
            var tryCatchParameter = Variable(typeof(TryCatchTest));

            var assignExpr = Lambda<Func<TryCatchTest, TryCatchTest>>(
                Block(
                    Assign(
                        tryCatchParameter,
                        TryCatch(
                            New(tryCatchParameter.Type.GetConstructor(Type.EmptyTypes)),
                            Catch(typeof(Exception), Default(tryCatchParameter.Type)))),
                    tryCatchParameter
                ),
                tryCatchParameter);

            var func = assignExpr.CompileFast(true);

            Assert.IsNotNull(func);

            var input = new TryCatchTest();
            var tryCatchResult = func(input);
            Assert.AreNotSame(input, tryCatchResult);
            Assert.IsNotNull(tryCatchResult);
        }

        [Test]
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

            Assert.IsNotNull(func);

            var tryCatch = func();
            Assert.IsNotNull(tryCatch);
        }

        [Test]
        public void Member_test_prop()
        {
            var a = new Test();
            var expr = Lambda<Func<int>>(
               Assign(Property(Constant(a), "Prop"), Constant(5)));

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
            Assert.AreEqual(5, a.Prop);
        }

        [Test]
        public void Member_test_field()
        {
            var a = new Test();
            var expr = Lambda<Func<int>>(
                Assign(Field(Constant(a), "Field"),
                Constant(5)));

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
            Assert.AreEqual(5, a.Field);
        }

        [Test]
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
            Assert.Throws<InvalidOperationException>(() => assignExpr.CompileSys());

            var func = assignExpr.CompileFast(true);
            Assert.IsNull(func);
        }

        [Test]
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

            Assert.IsNotNull(func);
            var test = func();
            Assert.IsNotNull(test);
            Assert.AreEqual(2, test.Prop);
        }

        [Test]
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

            var func = assignExpr.CompileFast(true);

            Assert.IsNotNull(func);

            var tryCatch = func();
            Assert.IsNotNull(tryCatch);
            Assert.IsNotNull(tryCatch.NestedTest);
            Assert.AreEqual("Value", tryCatch.NestedTest.Nested);
        }
        public class Test
        {
            public int Prop { get; set; }
            public int Field;
        }

        [Test]
        public void Array_index_assign_body_less()
        {
            var expr = Lambda<Func<int>>(
                Assign(ArrayAccess(NewArrayInit(typeof(int), Constant(0), Constant(0)), Constant(1)),
                    Constant(5)));

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
        }

        [Test]
        public void Array_index_assign_ref_type_body_less()
        {
            var a = new object();
            var expr = Lambda<Func<object>>(
                Assign(ArrayAccess(NewArrayInit(typeof(object), Constant(null), Constant(null)), Constant(1)),
                    Constant(a)));

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual(a, f());
        }

        [Test]
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

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
        }

        [Test]
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

            Assert.IsNotNull(f);
            Assert.AreEqual(a, f());
        }

        [Test]
        public void Array_multi_dimensional_index_assign_value_type_block()
        {
            var variable = Variable(typeof(int[,]));
            var arr = NewArrayBounds(typeof(int), Constant(2), Constant(1)); // new int[2,1]
            var expr = Lambda<Func<int>>(
                Block(new[] { variable },
                    Assign(variable, arr),
                    Assign(ArrayAccess(variable, Constant(1), Constant(0)), Constant(5)), // a[1,0] = 5
                    ArrayAccess(variable, Constant(1), Constant(0)))); // ret a[1,0]

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
        }

        [Test]
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

            Assert.IsNotNull(f);
            Assert.AreEqual(a, f());
        }

        [Test]
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

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
        }

        [Test]
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

            Assert.IsNotNull(f);
            Assert.AreEqual(5, f());
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
