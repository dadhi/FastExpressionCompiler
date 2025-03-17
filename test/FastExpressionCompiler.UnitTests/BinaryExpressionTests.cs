using System;
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
    public class BinaryExpressionTests : ITest
    {
        public int Run()
        {
            Issue399_Coalesce_for_nullable_long_and_non_nullable_long();
            Issue399_Coalesce_for_nullable_long_Automapper_test_Should_substitute_zero_for_null();
            Add_compiles();
            AddAssign_compiles();
            AddAssignChecked_compiles();
            AddChecked_compiles();
            And_compiles();
            AndAlso_compiles();
            AndAssign_compiles();
            ArrayIndex_compiles();
            Assign_compiles();
            Coalesce_compiles();
            Divide_compiles();
            DivideAssign_compiles();
            Equal_compiles();
            ExclusiveOr_compiles();
            ExclusiveOrAssign_compiles();
            GreaterThan_compiles();
            GreaterThanOrEqual_compiles();
            LeftShift_compiles();
            LeftShiftAssign_compiles();
            LessThan_compiles();
            LessThanOrEqual_compiles();
            MakeBinary_Add_compiles();
            MakeBinary_ArrayIndex_compiles();
            MakeBinary_Assign_compiles();
            MakeBinary_Coalesce_compiles();
            Modulo_compiles();
            ModuloAssign_compiles();
            Multiply_compiles();
            MultiplyAssign_compiles();
            MultiplyAssignChecked_compiles();
            MultiplyChecked_compiles();
            NotEqual_compiles();
            Or_compiles();
            OrAssign_compiles();
            OrElse_compiles();
            Power_compiles();
            PowerAssign_compiles();
            ReferenceEqual_compiles();
            ReferenceEqual_in_Action_compiles();
            ReferenceNotEqual_compiles();
            RightShift_compiles();
            RightShiftAssign_compiles();
            Subtract_compiles();
            SubtractAssign_compiles();
            SubtractAssignChecked_compiles();
            SubtractChecked_compiles();
            return 48;
        }

        [Test]
        public void Add_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Add(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(1);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void AddAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AddAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(1);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void AddAssignChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AddAssignChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(1);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void AddChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AddChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(1);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void And_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                And(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(1, result);
        }

        [Test]
        public void AndAlso_compiles()
        {
            var param = Parameter(typeof(bool), "b");
            var expression = Lambda<Func<bool, bool>>(
                AndAlso(param, Constant(false)),
                param);

            bool result = expression.CompileFast(true)(true);

            Asserts.IsFalse(result);
        }

        [Test]
        public void AndAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                AndAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(1, result);
        }

        [Test]
        public void ArrayIndex_compiles()
        {
            var param = Parameter(typeof(string[]), "s");
            var expression = Lambda<Func<string[], string>>(
                ArrayIndex(param, Constant(1)),
                param);

            string result = expression.CompileFast(true)(new[] { "1", "2" });

            Asserts.AreEqual("2", result);
        }

        [Test]
        public void Assign_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                Assign(param, Constant("test")),
                param);

            string result = expression.CompileFast(true)("original");

            Asserts.AreEqual("test", result);
        }

        [Test]
        public void Coalesce_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                Coalesce(param, Constant("<null>")),
                param);

            string result = expression.CompileFast(true)(null);

            Asserts.AreEqual("<null>", result);
        }

        [Test]
        public void Divide_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Divide(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(6);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void DivideAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                DivideAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(6);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void Equal_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                Equal(param, Constant(1)),
                param);

            bool result = expression.CompileFast(true)(1);

            Asserts.IsTrue(result);
        }

        [Test]
        public void ExclusiveOr_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                ExclusiveOr(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(6, result);
        }

        [Test]
        public void ExclusiveOrAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                ExclusiveOrAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(6, result);
        }

        [Test]
        public void GreaterThan_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                GreaterThan(param, Constant(2)),
                param);

            bool result = expression.CompileFast(true)(3);

            Asserts.IsTrue(result);
        }

        [Test]
        public void GreaterThanOrEqual_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                GreaterThanOrEqual(param, Constant(2)),
                param);

            var fs = expression.CompileSys();
            fs.PrintIL();

            var fx = expression.CompileFast(true);
            fx.PrintIL();

            Asserts.IsTrue(fx(2));
        }

        [Test]
        public void LeftShift_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                LeftShift(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(3);

            Asserts.AreEqual(12, result);
        }

        [Test]
        public void LeftShiftAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                LeftShiftAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(3);

            Asserts.AreEqual(12, result);
        }

        [Test]
        public void LessThan_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                LessThan(param, Constant(2)),
                param);

            bool result = expression.CompileFast(true)(1);

            Asserts.IsTrue(result);
        }

        [Test]
        public void LessThanOrEqual_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                LessThanOrEqual(param, Constant(2)),
                param);

            bool result = expression.CompileFast(true)(2);

            Asserts.IsTrue(result);
        }

        [Test]
        public void MakeBinary_Add_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MakeBinary(ExpressionType.Add, param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(1);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void MakeBinary_ArrayIndex_compiles()
        {
            var param = Parameter(typeof(string[]), "s");
            var expression = Lambda<Func<string[], string>>(
                MakeBinary(ExpressionType.ArrayIndex, param, Constant(1)),
                param);

            string result = expression.CompileFast(true)(new[] { "1", "2" });

            Asserts.AreEqual("2", result);
        }

        [Test]
        public void MakeBinary_Assign_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                MakeBinary(ExpressionType.Assign, param, Constant("test")),
                param);

            string result = expression.CompileFast(true)("original");

            Asserts.AreEqual("test", result);
        }

        [Test]
        public void MakeBinary_Coalesce_compiles()
        {
            var param = Parameter(typeof(string), "s");
            var expression = Lambda<Func<string, string>>(
                MakeBinary(ExpressionType.Coalesce, param, Constant("<null>")),
                param);

            string result = expression.CompileFast(true)(null);

            Asserts.AreEqual("<null>", result);
        }

        [Test]
        public void Issue399_Coalesce_for_nullable_long_Automapper_test_Should_substitute_zero_for_null()
        {
            var paramSource = Parameter(typeof(Source), "s");
            var paramDest = Parameter(typeof(Destination), "d");
            var tmpVar = Parameter(typeof(long?), "tmp");
            var e = Lambda<Action<Source, Destination>>(
                Block(new[] { tmpVar },
                    Assign(tmpVar, Coalesce(Property(paramSource, nameof(Source.Number)), Constant(0L, typeof(long?)))),
                    Assign(Property(paramDest, nameof(Destination.Number)), tmpVar)
                ),
                paramSource, paramDest);

            e.PrintCSharp();

            var fs = e.CompileSys();
            fs.PrintIL();
            var d = new Destination();
            fs(new Source(), d);
            Asserts.AreEqual(0, d.Number);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            d = new Destination();
            ff(new Source(), d);
            Asserts.AreEqual(0, d.Number);
        }

        [Test]
        public void Issue399_Coalesce_for_nullable_long_and_non_nullable_long()
        {
            var paramSource = Parameter(typeof(Source), "s");
            var paramDest = Parameter(typeof(Destination), "d");
            var tmpVar = Parameter(typeof(long), "tmp");
            var e = Lambda<Action<Source, Destination>>(
                Block(new[] { tmpVar },
                    Assign(tmpVar, Coalesce(Property(paramSource, nameof(Source.Number)), Constant(0L))),
                    Assign(Property(paramDest, nameof(Destination.NumberNonNullable)), tmpVar)
                ),
                paramSource, paramDest);

            e.PrintCSharp();

            var fs = e.CompileSys();
            fs.PrintIL();
            var d = new Destination();
            fs(new Source(), d);
            Asserts.AreEqual(0, d.NumberNonNullable);

            var ff = e.CompileFast(true);
            ff.PrintIL();
            d = new Destination();
            ff(new Source(), d);
            Asserts.AreEqual(0, d.NumberNonNullable);
        }

        class Source
        {
            public long? Number { get; set; }
        }
        class Destination
        {
            public long? Number { get; set; }
            public long NumberNonNullable { get; set; }
        }

        [Test]
        public void Modulo_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Modulo(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(2, result);
        }

        [Test]
        public void ModuloAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                ModuloAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(2, result);
        }

        [Test]
        public void Multiply_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Multiply(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(2);

            Asserts.AreEqual(6, result);
        }

        [Test]
        public void MultiplyAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MultiplyAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(2);

            Asserts.AreEqual(6, result);
        }

        [Test]
        public void MultiplyAssignChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MultiplyAssignChecked(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(2);

            Asserts.AreEqual(6, result);
        }

        [Test]
        public void MultiplyChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                MultiplyChecked(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(2);

            Asserts.AreEqual(6, result);
        }

        [Test]
        public void NotEqual_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, bool>>(
                NotEqual(param, Constant(1)),
                param);

            bool result = expression.CompileFast(true)(1);

            Asserts.IsFalse(result);
        }

        [Test]
        public void Or_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Or(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(7, result);
        }

        [Test]
        public void OrAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                OrAssign(param, Constant(3)),
                param);

            int result = expression.CompileFast(true)(5);

            Asserts.AreEqual(7, result);
        }

        [Test]
        public void OrElse_compiles()
        {
            var param = Parameter(typeof(bool), "b");
            var expression = Lambda<Func<bool, bool>>(
                OrElse(param, Constant(true)),
                param);

            bool result = expression.CompileFast(true)(false);

            Asserts.IsTrue(result);
        }

        [Test]
        public void Power_compiles()
        {
            var param = Parameter(typeof(double), "d");
            var expression = Lambda<Func<double, double>>(
                Power(param, Constant(2.0)),
                param);

            double result = expression.CompileFast(true)(3.0);

            Asserts.AreEqual(9.0, result);
        }

        [Test]
        public void PowerAssign_compiles()
        {
            var param = Parameter(typeof(double), "d");
            var expression = Lambda<Func<double, double>>(
                PowerAssign(param, Constant(2.0)),
                param);

            double result = expression.CompileFast(true)(3.0);

            Asserts.AreEqual(9.0, result);
        }

        [Test]
        public void ReferenceEqual_compiles()
        {
            const string Value = "test";
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Func<object, bool>>(
                ReferenceEqual(param, Constant(Value)),
                param);

            bool result = expression.CompileFast(true)(Value);

            Asserts.IsTrue(result);
        }

        [Test]
        public void ReferenceEqual_in_Action_compiles()
        {
            const string Value = "test";
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Action<object>>(
                ReferenceEqual(param, Constant(Value)),
                param);

            var fs = expression.CompileSys();
            fs.PrintIL();

            var fx = expression.CompileFast(true);
            fx.PrintIL();
            fx(Value);
        }

        [Test]
        public void ReferenceNotEqual_compiles()
        {
            const string Value = "test";
            var param = Parameter(typeof(object), "o");
            var expression = Lambda<Func<object, bool>>(
                ReferenceNotEqual(param, Constant(Value)),
                param);

            var fs = expression.CompileSys();
            fs.PrintIL();

            var fx = expression.CompileFast(true);
            fx.PrintIL();

            Asserts.IsFalse(fx(Value));
        }

        [Test]
        public void RightShift_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                RightShift(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(12);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void RightShiftAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                RightShiftAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(12);

            Asserts.AreEqual(3, result);
        }

        [Test]
        public void Subtract_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                Subtract(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(3);

            Asserts.AreEqual(1, result);
        }

        [Test]
        public void SubtractAssign_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                SubtractAssign(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(3);

            Asserts.AreEqual(1, result);
        }

        [Test]
        public void SubtractAssignChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                SubtractAssignChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(3);

            Asserts.AreEqual(1, result);
        }

        [Test]
        public void SubtractChecked_compiles()
        {
            var param = Parameter(typeof(int), "i");
            var expression = Lambda<Func<int, int>>(
                SubtractChecked(param, Constant(2)),
                param);

            int result = expression.CompileFast(true)(3);

            Asserts.AreEqual(1, result);
        }
    }
}
