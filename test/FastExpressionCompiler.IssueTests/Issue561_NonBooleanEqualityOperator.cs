// The safety guard uses the dynamic-method decoder supported on modern .NET.
// The fatal repro itself is also exercised in a separate child-process harness.
#if NET8_0_OR_GREATER
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression.ILDecoder;
using FastExpressionCompiler.LightExpression.ImTools;
using static FastExpressionCompiler.LightExpression.ExpressionCompiler;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using FastExpressionCompiler.ILDecoder;
namespace FastExpressionCompiler.IssueTests;
#endif

public struct Issue561_NonBooleanEqualityOperator : ITestX
{
    public void Run(TestRun t)
    {
        Equal_returns_the_interface_from_the_selected_operator(t);
        NotEqual_returns_the_interface_from_the_selected_operator(t);
        Reversed_equal_returns_the_interface_from_the_selected_operator(t);
        Reversed_not_equal_returns_the_interface_from_the_selected_operator(t);
        Ignored_result_still_calls_the_operator(t);
        Operands_execute_once_in_order_with_nested_try(t);
        Non_lifted_nullable_parameters_reach_the_selected_method(t);
        Selected_methods_keep_non_boolean_value_results(t);
        Boolean_method_is_not_replaced_by_constant_comparison(t);
        Converted_simple_right_operands_reach_the_selected_method(t);
        Converted_simple_right_operands_inside_try(t);
#if LIGHT_EXPRESSION
        Converted_intrinsic_right_operand_can_emit_exception_regions(t);
#endif
    }

    public interface ICustomObject { void Ping(); }

    private static string Trace;

    public sealed class CustomObject : ICustomObject
    {
        public static int OperatorCalls;
        public static int PingCalls;

        public static ICustomObject operator ==(double left, CustomObject right) => Result();
        public static ICustomObject operator !=(double left, CustomObject right) => Result();
        public static ICustomObject operator ==(CustomObject left, double right) => Result();
        public static ICustomObject operator !=(CustomObject left, double right) => Result();
        private static ICustomObject Result() { ++OperatorCalls; Trace += "O"; return new CustomObject(); }
        public void Ping() { ++PingCalls; }
        public override bool Equals(object obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => base.GetHashCode();
    }

    public void Equal_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => 1 == new CustomObject());

    public void NotEqual_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => 1 != new CustomObject());

    public void Reversed_equal_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => new CustomObject() == 1);

    public void Reversed_not_equal_returns_the_interface_from_the_selected_operator(TestContext t) =>
        Check(t, () => new CustomObject() != 1);

    private static void Check(TestContext t, System.Linq.Expressions.Expression<Func<ICustomObject>> expression)
    {
        var binary = (System.Linq.Expressions.BinaryExpression)expression.Body;
        t.AreEqual(typeof(ICustomObject), binary.Type);
        t.IsNotNull(binary.Method);
        t.IsTrue(!binary.IsLifted);

        CustomObject.OperatorCalls = CustomObject.PingCalls = 0;
        var normal = expression.Compile()();
        t.AreEqual(1, CustomObject.OperatorCalls);
        normal.Ping();
        t.AreEqual(1, CustomObject.PingCalls);

        foreach (var flags in new[] { CompilerFlags.Default, CompilerFlags.NoInvocationLambdaInlining,
            CompilerFlags.EnableDelegateDebugInfo })
        {
            var fast = Compile(expression, flags);
            if (!t.IsNotNull(fast)) return;
            if (!CheckSelectedCall(t, fast, binary.Method)) return;

            CustomObject.OperatorCalls = CustomObject.PingCalls = 0;
            var result = fast();
            t.AreEqual(1, CustomObject.OperatorCalls);
            if (!t.IsNotNull(result)) return;
            t.IsTrue(result is CustomObject);
            result.Ping();
            t.AreEqual(1, CustomObject.PingCalls);
        }
    }

    private static TDelegate Compile<TDelegate>(System.Linq.Expressions.Expression<TDelegate> expression,
        CompilerFlags flags = CompilerFlags.Default) where TDelegate : Delegate =>
