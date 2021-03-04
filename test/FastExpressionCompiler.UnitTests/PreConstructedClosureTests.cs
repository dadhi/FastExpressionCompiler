using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class PreConstructedClosureTests : ITest
    {
        public int Run()
        {
            Can_pass_closure_with_constant_to_TryCompile();
            Can_pass_ANY_class_closure_with_constant_to_TryCompile();
            Can_pass_closure_with_block_to_TryCompile();
            Can_pass_closure_with_variable_block_to_TryCompile();
            Can_pass_closure_with_try_catch_to_TryCompile();
            Can_use_block_when_compiling_a_static_delegate();
            Can_use_variable_block_when_compiling_a_static_delegate();
            Can_Not_use_primitive_types_in_manual_lambda_closure();

#if !LIGHT_EXPRESSION

            Can_prevent_closure_creation_when_compiling_a_static_delegate();
            Can_pass_closure_to_hoisted_expr_with_nested_lambda();
            Can_use_primitive_types_in_hoisted_lambda_closure();
            return 11;
#else            
            return 8;
#endif
        }

        [Test]
        public void Can_pass_closure_with_constant_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(xConstExpr);

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Assert.IsNotNull(f);

            var result = f();
            Assert.AreSame(x, result);
        }

        [Test]
        public void Can_pass_ANY_class_closure_with_constant_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(xConstExpr);

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Assert.IsNotNull(f);

            var result = f();
            Assert.AreSame(x, result);
        }

        [Test]
        public void Can_pass_closure_with_block_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(Block(xConstExpr));

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Assert.IsNotNull(f);

            var result = f();
            Assert.AreSame(x, result);
        }

        [Test]
        public void Can_pass_closure_with_variable_block_to_TryCompile()
        {
            var intVariable = Variable(typeof(int));
            var intDoubler = new IntDoubler();
            var intDoublerConstExpr = Constant(intDoubler);

            var expr = Lambda<Action>(Block(
                new[] { intVariable },
                Assign(intVariable, Constant(1)),
                Call(intDoublerConstExpr, nameof(IntDoubler.Double), Type.EmptyTypes, intVariable)));

            var f = expr.TryCompileWithPreCreatedClosure<Action>(intDoublerConstExpr);
            Assert.IsNotNull(f);

            f();
            Assert.AreEqual(2, intDoubler.DoubleValue);
        }

        [Test]
        public void Can_pass_closure_with_try_catch_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(TryCatch(
                xConstExpr,
                Catch(typeof(Exception), Default(xConstExpr.Type))));

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Assert.IsNotNull(f);

            var result = f();
            Assert.AreSame(x, result);
        }

        public class X { }

        public class ClosureX
        {
            public readonly X X;
            public ClosureX(X x) { X = x; }
        }

        public class IntDoubler
        {
            public int DoubleValue { get; set; }
            public void Double(int value) => DoubleValue = value * 2;
        }

        public class ClosureIntHolder
        {
            public readonly IntDoubler Value;
            public ClosureIntHolder(IntDoubler value) { Value = value; }
        }

        [Test]
        public void Can_use_block_when_compiling_a_static_delegate()
        {
            var expr = Lambda<Func<X>>(Block(New(typeof(X).GetConstructor(Type.EmptyTypes))));

            var f = expr.TryCompileWithoutClosure<Func<X>>();
            Assert.IsNotNull(f);

            var result = f();
            Assert.IsNotNull(result);
        }

        [Test]
        public void Can_use_variable_block_when_compiling_a_static_delegate()
        {
            var intDoublerVariable = Variable(typeof(IntDoubler));

            var expr = Lambda<Func<IntDoubler>>(Block(
                new[] { intDoublerVariable },
                Assign(intDoublerVariable, New(intDoublerVariable.Type.GetConstructor(Type.EmptyTypes))),
                Call(intDoublerVariable, nameof(IntDoubler.Double), Type.EmptyTypes, Constant(5)),
                intDoublerVariable));

            var f = expr.TryCompileWithoutClosure<Func<IntDoubler>>();
            Assert.IsNotNull(f);

            var result = f();
            Assert.IsNotNull(result);
            Assert.AreEqual(10, result.DoubleValue);
        }

#if !LIGHT_EXPRESSION
        [Test]
        public void Can_prevent_closure_creation_when_compiling_a_static_delegate()
        {
            Expression<Func<X>> expr = () => new X();

            var f = expr.TryCompileWithoutClosure<Func<X>>();
            Assert.IsNotNull(f);

            var result = f();
            Assert.IsNotNull(result);
        }

        [Test]
        public void Can_pass_closure_to_hoisted_expr_with_nested_lambda()
        {
            var x = new X();
            Expression<Func<Y>> expr = () => new Y(x, () => x);

            var f1 = expr.TryCompile<Func<Y>>();
            Assert.IsNotNull(f1);

            var y = f1();
            Assert.IsNotNull(y);
            Assert.AreSame(y.A, y.B);
        }

        [Test]
        public void Can_use_primitive_types_in_hoisted_lambda_closure()
        {
            var i = 3;
            Expression<Func<int>> expr = () => i + 1;

            var fs = expr.CompileSys();
            Assert.IsNotNull(fs);

            i = 13;
            Assert.AreEqual(14, fs());
        }
#endif

        [Test]
        public void Can_Not_use_primitive_types_in_manual_lambda_closure()
        {
            var i = 3;
            var expr = Lambda<Func<int>>(Increment(Constant(i)));

            var fs = expr.CompileSys();
            Assert.IsNotNull(fs);

            i = 13;
            Assert.AreEqual(4, fs());
        }


        public class Y
        {
            public readonly X A;
            public readonly X B;

            public Y(X a, X b)
            {
                A = a;
                B = b;
            }

            public Y(X a, Func<X> fb)
            {
                A = a;
                B = fb();
            }
        }
    }
}
