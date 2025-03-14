using System;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    public class ValueTypeTests : ITest
    {
        public int Run()
        {
            Struct_Convert_to_interface();
            Should_support_struct_params_with_field_access();
            Should_support_virtual_calls_on_struct_arguments();
            Should_support_virtual_calls_with_parameters_on_struct_arguments();
            Can_create_struct();
            Can_init_struct_member();
            Can_get_struct_member();
            Action_using_with_struct_closure_field();

            return 8;
        }

        public void Should_support_struct_params_with_field_access()
        {
            System.Linq.Expressions.Expression<Func<StructA, int>> sExpr = a => a.N;
            var expr = sExpr.FromSysExpression();

            var f = expr.CompileFast(true);

            Asserts.AreEqual(42, f(new StructA { N = 42 }));
        }


        public void Should_support_virtual_calls_on_struct_arguments()
        {
            System.Linq.Expressions.Expression<Func<StructA, string>> sExpr = a => a.ToString();
            var expr = sExpr.FromSysExpression();

            var f = expr.CompileFast(true);

            Asserts.AreEqual("42", f(new StructA { N = 42 }));
        }

        public void Should_support_virtual_calls_with_parameters_on_struct_arguments()
        {
            object aa = new StructA();
            System.Linq.Expressions.Expression<Func<StructA, bool>> sExpr = a => a.Equals(aa);
            var expr = sExpr.FromSysExpression();

            var f = expr.CompileFast(true);

            Asserts.AreEqual(false, f(new StructA { N = 42 }));
        }

        public void Can_create_struct()
        {
            System.Linq.Expressions.Expression<Func<StructA>> sExpr = () => new StructA();
            var expr = sExpr.FromSysExpression();

            var newA = expr.CompileFast<Func<StructA>>(true);

            Asserts.AreEqual(0, newA().N);
        }

        public void Can_init_struct_member()
        {
            System.Linq.Expressions.Expression<Func<StructA>> sExpr = () => new StructA { N = 43, M = 34, Sf = "sf", Sp = "sp" };
            var expr = sExpr.FromSysExpression();

            var newA = expr.CompileFast<Func<StructA>>(true);

            var a = newA();
            Asserts.AreEqual(43, a.N);
            Asserts.AreEqual(34, a.M);
            Asserts.AreEqual("sf", a.Sf);
            Asserts.AreEqual("sp", a.Sp);
        }


        public void Can_get_struct_member()
        {
            System.Linq.Expressions.Expression<Func<int>> sExprN = () => new StructA { N = 43, M = 34, Sf = "sf", Sp = "sp" }.N;
            System.Linq.Expressions.Expression<Func<int>> sExprM = () => new StructA { N = 43, M = 34, Sf = "sf", Sp = "sp" }.M;
            System.Linq.Expressions.Expression<Func<string>> sExprSf = () => new StructA { N = 43, M = 34, Sf = "sf", Sp = "sp" }.Sf;
            System.Linq.Expressions.Expression<Func<string>> sExprSp = () => new StructA { N = 43, M = 34, Sf = "sf", Sp = "sp" }.Sp;

            var exprN = sExprN;
            var exprM = sExprM;
            var exprSf = sExprSf;
            var exprSp = sExprSp;

            var n = exprN.CompileFast<Func<int>>(true);
            var m = exprM.CompileFast<Func<int>>(true);
            var sf = exprSf.CompileFast<Func<string>>(true);
            var sp = exprSp.CompileFast<Func<string>>(true);

            Asserts.AreEqual(43, n());
            Asserts.AreEqual(34, m());
            Asserts.AreEqual("sf", sf());
            Asserts.AreEqual("sp", sp());
        }

        struct StructA
        {
            public int N;
            public int M { get; set; }
            public string Sf;
            public string Sp { get; set; }

            public override string ToString() => N.ToString();
        }

        public void Action_using_with_struct_closure_field()
        {
            var s = new SS();
            System.Linq.Expressions.Expression<Action<string>> sExpr = a => s.SetValue(a);
            var expr = sExpr.FromSysExpression();

            var lambda = expr.CompileFast(ifFastFailedReturnNull: true);
            lambda("a");
            Asserts.AreEqual("a", s.Value);
        }

        public void Struct_Convert_to_interface()
        {
            System.Linq.Expressions.Expression<Func<int, IComparable>> sExpr = a => a;
            System.Linq.Expressions.Expression<Func<DateTimeKind, IComparable>> sExpr2 = a => a;
            System.Linq.Expressions.Expression<Func<SS, IDisposable>> sExpr3 = a => a;

            var expr = sExpr.FromSysExpression();

            var fs1 = expr.CompileSys();
            fs1.PrintIL();
            Asserts.AreEqual(12, fs1(12));
            var ff1 = expr.CompileFast(true);
            ff1.PrintIL();
            Asserts.AreEqual(12, ff1(12));

            var expr2 = sExpr2.FromSysExpression();
            var ff2 = expr2.CompileFast(true);
            Asserts.AreEqual(DateTimeKind.Local, ff2(DateTimeKind.Local));

            var expr3 = sExpr3.FromSysExpression();
            var ff3 = expr3.CompileFast(true);
            Asserts.AreEqual(new SS { Value = "a" }, ff3(new SS { Value = "a" }));
        }

        public void DateTimeTest()
        {
            var date = new DateTime(2010, 10, 23, 14, 56, 54);
            var convertExpr = Convert(Constant(date), typeof(DateTime?));
            var parameterExpr = Parameter(typeof(DateTime?), "x");
            var lessThanOrEqualExpr = LessThanOrEqual(parameterExpr, convertExpr);
            var lambda = Lambda<Func<DateTime?, bool>>(lessThanOrEqualExpr, parameterExpr);

            Func<DateTime?, bool> func = x => x <= date;

            var fastCompiledResult = lambda.CompileFast()(null);
            var compiledResult = lambda.CompileSys()(null);
            var funcResult = func(null);

            Asserts.AreEqual(compiledResult, fastCompiledResult);
            Asserts.AreEqual(funcResult, fastCompiledResult);
        }

        public struct SS : IDisposable
        {
            public string Value;

            public void SetValue(string s)
            {
                Value = s;
            }
            public void Dispose() { }
        }
    }
}