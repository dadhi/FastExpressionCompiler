using System;


#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{

    public class ToCSharpStringTests : ITest
    {
        public int Run()
        {
            Outputs_default_null_for_reference_types();
            Outputs_closed_generic_type_constant_correctly();
            Outputs_type_equals();
            return 3;
        }


        public void Outputs_closed_generic_type_constant_correctly()
        {
            var e = Lambda<Func<Type>>(Constant(typeof(A<string>)));

            var cs = e.ToCSharpString();

            Asserts.Contains("A<string>", cs);

            var f = e.CompileFast(true);
            Asserts.AreEqual(typeof(A<string>), f());
        }


        public void Outputs_type_equals()
        {
            var p = Parameter(typeof(object), "p");
            var eSealed = TypeEqual(p, typeof(string));
            var eStruct = TypeEqual(p, typeof(int));
            var eArray = TypeEqual(p, typeof(object[]));
            var eOpen = TypeEqual(p, typeof(System.Collections.Generic.List<string>));

            Asserts.AreEqual("(p is string);", eSealed.ToCSharpString());
            Asserts.AreEqual("(p is int);", eStruct.ToCSharpString());
            Asserts.AreEqual("(p.GetType() == typeof(object[]));", eArray.ToCSharpString());
            Asserts.AreEqual("(p.GetType() == typeof(List<string>));", eOpen.ToCSharpString());
        }

        public void Outputs_default_null_for_reference_types()
        {
            Asserts.AreEqual("(string)null;", Constant(null, typeof(string)).ToCSharpString());
            Asserts.AreEqual("(string)null;", Default(typeof(string)).ToCSharpString());
            Asserts.AreEqual("(List<string>)null;", Constant(null, typeof(System.Collections.Generic.List<string>)).ToCSharpString());
            Asserts.AreEqual("(List<string>)null;", Default(typeof(System.Collections.Generic.List<string>)).ToCSharpString());
            Asserts.AreEqual("(int?)null;", Constant(null, typeof(int?)).ToCSharpString());
            Asserts.AreEqual("(int?)null;", Default(typeof(int?)).ToCSharpString());

            Asserts.AreEqual("default(int);", Default(typeof(int)).ToCSharpString());

            var e = Block(
                new[] { Variable(typeof(int), "integer"), Variable(typeof(int?), "maybe_integer"), Variable(typeof(string), "str") },
                Empty()
            );

            Asserts.AreEqual(
                "int integer = default;" + Environment.NewLine +
                "int? maybe_integer = null;" + Environment.NewLine +
                "string str = null;;",
                e.ToCSharpString().Trim());
        }

        class A<X> { }
    }
}
