﻿using System;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using ExpressionType = System.Linq.Expressions.ExpressionType;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class BinaryExpressionTests
    {
        [Test]
        public void Add_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Add(param, Constant(2)),
                param);

            int result = expression.CompileFast()(1);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void AddAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AddAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast()(1);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void AddAssignChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AddAssignChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast()(1);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void AddChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AddChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast()(1);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void And_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                And(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void AndAlso_compiles()
        {
            var param = Parameter(typeof(bool), "b");
            var expression = Lambda<Func<bool, bool>>(
                AndAlso(param, Constant(false)),
                param);

            bool result = expression.CompileFast()(true);

            Assert.IsFalse(result);
        }

        [Test]
        public void AndAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AndAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void ArrayIndex_compiles()
        {
            var param = Parameter(typeof(string[]), "s");
            var expression = Lambda<Func<string[], string>>(
                ArrayIndex(param, Constant(1)),
                param);

            string result = expression.CompileFast()(new[] { "1", "2" });

            Assert.AreEqual("2", result);
        }

        [Test]
        public void Assign_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                Assign(param, Constant("test")),
                param);

            string result = expression.CompileFast()("original");

            Assert.AreEqual("test", result);
        }

        [Test]
        public void Coalesce_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                Coalesce(param, Constant("<null>")),
                param);

            string result = expression.CompileFast()(null);

            Assert.AreEqual("<null>", result);
        }

        [Test]
        public void Divide_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Divide(param, Constant(2)),
                param);

            int result = expression.CompileFast()(6);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void DivideAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                DivideAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast()(6);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void Equal_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                Equal(param, Constant(1)),
                param);

            bool result = expression.CompileFast()(1);

            Assert.IsTrue(result);
        }

        [Test]
        public void ExclusiveOr_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                ExclusiveOr(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(6, result);
        }

        [Test]
        public void ExclusiveOrAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                ExclusiveOrAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(6, result);
        }

        [Test]
        public void GreaterThan_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                GreaterThan(param, Constant(2)),
                param);

            bool result = expression.CompileFast()(3);

            Assert.IsTrue(result);
        }

        [Test]
        public void GreaterThanOrEqual_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                GreaterThanOrEqual(param, Constant(2)),
                param);

            bool result = expression.CompileFast()(2);

            Assert.IsTrue(result);
        }

        [Test]
        public void LeftShift_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                LeftShift(param, Constant(2)),
                param);

            int result = expression.CompileFast()(3);

            Assert.AreEqual(12, result);
        }

        [Test]
        public void LeftShiftAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                LeftShiftAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast()(3);

            Assert.AreEqual(12, result);
        }

        [Test]
        public void LessThan_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                LessThan(param, Constant(2)),
                param);

            bool result = expression.CompileFast()(1);

            Assert.IsTrue(result);
        }

        [Test]
        public void LessThanOrEqual_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                LessThanOrEqual(param, Constant(2)),
                param);

            bool result = expression.CompileFast()(2);

            Assert.IsTrue(result);
        }

        [Test]
        public void MakeBinary_Add_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MakeBinary(ExpressionType.Add, param, Constant(2)),
                param);

            int result = expression.CompileFast()(1);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void MakeBinary_ArrayIndex_compiles()
        {
            var param = Parameter(typeof(string[]), "s");
            var expression = Lambda<Func<string[], string>>(
                MakeBinary(ExpressionType.ArrayIndex, param, Constant(1)),
                param);

            string result = expression.CompileFast()(new[] { "1", "2" });

            Assert.AreEqual("2", result);
        }

        [Test]
        public void MakeBinary_Assign_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                MakeBinary(ExpressionType.Assign, param, Constant("test")),
                param);

            string result = expression.CompileFast()("original");

            Assert.AreEqual("test", result);
        }

        [Test]
        public void MakeBinary_Coalesce_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                MakeBinary(ExpressionType.Coalesce, param, Constant("<null>")),
                param);

            string result = expression.CompileFast()(null);

            Assert.AreEqual("<null>", result);
        }

        [Test]
        public void Modulo_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Modulo(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(2, result);
        }

        [Test]
        public void ModuloAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                ModuloAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(2, result);
        }

        [Test]
        public void Multiply_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Multiply(param, Constant(3)),
                param);

            int result = expression.CompileFast()(2);

            Assert.AreEqual(6, result);
        }

        [Test]
        public void MultiplyAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MultiplyAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast()(2);

            Assert.AreEqual(6, result);
        }

        [Test]
        public void MultiplyAssignChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MultiplyAssignChecked(param, Constant(3)),
                param);

            int result = expression.CompileFast()(2);

            Assert.AreEqual(6, result);
        }

        [Test]
        public void MultiplyChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MultiplyChecked(param, Constant(3)),
                param);

            int result = expression.CompileFast()(2);

            Assert.AreEqual(6, result);
        }

        [Test]
        public void NotEqual_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                NotEqual(param, Constant(1)),
                param);

            bool result = expression.CompileFast()(1);

            Assert.IsFalse(result);
        }

        [Test]
        public void Or_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Or(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(7, result);
        }

        [Test]
        public void OrAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                OrAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast()(5);

            Assert.AreEqual(7, result);
        }

        [Test]
        public void OrElse_compiles()
        {
            var param = Parameter(typeof(bool), "b");
            var expression = Lambda<Func<bool, bool>>(
                OrElse(param, Constant(true)),
                param);

            bool result = expression.CompileFast()(false);

            Assert.IsTrue(result);
        }

        [Test]
        public void Power_compiles()
        {
            var param = Parameter(typeof(double), "d");
            var expression = Lambda<Func<double, double>>(
                Power(param, Constant(2.0)),
                param);

            double result = expression.CompileFast()(3.0);

            Assert.AreEqual(9.0, result);
        }

        [Test]
        public void PowerAssign_compiles()
        {
            var param = Parameter(typeof(double), "d");
            var expression = Lambda<Func<double, double>>(
                PowerAssign(param, Constant(2.0)),
                param);

            double result = expression.CompileFast()(3.0);

            Assert.AreEqual(9.0, result);
        }

        [Test]
        public void ReferenceEqual_compiles()
        {
            const string Value = "test";
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Func<object, bool>>(
                ReferenceEqual(param, Constant(Value)),
                param);

            bool result = expression.CompileFast()(Value);

            Assert.IsTrue(result);
        }

        [Test]
        public void ReferenceNotEqual_compiles()
        {
            const string Value = "test";
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Func<object, bool>>(
                ReferenceNotEqual(param, Constant(Value)),
                param);

            bool result = expression.CompileFast()(Value);

            Assert.IsFalse(result);
        }

        [Test]
        public void RightShift_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                RightShift(param, Constant(2)),
                param);

            int result = expression.CompileFast()(12);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void RightShiftAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                RightShiftAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast()(12);

            Assert.AreEqual(3, result);
        }

        [Test]
        public void Subtract_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Subtract(param, Constant(2)),
                param);

            int result = expression.CompileFast()(3);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void SubtractAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                SubtractAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast()(3);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void SubtractAssignChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                SubtractAssignChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast()(3);

            Assert.AreEqual(1, result);
        }

        [Test]
        public void SubtractChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                SubtractChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast()(3);

            Assert.AreEqual(1, result);
        }
    }
}
