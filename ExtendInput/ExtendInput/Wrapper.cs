namespace ExtendInput
{
    internal class Wrapper<T>
    {
        public T Value { get; set; }
        public Wrapper(T Value)
        {
            this.Value = Value;
        }
    }
}