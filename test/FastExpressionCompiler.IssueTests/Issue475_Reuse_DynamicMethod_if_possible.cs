using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;




#if LIGHT_EXPRESSION
using ExpressionType = System.Linq.Expressions.ExpressionType;
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests;
#else
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.IssueTests;
#endif

public class Issue475_Reuse_DynamicMethod_if_possible : ITestX
{
    public void Run(TestRun t)
    {
        TryToReuseIlGenerator_for_any_signature(t);
        TryToReuseIlGenerator(t);
        TryToReuseTheDynamicMethod_FailedWithInternalClrError_BecauseTheResultDelegateIsLinkedWithDynamicMethod(t);
    }

    internal static FieldInfo IlGeneratorField = typeof(DynamicMethod).GetField("_ilGenerator", BindingFlags.Instance | BindingFlags.NonPublic);

    static readonly Type DynamicILGeneratorType =
        Type.GetType("System.Reflection.Emit.DynamicILGenerator");
    static readonly FieldInfo DynamicILGeneratorScopeField = DynamicILGeneratorType.GetField("m_scope", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly FieldInfo DynamicScopeTokensField = DynamicILGeneratorScopeField.FieldType.GetField("m_tokens", BindingFlags.Instance | BindingFlags.NonPublic);
    static readonly PropertyInfo DynamicScopeTokensItem = DynamicScopeTokensField.FieldType.GetProperty("Item");
    static readonly ConstructorInfo ScopeTreeCtor = DynamicILGeneratorType.BaseType.GetField("m_ScopeTree", BindingFlags.Instance | BindingFlags.NonPublic).FieldType
        .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);

