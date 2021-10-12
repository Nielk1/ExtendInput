using System;

namespace ExtendInput.Controls
{
    public class ControlButtonPair : IControl, IControlPair<ControlButton>
    {
        public bool HasStage2 { get; private set; }
        public ControlButton Left { get; private set; }
        public ControlButton Right { get; private set; }

        public ControlButtonPair(bool HasStage2 = false)
        {
            this.HasStage2 = HasStage2;
            Left = new ControlButton(); // TODO introduce stage 2 here
            Right = new ControlButton(); // TODO introduce stage 2 here
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
            ControlButtonPair newData = new ControlButtonPair(this.HasStage2);

            newData.Left = (ControlButton)this.Left.Clone();
            newData.Right = (ControlButton)this.Right.Clone();

            return newData;
        }
    }
}
