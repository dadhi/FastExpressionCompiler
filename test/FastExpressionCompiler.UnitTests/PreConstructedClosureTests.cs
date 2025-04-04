﻿using System;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{

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

            Can_prevent_closure_creation_when_compiling_a_static_delegate();
            Can_pass_closure_to_hoisted_expr_with_nested_lambda();
            Can_use_primitive_types_in_hoisted_lambda_closure();

            return 11;
        }


        public void Can_pass_closure_with_constant_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(xConstExpr);

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Asserts.IsNotNull(f);

            var result = f();
            Asserts.AreSame(x, result);
        }


        public void Can_pass_ANY_class_closure_with_constant_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(xConstExpr);

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Asserts.IsNotNull(f);

            var result = f();
            Asserts.AreSame(x, result);
        }


        public void Can_pass_closure_with_block_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(Block(xConstExpr));

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Asserts.IsNotNull(f);

            var result = f();
            Asserts.AreSame(x, result);
        }


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
            Asserts.IsNotNull(f);

            f();
            Asserts.AreEqual(2, intDoubler.DoubleValue);
        }


        public void Can_pass_closure_with_try_catch_to_TryCompile()
        {
            var x = new X();
            var xConstExpr = Constant(x);
            var expr = Lambda<Func<X>>(TryCatch(
                xConstExpr,
                Catch(typeof(Exception), Default(xConstExpr.Type))));

            var f = expr.TryCompileWithPreCreatedClosure<Func<X>>(xConstExpr);
            Asserts.IsNotNull(f);

            var result = f();
            Asserts.AreSame(x, result);
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


        public void Can_use_block_when_compiling_a_static_delegate()
        {
            var expr = Lambda<Func<X>>(Block(New(typeof(X).GetConstructor(Type.EmptyTypes))));

            var f = expr.TryCompileWithoutClosure<Func<X>>();
            Asserts.IsNotNull(f);

            var result = f();
            Asserts.IsNotNull(result);
        }


        public void Can_use_variable_block_when_compiling_a_static_delegate()
        {
            var intDoublerVariable = Variable(typeof(IntDoubler));

            var expr = Lambda<Func<IntDoubler>>(Block(
                new[] { intDoublerVariable },
                Assign(intDoublerVariable, New(intDoublerVariable.Type.GetConstructor(Type.EmptyTypes))),
                Call(intDoublerVariable, nameof(IntDoubler.Double), Type.EmptyTypes, Constant(5)),
                intDoublerVariable));

            var f = expr.TryCompileWithoutClosure<Func<IntDoubler>>();
            Asserts.IsNotNull(f);

            var result = f();
            Asserts.IsNotNull(result);
            Asserts.AreEqual(10, result.DoubleValue);
        }


        public void Can_prevent_closure_creation_when_compiling_a_static_delegate()
        {
            System.Linq.Expressions.Expression<Func<X>> sExpr = () => new X();
            var expr = sExpr.FromSysExpression();

            var f = expr.TryCompileWithoutClosure<Func<X>>();
            Asserts.IsNotNull(f);

            var result = f();
            Asserts.IsNotNull(result);
        }


        public void Can_pass_closure_to_hoisted_expr_with_nested_lambda()
        {
            var x = new X();
            System.Linq.Expressions.Expression<Func<Y>> sExpr = () => new Y(x, () => x);
            var expr = sExpr.FromSysExpression();

            var f1 = expr.TryCompile<Func<Y>>();
            Asserts.IsNotNull(f1);

            var y = f1();
            Asserts.IsNotNull(y);
            Asserts.AreSame(y.A, y.B);
        }


        public void Can_use_primitive_types_in_hoisted_lambda_closure()
        {
            var i = 3;
            System.Linq.Expressions.Expression<Func<int>> sExpr = () => i + 1;
            var expr = sExpr.FromSysExpression();

            var fs = expr.CompileSys();
            Asserts.IsNotNull(fs);

            i = 13;
            Asserts.AreEqual(14, fs());
        }


        public void Can_Not_use_primitive_types_in_manual_lambda_closure()
        {
            var i = 3;
            var expr = Lambda<Func<int>>(Increment(Constant(i)));

            var fs = expr.CompileSys();
            Asserts.IsNotNull(fs);

            i = 13;
            Asserts.AreEqual(4, fs());
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
