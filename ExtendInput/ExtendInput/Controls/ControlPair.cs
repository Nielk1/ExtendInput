using System;

namespace ExtendInput.Controls
{
    public class ControlPair<CT> : IControl, IControlPair<CT> where CT : IControl
    {
        public CT Left { get; private set; }
        public CT Right { get; private set; }

        public ControlPair(CT Left, CT Right)
        {
            this.Left = Left;
            this.Right = Right;
        }

        public T Value<T>(string key)
        {
            string[] split = key.Split(new char[] { ':' }, 2);
            switch (split[0])
            {
                case "l":
                    return Left.Value<T>(split.Length > 1 ? split[1] : string.Empty);
                case "r":
                    return Right.Value<T>(split.Length > 1 ? split[1] : string.Empty);
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            //return typeof(bool);
            string[] split = key.Split(new char[] { ':' }, 2);
            switch (split[0])
            {
                case "l":
                    return Left.Type(split.Length > 1 ? split[1] : string.Empty);
                case "r":
                    return Right.Type(split.Length > 1 ? split[1] : string.Empty);
                default:
                    return default;
            }
        }

        public object Clone()
        {
            return new ControlPair<CT>(
                (CT)this.Left.Clone(),
                (CT)this.Right.Clone());
        }
    }
}