#if LIGHT_EXPRESSION
        expression.ToLightExpression().CompileFast(true, flags);
#else
        expression.CompileFast(true, flags);
#endif

    private static bool CheckSelectedCall(TestContext t, Delegate fast, MethodInfo method)
    {
        // On affected compilers the returned reference is corrupt. Check the
        // selected call before invocation so this regression fails without
        // taking down the whole runner. This does not assert an IL layout.
        var instructions = fast.TryGetDebugInfo()?.ILInstructions ?? fast.Method.ReadAllInstructions();
        if (instructions.OfType<InlineMethodInstruction>().Any(i => i.Method.Equals(method))) return true;
        t.Fail("Issue561: selected operator call is missing; corrupt-reference delegate was not invoked.");
        return false;
    }

    public void Ignored_result_still_calls_the_operator(TestContext t)
    {
        System.Linq.Expressions.Expression<Func<ICustomObject>> original = () => 1 == new CustomObject();
        var method = ((System.Linq.Expressions.BinaryExpression)original.Body).Method;
        var expression = System.Linq.Expressions.Expression.Lambda<Action>(
            System.Linq.Expressions.Expression.Block(original.Body, System.Linq.Expressions.Expression.Empty()));
        var fast = Compile(expression);
        if (!t.IsNotNull(fast) || !CheckSelectedCall(t, fast, method)) return;
        CustomObject.OperatorCalls = 0;
        fast();
        t.AreEqual(1, CustomObject.OperatorCalls);
    }

    public static double ReadLeft() { Trace += "L"; return 1; }
    public static int ReadArgument() { Trace += "A"; return 2; }
    public static void FinishArgument() { Trace += "F"; }
    public static CustomObject ReadRight(int value) { Trace += "R"; return new CustomObject(); }

    public void Operands_execute_once_in_order_with_nested_try(TestContext t)
    {
        var owner = typeof(Issue561_NonBooleanEqualityOperator);
        var left = System.Linq.Expressions.Expression.Call(owner.GetMethod(nameof(ReadLeft)));
        var right = System.Linq.Expressions.Expression.Call(owner.GetMethod(nameof(ReadRight)),
            System.Linq.Expressions.Expression.TryFinally(
                System.Linq.Expressions.Expression.Call(owner.GetMethod(nameof(ReadArgument))),
                System.Linq.Expressions.Expression.Call(owner.GetMethod(nameof(FinishArgument)))));
        var body = System.Linq.Expressions.Expression.Equal(left, right);
        var expression = System.Linq.Expressions.Expression.Lambda<Func<ICustomObject>>(body);
        var system = expression.Compile();
        Trace = ""; system().Ping();
        var expected = Trace;
        t.AreEqual("LAFRO", expected);
        var fast = Compile(expression);
        if (!t.IsNotNull(fast) || !CheckSelectedCall(t, fast, body.Method)) return;
        Trace = ""; CustomObject.OperatorCalls = CustomObject.PingCalls = 0;
        fast().Ping();
        t.AreEqual(expected, Trace);
        t.AreEqual(1, CustomObject.OperatorCalls);
        t.AreEqual(1, CustomObject.PingCalls);
    }

    public static ICustomObject CompareNullable(double? left, CustomObject right)
    {
        Trace += left.HasValue ? "value" : "null";
        ++CustomObject.OperatorCalls;
        return new CustomObject();
    }

    public void Non_lifted_nullable_parameters_reach_the_selected_method(TestContext t)
    {
        var left = System.Linq.Expressions.Expression.Parameter(typeof(double?));
        var body = System.Linq.Expressions.Expression.Equal(left,
            System.Linq.Expressions.Expression.New(typeof(CustomObject)), false,
            typeof(Issue561_NonBooleanEqualityOperator).GetMethod(nameof(CompareNullable)));
        t.IsTrue(!body.IsLifted);
        var expression = System.Linq.Expressions.Expression.Lambda<Func<double?, ICustomObject>>(body, left);
        var system = expression.Compile();
        var fast = Compile(expression);
        if (!t.IsNotNull(fast) || !CheckSelectedCall(t, fast, body.Method)) return;
        foreach (var value in new double?[] { null, 2 })
        {
            Trace = ""; system(value).Ping(); var expected = Trace;
            Trace = ""; CustomObject.OperatorCalls = CustomObject.PingCalls = 0;
            fast(value).Ping();
            t.AreEqual(expected, Trace);
            t.AreEqual(1, CustomObject.OperatorCalls);
            t.AreEqual(1, CustomObject.PingCalls);
        }
    }

    public static int CompareValue(int left, int right) { ++CustomObject.OperatorCalls; return left * 10 + right; }
    public static bool CompareBoolean(int left, int right) { ++CustomObject.OperatorCalls; return left != right; }

    public void Selected_methods_keep_non_boolean_value_results(TestContext t)
    {
        var left = System.Linq.Expressions.Expression.Parameter(typeof(int));
        var method = typeof(Issue561_NonBooleanEqualityOperator).GetMethod(nameof(CompareValue));
        foreach (var kind in new[] { ExpressionType.Equal, ExpressionType.NotEqual, ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual, ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual })
        {
            var body = System.Linq.Expressions.Expression.MakeBinary(kind, left,
                System.Linq.Expressions.Expression.Constant(2), false, method);
            var expression = System.Linq.Expressions.Expression.Lambda<Func<int, int>>(body, left);
            var fast = Compile(expression);
            if (!t.IsNotNull(fast) || !CheckSelectedCall(t, fast, method)) return;
            var expected = expression.Compile()(3);
            CustomObject.OperatorCalls = 0;
            t.AreEqual(expected, fast(3));
            t.AreEqual(1, CustomObject.OperatorCalls);
        }
    }

    public void Boolean_method_is_not_replaced_by_constant_comparison(TestContext t)
    {
        var method = typeof(Issue561_NonBooleanEqualityOperator).GetMethod(nameof(CompareBoolean));
        var body = System.Linq.Expressions.Expression.Equal(System.Linq.Expressions.Expression.Constant(1),
            System.Linq.Expressions.Expression.Constant(2), false, method);
        var expression = System.Linq.Expressions.Expression.Lambda<Func<bool>>(body);
        var fast = Compile(expression);
        if (!t.IsNotNull(fast) || !CheckSelectedCall(t, fast, method)) return;
        var expected = expression.Compile()();
        CustomObject.OperatorCalls = 0;
        t.AreEqual(expected, fast());
        t.AreEqual(1, CustomObject.OperatorCalls);
    }
    public void Converted_simple_right_operands_reach_the_selected_method(TestContext t)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(int));
        var method = typeof(Issue561_NonBooleanEqualityOperator).GetMethod(nameof(CompareValue));
        foreach (var right in new System.Linq.Expressions.Expression[] { parameter,
            System.Linq.Expressions.Expression.Constant(2), System.Linq.Expressions.Expression.Default(typeof(int)) })
        {
            var converted = System.Linq.Expressions.Expression.Convert(
                System.Linq.Expressions.Expression.Convert(right, typeof(long)), typeof(int));
            var body = System.Linq.Expressions.Expression.Equal(
                System.Linq.Expressions.Expression.Constant(3), converted, false, method);
            var expression = System.Linq.Expressions.Expression.Lambda<Func<int, int>>(body, parameter);
            var fast = Compile(expression);
            if (!t.IsNotNull(fast) || !CheckSelectedCall(t, fast, method)) return;
            var expected = expression.Compile()(4);
            CustomObject.OperatorCalls = 0;
            t.AreEqual(expected, fast(4));
            t.AreEqual(1, CustomObject.OperatorCalls);
        }
    }

    public void Converted_simple_right_operands_inside_try(TestContext t)
    {
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(int));
        var method = typeof(Issue561_NonBooleanEqualityOperator).GetMethod(nameof(CompareValue));
        foreach (var right in new System.Linq.Expressions.Expression[] { parameter,
            System.Linq.Expressions.Expression.Constant(2), System.Linq.Expressions.Expression.Default(typeof(int)) })
        {
            var converted = System.Linq.Expressions.Expression.Convert(
                System.Linq.Expressions.Expression.Convert(right, typeof(long)), typeof(int));
            foreach (var kind in new[] { ExpressionType.Equal, ExpressionType.Add })
            {
                var binary = System.Linq.Expressions.Expression.MakeBinary(kind,
                    System.Linq.Expressions.Expression.Constant(3), converted, false,
                    kind == ExpressionType.Equal ? method : null);
                var body = System.Linq.Expressions.Expression.TryFinally(binary, System.Linq.Expressions.Expression.Empty());
                var expression = System.Linq.Expressions.Expression.Lambda<Func<int, int>>(body, parameter);
                var fast = Compile(expression);
                if (!t.IsNotNull(fast)) return;
                var expected = expression.Compile()(4);
                CustomObject.OperatorCalls = 0;
                t.AreEqual(expected, fast(4));
                t.AreEqual(kind == ExpressionType.Equal ? 1 : 0, CustomObject.OperatorCalls);
            }
        }
    }

