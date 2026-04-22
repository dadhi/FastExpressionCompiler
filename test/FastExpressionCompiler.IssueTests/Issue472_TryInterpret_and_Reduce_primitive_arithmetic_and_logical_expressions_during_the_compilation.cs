using System;

#if LIGHT_EXPRESSION
using ExpressionType = System.Linq.Expressions.ExpressionType;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue472_TryInterpret_and_Reduce_primitive_arithmetic_and_logical_expressions_during_the_compilation : ITestX
{
    public void Run(TestRun t)
    {
        Logical_expression_started_with_not_Without_Interpreter_due_param_use(t);
        Logical_expression_started_with_not(t);
        Condition_with_null_constant_equal_to_default_of_class_type_is_eliminated(t);
        Condition_with_default_class_type_equal_to_null_constant_is_eliminated(t);
        Condition_with_two_defaults_of_class_type_is_eliminated(t);
        Condition_with_not_equal_null_and_default_of_class_type_is_eliminated(t);
        Condition_with_nullable_default_equal_to_null_is_eliminated(t);
        Condition_with_null_constant_equal_to_non_null_constant_is_not_eliminated(t);
        Switch_with_constant_int_value_eliminates_dead_cases(t);
        Switch_with_constant_string_value_eliminates_dead_cases(t);
        Switch_with_constant_enum_value_eliminates_dead_cases(t);
        Switch_with_constant_int_matching_no_case_falls_through_to_default(t);
        Switch_with_interpreted_int_expression_eliminates_dead_cases(t);
    }

    public void Logical_expression_started_with_not(TestContext t)
    {
        var p = Parameter(typeof(bool), "p");
        var expr = Lambda<Func<bool, bool>>(
            OrElse(
                Not(AndAlso(Constant(true), Constant(false))), p),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.IsTrue(fs(true));
        t.IsTrue(fs(false));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.IsTrue(ff(true));
        t.IsTrue(ff(false));
    }

    public void Logical_expression_started_with_not_Without_Interpreter_due_param_use(TestContext t)
    {
        var p = Parameter(typeof(bool), "p");
        var expr = Lambda<Func<bool, bool>>(
            Not(AndAlso(Constant(true), p)),
            p);

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.IsFalse(fs(true));
        t.IsTrue(fs(false));

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.IsFalse(ff(true));
        t.IsTrue(ff(false));
    }

    // Branch elimination: Constant(null) == Default(typeof(X)) where X is a class -> always true
    // Models the AutoMapper pattern: after inlining a null argument into a null-check lambda
    public void Condition_with_null_constant_equal_to_default_of_class_type_is_eliminated(TestContext t)
    {
        // Condition(Equal(Constant(null), Default(typeof(string))), Constant("trueBranch"), Constant("falseBranch"))
        // Since null == default(string) is always true, this should reduce to "trueBranch"
        var expr = Lambda<Func<string>>(
            Condition(
                Equal(Constant(null, typeof(string)), Default(typeof(string))),
                Constant("trueBranch"),
                Constant("falseBranch")));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("trueBranch", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("trueBranch", ff());
    }

    // Branch elimination: Default(typeof(X)) == Constant(null) where X is a class -> always true (symmetric)
    public void Condition_with_default_class_type_equal_to_null_constant_is_eliminated(TestContext t)
    {
        var expr = Lambda<Func<string>>(
            Condition(
                Equal(Default(typeof(string)), Constant(null, typeof(string))),
                Constant("trueBranch"),
                Constant("falseBranch")));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("trueBranch", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("trueBranch", ff());
    }

    // Branch elimination: Default(typeof(X)) == Default(typeof(X)) where X is a class -> always true
    public void Condition_with_two_defaults_of_class_type_is_eliminated(TestContext t)
    {
        var expr = Lambda<Func<string>>(
            Condition(
                Equal(Default(typeof(string)), Default(typeof(string))),
                Constant("trueBranch"),
                Constant("falseBranch")));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("trueBranch", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("trueBranch", ff());
    }

    // Branch elimination: Constant(null) != Default(typeof(X)) where X is a class -> always false
    public void Condition_with_not_equal_null_and_default_of_class_type_is_eliminated(TestContext t)
    {
        var expr = Lambda<Func<string>>(
            Condition(
                NotEqual(Constant(null, typeof(string)), Default(typeof(string))),
                Constant("trueBranch"),
                Constant("falseBranch")));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("falseBranch", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("falseBranch", ff());
    }

    // Branch elimination: Constant(null) == Default(typeof(int?)) -> always true (null == default(int?) is null == null)
    public void Condition_with_nullable_default_equal_to_null_is_eliminated(TestContext t)
    {
        var expr = Lambda<Func<int?>>(
            Condition(
                Equal(Constant(null, typeof(int?)), Default(typeof(int?))),
                Constant(42, typeof(int?)),
                Constant(0, typeof(int?))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual(42, fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual(42, ff());
    }

    // Sanity check: Constant(null) == Constant("hello") should NOT be eliminated (false, not a null-null case)
    public void Condition_with_null_constant_equal_to_non_null_constant_is_not_eliminated(TestContext t)
    {
        var expr = Lambda<Func<string>>(
            Condition(
                Equal(Constant(null, typeof(string)), Constant("hello", typeof(string))),
                Constant("trueBranch"),
                Constant("falseBranch")));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("falseBranch", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("falseBranch", ff());
    }

    // Switch branch elimination (#489): constant int switch value selects single case
    public void Switch_with_constant_int_value_eliminates_dead_cases(TestContext t)
    {
        // Switch(Constant(2), default:"Z", case 1:"A", case 2:"B", case 5:"C")
        // The switch value is a constant 2, so it should reduce to "B"
        var expr = Lambda<Func<string>>(
            Switch(Constant(2),
                Constant("Z"),
                SwitchCase(Constant("A"), Constant(1)),
                SwitchCase(Constant("B"), Constant(2)),
                SwitchCase(Constant("C"), Constant(5))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("B", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("B", ff());
    }

    // Switch branch elimination (#489): constant string switch value selects single case
    public void Switch_with_constant_string_value_eliminates_dead_cases(TestContext t)
    {
        var expr = Lambda<Func<string>>(
            Switch(Constant("hello"),
                Constant("unknown"),
                SwitchCase(Constant("hit_hello"), Constant("hello")),
                SwitchCase(Constant("hit_world"), Constant("world"))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("hit_hello", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("hit_hello", ff());
    }

    public enum Color { Red, Green, Blue }

    // Switch branch elimination (#489): constant enum switch value selects single case
    public void Switch_with_constant_enum_value_eliminates_dead_cases(TestContext t)
    {
        var expr = Lambda<Func<string>>(
            Switch(Constant(Color.Green),
                Constant("unknown"),
                SwitchCase(Constant("red"), Constant(Color.Red)),
                SwitchCase(Constant("green"), Constant(Color.Green)),
                SwitchCase(Constant("blue"), Constant(Color.Blue))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("green", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("green", ff());
    }

    // Switch branch elimination (#489): constant int matches no case -> default is emitted
    public void Switch_with_constant_int_matching_no_case_falls_through_to_default(TestContext t)
    {
        var expr = Lambda<Func<string>>(
            Switch(Constant(99),
                Constant("default"),
                SwitchCase(Constant("A"), Constant(1)),
                SwitchCase(Constant("B"), Constant(2))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("default", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("default", ff());
    }

    // Switch branch elimination (#489): computed int (arithmetic on constants) selects case
    public void Switch_with_interpreted_int_expression_eliminates_dead_cases(TestContext t)
    {
        // Switch(1 + 4, ...) -> Switch(5, ...) -> "C"
        var expr = Lambda<Func<string>>(
            Switch(Add(Constant(1), Constant(4)),
                Constant("Z"),
                SwitchCase(Constant("A"), Constant(1)),
                SwitchCase(Constant("B"), Constant(2)),
                SwitchCase(Constant("C"), Constant(5))));

        expr.PrintCSharp();

        var fs = expr.CompileSys();
        fs.PrintIL();
        t.AreEqual("C", fs());

        var ff = expr.CompileFast(false);
        ff.PrintIL();
        t.AreEqual("C", ff());
    }
}