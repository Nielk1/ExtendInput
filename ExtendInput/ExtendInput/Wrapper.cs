namespace ExtendInput
{
    internal class Wrapper<T>
    {
        public T Value { get; set; }
        public Wrapper(T Value)
        {
            this.Value = Value;
        }

        public static implicit operator T(Wrapper<T> d) => d.Value;
        public static implicit operator Wrapper<T>(T b) => new Wrapper<T>(b);
    }
}