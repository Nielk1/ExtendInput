using System;

namespace ExtendInput.Controls
{
    public class ControlStick : IControl
    {
        public bool HasClick { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public bool Click { get; private set; }

        public float? PendingX { get; set; }
        public float? PendingY { get; set; }
        public bool? PendingClick { get; set; }

        public ControlStick(bool HasClick)
        {
            this.HasClick = HasClick;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "x":
                    return (T)Convert.ChangeType(X, typeof(T));
                case "y":
                    return (T)Convert.ChangeType(Y, typeof(T));
                case "click":
                    return (T)Convert.ChangeType(Click, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            switch (key)
            {
                case "x":
                    return typeof(float);
                case "y":
                    return typeof(float);
                case "click":
                    return typeof(bool);
                default:
                    return default;
            }
        }

        public object Clone()
        {
            ControlStick newData = new ControlStick(this.HasClick);

            newData.X = this.X;
            newData.Y = this.Y;
            newData.Click = this.Click;

            return newData;
        }

        public void ProcessPendingInputs()
        {
            X = PendingX ?? X;
            Y = PendingY ?? Y;
            Click = PendingClick ?? Click;

            PendingX = null;
            PendingY = null;
            PendingClick = null;
        }
    }
}