#if LIGHT_EXPRESSION
    private sealed class TryFinallyConversion : UnaryExpression
    {
        private readonly Expression _body;
        public TryFinallyConversion(Expression operand, Expression body) : base(operand) => _body = body;
        public override ExpressionType NodeType => ExpressionType.Convert;
        public override bool IsIntrinsic => true;
        public override Result TryCollectInfo(ref CompilerContext context, NestedLambdaInfo nestedLambda,
            ref SmallList<NestedLambdaInfo> rootNestedLambdas) =>
            ExpressionCompiler.TryCollectInfo(ref context, _body, nestedLambda, ref rootNestedLambdas);
        public override bool TryEmit(ref CompilerContext context, System.Reflection.Emit.ILGenerator il,
            ParentFlags parent, int byRefIndex = -1) =>
            EmittingVisitor.TryEmit(_body, il, ref context, parent, byRefIndex);
    }

    public void Converted_intrinsic_right_operand_can_emit_exception_regions(TestContext t)
    {
        var owner = typeof(Issue561_NonBooleanEqualityOperator);
        var parameter = Expression.Parameter(typeof(int));
        var read = Expression.Call(owner.GetMethod(nameof(ReadArgument)));
        var intrinsic = new TryFinallyConversion(parameter,
            Expression.TryFinally(read, Expression.Call(owner.GetMethod(nameof(FinishArgument)))));
        // The intrinsic is hidden inside two ordinary conversion wrappers.
        var right = Expression.Convert(Expression.Convert(intrinsic, typeof(long)), typeof(int));
        var method = owner.GetMethod(nameof(CompareValue));
        var body = Expression.Equal(read, right, false, method);
        var fast = Expression.Lambda<Func<int, int>>(body, parameter).CompileFast(true);
        if (!t.IsNotNull(fast) || !CheckSelectedCall(t, fast, method)) return;
        Trace = "";
        CustomObject.OperatorCalls = 0;
        t.AreEqual(22, fast(4));
        t.AreEqual("AAF", Trace);
        t.AreEqual(1, CustomObject.OperatorCalls);

        var arithmetic = Expression.Lambda<Func<int, int>>(Expression.Add(read, right), parameter).CompileFast(true);
        if (!t.IsNotNull(arithmetic)) return;
        Trace = "";
        CustomObject.OperatorCalls = 0;
        t.AreEqual(4, arithmetic(4));
        t.AreEqual("AAF", Trace);
        t.AreEqual(0, CustomObject.OperatorCalls);
    }
#endif

}
#endif
