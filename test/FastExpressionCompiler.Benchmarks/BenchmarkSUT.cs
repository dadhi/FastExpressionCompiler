namespace FastExpressionCompiler.Benchmarks
{
    public class A { }

    public class B { }

    public class X
    {
        public A A { get; }
        public B B { get; }

        public X(A a, B b)
        {
            A = a;
            B = b;
        }
    }
}
