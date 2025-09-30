using System;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue127_Switch_is_supported : ITest
    {
        public int Run()
        {
            Switch_nullable_enum_value_equals_via_comparison_method_with_non_nullable_parameters();
            Switch_nullable_enum_value_equals_to_non_nullable_cases_via_comparison_method_Impossible_both_should_be_nullable();
            SwitchIsSupported_string_with_comparison_method();

            Switch_nullable_enum_value_equals_to_nullable_cases();
            SwitchIsSupported_nullable_enum_comparing_with_null();
            SwitchIsSupported_bool_value();
            SwitchIsSupported1();
            SwitchIsSupported31();
            SwitchIsSupported11();
            SwitchIsSupported12();
            SwitchIsSupported2();
            SwitchIsSupported30();
            SwitchIsSupported33();
            SwitchIsSupported3();
            SwitchIsSupported_string();
            SwitchIsSupported6();

            return 16;
        }

        public void SwitchIsSupported1()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1)),
                    SwitchCase(Constant("B"), Constant(2)),
                    SwitchCase(Constant("C"), Constant(5))  //Difference of 3 creates empty branches, more creates Conditions
                );

            var expr = Lambda<Func<int, string>>(blockExpr, eVar);
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual("A", fs(1));
            Asserts.AreEqual("B", fs(2));
            Asserts.AreEqual("C", fs(5));
            Asserts.AreEqual("Z", fs(45));

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual("A", ff(1));
            Asserts.AreEqual("B", ff(2));
            Asserts.AreEqual("C", ff(5));
            Asserts.AreEqual("Z", ff(45));
        }

        public enum MyEnum
        {
            A, B, C
        }

        public void Switch_nullable_enum_value_equals_to_nullable_cases()
        {
            var eVar = Parameter(typeof(MyEnum?));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(MyEnum.A, typeof(MyEnum?))),
                    SwitchCase(Constant("B"), Constant(MyEnum.B, typeof(MyEnum?))),
                    SwitchCase(Constant("C"), Constant(MyEnum.C, typeof(MyEnum?)))
                );

            var expr = Lambda<Func<MyEnum?, string>>(blockExpr, eVar);
            expr.PrintCSharp();

            var ff = expr.CompileSys();
            ff.PrintIL();
            Asserts.AreEqual("A", ff(MyEnum.A));
            Asserts.AreEqual("B", ff(MyEnum.B));
            Asserts.AreEqual("C", ff(MyEnum.C));
            Asserts.AreEqual("Z", ff(null));

            var fs = expr.CompileFast(true);
            fs.PrintIL();
            Asserts.AreEqual("A", fs(MyEnum.A));
            Asserts.AreEqual("B", fs(MyEnum.B));
            Asserts.AreEqual("C", fs(MyEnum.C));
            Asserts.AreEqual("Z", fs(null));
        }

        public static bool MyEnumEquals(MyEnum? y, MyEnum? x) =>
            x.GetValueOrDefault() == y.GetValueOrDefault() && x.HasValue == y.HasValue;

        public void Switch_nullable_enum_value_equals_to_non_nullable_cases_via_comparison_method_Impossible_both_should_be_nullable()
        {
            var eVar = Parameter(typeof(MyEnum?));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    GetType().GetMethod(nameof(MyEnumEquals)),
                    SwitchCase(Constant("A"), Constant(MyEnum.A, typeof(MyEnum?))),
                    SwitchCase(Constant("B"), Constant(MyEnum.B, typeof(MyEnum?))),
                    SwitchCase(Constant("C"), Constant(MyEnum.C, typeof(MyEnum?)))
                );

            var expr = Lambda<Func<MyEnum?, string>>(blockExpr, eVar);
            expr.PrintCSharp();

            var ff = expr.CompileSys();
            ff.PrintIL();
            Asserts.AreEqual("A", ff(MyEnum.A));
            Asserts.AreEqual("B", ff(MyEnum.B));
            Asserts.AreEqual("C", ff(MyEnum.C));
            Asserts.AreEqual("Z", ff(null));

            var fs = expr.CompileFast(true);
            fs.PrintIL();
            Asserts.AreEqual("A", fs(MyEnum.A));
            Asserts.AreEqual("B", fs(MyEnum.B));
            Asserts.AreEqual("C", fs(MyEnum.C));
            Asserts.AreEqual("Z", fs(null));
        }

        public static bool MyEnumEqualsNonNullable(MyEnum y, MyEnum x) => y == x;

        public void Switch_nullable_enum_value_equals_via_comparison_method_with_non_nullable_parameters()
        {
            var eVar = Parameter(typeof(MyEnum?));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    GetType().GetMethod(nameof(MyEnumEqualsNonNullable)),
                    SwitchCase(Constant("A"), Constant(MyEnum.A, typeof(MyEnum?))),
                    SwitchCase(Constant("B"), Constant(MyEnum.B, typeof(MyEnum?))),
                    SwitchCase(Constant("C"), Constant(MyEnum.C, typeof(MyEnum?)))
                );

            var expr = Lambda<Func<MyEnum?, string>>(blockExpr, eVar);
            expr.PrintCSharp();

            var ff = expr.CompileSys();
            ff.PrintIL();
            Asserts.AreEqual("A", ff(MyEnum.A));
            Asserts.AreEqual("B", ff(MyEnum.B));
            Asserts.AreEqual("C", ff(MyEnum.C));
            Asserts.AreEqual("Z", ff(null));

            var fs = expr.CompileFast(true);
            fs.PrintIL();
            Asserts.AreEqual("A", fs(MyEnum.A));
            Asserts.AreEqual("B", fs(MyEnum.B));
            Asserts.AreEqual("C", fs(MyEnum.C));
            Asserts.AreEqual("Z", fs(null));
        }

        public void SwitchIsSupported_nullable_enum_comparing_with_null()
        {
            var eVar = Parameter(typeof(MyEnum));
            var blockExpr =
                Switch(eVar,
                    Constant(null, typeof(long?)),
                    SwitchCase(Constant(1L, typeof(long?)), Constant(MyEnum.A)),
                    SwitchCase(Constant(2L, typeof(long?)), Constant(MyEnum.B))
                );

            var e = Lambda<Func<MyEnum, long?>>(blockExpr, eVar);

            var fs = e.CompileSys();
            fs.PrintIL();

            var f = e.CompileFast(true);
            f.PrintIL();

            Asserts.IsNotNull(f);
            Asserts.AreEqual(2L, f(MyEnum.B));
            Asserts.AreEqual(null, f(MyEnum.C));
        }

        public void SwitchIsSupported11()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1)),
                    SwitchCase(Constant("B"), Constant(2)),
                    SwitchCase(Constant("C"), Constant(7)),  //Difference of 3 creates empty branches, more creates Conditions
                    SwitchCase(Constant("C"), Constant(8))  //Difference of 3 creates empty branches, more creates Conditions
                );

            var lambda = Lambda<Func<int, string>>(blockExpr, eVar);
            var f = lambda.CompileFast(true);

            Asserts.IsNotNull(f);
            Asserts.AreEqual("B", f(2));
        }

        public void SwitchIsSupported12()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1)),
                    SwitchCase(Constant("B"), Constant(2)),
                    SwitchCase(Constant("C"), Constant(7))  //Difference of 3 creates empty branches, more creates Conditions
                );

            var lambda = Lambda<Func<int, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual("B", fastCompiled(2));
        }

        public void SwitchIsSupported2()
        {
            var eVar = Parameter(typeof(long));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1L)),
                    SwitchCase(Constant("B"), Constant(2L)),
                    SwitchCase(Constant("C"), Constant(3L))
                );

            var lambda = Lambda<Func<long, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual("B", fastCompiled(2));
        }

        public void SwitchIsSupported30()
        {
            var eVar = Parameter(typeof(int?));
            var blockExpr = Equal(eVar, Constant(94, typeof(int?)));

            var lambda = Lambda<Func<int?, bool>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual(true, fastCompiled(94));
        }

        public void SwitchIsSupported33()
        {
            var blockExpr = Convert(Constant(94), typeof(int?));

            var lambda = Lambda<Func<int?>>(blockExpr);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual(94, fastCompiled());
        }

        public void SwitchIsSupported3()
        {
            var eVar = Parameter(typeof(int?));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant((int?)94, typeof(int?))),
                    SwitchCase(Constant("B"), Constant((int?)96, typeof(int?))),
                    SwitchCase(Constant("C"), Constant((int?)98, typeof(int?)))
                );

            var lambda = Lambda<Func<int?, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual("B", fastCompiled(96));
        }

        public void SwitchIsSupported31()
        {
            var eVar = Parameter(typeof(int));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(1))
                );

            var e = Lambda<Func<int, string>>(blockExpr, eVar);
            e.PrintCSharp();

            var fs = e.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual("A", fs(1));
            Asserts.AreEqual("Z", fs(42));

            var ff = e.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual("A", ff(1));
            Asserts.AreEqual("Z", ff(42));
        }

        public void SwitchIsSupported_bool_value()
        {
            var eVar = Parameter(typeof(bool));
            var blockExpr =
                Switch(eVar,
                    Constant("Z"),
                    SwitchCase(Constant("A"), Constant(true)),
                    SwitchCase(Constant("B"), Constant(false))
                );

            var expr = Lambda<Func<bool, string>>(blockExpr, eVar);
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual("A", fs(true));
            Asserts.AreEqual("B", fs(false));

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual("A", ff(true));
            Asserts.AreEqual("B", ff(false));
        }

        public void SwitchIsSupported_string()
        {
            var eVar = Parameter(typeof(string));
            var blockExpr =
                Switch(eVar,
                    Constant("C"),
                    SwitchCase(Constant("A"), Constant("A")),
                    SwitchCase(Constant("B"), Constant("B"))
                );

            var expr = Lambda<Func<string, string>>(blockExpr, eVar);
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual("A", fs("A"));
            Asserts.AreEqual("B", fs("B"));
            Asserts.AreEqual("C", fs("Z"));

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual("A", ff("A"));
            Asserts.AreEqual("B", ff("B"));
            Asserts.AreEqual("C", ff("Z"));
        }

        public static bool StringCompareOrdinalIgnoreCase(string x, string y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x, y);

        public void SwitchIsSupported_string_with_comparison_method()
        {
            var eVar = Parameter(typeof(string));
            var blockExpr =
                Switch(eVar,
                    Constant("C"),
                    GetType().GetMethod(nameof(StringCompareOrdinalIgnoreCase)),
                    SwitchCase(Constant("A"), Constant("a")),
                    SwitchCase(Constant("B"), Constant("b"))
                );

            var expr = Lambda<Func<string, string>>(blockExpr, eVar);
            expr.PrintCSharp();

            var fs = expr.CompileSys();
            fs.PrintIL();
            Asserts.AreEqual("A", fs("A"));
            Asserts.AreEqual("B", fs("B"));
            Asserts.AreEqual("C", fs("Z"));

            var ff = expr.CompileFast(true);
            ff.PrintIL();
            Asserts.AreEqual("A", ff("A"));
            Asserts.AreEqual("B", ff("B"));
            Asserts.AreEqual("C", ff("Z"));
        }

        class Helper
        {
            public string V { get; set; }
        }

        public void SwitchIsSupported6()
        {
            var eVar = Parameter(typeof(Helper));
            var blockExpr =
                Switch(Property(eVar, "V"),
                    Constant("C"),
                    SwitchCase(Constant("A"), Constant("A")),
                    SwitchCase(Constant("B"), Constant("B")),
                    SwitchCase(Constant("C"), Constant("C")),
                    SwitchCase(Constant("D"), Constant("D")),
                    SwitchCase(Constant("E"), Constant("E")),
                    SwitchCase(Constant("F"), Constant("F")),
                    SwitchCase(Constant("G"), Constant("G")),
                    SwitchCase(Constant("H"), Constant("H")),
                    SwitchCase(Constant("I"), Constant("I"))
                );

            var lambda = Lambda<Func<Helper, string>>(blockExpr, eVar);
            var fastCompiled = lambda.CompileFast(true);
            Asserts.IsNotNull(fastCompiled);
            Asserts.AreEqual("A", fastCompiled(new Helper() { V = "A" }));
            Asserts.AreEqual("C", fastCompiled(new Helper() { V = "Z" }));
        }
    }
}
