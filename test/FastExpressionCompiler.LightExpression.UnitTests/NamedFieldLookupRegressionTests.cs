using System;
using System.Reflection;
using System.Reflection.Emit;
using static FastExpressionCompiler.LightExpression.Expression;

namespace FastExpressionCompiler.LightExpression.UnitTests;

public struct NamedFieldLookupRegressionTests : ITestX
{
    public void Run(TestRun t)
    {
        Declared_hidden_inherited_and_private_fields_keep_identity(t);
        Unusual_duplicate_field_metadata_keeps_first_declared_match(t);
    }

    public class Base
    {
        public int Inherited = 7;
        private int Private = 11;
        public int Hidden = 13;
        public static int Static = 17;
        public int ReadPrivate() => Private;
    }
    public class Derived : Base { public new int Hidden = 19; }

    public void Declared_hidden_inherited_and_private_fields_keep_identity(TestContext t)
    {
        var p = Parameter(typeof(Derived));
        var input = new Derived();
        foreach (var name in new[] { "Inherited", "Private", "Hidden", "Static" })
        {
            var owner = name == "Hidden" ? typeof(Derived) : typeof(Base);
            var expected = owner.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var node = Field(p, name);
            t.AreEqual(expected, node.Member);
            var f = Lambda<Func<Derived, int>>(node, p).CompileFast(true);
            if (!t.IsNotNull(f)) return;
            t.AreEqual((int)expected.GetValue(input), f(input));
        }
    }

    public void Unusual_duplicate_field_metadata_keeps_first_declared_match(TestContext t)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("FieldLookupPerfRegression"), AssemblyBuilderAccess.Run);
        var builder = assembly.DefineDynamicModule("Main").DefineType("DuplicateFields", TypeAttributes.Public);
        builder.DefineField("Same", typeof(int), FieldAttributes.Public);
        builder.DefineField("Same", typeof(string), FieldAttributes.Public);
        var type = builder.CreateTypeInfo().AsType();
        var expected = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)[0];
        t.AreEqual(expected, Field(Parameter(type), "Same").Member);
    }
}
