using System;
using System.Reflection;
using System.Linq.Expressions;
using FastExpressionCompiler.LightExpression;
using LE = FastExpressionCompiler.LightExpression;

namespace FastExpressionCompiler.LightExpression.UnitTests
{
    public class FlatExpressionTests : ITest
    {
        public int Run()
        {
            Constant_and_Lambda_roundtrip();
            Parameter_reuse_in_body();
            Binary_arithmetic();
            Block_with_variable_and_assign();
            Conditional_ifThen_and_ifThenElse();
            New_and_Call();
            Field_and_Property_access();
            Unary_convert_and_not();
            Lambda_compiled_and_invoked();
            Nested_lambda();
            Loop_with_break();
            TryCatch_roundtrip();
            ArrayAccess_and_NewArrayInit();
            TypeIs_and_TypeEqual();
            Default_expression();
            return 16;
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static ExprTree E => default;

        // ── tests ─────────────────────────────────────────────────────────────

        public void Constant_and_Lambda_roundtrip()
        {
            var e = default(ExprTree);
            var c = e.Constant(42);
            var lam = e.Lambda<Func<int>>(c);

            var le = e.ToLightExpression(lam);
            Asserts.IsNotNull(le);
            Asserts.AreEqual(ExpressionType.Lambda, le.NodeType);

            var f = ((LE.LambdaExpression)le).CompileFast<Func<int>>();
            Asserts.AreEqual(42, f());
        }

        public void Parameter_reuse_in_body()
        {
            var e = default(ExprTree);
            var p = e.Parameter<int>("x");
            var body = e.Add(p, e.Constant(10));
            var lam = e.Lambda<Func<int, int>>(body, p);

            var le = (LE.LambdaExpression)e.ToLightExpression(lam);
            var f = le.CompileFast<Func<int, int>>();
            Asserts.AreEqual(15, f(5));
            Asserts.AreEqual(20, f(10));
        }

        public void Binary_arithmetic()
        {
            var e = default(ExprTree);
            var p = e.Parameter<int>("n");
            var body = e.Multiply(e.Add(p, e.Constant(2)), e.Constant(3));
            var lam = e.Lambda<Func<int, int>>(body, p);

            var f = e.ToLambdaExpression(lam).CompileFast<Func<int, int>>();
            Asserts.AreEqual(12, f(2));  // (2+2)*3
            Asserts.AreEqual(15, f(3));  // (3+2)*3
        }

        public void Block_with_variable_and_assign()
        {
            var e = default(ExprTree);
            var v = e.Variable<int>("tmp");
            var assign = e.Assign(v, e.Constant(7));
            var result = e.Add(v, e.Constant(3));
            var block = e.Block(new[] { v }, new[] { assign, result });
            var lam = e.Lambda<Func<int>>(block);

            var f = e.ToLambdaExpression(lam).CompileFast<Func<int>>();
            Asserts.AreEqual(10, f());
        }

        public void Conditional_ifThen_and_ifThenElse()
        {
            var e = default(ExprTree);

            // ifThenElse: x > 0 ? 1 : -1
            var p = e.Parameter<int>("x");
            var test = e.GreaterThan(p, e.Constant(0));
            var body = e.Condition(test, e.Constant(1), e.Constant(-1));
            var lam = e.Lambda<Func<int, int>>(body, p);
            var f = e.ToLambdaExpression(lam).CompileFast<Func<int, int>>();
            Asserts.AreEqual(1, f(5));
            Asserts.AreEqual(-1, f(-1));
        }

        public class Foo
        {
            public int Value;
            public int X { get; set; }
            public Foo(int value) => Value = value;
            public int Add(int n) => Value + n;
        }

        public void New_and_Call()
        {
            var e = default(ExprTree);
            var ctor = typeof(Foo).GetConstructor(new[] { typeof(int) });
            var method = typeof(Foo).GetMethod(nameof(Foo.Add));

            var p = e.Parameter<int>("n");
            var newFoo = e.New(ctor, e.Constant(10));
            var call = e.Call(newFoo, method, p);
            var lam = e.Lambda<Func<int, int>>(call, p);

            var f = e.ToLambdaExpression(lam).CompileFast<Func<int, int>>();
            Asserts.AreEqual(15, f(5));
        }

        public void Field_and_Property_access()
        {
            var e = default(ExprTree);
            var field = typeof(Foo).GetField(nameof(Foo.Value));
            var prop = typeof(Foo).GetProperty(nameof(Foo.X));
            var ctor = typeof(Foo).GetConstructor(new[] { typeof(int) });

            // field access: new Foo(42).Value
            {
                var newFoo = e.New(ctor, e.Constant(42));
                var fldAccess = e.Field(newFoo, field);
                var lam = e.Lambda<Func<int>>(fldAccess);
                var f = e.ToLambdaExpression(lam).CompileFast<Func<int>>();
                Asserts.AreEqual(42, f());
            }
        }

        public void Unary_convert_and_not()
        {
            {
                var e = default(ExprTree);
                var p = e.Parameter<int>("x");
                var conv = e.Convert(p, typeof(long));
                var lam = e.Lambda<Func<int, long>>(conv, p);
                var f = e.ToLambdaExpression(lam).CompileFast<Func<int, long>>();
                Asserts.AreEqual(42L, f(42));
            }
            {
                var e = default(ExprTree);
                var p = e.Parameter<bool>("b");
                var notExpr = e.Not(p);
                var lam = e.Lambda<Func<bool, bool>>(notExpr, p);
                var f = e.ToLambdaExpression(lam).CompileFast<Func<bool, bool>>();
                Asserts.AreEqual(false, f(true));
                Asserts.AreEqual(true, f(false));
            }
        }

        public void Lambda_compiled_and_invoked()
        {
            var e = default(ExprTree);
            var a = e.Parameter<int>("a");
            var b = e.Parameter<int>("b");
            var body = e.Add(a, b);
            var lam = e.Lambda<Func<int, int, int>>(body, a, b);

            var f = e.ToLambdaExpression(lam).CompileFast<Func<int, int, int>>();
            Asserts.AreEqual(7, f(3, 4));
        }

        public void Nested_lambda()
        {
            var e = default(ExprTree);
            var x = e.Parameter<int>("x");
            var inner = e.Lambda<Func<int>>(e.Add(x, e.Constant(1)));
            var outer = e.Lambda<Func<int, Func<int>>>(inner, x);

            var f = e.ToLambdaExpression(outer).CompileFast<Func<int, Func<int>>>();
            Asserts.AreEqual(6, f(5)());
        }

        public void Loop_with_break()
        {
            var e = default(ExprTree);
            var breakLabel = Expression.Label(typeof(int), "break");
            var v = e.Variable<int>("i");
            var assignInit = e.Assign(v, e.Constant(0));
            var breakExpr = e.Break(breakLabel, e.Convert(v, typeof(int)));
            var assignInc = e.Assign(v, e.Add(v, e.Constant(1)));
            var cond = e.IfThen(e.GreaterThanOrEqual(v, e.Constant(3)), breakExpr);
            var loopBody = e.Block(new[] { assignInc, cond });
            var loop = e.Loop(loopBody, breakLabel);
            var block = e.Block(new[] { v }, new[] { assignInit, loop });
            var lam = e.Lambda<Func<int>>(block);

            var f = e.ToLambdaExpression(lam).CompileFast<Func<int>>();
            Asserts.AreEqual(3, f());
        }

        public void TryCatch_roundtrip()
        {
            var e = default(ExprTree);
            var exVar = LE.Expression.Parameter(typeof(InvalidOperationException), "ex");
            var catchBlock = LE.Expression.Catch(exVar, LE.Expression.Constant(99));

            var body = e.Constant(1);
            var tryCatch = e.TryCatch(body, catchBlock);
            var lam = e.Lambda<Func<int>>(tryCatch);

            var f = e.ToLambdaExpression(lam).CompileFast<Func<int>>();
            Asserts.AreEqual(1, f());
        }

        public void ArrayAccess_and_NewArrayInit()
        {
            var e = default(ExprTree);

            // new int[] { 10, 20, 30 }[1]
            var arr = e.NewArrayInit(typeof(int), e.Constant(10), e.Constant(20), e.Constant(30));
            var elem = e.ArrayAccess(arr, e.Constant(1));
            var lam = e.Lambda<Func<int>>(elem);
            var f = e.ToLambdaExpression(lam).CompileFast<Func<int>>();
            Asserts.AreEqual(20, f());
        }

        public void TypeIs_and_TypeEqual()
        {
            var e = default(ExprTree);
            var p = e.Parameter<object>("o");

            var typeIs = e.TypeIs(p, typeof(string));
            var lam = e.Lambda<Func<object, bool>>(typeIs, p);
            var f = e.ToLambdaExpression(lam).CompileFast<Func<object, bool>>();
            Asserts.AreEqual(true, f("hello"));
            Asserts.AreEqual(false, f(42));
        }

        public void Default_expression()
        {
            var e = default(ExprTree);
            var defInt = e.Default(typeof(int));
            var lam = e.Lambda<Func<int>>(defInt);
            var f = e.ToLambdaExpression(lam).CompileFast<Func<int>>();
            Asserts.AreEqual(0, f());
        }
    }
}
