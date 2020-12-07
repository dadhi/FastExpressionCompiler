using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
#pragma warning disable CS0164, CS0649

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue284_Invalid_Program_after_Coalesce : ITest
    {
        public int Run()
        {
            Invalid_expresion_with_Coalesce_when_invoked_should_throw_NullRef_the_same_as_system_compiled();
            Invalid_Program_after_Coalesce();
            return 2;
        }

        [Test]
        public void Invalid_expresion_with_Coalesce_when_invoked_should_throw_NullRef_the_same_as_system_compiled()
        {
            var input = Parameter(typeof(Variable));
            var text  = Parameter(typeof(string));
            var ctor  = typeof(Variable).GetConstructor(new Type[] { typeof(string) });
            var prop  = typeof(Variable).GetProperty(nameof(Variable.Name));

            var lambda = Lambda<Func<Variable, string, Variable>>(
                Block(
                    Coalesce(input, New(ctor, Constant("default"))),
                    Assign(Property(input, prop), text),
                    input), 
                input, text);

            // var t = lambda.ToExpressionString();

            var fs = lambda.CompileSys();
            fs.PrintIL();
            Assert.Throws(typeof(NullReferenceException), () =>
                fs(null, "a"));

            var fx = lambda.CompileFast(true);
            fx.PrintIL();
            Assert.Throws(typeof(NullReferenceException), () =>
                fx(null, "a"));
        }

        [Test]
        public void Invalid_Program_after_Coalesce()
        {
            var input = Parameter(typeof(Variable));
            var text = Parameter(typeof(string));
            var ctor = typeof(Variable).GetConstructor(new Type[] { typeof(string) });
            var property = typeof(Variable).GetProperty(nameof(Variable.Name));

            var lambda = Lambda<Func<Variable, string,Variable>>(
                Block(
                    Assign(input, // NOTE: Don't forget to Assign!
                        Coalesce(input, New(ctor, Constant("default")))),
                    Assign(Property(input, property), text),
                    input), 
                input, text);

            lambda.PrintCSharp();

            var fs = lambda.CompileSys();
            fs.PrintIL();
            Assert.NotNull(fs(null, "a"));

            var f = lambda.CompileFast(true, CompilerFlags.EnableDelegateDebugInfo);
            f.PrintIL();
            Assert.NotNull(f(null, "a"));
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