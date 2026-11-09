using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace FastExpressionCompiler.TestRegistrationGenerator.Tests;

public class FixtureGeneratorTests
{
    private static readonly MetadataReference[] Platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
        .Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p)).ToArray();
    private static readonly CSharpParseOptions Parse = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);

    [Fact]
    public void Generated_calls_execute_each_marked_entry_once_and_leave_helpers_unregistered()
    {
        var standard = FixtureAssembly(false, """
            [RunTest] public partial class Marked : ITestX { public void Run(TestRun t) => t.Count += 3; }
            public partial class Marked { }
            [RunTest] public class Legacy { public int Run() => 7; }
            [RunTest(TestRegistrationGroup.Legacy)] public class Dual : ITestX {
                public int Run() => 2;
                public void Run(TestRun t) => throw new System.Exception("Wrong entry point");
            }
            [RunTest(TestRegistrationGroup.RuntimeContracts)] public class Contract : ITestX { public void Run(TestRun t) => t.Count += 10; }
            public class Unmarked : ITestX { public void Run(TestRun t) => throw new System.Exception("Unmarked helper ran"); }
            """);
        var light = FixtureAssembly(true, """
            [RunTest] public class Marked : ITestX { public void Run(TestRun t) => t.Count += 5; }
            [RunTest(TestRegistrationGroup.RuntimeContracts)] public class Contract : ITestX { public void Run(TestRun t) => t.Count += 20; }
            """);
        var result = Generate(standard, light);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal("13,25,9,2", Execute(result.Compilation, standard, light));
    }

    [Theory]
    [InlineData("NETFRAMEWORK", "1,0,0,0")]
    [InlineData("NET8_0_OR_GREATER", "2,0,0,0")]
    public void Fixture_compilation_conditions_are_respected(string symbol, string expected)
    {
        var standard = FixtureAssembly(false, """
            #if NETFRAMEWORK
            [RunTest] public class Framework : ITestX { public void Run(TestRun t) => t.Count += 1; }
            #else
            [RunTest] public class Modern : ITestX { public void Run(TestRun t) => t.Count += 2; }
            #endif
            """, symbol);
        var light = FixtureAssembly(true, "");
        Assert.Equal(expected, Execute(Generate(standard, light).Compilation, standard, light));
    }

    [Fact]
    public void Changing_a_referenced_fixture_assembly_updates_incremental_registration()
    {
        var empty = FixtureAssembly(false, "");
        var light = FixtureAssembly(true, "");
        var first = Generate(empty, light);
        Assert.Equal("0,0,0,0", Execute(first.Compilation, empty, light));
        var added = FixtureAssembly(false, "[RunTest] public class Added : ITestX { public void Run(TestRun t) => t.Count += 11; }");
        var second = Generate(added, light, driver: first.Driver);
        Assert.Equal("11,0,0,0", Execute(second.Compilation, added, light));
    }

    [Theory]
    [InlineData("[RunTest] public class MissingConstructor : ITestX { private MissingConstructor(int n) {} public void Run(TestRun t) {} }")]
    [InlineData("public class Outer { [RunTest] public class Nested : ITestX { public void Run(TestRun t) {} } }")]
    [InlineData("[RunTest] public class Ambiguous : ITestX { public void Run(TestRun t) {} public int Run() => 0; }")]
    public void Invalid_marked_fixtures_produce_build_errors(string source)
    {
        var result = Generate(FixtureAssembly(false, source), FixtureAssembly(true, ""));
        Assert.Contains(result.Diagnostics, d => d.Id == "FECTEST001" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Omitting_the_generator_is_a_compiler_error()
    {
        var compilation = Host(FixtureAssembly(false, ""), FixtureAssembly(true, ""));
        Assert.Contains(compilation.GetDiagnostics(), d => d.Id == "CS8795");
    }

    [Fact]
    public void Internal_marked_fixtures_are_rejected_before_reference_assembly_emission()
    {
        var source = FixtureCompilation(false, "[RunTest] internal class Hidden : ITestX { public void Run(TestRun t) {} }");
        var driver = CSharpGeneratorDriver.Create(new[] { new FixtureGenerator().AsSourceGenerator() }, parseOptions: Parse)
            .RunGeneratorsAndUpdateCompilation(source, out _, out var diagnostics);
        Assert.Contains(diagnostics, d => d.Id == "FECTEST001" && d.Location.IsInSource);
        Assert.Empty(driver.GetRunResult().GeneratedTrees);
    }

    private static byte[] FixtureAssembly(bool light, string fixtures, string symbol = null) => Emit(FixtureCompilation(light, fixtures, symbol));

    private static CSharpCompilation FixtureCompilation(bool light, string fixtures, string symbol = null)
    {
        var ns = light ? "FastExpressionCompiler.LightExpression" : "FastExpressionCompiler";
        var source = """
            namespace NS {
                public enum TestRegistrationGroup { Default, RuntimeContracts, Legacy }
                [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
                public sealed class RunTestAttribute : System.Attribute {
                    public RunTestAttribute(TestRegistrationGroup group = TestRegistrationGroup.Default) { }
                }
                public interface ITestX { void Run(TestRun t); }
                public sealed class TestRun {
                    public int Count;
                    public void Run<T>(T fixture) where T : ITestX => fixture.Run(this);
                }
            }
            namespace NS.UnitTests {
            FIXTURES
            }
            """.Replace("NS", ns).Replace("FIXTURES", fixtures);
        var options = symbol == null ? Parse : Parse.WithPreprocessorSymbols(symbol);
        return CSharpCompilation.Create(ns + ".UnitTests", new[] { CSharpSyntaxTree.ParseText(source, options) },
            Platform, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static CSharpCompilation Host(byte[] standard, byte[] light)
    {
        var stub = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "GeneratedTests.stub.cs"));
        var host = """
            public static class Host {
                public static string Run() {
                    var s = new FastExpressionCompiler.TestRun();
                    var l = new FastExpressionCompiler.LightExpression.TestRun();
                    FastExpressionCompiler.UnitTests.GeneratedTests.RunStandard(s);
                    FastExpressionCompiler.UnitTests.GeneratedTests.RunLight(l);
                    FastExpressionCompiler.UnitTests.GeneratedTests.RunContracts(s, l);
                    int total = 0, count = 0;
                    System.Action<System.Func<int>, string> run = (test, name) => { total += test(); ++count; };
                    FastExpressionCompiler.UnitTests.GeneratedTests.RunUnitLegacy(run);
                    FastExpressionCompiler.UnitTests.GeneratedTests.RunIssueLegacy(run);
                    return s.Count + "," + l.Count + "," + total + "," + count;
                }
            }
            """;
        return CSharpCompilation.Create("GeneratedHost", new[] { CSharpSyntaxTree.ParseText(stub, Parse), CSharpSyntaxTree.ParseText(host, Parse) },
            Platform.Concat(new[] { MetadataReference.CreateFromImage(standard), MetadataReference.CreateFromImage(light) }),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static (Compilation Compilation, GeneratorDriver Driver, IEnumerable<Diagnostic> Diagnostics) Generate(
        byte[] standard, byte[] light, GeneratorDriver driver = null)
    {
        driver = (driver ?? CSharpGeneratorDriver.Create(new[] { new FixtureGenerator().AsSourceGenerator() }, parseOptions: Parse))
            .RunGeneratorsAndUpdateCompilation(Host(standard, light), out var compilation, out var diagnostics);
        return (compilation, driver, diagnostics);
    }

    private static byte[] Emit(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return stream.ToArray();
    }

    private static string Execute(Compilation compilation, byte[] standard, byte[] light)
    {
        var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
        context.Resolving += (_, name) => name.Name == "FastExpressionCompiler.UnitTests" ? context.LoadFromStream(new MemoryStream(standard))
            : name.Name == "FastExpressionCompiler.LightExpression.UnitTests" ? context.LoadFromStream(new MemoryStream(light)) : null;
        try { return (string)context.LoadFromStream(new MemoryStream(Emit(compilation))).GetType("Host").GetMethod("Run").Invoke(null, null); }
        finally { context.Unload(); }
    }
}
