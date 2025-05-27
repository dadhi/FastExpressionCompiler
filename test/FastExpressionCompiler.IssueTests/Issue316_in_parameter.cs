using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using System;


#pragma warning disable CS0162

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{

    public class Issue316_in_parameter : ITest
    {
        public int Run()
        {
            Test_constructor_in_struct_parameter_constant();
            Test_method_in_struct_parameter_constant();
            Test_get_parameters();
#if LIGHT_EXPRESSION
            Test_constructor_in_struct_parameter_constant_with_NoArgByRef_New();
            return 4;
#endif
            return 3;
        }

        public class ParseException : Exception
        {
            public ParseException(string message, in TextPosition position) : base(message)
            {
                Position = position;
            }

            public TextPosition Position { get; set; }
        }

        public class ParseExceptionNoByRefArgs : Exception
        {
            public ParseExceptionNoByRefArgs(string message, TextPosition position) : base(message)
            {
                Position = position;
            }

            public TextPosition Position { get; set; }
        }

        public struct TextPosition
        {
            public int Position;
        }


        public void Test_constructor_in_struct_parameter_constant()
        {
            var position = new TextPosition { Position = 42 };
            var program = Throw(New(typeof(ParseException).GetConstructors()[0], Constant("314"), Constant(position)));

            var expr = Lambda<Action>(program);
            expr.PrintCSharp(s => s.Replace(GetType().Name + ".", ""));

            var fs = expr.CompileSys();
            fs.PrintIL("sys");
            Asserts.Throws<ParseException>(() => fs());

            var ff = expr.CompileFast();
            ff.PrintIL("fast");
            Asserts.Throws<ParseException>(() => ff());
        }

#if LIGHT_EXPRESSION

        public void Test_constructor_in_struct_parameter_constant_with_NoArgByRef_New()
        {
            var position = new TextPosition { Position = 42 };
            var program = Throw(NewNoByRefArgs(typeof(ParseExceptionNoByRefArgs).GetConstructors()[0], Constant("314"), Constant(position)));

            var expr = Expression.Lambda<Action>(program);
            expr.PrintCSharp(s => s.Replace(GetType().Name + ".", ""));

            var fSys = expr.CompileSys();
            fSys.PrintIL("sys");
            Asserts.Throws<ParseExceptionNoByRefArgs>(() => fSys());

            var fFast = expr.CompileFast();
            fFast.PrintIL("fast");
            Asserts.Throws<ParseExceptionNoByRefArgs>(() => fFast());
        }
#endif

        public static void ThrowParseException(in TextPosition position) => throw new ParseException("from method", in position);


        public void Test_method_in_struct_parameter_constant()
        {
            var position = new TextPosition { Position = 42 };
            var program = Call(GetType().GetMethod(nameof(ThrowParseException)), Constant(position));

            var expr = Expression.Lambda<Action>(program);
            expr.PrintCSharp(s => s.Replace(GetType().Name + ".", ""));

            var fSys = expr.CompileSys();
            fSys.PrintIL("sys");
            Asserts.Throws<ParseException>(() => fSys());

            var fFast = expr.CompileFast();
            fFast.PrintIL("fast");
            Asserts.Throws<ParseException>(() => fFast());
        }


        public void Test_get_parameters()
        {
            var c = typeof(A).GetConstructors()[0];
            Asserts.IsFalse(c.GetParameters() == c.GetParameters());
            Asserts.IsTrue(c.GetParameters()[0] == c.GetParameters()[0]);
        }

        public class A
        {
            public A(B b, C c) { }
        }

        public class B { }
        public class C { }
    }
}