using System;

namespace ExtendInput.Controls
{
    public class ControlButtonPair : IControl, IControlPair<ControlButton>
    {
        public ControlButton Left { get; private set; }
        public ControlButton Right { get; private set; }

        public ControlButtonPair(ButtonProperties Properties)
        {
            Left = new ControlButton(Properties);
            Right = new ControlButton(Properties);
        }
        public ControlButtonPair(ControlButton Left, ControlButton Right)
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
            ControlButtonPair newData = new ControlButtonPair(
                (ControlButton)this.Left.Clone(),
                (ControlButton)this.Right.Clone());

            return newData;
        }
    }
}
