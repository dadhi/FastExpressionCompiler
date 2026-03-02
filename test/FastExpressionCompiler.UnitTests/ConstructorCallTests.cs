
using System;
using System.Reflection;

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.UnitTests;
#else
using static System.Linq.Expressions.Expression;
namespace FastExpressionCompiler.UnitTests;
#endif

public class ConstructorCallTests : ITest
{
    public int Run()
    {
        Constructor_ignored_result(TestClass.Ctor);
        Constructor_ignored_result(TestStruct.Ctor);

        Constructor_read_struct_property(TestClass.Ctor, "A");
        Constructor_read_struct_property(TestClass.Ctor, "PropertyA");
        Constructor_read_struct_property(TestStruct.Ctor, "A");
        Constructor_read_struct_property(TestStruct.Ctor, "PropertyA");

        Constructor_read_reference_property(TestClass.Ctor, "B");
        Constructor_read_reference_property(TestClass.Ctor, "PropertyB");
        Constructor_read_reference_property(TestStruct.Ctor, "B");
        Constructor_read_reference_property(TestStruct.Ctor, "PropertyB");

        Constructor_assign_ignore_property(TestClass.Ctor);
        Constructor_assign_ignore_property(TestStruct.Ctor);
        Constructor_assign_read_property(TestClass.Ctor);
        Constructor_assign_read_property(TestStruct.Ctor);
        Constructor_assign_ignore_property_struct_parameter();

        Read_field_in_ctor_param();

        Nested_constructor_calls();
        Condition_in_constructor_arguments();
        TryCatch_in_constructor_arguments();
        Constructor_in_array_index();
        Constructor_in_array_index_argument();
        Constructor_in_custom_indexer_argument();
        Constructor_in_indexer_target_struct(TestClass.Ctor);
        Constructor_in_indexer_target_struct(TestStruct.Ctor);
        Nullable_constructor_and_conversion();
        Ctor_in_instance_method_this(TestClass.Ctor);
        Ctor_in_instance_method_this(TestStruct.Ctor);
        Ctor_in_static_method_arg(TestClass.Ctor);
        Ctor_in_static_method_arg(TestStruct.Ctor);
        Constructor_nested_in_block_member_access();
        Constructor_conversions_and_boxing();

        return 32;
    }

    public void Constructor_conversions_and_boxing()
    {
        var fBox = Lambda<Func<object>>(
            Convert(New(TestStruct.Ctor, Constant(42), Constant("a")), typeof(object))
        ).CompileFast(true);
        Asserts.AreEqual(42, ((TestStruct)fBox()).A);

        var fInterface = Lambda<Func<ITestInterfacce>>(
            Convert(New(TestStruct.Ctor, Constant(43), Constant("a")), typeof(ITestInterfacce))
        ).CompileFast(true);
        Asserts.AreEqual(43, fInterface().PropertyA);

        var fImplicit = Lambda<Func<int>>(
            Convert(New(TestStruct.Ctor, Constant(44), Constant("a")), typeof(int))
        ).CompileFast(true);
        Asserts.AreEqual(44, fImplicit());

        var fClassBox = Lambda<Func<object>>(
            Convert(New(TestClass.Ctor, Constant(45), Constant("a")), typeof(object))
        ).CompileFast(true);
        Asserts.AreEqual(45, ((TestClass)fClassBox()).A);
    }

    public void Constructor_ignored_result(ConstructorInfo ctor)
    {
        var f = Lambda<Action>(New(ctor, Constant(++Ctr), Constant("abc"))).CompileFast(true);

        Asserts.IsNotNull(f);
        f();

        Asserts.AreEqual(LastA, Ctr);
    }

    public void Constructor_read_struct_property(ConstructorInfo ctor, string prop)
    {
        var f = Lambda<Func<int>>(
            PropertyOrField(New(ctor, Constant(++Ctr), Constant("abc")), prop)
        ).CompileFast(true);

        Asserts.IsNotNull(f);

        Asserts.AreEqual(Ctr, f());
        Asserts.AreEqual(LastA, Ctr);
    }

    public void Constructor_read_reference_property(ConstructorInfo ctor, string prop)
    {
        var param = Parameter(typeof(string));
        var f = Lambda<Func<string, string>>(
            PropertyOrField(New(ctor, Constant(0), param), prop),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);

        Asserts.AreEqual("test1", f("test1"));
        Asserts.AreEqual("test2", f("test2"));
    }

