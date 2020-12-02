using System;

namespace ExtendInput.Controls
{
    public class ControlButtonPair : IControl, IControlPair<ControlButton>
    {
        public ControlButton Left { get; private set; }
        public ControlButton Right { get; private set; }

        public ControlButtonPair()
        {
            Left = new ControlButton();
            Right = new ControlButton();
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "l":
                    return Left.Value<T>(string.Empty);
                case "r":
                    return Right.Value<T>(string.Empty);
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            return typeof(bool);
        }

        public object Clone()
        {
            ControlButtonPair newData = new ControlButtonPair();

            newData.Left = (ControlButton)this.Left.Clone();
            newData.Right = (ControlButton)this.Right.Clone();

            return newData;
        }

        public void ProcessPendingInputs()
        {
            Left.ProcessPendingInputs();
            Right.ProcessPendingInputs();
        }
    }
}
