using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlTrigger2Stage : IControl
    {
        float AnalogStage1 { get; set; }
        bool DigitalStage2 { get; set; }

    }
    public class ControlTrigger2Stage : ControlTrigger, IControlTrigger2Stage, IControlTrigger
    {
        public bool DigitalStage2 { get; set; }

        public ControlTrigger2Stage()
        {
        }

        public override T Value<T>(string key)
        {
            switch (key)
            {
                case "stage2":
                    return (T)Convert.ChangeType(DigitalStage2, typeof(T));
                default:
                    return base.Value<T>(key);
            }
        }
        public override Type Type(string key)
        {
            switch (key)
            {
                case "stage2":
                    return typeof(bool);
                default:
                    return base.Type(key);
            }
        }

        public override object Clone()
        {
            ControlTrigger2Stage newData = new ControlTrigger2Stage();

            newData.AnalogStage1 = this.AnalogStage1;
            newData.DigitalStage2 = this.DigitalStage2;

            return newData;
        }
    }
}