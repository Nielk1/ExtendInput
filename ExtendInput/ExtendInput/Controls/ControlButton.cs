using System;

namespace ExtendInput.Controls
{
    public class ControlButton : IControl
    {
        public bool Button0 { get; private set; }
        public bool? PendingButton0 { get; set; }
        public T Value<T>(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "click")
                return (T)Convert.ChangeType(Button0, typeof(T));
            return default;
        }
        public Type Type(string key)
        {
            return typeof(bool);
        }

        public object Clone()
        {
            ControlButton newData = new ControlButton();

            newData.Button0 = this.Button0;

            return newData;
        }

        public void ProcessPendingInputs()
        {
            Button0 = PendingButton0 ?? Button0;
            PendingButton0 = null;
        }
    }
}
