using System;

namespace ExtendInput.Controls
{
    public class ControlButton : IControl
    {
        public bool Button0 { get; set; }
        public T Value<T>(string key)
        {
            return (T)Convert.ChangeType(Button0, typeof(T));
            //return default;
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
    }
}
