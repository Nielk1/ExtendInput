using System;

namespace ExtendInput.Controls
{
    public interface IControlStick : IControl
    {
        float X { get; set; }
        float Y { get; set; }
    }
    public class ControlStick : IControlStick
    {
        public float X { get; set; }
        public float Y { get; set; }

        public ControlStick() { }
        
        public virtual T Value<T>(string key)
        {
            switch (key)
            {
                case "x":
                    return (T)Convert.ChangeType(X, typeof(T));
                case "y":
                    return (T)Convert.ChangeType(Y, typeof(T));
                default:
                    return default;
            }
        }
        public virtual Type Type(string key)
        {
            switch (key)
            {
                case "x":
                    return typeof(float);
                case "y":
                    return typeof(float);
                default:
                    return default;
            }
        }

        public virtual object Clone()
        {
            ControlStick newData = new ControlStick();

            newData.X = this.X;
            newData.Y = this.Y;

            return newData;
        }
    }
}
