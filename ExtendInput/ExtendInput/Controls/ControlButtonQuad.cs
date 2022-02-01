using System;

namespace ExtendInput.Controls
{
    public interface IControlButtonQuad : IControl
    {
        bool ButtonN { get; set; }
        bool ButtonE { get; set; }
        bool ButtonS { get; set; }
        bool ButtonW { get; set; }
    }
    public class ControlButtonQuad : IControlButtonQuad
    {
        public bool ButtonN { get; set; }
        public bool ButtonE { get; set; }
        public bool ButtonS { get; set; }
        public bool ButtonW { get; set; }

        public ControlButtonQuad()
        {
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "n":
                    return (T)Convert.ChangeType(ButtonN, typeof(T));
                case "e":
                    return (T)Convert.ChangeType(ButtonE, typeof(T));
                case "s":
                    return (T)Convert.ChangeType(ButtonS, typeof(T));
                case "w":
                    return (T)Convert.ChangeType(ButtonW, typeof(T));
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
            ControlButtonQuad newData = new ControlButtonQuad();

            newData.ButtonN = this.ButtonN;
            newData.ButtonE = this.ButtonE;
            newData.ButtonS = this.ButtonS;
            newData.ButtonW = this.ButtonW;

            return newData;
        }
    }
}
