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
    public class AssignTests
    {
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

            var f = expr.CompileFast(true);

            Assert.IsNotNull(f);
            Assert.AreEqual("aaa", f("ignored")());
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
    }
}
