using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlTrigger : IControl
    {
        float AnalogStage1 { get; set; }

    }
    public class ControlTrigger : IControlTrigger
    {
        public float AnalogStage1 { get; set; }

        public ControlTrigger()
        {
        }

        public virtual T Value<T>(string key)
        {
            switch (key)
            {
                case "":
                    return (T)Convert.ChangeType(AnalogStage1, typeof(T));
                default:
                    return default;
            }
        }
        public virtual Type Type(string key)
        {
            return typeof(float);
        }

        public virtual object Clone()
        {
            ControlTrigger newData = new ControlTrigger();

            newData.AnalogStage1 = this.AnalogStage1;

            return newData;
        }
    }
}