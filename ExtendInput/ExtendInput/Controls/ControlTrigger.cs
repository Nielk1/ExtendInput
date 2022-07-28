using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
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
    [GenericControl("Trigger")]
    public class ControlTrigger : IControlTrigger, IGenericControl
    {
        public float AnalogStage1 { get; set; }

        public ControlTrigger() { }


        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlTrigger(string factoryName, AddressableValue[] addressableValues)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
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

        public void SetGenericValue(IReport report)
        {
            AnalogStage1 = addressableValues[0].GetFloat(report) ?? AnalogStage1;
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            return false;
        }
    }
}