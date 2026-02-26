using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CS0164, CS0649

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    public class Issue284_Invalid_Program_after_Coalesce : ITest
    {
        public int Run()
        {
            New_test();
            Invalid_expression_with_Coalesce_when_invoked_should_throw_NullRef_the_same_as_system_compiled();
            Invalid_Program_after_Coalesce();
            Coalesce_in_Assign_in_Block();
            return 4;
        }

        public void Invalid_expression_with_Coalesce_when_invoked_should_throw_NullRef_the_same_as_system_compiled()
        {
            var input = Parameter(typeof(Variable));
            var text = Parameter(typeof(string));
            var ctor = typeof(Variable).GetConstructor(new Type[] { typeof(string) });
            var prop = typeof(Variable).GetProperty(nameof(Variable.Name));

            var lambda = Lambda<Func<Variable, string, Variable>>(
                Block(
                    Coalesce(input, New(ctor, Constant("default"))),
                    Assign(Property(input, prop), text),
                    input),
                input, text);

            lambda.PrintCSharp();

            var fs = lambda.CompileSys();
            fs.PrintIL();
            Asserts.Throws<NullReferenceException>(() =>
                fs(null, "a"));

            var fx = lambda.CompileFast(true);
            fx.PrintIL();
            Asserts.Throws<NullReferenceException>(() =>
                fx(null, "a"));
        }

        public void Invalid_Program_after_Coalesce()
        {
            var input = Parameter(typeof(Variable));
            var text = Parameter(typeof(string));
            var ctor = typeof(Variable).GetConstructor(new Type[] { typeof(string) });
            var property = typeof(Variable).GetProperty(nameof(Variable.Name));

            var lambda = Lambda<Func<Variable, string, Variable>>(
                Block(
                    Assign(input, // NOTE: Don't forget to Assign!
                        Coalesce(input, New(ctor, Constant("default")))),
                    Assign(Property(input, property), text),
                    input),
                input, text);

            lambda.PrintCSharp();

            var fs = lambda.CompileSys();
            fs.PrintIL();
            Asserts.IsNotNull(fs(null, "a"));

            var f = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            f.PrintIL();
            var v = f(null, "a");
            Asserts.AreEqual("a", v.Name);
        }

        public void Coalesce_in_Assign_in_Block()
        {
            var input = Parameter(typeof(Variable));
            var text = Parameter(typeof(string));
            var ctor = typeof(Variable).GetConstructor(new Type[] { typeof(string) });
            var property = typeof(Variable).GetProperty(nameof(Variable.Name));

            var lambda = Lambda<Func<Variable, string, Variable>>(
                Block(
                    Assign(input, Coalesce(input, New(ctor, Constant("default")))),
                    input),
                input, text);

            lambda.PrintCSharp();

            var fs = lambda.CompileSys();
            fs.PrintIL();
            Asserts.IsNotNull(fs(null, "a"));

            var f = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            f.PrintIL();
            var v = f(null, "a");
            Asserts.AreEqual("default", v.Name);
        }

        public void New_test()
        {
            var input = Parameter(typeof(Variable));
            var text = Parameter(typeof(string));
            var ctor = typeof(Variable).GetConstructor(new Type[] { typeof(string) });
            var property = typeof(Variable).GetProperty(nameof(Variable.Name));

            var lambda = Lambda<Func<Variable, string, Variable>>(
                Block(
                    Coalesce(input, New(ctor, Constant("default"))),
                    Assign(Property(input, property), text),
                    input),
                input,
                text);

            lambda.PrintCSharp(s => s.Replace(GetType().Name + ".", ""));

            var fSys = lambda.CompileSys();
            fSys.PrintIL();
            Asserts.Throws<NullReferenceException>(() =>
                fSys(null, "a"));

            var fFec = lambda.CompileFast();
            Asserts.IsNotNull(fFec);
            fFec.PrintIL();
            Asserts.Throws<NullReferenceException>(() =>
                fFec(null, "a"));
        }

        public class Variable
        {
            public string Name { get; set; }
            public Variable(string name)
            {
                Name = name;
            }
        }
    }
}