using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests
#endif
{
    [TestFixture]
    public class BlockTests
    {
        public int Run()
        {
            Block_local_variable_assignment();
            Block_local_variable_assignment_array_closure();
            Block_local_variable_assignment_with_lambda_with_param();
            Block_local_variable_assignment_with_lambda_with_param_array_closure();
            Block_local_variable_assignment_return_with_variable();
            Block_local_variable_assignment_return_with_variable_array_closure();
            Block_local_variable_assignment_with_member_init();
            Block_local_variable_assignment_with_member_init_array_closure();
            Block_local_variable_assignment_with_lambda_invoke();
            Block_returning_the_nested_lambda_assigning_the_outer_parameter();
            Block_calling_non_void_method_returning_the_nested_lambda_assigning_the_outer_parameter();
            Block_assigning_local_variable_then_returning_the_nested_lambda_which_reassigns_the_variable();
            Block_assigning_local_ValueType_variable_then_returning_the_nested_lambda_which_reassigns_the_variable();
            Block_local_variable_assignment_with_lambda_invoke_plus_external_assignment();
            Block_local_variable_assignment_with_lambda_invoke_array_closure();
            Block_local_variable_assignment_with_lambda_invoke_with_param_override();
            Block_local_variable_assignment_with_lambda_invoke_with_param_override_array_closure();

            return 17;
        }

        [Test]
        public void Block_local_variable_assignment()
        {
            var variable = Variable(typeof(int));
            var variable2 = Variable(typeof(int));

            var block = Block(new[] { variable, variable2 },
                Assign(variable, Constant(5)),
                Assign(variable2, Constant(6)));

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = lambda.CompileFast<Func<int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(6, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_array_closure()
        {
            var variables = Vars<int>().Take(2).ToArray();

            var block = Block(new[] { variables[0], variables[1] },
                Assign(variables[0], Constant(5)),
                Assign(variables[1], Constant(6)));

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = lambda.CompileFast<Func<int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(6, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_invoke()
        {
            var variable = Variable(typeof(int));

            var block = Block(new[] { variable },
                Invoke(Lambda(Assign(variable, Constant(6)))));

            var lambda = Lambda<Func<int>>(block);

#if LIGHT_EXPRESSION
            lambda.PrintCSharp();
#endif

            var s = lambda.CompileSys();
            s.PrintIL("system il");

            var f = lambda.CompileFast<Func<int>>(true);
            f.PrintIL("fec il");

            Assert.NotNull(f);
            Assert.AreEqual(6, f());
        }

        public static int Inc(int i) => i + 1; 

        [Test]
        public void Block_calling_non_void_method_returning_the_nested_lambda_assigning_the_outer_parameter()
        {
            var p = Parameter(typeof(int), "p");

            var lambda = Lambda<Func<int, Func<int>>>(
                Block(
                    Call(null, GetType().GetTypeInfo().GetDeclaredMethod(nameof(Inc)), p),
                    Lambda(Assign(p, Constant(42)))
                ),
                p);

#if LIGHT_EXPRESSION
            lambda.PrintCSharp();
#endif
            var s = lambda.CompileSys();
            s.PrintIL("system il");

            var f = lambda.CompileFast<Func<int, Func<int>>>(true);
            f.PrintIL("fec il");

            Assert.NotNull(f);
            var ff = f(17);
            Assert.IsInstanceOf<Func<int>>(ff);

            Assert.AreEqual(42, ff());
        }

        [Test]
        public void Block_returning_the_nested_lambda_assigning_the_outer_parameter()
        {
            var p = Parameter(typeof(int), "p");

            var lambda = Lambda<Func<int, Func<int>>>(
                    Lambda(Assign(p, Constant(42))),
                p);

#if LIGHT_EXPRESSION
            lambda.PrintCSharp();
#endif
            var s = lambda.CompileSys();
            s.PrintIL("system il");

            var f = lambda.CompileFast<Func<int, Func<int>>>(true);
            f.PrintIL("fec il");

            Assert.NotNull(f);
            var ff = f(17);
            Assert.IsInstanceOf<Func<int>>(ff);

            Assert.AreEqual(42, ff());
        }

        [Test]
        public void Block_assigning_local_variable_then_returning_the_nested_lambda_which_reassigns_the_variable()
        {
            var variable = Variable(typeof(string));

            var lambda = Lambda<Func<Func<string>>>(
                Block(new[] { variable },
                    Assign(variable, Constant("35")),
                    Lambda(Assign(variable, Constant("42")))
                )
            );

#if LIGHT_EXPRESSION
            lambda.PrintCSharp();
#endif

            var s = lambda.CompileSys();
            s.PrintIL("system il");

            var f = lambda.CompileFast(true);
            f.PrintIL("fec il");

            Assert.NotNull(f);
            var ff = f();
            Assert.IsInstanceOf<Func<string>>(ff);
            Assert.AreEqual("42", ff());
        }

        [Test]
        public void Block_assigning_local_ValueType_variable_then_returning_the_nested_lambda_which_reassigns_the_variable()
        {
            var variable = Variable(typeof(int));

            var lambda = Lambda<Func<Func<int>>>(
                Block(new[] { variable },
                    Assign(variable, Constant(35)),
                    Lambda(Assign(variable, Constant(42)))
                )
            );

            var f = lambda.CompileFast(true);
            Assert.NotNull(f);

            var ff = f();
            Assert.IsInstanceOf<Func<int>>(ff);
            Assert.AreEqual(42, ff());
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_invoke_plus_external_assignment()
        {
            var variable = Variable(typeof(int));

            var block = Block(new[] { variable },
                Assign(variable, Constant(5)),
                Invoke(Lambda(Assign(variable, Constant(6)))));

            var lambda = Lambda<Func<int>>(block);

            var f = lambda.CompileFast<Func<int>>(true);
            Assert.AreEqual(6, f());
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_invoke_array_closure()
        {
            var variables = Vars<int>().Take(20).ToArray();

            var block = Block(variables,
                Assign(variables[0], Constant(5)),
                Invoke(Lambda(Assign(variables[0], Constant(6)))));

            var expr = Lambda<Func<int>>(block);

            var f = expr.CompileFast<Func<int>>(true);

            Assert.NotNull(f);
            Assert.AreEqual(6, f());
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_with_param()
        {
            var variable = Variable(typeof(int));
            var variable2 = Variable(typeof(int));
            var param = Parameter(typeof(int));

            var block = Block(new[] { variable },
                Assign(variable, Constant(5)),
                Invoke(Lambda(Block(new[] { variable2 },
                Assign(variable2, param)))));

            var lambda = Lambda<Func<int, int>>(block, param);

            var fastCompiled = lambda.CompileFast<Func<int, int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(8, fastCompiled(8));
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_with_param_array_closure()
        {
            var variables = Vars<int>().Take(20).ToArray();
            var param = Parameter(typeof(int));

            var block = Block(new[] { variables[0] },
                Assign(variables[0], Constant(5)),
                Invoke(Lambda(Block(new[] { variables[1] },
                    Assign(variables[1], param)))));

            var lambda = Lambda<Func<int, int>>(block, param);

            var fastCompiled = lambda.CompileFast<Func<int, int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(8, fastCompiled(8));
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_invoke_with_param_override()
        {
            var variable = Variable(typeof(int));
            var param = Parameter(typeof(int));

            var block = Block(new[] { variable },
                Assign(variable, Constant(5)),
                Invoke(Lambda(Block(Assign(param, variable)))));

            var lambda = Lambda<Func<int, int>>(block, param);

            var fastCompiled = lambda.CompileFast<Func<int, int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled(8));
        }

        [Test]
        public void Block_local_variable_assignment_with_lambda_invoke_with_param_override_array_closure()
        {
            var variables = Vars<int>().Take(20).ToArray();
            var param = Parameter(typeof(int));

            var block = Block(new[] { variables[0] },
                Assign(variables[0], Constant(5)),
                Invoke(Lambda(Block(Assign(param, variables[0])))));

            var lambda = Lambda<Func<int, int>>(block, param);

            var fastCompiled = lambda.CompileFast<Func<int, int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled(8));
        }

        [Test]
        public void Block_local_variable_assignment_return_with_variable()
        {
            var variable = Variable(typeof(int));

            var block = Block(new[] { variable },
                Assign(variable, Constant(5)),
                variable);

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = lambda.CompileFast<Func<int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_return_with_variable_array_closure()
        {
            var variables = Vars<int>().Take(20).ToArray();

            var block = Block(new[] { variables[0] },
                Assign(variables[0], Constant(5)),
                variables[0]);

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = lambda.CompileFast<Func<int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_with_member_init()
        {
            var variable = Variable(typeof(A));

            var block = Block(new[] { variable },
                Assign(variable, MemberInit(New(
                    typeof(A).GetTypeInfo().DeclaredConstructors.First()), 
                    Bind(typeof(A).GetTypeInfo().GetDeclaredProperty("K"), Constant(5)))),
                Property(variable, typeof(A).GetTypeInfo().GetDeclaredProperty("K")));

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = lambda.CompileFast<Func<int>>();

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        [Test]
        public void Block_local_variable_assignment_with_member_init_array_closure()
        {
            var variables = Vars<A>().Take(20).ToArray();

            var block = Block(new[] { variables[0] },
                Assign(variables[0], MemberInit(New(typeof(A).GetTypeInfo().DeclaredConstructors.First()), 
                Bind(typeof(A).GetTypeInfo().GetDeclaredProperty("K"), Constant(5)))),
                Property(variables[0], typeof(A).GetTypeInfo().GetDeclaredProperty("K")));

            var lambda = Lambda<Func<int>>(block);

            var fastCompiled = lambda.CompileFast<Func<int>>(true);

            Assert.NotNull(fastCompiled);
            Assert.AreEqual(5, fastCompiled());
        }

        private class A
        {
            public int K { get; set; }
        }

        private IEnumerable<ParameterExpression> Vars<T>()
        {
            while (true)
                yield return Variable(typeof(T));
            // ReSharper disable once IteratorNeverReturns
        }
    }
}
