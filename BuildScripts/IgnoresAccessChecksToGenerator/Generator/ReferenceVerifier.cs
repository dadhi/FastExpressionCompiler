using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

internal static class ReferenceVerifier
{
    public static void Verify(string input, string output, bool includePrivate)
    {
        using var source = new PEReader(File.OpenRead(input));
        using var facade = new PEReader(File.OpenRead(output));
        var a = source.GetMetadataReader();
        var b = facade.GetMetadataReader();
        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException(message);
        }
        var aa = a.GetAssemblyDefinition();
        var ba = b.GetAssemblyDefinition();
        Require(a.GetString(aa.Name) == b.GetString(ba.Name) && a.GetString(aa.Culture) == b.GetString(ba.Culture) && aa.Version == ba.Version && aa.Flags == ba.Flags &&
            a.GetBlobBytes(aa.PublicKey).SequenceEqual(b.GetBlobBytes(ba.PublicKey)), "Assembly identity changed");
        Require(facade.PEHeaders.CorHeader!.Flags.HasFlag(CorFlags.ILOnly), "Facade must be IL-only");
        Require(facade.PEHeaders.CorHeader.ManagedNativeHeaderDirectory.Size == 0, "Native implementation leaked into facade");
        Require(a.TypeDefinitions.Count == b.TypeDefinitions.Count && a.FieldDefinitions.Count == b.FieldDefinitions.Count &&
            a.MethodDefinitions.Count == b.MethodDefinitions.Count, "Definition row counts changed");
        foreach (var handle in a.TypeDefinitions)
        {
            var x = a.GetTypeDefinition(handle);
            var y = b.GetTypeDefinition(handle);
            Require(a.GetString(x.Name) == b.GetString(y.Name) && a.GetString(x.Namespace) == b.GetString(y.Namespace) && x.BaseType == y.BaseType &&
                x.GetDeclaringType() == y.GetDeclaringType() && x.GetFields().SequenceEqual(y.GetFields()) && x.GetMethods().SequenceEqual(y.GetMethods()), "Type token/range changed");
            if (includePrivate && a.GetString(x.Name) != "<Module>")
                Require((y.Attributes & TypeAttributes.VisibilityMask) == (y.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public), "Type not publicized");
        }
        foreach (var handle in a.FieldDefinitions)
        {
            var x = a.GetFieldDefinition(handle);
            var y = b.GetFieldDefinition(handle);
            Require(a.GetString(x.Name) == b.GetString(y.Name) && a.GetBlobBytes(x.Signature).SequenceEqual(b.GetBlobBytes(y.Signature)), "Field name/signature changed");
            if (includePrivate) Require((y.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.Public, "Private field not exposed");
        }
        foreach (var handle in a.MethodDefinitions)
        {
            var x = a.GetMethodDefinition(handle);
            var y = b.GetMethodDefinition(handle);
            Require(a.GetString(x.Name) == b.GetString(y.Name) && a.GetBlobBytes(x.Signature).SequenceEqual(b.GetBlobBytes(y.Signature)) &&
                x.GetParameters().SequenceEqual(y.GetParameters()), "Method name/signature/parameter range changed");
            Require(y.RelativeVirtualAddress == 0, "Executable body leaked into facade");
            if (includePrivate) Require((y.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public, "Private method not exposed");
        }
        var refAttribute = b.CustomAttributes.Any(h =>
        {
            var attr = b.GetCustomAttribute(h);
            return attr.Parent.Kind == HandleKind.AssemblyDefinition && attr.Constructor.Kind == HandleKind.MethodDefinition &&
                b.GetString(b.GetTypeDefinition(b.GetMethodDefinition((MethodDefinitionHandle)attr.Constructor).GetDeclaringType()).Name) == "ReferenceAssemblyAttribute";
        });
        Require(refAttribute, "Missing ReferenceAssemblyAttribute");
        Console.WriteLine("Verified identity, IL-only/reference marker, all definition tokens/ranges/signatures and absence of method bodies");
    }
}