    public void Constructor_assign_ignore_property(ConstructorInfo ctor)
    {
        var param = Parameter(typeof(int));
        var f = Lambda<Action<int>>(
            Assign(Property(New(ctor, Constant(0), Constant("test")), "PropertyA"), param),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);

        f(44);
        Asserts.AreEqual(44, LastA);
        f(45);
        Asserts.AreEqual(45, LastA);
    }

    public void Constructor_assign_read_property(ConstructorInfo ctor)
    {
        var param = Parameter(typeof(int));
        var f = Lambda<Func<int, int>>(
            Assign(Property(New(ctor, Constant(0), Constant("test")), "PropertyA"), param),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);

        Asserts.AreEqual(46, f(46));
        Asserts.AreEqual(46, LastA);
        Asserts.AreEqual(47, f(47));
        Asserts.AreEqual(47, LastA);
    }

    public void Constructor_assign_ignore_property_struct_parameter()
    {
        var structParam = Parameter(typeof(TestStruct));
        var valueParam = Parameter(typeof(int));

        var f = Lambda<Action<TestStruct, int>>(
            Assign(Property(structParam, "PropertyA"), valueParam),
            structParam, valueParam
        ).CompileFast(true);

        Asserts.IsNotNull(f);

        var s = new TestStruct(0, "x");
        f(s, 44);
        Asserts.AreEqual(44, LastA);
        f(s, 45);
        Asserts.AreEqual(45, LastA);
    }

    public TestStruct fieldStruct;
    public void Read_field_in_ctor_param()
    {
        var param = Parameter(typeof(ConstructorCallTests));
        var f = Lambda<Func<ConstructorCallTests, TestClass>>(
            New(TestClass.Ctor, Field(Field(param, nameof(fieldStruct)), "A"), Field(Field(param, nameof(fieldStruct)), "B")),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);

        this.fieldStruct = new(12333, "1");
        var result = f(this);
        Asserts.AreEqual(12333, result.A);
        Asserts.AreEqual("1", result.B);
    }

    public void Nested_constructor_calls()
    {
        var f = Lambda<Func<TestClass>>(
            New(TestClass.Ctor, 
                PropertyOrField(New(TestClass.Ctor, Constant(100), Constant("inner")), "A"),
                Constant("outer"))
        ).CompileFast(true);

        Asserts.IsNotNull(f);
        var result = f();
        Asserts.AreEqual(100, result.A);
        Asserts.AreEqual("outer", result.B);
    }

    public void Condition_in_constructor_arguments()
    {
        var param = Parameter(typeof(bool));
        var f = Lambda<Func<bool, TestClass>>(
            New(TestClass.Ctor,
                Condition(param, Constant(42), Constant(99)),
                Condition(param, Constant("a"), Constant("b"))),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);

        Asserts.AreEqual(42, f(true).A);
        Asserts.AreEqual("a", f(true).B);
        Asserts.AreEqual(99, f(false).A);
        Asserts.AreEqual("b", f(false).B);
    }

    public void TryCatch_in_constructor_arguments()
    {
        var param = Parameter(typeof(int));
        var exVar = Parameter(typeof(Exception));
        
        var f = Lambda<Func<int, TestClass>>(
            New(TestClass.Ctor,
                TryCatch(
                    Divide(Constant(123), param),
                    Catch(exVar, Constant(456))
                ),
                TryCatch(
                    Call(Divide(Constant(100), param), "ToString", Type.EmptyTypes),
                    Catch(exVar, Constant("caught"))
                )),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);
        
        var resultNoException = f(1);
        Asserts.AreEqual(123, resultNoException.A);
        Asserts.AreEqual("100", resultNoException.B);
        
        var resultWithException = f(0);
        Asserts.AreEqual(456, resultWithException.A);
        Asserts.AreEqual("caught", resultWithException.B);
    }

    public void Constructor_in_array_index()
    {
        TestClass[] array = [ new(0, "zero"), new(1, "one"), new(2, "two") ];
        
        var f = Lambda<Func<TestClass>>(
            ArrayIndex(
                Constant(array),
                PropertyOrField(New(TestClass.Ctor, Constant(1), Constant("index")), "A"))
        ).CompileFast(true);

        Asserts.IsNotNull(f);
        var result = f();
        
        Asserts.AreEqual(1, result.A);
        Asserts.AreEqual("one", result.B);
    }

    public void Constructor_in_array_index_argument()
    {
        TestClass[] array = [ new(0, "zero"), new(1, "one"), new(2, "two") ];

        var intPtrCtor = typeof(IntPtr).GetConstructor([typeof(int)])!;

        var indexExpr = Convert(New(intPtrCtor, Constant(0)), typeof(int));
        var f = Lambda<Func<TestClass>>(
            ArrayIndex(
                Constant(array),
                indexExpr)
        ).CompileFast(true);

        Asserts.IsNotNull(f);
        var result = f();

        Asserts.AreEqual(0, result.A);
        Asserts.AreEqual("zero", result.B);
    }

