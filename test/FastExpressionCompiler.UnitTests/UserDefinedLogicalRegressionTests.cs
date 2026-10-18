using System;
using SysExpr = System.Linq.Expressions.Expression;

#if LIGHT_EXPRESSION
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
namespace FastExpressionCompiler.UnitTests;
#endif

public struct UserDefinedLogicalRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Reference_operators_short_circuit_and_evaluate_once(t);
        Explicit_methods_are_honored(t);
        Struct_operators_and_ignored_results(t);
        Right_operand_can_contain_try_catch(t);
    }

    public void Reference_operators_short_circuit_and_evaluate_once(TestContext t) => CheckReference(t, false);
    public void Explicit_methods_are_honored(TestContext t) => CheckReference(t, true);

    private static void CheckReference(TestContext t, bool explicitMethod)
    {
        foreach (var and in new[] { false, true })
        foreach (var value in new[] { 0, 5 })
        {
            var left = new LogicalValue(value);
            var right = new LogicalValue(3);
            var leftCalls = 0;
            var rightCalls = 0;
            Func<LogicalValue> getLeft = () => { ++leftCalls; return left; };
            Func<LogicalValue> getRight = () => { ++rightCalls; return right; };
            var l = SysExpr.Parameter(typeof(Func<LogicalValue>));
            var r = SysExpr.Parameter(typeof(Func<LogicalValue>));
            var method = explicitMethod ? typeof(LogicalValue).GetMethod(and ? "And" : "Or") : null;
            var body = and ? SysExpr.AndAlso(SysExpr.Invoke(l), SysExpr.Invoke(r), method)
                : SysExpr.OrElse(SysExpr.Invoke(l), SysExpr.Invoke(r), method);
            var fast = SysExpr.Lambda<Func<Func<LogicalValue>, Func<LogicalValue>, LogicalValue>>(body, l, r)
                .FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            var result = fast(getLeft, getRight);
            var shortCircuited = and ? value == 0 : value != 0;
            t.AreEqual(shortCircuited ? value : and ? value & 3 : value | 3, result.Value);
            t.AreEqual(1, leftCalls);
            t.AreEqual(shortCircuited ? 0 : 1, rightCalls);
            t.AreEqual(and ? 0 : 1, left.TrueCalls);
            t.AreEqual(and ? 1 : 0, left.FalseCalls);
            t.AreEqual(0, right.TrueCalls + right.FalseCalls);
            t.AreEqual(!shortCircuited && explicitMethod ? 1 : 0, left.MethodCalls);
            t.AreEqual(!shortCircuited && !explicitMethod ? 1 : 0, left.OperatorCalls);
            if (shortCircuited) t.AreEqual(left, result);
        }
    }

    public void Struct_operators_and_ignored_results(TestContext t)
    {
        foreach (var and in new[] { false, true })
        {
            var l = SysExpr.Parameter(typeof(LogicalStruct));
            var r = SysExpr.Parameter(typeof(Func<LogicalStruct>));
            var body = and ? SysExpr.AndAlso(l, SysExpr.Invoke(r)) : SysExpr.OrElse(l, SysExpr.Invoke(r));
            var fast = SysExpr.Lambda<Func<LogicalStruct, Func<LogicalStruct>, LogicalStruct>>(body, l, r)
                .FromSysExpression().CompileFast(true);
            var ignored = SysExpr.Lambda<Action<LogicalStruct, Func<LogicalStruct>>>(SysExpr.Block(body, SysExpr.Empty()), l, r)
                .FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast) || !t.IsNotNull(ignored)) return;
            foreach (var value in new[] { 0, 5 })
            {
                var calls = 0;
                Func<LogicalStruct> right = () => { ++calls; return new LogicalStruct(3); };
                var shortCircuited = and ? value == 0 : value != 0;
                t.AreEqual(shortCircuited ? value : and ? value & 3 : value | 3, fast(new LogicalStruct(value), right).Value);
                ignored(new LogicalStruct(value), right);
                t.AreEqual(shortCircuited ? 0 : 2, calls);
            }
        }
    }

    public void Right_operand_can_contain_try_catch(TestContext t)
    {
        foreach (var and in new[] { false, true })
        {
            var l = SysExpr.Parameter(typeof(LogicalStruct));
            var right = SysExpr.TryCatch(SysExpr.Throw(SysExpr.New(typeof(InvalidOperationException)), typeof(LogicalStruct)),
                SysExpr.Catch(typeof(InvalidOperationException), SysExpr.Constant(new LogicalStruct(3))));
            var body = and ? SysExpr.AndAlso(l, right) : SysExpr.OrElse(l, right);
            var expression = SysExpr.Lambda<Func<LogicalStruct, LogicalStruct>>(body, l);
            var fast = expression.FromSysExpression().CompileFast(true);
            if (!t.IsNotNull(fast)) return;
            foreach (var value in new[] { 0, 5 })
                t.AreEqual(and ? value == 0 ? value : value & 3 : value != 0 ? value : value | 3, fast(new LogicalStruct(value)).Value);
        }
    }

    public sealed class LogicalValue
    {
        public readonly int Value;
        public int TrueCalls, FalseCalls, OperatorCalls, MethodCalls;
        public LogicalValue(int value) => Value = value;
        public static bool operator true(LogicalValue value) { ++value.TrueCalls; return value.Value != 0; }
        public static bool operator false(LogicalValue value) { ++value.FalseCalls; return value.Value == 0; }
        public static LogicalValue operator &(LogicalValue left, LogicalValue right)
        { ++left.OperatorCalls; return new LogicalValue(left.Value & right.Value); }
        public static LogicalValue operator |(LogicalValue left, LogicalValue right)
        { ++left.OperatorCalls; return new LogicalValue(left.Value | right.Value); }
        public static LogicalValue And(LogicalValue left, LogicalValue right)
        { ++left.MethodCalls; return new LogicalValue(left.Value & right.Value); }
        public static LogicalValue Or(LogicalValue left, LogicalValue right)
        { ++left.MethodCalls; return new LogicalValue(left.Value | right.Value); }
    }

    public struct LogicalStruct
    {
        public readonly int Value;
        public LogicalStruct(int value) => Value = value;
        public static bool operator true(LogicalStruct value) => value.Value != 0;
        public static bool operator false(LogicalStruct value) => value.Value == 0;
        public static LogicalStruct operator &(LogicalStruct left, LogicalStruct right) => new(left.Value & right.Value);
        public static LogicalStruct operator |(LogicalStruct left, LogicalStruct right) => new(left.Value | right.Value);
    }
}