    static readonly ConstructorInfo DynamicILGeneratorCtor = DynamicILGeneratorType.GetConstructor(
        BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(DynamicMethod), typeof(byte[]), typeof(int)], null);

    static Func<DynamicMethod, ILGenerator, ILGenerator> GetILGeneratorMethodSignature_CreateANewOnbe_SetItToDynamicMethod()
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(ILGenerator),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(DynamicMethod), typeof(ILGenerator)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Dup);

        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldfld, DynamicILGeneratorScopeField); // load scope
        il.Emit(OpCodes.Ldfld, DynamicScopeTokensField); // load scope tokens
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Call, DynamicScopeTokensItem.GetMethod); // get the 1st item, a sig byte[]

        il.Emit(OpCodes.Ldc_I4, 64);
        il.Emit(OpCodes.Newobj, DynamicILGeneratorCtor);

        var newILLocalVar = il.GetNextLocalVarIndex(typeof(ILGenerator));
        ExpressionCompiler.EmittingVisitor.EmitStoreAndLoadLocalVariable(il, newILLocalVar);

        il.Emit(OpCodes.Stfld, IlGeneratorField);

        ExpressionCompiler.EmittingVisitor.EmitLoadLocalVariable(il, newILLocalVar);
        il.Emit(OpCodes.Ret);

        return (Func<DynamicMethod, ILGenerator, ILGenerator>)dynMethod.CreateDelegate(typeof(Func<DynamicMethod, ILGenerator, ILGenerator>), ExpressionCompiler.EmptyArrayClosure);
    }

    internal static Func<DynamicMethod, ILGenerator, ILGenerator> ConfigureDynamicILGenerator = GetILGeneratorMethodSignature_CreateANewOnbe_SetItToDynamicMethod();

    internal static MethodInfo ArrayClearMethod = typeof(Array).GetMethod(nameof(Array.Clear), [typeof(Array), typeof(int), typeof(int)]);

    internal static FieldInfo ListOfObjectsSize = typeof(List<object>).GetField("_size", BindingFlags.Instance | BindingFlags.NonPublic);

    /*
        public ILGenerator GetILGenerator(int streamSize)
        {
            if (_ilGenerator == null)
            {
                byte[] methodSignature = SignatureHelper.GetMethodSigHelper(
                    null, CallingConvention, ReturnType, null, null, _parameterTypes, null, null).GetSignature(true);
                _ilGenerator = new DynamicILGenerator(this, methodSignature, streamSize);
            }
            return _ilGenerator;
        }

        internal sealed class DynamicScope
        {
            internal readonly List<object?> m_tokens = new List<object?> { null };
            public int GetTokenFor(byte[] signature)
            {
                m_tokens.Add(signature);
                return m_tokens.Count - 1 | (int)MetadataTokenType.Signature;
            }
        }

        internal DynamicScope m_scope;
        private readonly int m_methodSigToken;

        public DynamicILGenerator(
            DynamicMethod method, 
            byte[] methodSignature, 
            int streamSize)
        {
            m_scope = new DynamicScope();
            m_methodSigToken = m_scope.GetTokenFor(methodSignature);

            m_ScopeTree = new ScopeTree();
            // clear ilstream bytes to reuse the buffer
            m_ILStream = new byte[Math.Max(size, DefaultSize)];

            m_localSignature = SignatureHelper.GetLocalVarSigHelper((method as RuntimeMethodBuilder)?.GetTypeBuilder().Module);
            
            // set to the new DynamicMethod
            m_methodBuilder = method;

        }
    */
    public void TryToReuseIlGenerator(TestContext t)
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        var func = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        t.AreEqual(41, func(41));

        var dynMethod2 = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        // Let start with reusing the same method signature from the old ILGenerator

        // ## Option 1
        // var ilScope = DynamicILGeneratorScopeField.GetValue(il);
        // var scopeTokens = DynamicScopeTokensField.GetValue(ilScope);
        // var methodSignature = (byte[])DynamicScopeTokensItemGet.Invoke(scopeTokens, [1]); // Stored at the index 1, because a 0 is reserved.
        // var il2 = (ILGenerator)Activator.CreateInstance(DynamicILGeneratorType,
        //     BindingFlags.NonPublic | BindingFlags.Instance,
        //     null,
        //     [dynMethod2, methodSignature, 64], null);
        // IlGeneratorField.SetValue(dynMethod2, il2);

        // ## Option 2
        // var il2 = ConfigureDynamicILGenerator(dynMethod2, il);

        // ## Option 3
        ResetDynamicILGenerator(dynMethod2, il);
        var il2 = il;

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        var func2 = (Func<int, int>)dynMethod2.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        t.AreEqual(83, func2(41));
        t.AreEqual(41, func(41)); // ensure that the first delegate is still working
    }

    public delegate void Action2ndByRef<T>(T arg0, ref T arg1);

    public void TryToReuseIlGenerator_for_any_signature(TestContext t)
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        var func = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        t.AreEqual(41, func(41));

        Type[] paramTypes = [typeof(ExpressionCompiler.ArrayClosure), typeof(int), typeof(int).MakeByRefType()];
        var dynMethod2 = new DynamicMethod(string.Empty,
            typeof(void),
            paramTypes,
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        // Let start with reusing the same method signature from the old ILGenerator
        // var il2 = dynMethod2.GetILGenerator();
        ReuseDynamicILGeneratorOfAnySignature(dynMethod2, il, typeof(void), paramTypes);
        var il2 = il;

        il2.Emit(OpCodes.Ldarg_2);
        il2.Emit(OpCodes.Ldarg_2);
        il2.Emit(OpCodes.Ldind_I4);
        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Stind_I4);
        il2.Emit(OpCodes.Ret);

        var func2 = (Action2ndByRef<int>)dynMethod2.CreateDelegate(typeof(Action2ndByRef<int>), ExpressionCompiler.EmptyArrayClosure);
        var arg = 41;
        func2(42, ref arg);
        t.AreEqual(83, arg);
        t.AreEqual(41, func(41)); // ensure that the first delegate is still working
    }

    internal static ILGenerator pooledILGenerator;

    public static object CreateDynamicILGenerator()
    {
        var paramTypes = ExpressionCompiler.RentOrNewClosureTypeToParamTypes(typeof(int), typeof(int).MakeByRefType());
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(void),
            paramTypes,
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldind_I4);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stind_I4);
        il.Emit(OpCodes.Ret);

        var func = (Action2ndByRef<int>)dynMethod.CreateDelegate(typeof(Action2ndByRef<int>), ExpressionCompiler.EmptyArrayClosure);
        ExpressionCompiler.FreeClosureTypeAndParamTypes(paramTypes);
        return func;
    }

    public static object TryPoolDynamicILGenerator()
    {
        var paramTypes = ExpressionCompiler.RentOrNewClosureTypeToParamTypes(typeof(int), typeof(int).MakeByRefType());
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(void),
            paramTypes,
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = Interlocked.Exchange(ref pooledILGenerator, null);
        if (il != null)
            ReuseDynamicILGeneratorOfAnySignature(dynMethod, il, typeof(void), paramTypes);
        else
            il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldind_I4);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Stind_I4);
        il.Emit(OpCodes.Ret);

        var func = (Action2ndByRef<int>)dynMethod.CreateDelegate(typeof(Action2ndByRef<int>), ExpressionCompiler.EmptyArrayClosure);
        Interlocked.Exchange(ref pooledILGenerator, il);
        ExpressionCompiler.FreeClosureTypeAndParamTypes(paramTypes);
        return func;
    }

    internal static MethodInfo GetMethodSigHelperMethod = typeof(SignatureHelper)
        .GetMethod("GetMethodSigHelper", BindingFlags.Static | BindingFlags.Public, null, [typeof(Module), typeof(Type), typeof(Type[])], null);

    internal static MethodInfo GetSignatureMethod = typeof(SignatureHelper)
        .GetMethod("GetSignature", BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(bool)], null);

    internal static MethodInfo GetTokenForMethod = DynamicILGeneratorScopeField.FieldType
        .GetMethod("GetTokenFor", BindingFlags.Instance | BindingFlags.Public, null, [typeof(byte[])], null);

    internal static FieldInfo MethodSigTokenField = DynamicILGeneratorType.GetField("m_methodSigToken", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static Action<DynamicMethod, ILGenerator, Type, Type[]> ReuseDynamicILGeneratorOfAnyMethodSignature()
    {
        const BindingFlags allDeclared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        var dynMethod = new DynamicMethod(string.Empty,
            typeof(void),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(DynamicMethod), typeof(ILGenerator), typeof(Type), typeof(Type[])],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        var baseFields = DynamicILGeneratorType.BaseType.GetFields(allDeclared);
        foreach (var field in baseFields)
        {
            var fieldName = field.Name;
            if (fieldName == "m_localSignature") // todo: skip, let's see how it works
                continue;

            // m_ScopeTree = new ScopeTree();
            if (fieldName == "m_ScopeTree")
            {
                il.Demit(OpCodes.Ldarg_2);
                il.Demit(OpCodes.Newobj, ScopeTreeCtor);
                il.Demit(OpCodes.Stfld, field);
                continue;
            }

            // m_methodBuilder = method; // dynamicMethod
            if (fieldName == "m_methodBuilder")
            {
                il.Demit(OpCodes.Ldarg_2);
                il.Demit(OpCodes.Ldarg_1);
                il.Demit(OpCodes.Stfld, field);
                continue;
            }

            // instead of m_ILStream = new byte[Math.Max(size, DefaultSize)];
            // let's clear it and reuse the buffer
            if (fieldName == "m_ILStream")
            {
                il.Demit(OpCodes.Ldarg_2);
                il.Demit(OpCodes.Ldfld, field);
                var ilStreamVar = ExpressionCompiler.EmittingVisitor.EmitStoreAndLoadLocalVariable(il, typeof(byte[]));
                il.Demit(OpCodes.Ldc_I4_0);
                ExpressionCompiler.EmittingVisitor.EmitLoadLocalVariable(il, ilStreamVar);
                il.Demit(OpCodes.Ldlen);
                il.Demit(OpCodes.Call, ArrayClearMethod);
                continue;
            }

            il.Demit(OpCodes.Ldarg_2);
            ExpressionCompiler.EmittingVisitor.EmitDefault(il, field.FieldType);
            il.Demit(OpCodes.Stfld, field);
        }

        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Ldfld, DynamicILGeneratorScopeField);
        var scopeVar = ExpressionCompiler.EmittingVisitor.EmitStoreAndLoadLocalVariable(il, DynamicILGeneratorScopeField.FieldType);
        il.Emit(OpCodes.Ldfld, DynamicScopeTokensField);
        il.Emit(OpCodes.Dup);

        // reset its List<T>._size to 1, keep the 0th item
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Stfld, ListOfObjectsSize);

        // set the 0th item to null
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Call, DynamicScopeTokensItem.SetMethod);

        //  byte[] methodSignature =
        //      SignatureHelper.GetMethodSigHelper(Module? mod, Type? returnType, Type[]? parameterTypes).GetSignature(true);
        il.Emit(OpCodes.Ldnull); // for the module
        il.Emit(OpCodes.Ldarg_3); // load return type
        il.Emit(OpCodes.Ldarg_S, 4); // load parameter types arrays
        il.Emit(OpCodes.Call, GetMethodSigHelperMethod);
        il.Emit(OpCodes.Ldc_I4_1); // load true
        il.Emit(OpCodes.Call, GetSignatureMethod);
        var signatureBytesVar = ExpressionCompiler.EmittingVisitor.EmitStoreLocalVariable(il, typeof(byte[])); // todo: perf could reuse byte[]?

        // m_methodSigToken = m_scope.GetTokenFor(methodSignature);
        il.Emit(OpCodes.Ldarg_2);
        ExpressionCompiler.EmittingVisitor.EmitLoadLocalVariable(il, scopeVar);
        ExpressionCompiler.EmittingVisitor.EmitLoadLocalVariable(il, signatureBytesVar);
        il.Emit(OpCodes.Call, GetTokenForMethod);
        il.Emit(OpCodes.Stfld, MethodSigTokenField);

        // store the reused ILGenerator to 
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, IlGeneratorField);

        il.Emit(OpCodes.Ret);

        return (Action<DynamicMethod, ILGenerator, Type, Type[]>)dynMethod.CreateDelegate(
            typeof(Action<DynamicMethod, ILGenerator, Type, Type[]>), ExpressionCompiler.EmptyArrayClosure);
    }

    internal static Action<DynamicMethod, ILGenerator, Type, Type[]> ReuseDynamicILGeneratorOfAnySignature = ReuseDynamicILGeneratorOfAnyMethodSignature();

    internal static Action<DynamicMethod, ILGenerator> ReuseDynamicILGeneratorOfTheSameSignature()
    {
        const BindingFlags allDeclared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var fields = DynamicILGeneratorType.GetFields(allDeclared);
        var baseFields = DynamicILGeneratorType.BaseType.GetFields(allDeclared);

        var dynMethod = new DynamicMethod(string.Empty,
            typeof(void),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(DynamicMethod), typeof(ILGenerator)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();
        foreach (var field in baseFields)
        {
            var fieldName = field.Name;
            if (fieldName == "m_localSignature")
                continue;

            // m_ScopeTree = new ScopeTree();
            if (fieldName == "m_ScopeTree")
            {
                il.Demit(OpCodes.Ldarg_2);
                il.Demit(OpCodes.Newobj, ScopeTreeCtor);
                il.Demit(OpCodes.Stfld, field);
                continue;
            }

            // m_methodBuilder = method; // dynamicMethod
            if (fieldName == "m_methodBuilder")
            {
                il.Demit(OpCodes.Ldarg_2);
                il.Demit(OpCodes.Ldarg_1);
                il.Demit(OpCodes.Stfld, field);
                continue;
            }

            // instead of m_ILStream = new byte[Math.Max(size, DefaultSize)];
            // let's clear it and reuse the buffer
            if (fieldName == "m_ILStream")
            {
                il.Demit(OpCodes.Ldarg_2);
                il.Demit(OpCodes.Ldfld, field);
                var ilStreamVar = ExpressionCompiler.EmittingVisitor.EmitStoreAndLoadLocalVariable(il, typeof(byte[]));
                il.Demit(OpCodes.Ldc_I4_0);
                ExpressionCompiler.EmittingVisitor.EmitLoadLocalVariable(il, ilStreamVar);
                il.Demit(OpCodes.Ldlen);
                il.Demit(OpCodes.Call, ArrayClearMethod);
                continue;
            }

            il.Demit(OpCodes.Ldarg_2);
            ExpressionCompiler.EmittingVisitor.EmitDefault(il, field.FieldType);
            il.Demit(OpCodes.Stfld, field);
        }

        foreach (var field in fields)
        {
            var fieldName = field.Name;
            if (fieldName == "m_methodSigToken")
                continue; // skip a readonly field

            if (fieldName == "m_scope")
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ldfld, DynamicScopeTokensField);
                il.Emit(OpCodes.Dup);

                // reset its List<T>._size to 2
                il.Emit(OpCodes.Ldc_I4_2);
                il.Emit(OpCodes.Stfld, ListOfObjectsSize);

                // clear the 1st item
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Call, DynamicScopeTokensItem.SetMethod);
                continue;
            }

            il.Emit(OpCodes.Ldarg_2);
            ExpressionCompiler.EmittingVisitor.EmitDefault(il, field.FieldType);
            il.Emit(OpCodes.Stfld, field);
        }

        // store the reused ILGenerator to 
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Stfld, IlGeneratorField);

        il.Emit(OpCodes.Ret);

        return (Action<DynamicMethod, ILGenerator>)dynMethod.CreateDelegate(
            typeof(Action<DynamicMethod, ILGenerator>), ExpressionCompiler.EmptyArrayClosure);
    }

    internal static Action<DynamicMethod, ILGenerator> ResetDynamicILGenerator = ReuseDynamicILGeneratorOfTheSameSignature();

    public static object ReuseILGenerator()
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        _ = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);

        var dynMethod2 = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        // Let start from reusing the same method signature from the old ILGenerator
        // var ilScope = DynamicILGeneratorScopeField.GetValue(il);
        // var scopeTokens = DynamicScopeTokensField.GetValue(ilScope);
        // var methodSignature = (byte[])DynamicScopeTokensItemGet.Invoke(scopeTokens, [1]); // Stored at the index 1, because a 0 is reserved.
        // var il2 = (ILGenerator)Activator.CreateInstance(DynamicILGeneratorType,
        //     BindingFlags.NonPublic | BindingFlags.Instance,
        //     null,
        //     [dynMethod2, methodSignature, 64], null);
        // IlGeneratorField.SetValue(dynMethod2, il2);

        // var il2 = ConfigureDynamicILGenerator(dynMethod2, il);

        ResetDynamicILGenerator(dynMethod2, il);
        var il2 = il;

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        return (Func<int, int>)dynMethod2.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
    }

    public static object NoReuseILGenerator()
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        _ = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);

        var dynMethod2 = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il2 = dynMethod2.GetILGenerator();

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        return (Func<int, int>)dynMethod2.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
    }

    internal static FieldInfo MethodHandleField = typeof(DynamicMethod).GetField("_methodHandle", BindingFlags.Instance | BindingFlags.NonPublic);
    internal static MethodInfo GetMethodDescriptorMethod = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic);
    internal static MethodInfo CreateDelegateMethod = typeof(Delegate).GetMethod("CreateDelegateNoSecurityCheck", BindingFlags.Static | BindingFlags.NonPublic);

    void TryToReuseTheDynamicMethod_FailedWithInternalClrError_BecauseTheResultDelegateIsLinkedWithDynamicMethod(TestContext t)
    {
        // Let's say there is a DynamicMethod of Func<int, int> returning its argument, 
        // at the end I want to reuse the created DynamicMethod for another Func<int, int> returning arg + 42

        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        var runtimeMethodHandle = GetMethodDescriptorMethod.Invoke(dynMethod, null);
        var func = (Func<int, int>)CreateDelegateMethod.Invoke(null, [typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure, runtimeMethodHandle]);

        // var func = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        t.AreEqual(41, func(41));

        // Debug.WriteLine($"Field_initLocals: {Field_initLocals.GetValue(dynMethod)}");
        IlGeneratorField.SetValue(dynMethod, null);

        // required: Keeps the old delegate if not set to null
        MethodHandleField.SetValue(dynMethod, null);

        var il2 = dynMethod.GetILGenerator();

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        t.AreEqual(41, func(41));

        // CreateDelegate does:
        // RuntimeMethodHandle runtimeMethodHandle = GetMethodDescriptor();
        // MulticastDelegate d = (MulticastDelegate)Delegate.CreateDelegateNoSecurityCheck(delegateType, target, runtimeMethodHandle);
        // // stash this MethodInfo by brute force.
        // d.StoreDynamicMethod(this);
        // return d;

        // todo: @wip Calling this second time in `-c:Release` will produce `Fatal error. Internal CLR error. (0x80131506) ## :-( Failed with ERROR: -1073741819` (the -c:Debug is working fine)
        // runtimeMethodHandle = GetMethodDescriptorMethod.Invoke(dynMethod, null);

        // var func2 = (Func<int, int>)CreateDelegateMethod.Invoke(null, [typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure, runtimeMethodHandle]);

        // var func2 = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
        // t.AreEqual(83, func2(41));
    }

    public static object ReuseDynamicMethod()
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        _ = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);

        IlGeneratorField.SetValue(dynMethod, null);
        // Field_initLocals.SetValue(dynMethod, true);

        var il2 = dynMethod.GetILGenerator();

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        return (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
    }

    public static object NoReuseDynamicMethod()
    {
        var dynMethod = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ret);

        _ = (Func<int, int>)dynMethod.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);

        var dynMethod2 = new DynamicMethod(string.Empty,
            typeof(int),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(int)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il2 = dynMethod2.GetILGenerator();

        il2.Emit(OpCodes.Ldarg_1);
        il2.Emit(OpCodes.Ldc_I4, 42);
        il2.Emit(OpCodes.Add);
        il2.Emit(OpCodes.Ret);

        return (Func<int, int>)dynMethod2.CreateDelegate(typeof(Func<int, int>), ExpressionCompiler.EmptyArrayClosure);
    }

    // internal static FieldInfo Field_initLocals = typeof(DynamicMethod).GetField("_initLocals", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static Action<DynamicMethod> GetDynamicMethodResetDelegate()
    {
        // Reset the DynamicMethod internals to enable its reuse
        // _ilGenerator = null;
        // _initLocals = true;
        // _methodHandle = null; // but this reset breaks the CLR

        var dynMethod = new DynamicMethod(string.Empty,
            typeof(void),
            [typeof(ExpressionCompiler.ArrayClosure), typeof(DynamicMethod)],
            typeof(ExpressionCompiler.ArrayClosure),
            true);

        var il = dynMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldnull);
        il.Emit(OpCodes.Stfld, IlGeneratorField);
        // il.Emit(OpCodes.Ldarg_1);
        // il.Emit(OpCodes.Ldc_I4_1);
        // il.Emit(OpCodes.Stfld, Field_initLocals);
        il.Emit(OpCodes.Ret);

        return (Action<DynamicMethod>)dynMethod.CreateDelegate(typeof(Action<DynamicMethod>), ExpressionCompiler.EmptyArrayClosure);
    }

    internal static Lazy<Action<DynamicMethod>> ResetDynamicMethod = new(GetDynamicMethodResetDelegate);
}