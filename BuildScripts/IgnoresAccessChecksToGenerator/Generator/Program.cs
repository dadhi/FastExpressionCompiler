using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

if (args.Length >= 3 && args[0] == "--verify")
{
    ReferenceVerifier.Verify(args[1], args[2], args.Contains("--private"));
    return;
}
if (args.Length < 2)
    throw new ArgumentException("Usage: Generator input-implementation.dll output-reference.dll [--private]");
ReferencePublicizer.Generate(args[0], args[1], args.Contains("--private"));

internal static class ReferencePublicizer
{
    public static void Generate(string input, string output, bool includePrivate)
    {
        using var file = File.OpenRead(input);
        using var pe = new PEReader(file);
        var r = pe.GetMetadataReader();
        if (r.GetString(r.GetAssemblyDefinition().Name) != "System.Private.CoreLib")
            throw new NotSupportedException("This local prototype currently supports CoreLib only, not arbitrary package assemblies.");
        var b = new MetadataBuilder();
        StringHandle S(StringHandle h) => h.IsNil ? default : b.GetOrAddString(r.GetString(h));
        BlobHandle B(BlobHandle h) => h.IsNil ? default : b.GetOrAddBlob(r.GetBlobBytes(h));
        GuidHandle G(GuidHandle h) => h.IsNil ? default : b.GetOrAddGuid(r.GetGuid(h));

        var module = r.GetModuleDefinition();
        b.AddModule(module.Generation, S(module.Name), G(module.Mvid), G(module.GenerationId), G(module.BaseGenerationId));
        var assembly = r.GetAssemblyDefinition();
        b.AddAssembly(S(assembly.Name), assembly.Version, S(assembly.Culture), B(assembly.PublicKey), assembly.Flags, assembly.HashAlgorithm);
        foreach (var h in r.AssemblyReferences)
        {
            var v = r.GetAssemblyReference(h);
            b.AddAssemblyReference(S(v.Name), v.Version, S(v.Culture), B(v.PublicKeyOrToken), v.Flags, B(v.HashValue));
        }
        foreach (var h in r.TypeReferences)
        {
            var v = r.GetTypeReference(h);
            b.AddTypeReference(v.ResolutionScope, S(v.Namespace), S(v.Name));
        }
        // Preserve row numbers in all signature-addressable tables; signature blobs can then be copied unchanged.
        for (var i = 1; i <= r.GetTableRowCount(TableIndex.TypeSpec); ++i)
            b.AddTypeSpecification(B(r.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle(i)).Signature));
        for (var i = 1; i <= r.GetTableRowCount(TableIndex.ModuleRef); ++i)
            b.AddModuleReference(S(r.GetModuleReference(MetadataTokens.ModuleReferenceHandle(i)).Name));
        foreach (var h in r.MemberReferences)
        {
            var v = r.GetMemberReference(h);
            b.AddMemberReference(v.Parent, S(v.Name), B(v.Signature));
        }
        for (var i = 1; i <= r.GetTableRowCount(TableIndex.MethodSpec); ++i)
        {
            var v = r.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(i));
            b.AddMethodSpecification(v.Method, B(v.Signature));
        }

        var fieldRow = 1;
        var methodRow = 1;
        MethodDefinitionHandle referenceAttributeCtor = default;
        foreach (var h in r.TypeDefinitions)
        {
            var v = r.GetTypeDefinition(h);
            var attributes = v.Attributes;
            var visibility = attributes & TypeAttributes.VisibilityMask;
            if (r.GetString(v.Name) != "<Module>" &&
                (includePrivate || visibility is TypeAttributes.NotPublic or TypeAttributes.NestedAssembly or TypeAttributes.NestedFamORAssem or TypeAttributes.NestedFamANDAssem))
                attributes = (attributes & ~TypeAttributes.VisibilityMask) | (v.IsNested ? TypeAttributes.NestedPublic : TypeAttributes.Public);
            b.AddTypeDefinition(attributes, S(v.Namespace), S(v.Name), v.BaseType,
                MetadataTokens.FieldDefinitionHandle(fieldRow), MetadataTokens.MethodDefinitionHandle(methodRow));
            fieldRow += v.GetFields().Count;
            methodRow += v.GetMethods().Count;
            if (v.IsNested)
                b.AddNestedType(h, v.GetDeclaringType());
            var layout = v.GetLayout();
            if (!layout.IsDefault)
                b.AddTypeLayout(h, (ushort)layout.PackingSize, (uint)layout.Size);
            foreach (var ih in v.GetInterfaceImplementations())
                b.AddInterfaceImplementation(h, r.GetInterfaceImplementation(ih).Interface);
            foreach (var mh in v.GetMethodImplementations())
            {
                var implementation = r.GetMethodImplementation(mh);
                b.AddMethodImplementation(h, implementation.MethodBody, implementation.MethodDeclaration);
            }
            if (r.GetString(v.Namespace) == "System.Runtime.CompilerServices" && r.GetString(v.Name) == "ReferenceAssemblyAttribute")
                referenceAttributeCtor = v.GetMethods().First(m => r.GetString(r.GetMethodDefinition(m).Name) == ".ctor");
        }
        foreach (var h in r.FieldDefinitions)
        {
            var v = r.GetFieldDefinition(h);
            var attributes = v.Attributes & ~FieldAttributes.HasFieldRVA;
            var access = attributes & FieldAttributes.FieldAccessMask;
            if (includePrivate || access is FieldAttributes.Assembly or FieldAttributes.FamORAssem or FieldAttributes.FamANDAssem)
                attributes = (attributes & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
            b.AddFieldDefinition(attributes, S(v.Name), B(v.Signature));
            var offset = v.GetOffset();
            if (offset >= 0)
                b.AddFieldLayout(h, offset);
            var marshal = v.GetMarshallingDescriptor();
            if (!marshal.IsNil)
                b.AddMarshallingDescriptor(h, B(marshal));
        }
        var paramRow = 1;
        foreach (var h in r.MethodDefinitions)
        {
            var v = r.GetMethodDefinition(h);
            var attributes = v.Attributes;
            var access = attributes & MethodAttributes.MemberAccessMask;
            if (includePrivate || access is MethodAttributes.Assembly or MethodAttributes.FamORAssem or MethodAttributes.FamANDAssem)
                attributes = (attributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
            b.AddMethodDefinition(attributes, v.ImplAttributes, S(v.Name), B(v.Signature), -1, MetadataTokens.ParameterHandle(paramRow));
            foreach (var ph in v.GetParameters())
            {
                var p = r.GetParameter(ph);
                b.AddParameter(p.Attributes, S(p.Name), p.SequenceNumber);
                var marshal = p.GetMarshallingDescriptor();
                if (!marshal.IsNil)
                    b.AddMarshallingDescriptor(ph, B(marshal));
                ++paramRow;
            }
            var import = v.GetImport();
            if (!import.Module.IsNil)
                b.AddMethodImport(h, import.Attributes, S(import.Name), import.Module);
        }
        for (var i = 1; i <= r.GetTableRowCount(TableIndex.GenericParam); ++i)
        {
            var v = r.GetGenericParameter(MetadataTokens.GenericParameterHandle(i));
            b.AddGenericParameter(v.Parent, v.Attributes, S(v.Name), v.Index);
        }
        for (var i = 1; i <= r.GetTableRowCount(TableIndex.GenericParamConstraint); ++i)
        {
            var v = r.GetGenericParameterConstraint(MetadataTokens.GenericParameterConstraintHandle(i));
            b.AddGenericParameterConstraint(v.Parameter, v.Type);
        }
        foreach (var h in r.PropertyDefinitions)
        {
            var v = r.GetPropertyDefinition(h);
            b.AddProperty(v.Attributes, S(v.Name), B(v.Signature));
            var a = v.GetAccessors();
            if (!a.Getter.IsNil) b.AddMethodSemantics(h, MethodSemanticsAttributes.Getter, a.Getter);
            if (!a.Setter.IsNil) b.AddMethodSemantics(h, MethodSemanticsAttributes.Setter, a.Setter);
            foreach (var m in a.Others) b.AddMethodSemantics(h, MethodSemanticsAttributes.Other, m);
        }
        foreach (var h in r.EventDefinitions)
        {
            var v = r.GetEventDefinition(h);
            b.AddEvent(v.Attributes, S(v.Name), v.Type);
            var a = v.GetAccessors();
            if (!a.Adder.IsNil) b.AddMethodSemantics(h, MethodSemanticsAttributes.Adder, a.Adder);
            if (!a.Remover.IsNil) b.AddMethodSemantics(h, MethodSemanticsAttributes.Remover, a.Remover);
            if (!a.Raiser.IsNil) b.AddMethodSemantics(h, MethodSemanticsAttributes.Raiser, a.Raiser);
            foreach (var m in a.Others) b.AddMethodSemantics(h, MethodSemanticsAttributes.Other, m);
        }
        foreach (var h in r.TypeDefinitions)
        {
            var v = r.GetTypeDefinition(h);
            if (v.GetProperties().Count != 0) b.AddPropertyMap(h, v.GetProperties().First());
            if (v.GetEvents().Count != 0) b.AddEventMap(h, v.GetEvents().First());
        }
        for (var i = 1; i <= r.GetTableRowCount(TableIndex.Constant); ++i)
        {
            var v = r.GetConstant(MetadataTokens.ConstantHandle(i));
            var blob = r.GetBlobReader(v.Value);
            b.AddConstant(v.Parent, blob.ReadConstant(v.TypeCode));
        }
        for (var i = 1; i <= r.GetTableRowCount(TableIndex.DeclSecurity); ++i)
        {
            var v = r.GetDeclarativeSecurityAttribute(MetadataTokens.DeclarativeSecurityAttributeHandle(i));
            b.AddDeclarativeSecurityAttribute(v.Parent, v.Action, B(v.PermissionSet));
        }
        foreach (var h in r.CustomAttributes)
        {
            var v = r.GetCustomAttribute(h);
            b.AddCustomAttribute(v.Parent, v.Constructor, B(v.Value));
        }
        if (referenceAttributeCtor.IsNil)
            throw new NotSupportedException("Prototype currently requires ReferenceAssemblyAttribute in the source assembly (CoreLib).");
        b.AddCustomAttribute(MetadataTokens.EntityHandle(TableIndex.Assembly, 1), referenceAttributeCtor, b.GetOrAddBlob(new byte[] { 1, 0, 0, 0 }));
        // A new IL-only image: no ReadyToRun/native sections, method bodies, or executable resources are copied.
        var builder = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
            new MetadataRootBuilder(b), new BlobBuilder(), flags: CorFlags.ILOnly);
        var image = new BlobBuilder();
        builder.Serialize(image);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        using var destination = File.Create(output);
        image.WriteContentTo(destination);
        Console.WriteLine($"{r.GetString(assembly.Name)} {assembly.Version}: {r.TypeDefinitions.Count} types, {r.FieldDefinitions.Count} fields, {r.MethodDefinitions.Count} methods; private={includePrivate}; {destination.Length:N0} bytes");
        var references = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Path.GetFullPath(input))!, "*.dll").Order())
        {
            using var source = File.OpenRead(path);
            using var runtimeImage = new PEReader(source);
            if (runtimeImage.HasMetadata)
                references.Add(Path.GetFullPath(path) == Path.GetFullPath(input) ? Path.GetFullPath(output) : Path.GetFullPath(path));
        }
        File.WriteAllLines(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "runtime-references.txt"), references);
    }
}
