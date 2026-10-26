using System.Runtime.CompilerServices;
#if LIGHT_EXPRESSION
using FastExpressionCompiler.LightExpression;
using FastExpressionCompiler.LightExpression.UnitTests;
using E = FastExpressionCompiler.LightExpression.Expression;
#else
using FastExpressionCompiler;
using FastExpressionCompiler.UnitTests;
using E = System.Linq.Expressions.Expression;
#endif

[assembly: IgnoresAccessChecksTo("System.Private.CoreLib")]

var expected = Environment.GetEnvironmentVariable("EXPECTED_CORELIB");
if (expected != null && !string.Equals(Path.GetFullPath(typeof(object).Assembly.Location), Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase))
    throw new Exception("FEC smoke executed a different runtime than the selected reference");
var parameter = E.Parameter(typeof(int), "value");
var local = E.Variable(typeof(int), "local");
var block = E.Block(new[] { local },
    E.Assign(local, E.Add(parameter, E.Constant(1))),
    E.TryFinally(E.Add(local, E.Constant(1)), E.Empty()));
var lambda = E.Lambda<Func<int, int>>(block, parameter);
for (var i = 0; i < 100; ++i)
{
    var compiled = lambda.CompileFast(true);
    if (compiled(40) != 42) throw new Exception("Compilation/local/exception smoke failed");
}
var nested = E.Lambda<Func<int, Func<int>>>(E.Lambda<Func<int>>(E.Add(parameter, E.Constant(2))), parameter).CompileFast(true);
if (nested(40)() != 42 || nested(50)() != 52) throw new Exception("Nested closure smoke failed");
var tests = new TestRun(TestFlags.RethrowException);
tests.Run(new RuntimeAccessorContractTests());
if (tests.FailedTestCount != 0) throw new Exception("Runtime contracts failed");
Console.WriteLine($"{typeof(ExpressionCompiler).Namespace}: 100 repeated local/exception compilations, nested capture and {tests.TotalTestCount} runtime contracts passed");

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute(string assemblyName) : Attribute
    {
        public string AssemblyName { get; } = assemblyName;
    }
}