    public void Constructor_in_custom_indexer_argument()
    {
        var intPtrCtor = typeof(IntPtr).GetConstructor([typeof(int)])!;

        var indexer = typeof(TestClass).GetProperty("Item", [typeof(IntPtr)])!;
        var p = Parameter(typeof(TestClass));
        var f = Lambda<Func<TestClass, int>>(
            MakeIndex(p, indexer, [New(intPtrCtor, Constant(2))]),
            p
        ).CompileFast(true);

        Asserts.AreEqual(42, f(new(40, "x")));
    }

    public void Constructor_in_indexer_target_struct(ConstructorInfo ctor)
    {
        var indexer = ctor.DeclaringType!.GetProperty("Item", [typeof(nint)])!;

        var f = Lambda<Func<int>>(
            MakeIndex(
                New(ctor, Constant(40), Constant("x")),
                indexer,
                [Constant(new IntPtr(2))])
        ).CompileFast(true);

        Asserts.AreEqual(42, f());
    }

    public void Nullable_constructor_and_conversion()
    {
        var ctor = typeof(int?).GetConstructor([typeof(int)]);

        var fConvert = Lambda<Func<int>>(
            Convert(New(ctor, Constant(42)), typeof(int))
        ).CompileFast(true);

        Asserts.IsNotNull(fConvert);
        Asserts.AreEqual(42, fConvert());

        var fValue = Lambda<Func<int>>(
            Property(New(ctor, Constant(43)), "Value")
        ).CompileFast(true);

        Asserts.IsNotNull(fValue);
        Asserts.AreEqual(43, fValue());
    }

    private static void Ctor_in_instance_method_this(ConstructorInfo ctor)
    {
        var type = ctor.DeclaringType;

        var param = Parameter(typeof(int));
        var f = Lambda<Func<int, int>>(
            Call(New(ctor, Constant(10), Constant("arg")), "InstanceAdd", [], param),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);
        Asserts.AreEqual(15, f(5));
        Asserts.AreEqual(25, f(15));
    }

    private static void Ctor_in_static_method_arg(ConstructorInfo ctor)
    {
        var param = Parameter(typeof(int));
        var f = Lambda<Func<int, int>>(
            Call(ctor.DeclaringType, "StaticAdd", [], New(ctor, Constant(10), Constant("arg")), param),
            param
        ).CompileFast(true);

        Asserts.IsNotNull(f);
        Asserts.AreEqual(15, f(5));
        Asserts.AreEqual(25, f(15));
    }

    [ThreadStatic]
    static int Ctr = 1;

    [ThreadStatic]
    static int LastA;
    [ThreadStatic]
    static string LastB;

    public void Constructor_nested_in_block_member_access()
    {
        var block = Block(
            New(typeof(TestStruct)),
            Constant(new TestStruct(42, "s"))
        );

        var expr = Property(block, nameof(TestStruct.PropertyA));

        var lambda = Lambda<Func<int>>(expr);

        var fastCompiled = lambda.CompileFast(true);
        Asserts.AreEqual(42, fastCompiled());
    }

    public interface ITestInterfacce { int PropertyA { get; } }

    public struct TestStruct : ITestInterfacce
    {
        public static readonly ConstructorInfo Ctor = typeof(TestStruct).GetConstructors()[0];
        public int A;

        public string B;
        public int PropertyA
        {
            get => A;
            set => LastA = A = value;
        }
        public string PropertyB => B;
        public TestStruct(int a, string b)
        {
            LastA = this.A = a;
            LastB = this.B = b;
        }

        public int this[nint delta] => A + (int)delta;

        public int InstanceAdd(int value) => A + value;

        public static int StaticAdd(TestStruct s, int value) => s.A + value;

        public static implicit operator int(TestStruct s) => s.A;
    }

    public class TestClass : ITestInterfacce
    {
        public static readonly ConstructorInfo Ctor = typeof(TestClass).GetConstructors()[0];
        public int A;

        public string B;
        public int PropertyA
        {
            get => A;
            set => LastA = A = value;
        }
        public string PropertyB => B;
        public TestClass(int a, string b)
        {
            LastA = this.A = a;
            LastB = this.B = b;
        }

        public int this[nint offset] => A + (int)offset;

        public int InstanceAdd(int value) => A + value;

        public static int StaticAdd(TestClass c, int value) => c.A + value;
    }

}
