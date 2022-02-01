using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlButton : IControl
    {
        bool DigitalStage1 { get; set; }

    }
    public class ControlButton : IControlButton
    {
        public bool DigitalStage1 { get; set; }

        public ControlButton()
        {
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "":
                    return (T)Convert.ChangeType(DigitalStage1, typeof(T));
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
            ControlButton newData = new ControlButton();

            newData.DigitalStage1 = this.DigitalStage1;

            return newData;
        }
    }
}