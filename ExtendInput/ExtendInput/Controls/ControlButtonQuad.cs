using System;

namespace ExtendInput.Controls
{
    public class ControlButtonQuad : IControl
    {
        public bool ButtonN { get; private set; }
        public bool ButtonE { get; private set; }
        public bool ButtonS { get; private set; }
        public bool ButtonW { get; private set; }

        public bool? PendingButtonN { get; set; }
        public bool? PendingButtonE { get; set; }
        public bool? PendingButtonS { get; set; }
        public bool? PendingButtonW { get; set; }

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

        public void ProcessPendingInputs()
        {
            ButtonN = PendingButtonN ?? ButtonN;
            ButtonE = PendingButtonE ?? ButtonE;
            ButtonS = PendingButtonS ?? ButtonS;
            ButtonW = PendingButtonW ?? ButtonW;

            PendingButtonN = null;
            PendingButtonE = null;
            PendingButtonS = null;
            PendingButtonW = null;
        }
    }
}
