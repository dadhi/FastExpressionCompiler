using System;
using System.Reflection;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public class Issue500_IndexOutOfRangeException_with_value_objects_implicit_conversions : ITest
{
    public int Run()
    {
        Implicit_conv_op_via_abstract_base_class_param_in_closure();
        Implicit_conv_op_via_non_abstract_base_class_param_in_closure();
        Implicit_conv_op_via_abstract_base_class_param_directly();
        return 3;
    }

    public abstract class PrimitiveValueObject<TInput, TOutput>
    {
        public TInput Value { get; protected set; } = default!;

        public static implicit operator TInput(PrimitiveValueObject<TInput, TOutput>? primitiveValueObject) =>
            primitiveValueObject == null ? default! : primitiveValueObject.Value;
    }

    public class MyPrimitive : PrimitiveValueObject<string, MyPrimitive>
    {
        public MyPrimitive(string val) => Value = val;
    }

    // Reproduces the original issue: implicit conversion operator on an abstract base class
    // used within a closure-captured variable wrapped in Convert(..., typeof(object))
    public void Implicit_conv_op_via_abstract_base_class_param_in_closure()
    {
        var captured = new MyPrimitive("Hello world");

        // Expression with implicit conversion on closure-captured variable
        System.Linq.Expressions.Expression<Func<string>> sysExpr = () => captured;
#if LIGHT_EXPRESSION
        var innerExpr = sysExpr.FromSysExpression();
#else
        var innerExpr = sysExpr;
#endif

        // Wrap in Convert to object (common pattern in LINQ providers)
        var toObject = Convert(innerExpr.Body, typeof(object));
        var lambda = Lambda<Func<object>>(toObject);

        lambda.PrintCSharp();

        var fs = lambda.CompileSys();
        Asserts.AreEqual("Hello world", (string)fs());

        var ff = lambda.CompileFast(true);
        Asserts.AreEqual("Hello world", (string)ff());
    }

    public class NonAbstractBase<TInput, TOutput>
    {
        public TInput Value { get; protected set; } = default!;

        public static implicit operator TInput(NonAbstractBase<TInput, TOutput>? obj) =>
            obj == null ? default! : obj.Value;
    }

    public class DerivedPrimitive : NonAbstractBase<string, DerivedPrimitive>
    {
        public DerivedPrimitive(string val) => Value = val;
    }

    // Same case but with a non-abstract base class (previously caused InvalidProgramException)
    public void Implicit_conv_op_via_non_abstract_base_class_param_in_closure()
    {
        var captured = new DerivedPrimitive("Hello world");

        System.Linq.Expressions.Expression<Func<string>> sysExpr = () => captured;
#if LIGHT_EXPRESSION
        var innerExpr = sysExpr.FromSysExpression();
#else
        var innerExpr = sysExpr;
#endif

        var toObject = Convert(innerExpr.Body, typeof(object));
        var lambda = Lambda<Func<object>>(toObject);

        lambda.PrintCSharp();

        var fs = lambda.CompileSys();
        Asserts.AreEqual("Hello world", (string)fs());

        var ff = lambda.CompileFast(true);
        Asserts.AreEqual("Hello world", (string)ff());
    }

    // Same conversion but with a direct (non-closure) parameter; the method must be passed explicitly
    // because the derived class does not directly declare the static op_Implicit (it is on the base class).
    public void Implicit_conv_op_via_abstract_base_class_param_directly()
    {
        var param = Parameter(typeof(MyPrimitive), "p");

        // Get the op_Implicit method declared on the abstract base class
        var opImplicit = typeof(PrimitiveValueObject<string, MyPrimitive>)
            .GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static);
        Asserts.IsNotNull(opImplicit);

        // Convert MyPrimitive -> string via op_Implicit (which takes PrimitiveValueObject<string,MyPrimitive>)
        // then Convert string -> object
        var toObject = Convert(Convert(param, typeof(string), opImplicit), typeof(object));
        var lambda = Lambda<Func<MyPrimitive, object>>(toObject, param);

        lambda.PrintCSharp();

        var fs = lambda.CompileSys();
        Asserts.AreEqual("Hello world", (string)fs(new MyPrimitive("Hello world")));

        var ff = lambda.CompileFast(true);
        Asserts.AreEqual("Hello world", (string)ff(new MyPrimitive("Hello world")));
    }
}
