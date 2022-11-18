using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IButtonPressure : IControl
    {
        float AnalogStage1 { get; set; }

    }
    [GenericControl("ButtonPressure")]
    public class ControlButtonPressure : IButtonPressure, IGenericControl
    {
        public bool DigitalStage1 { get; set; }
        public float AnalogStage1 { get; set; }

        public ControlButtonPressure() { }


        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlButtonPressure(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
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
            ControlButtonPressure newData = new ControlButtonPressure();

            newData.DigitalStage1 = this.DigitalStage1;
            newData.AnalogStage1 = this.AnalogStage1;

            return newData;
        }

        public void ProcessReportForGenericController(IReport report)
        {
            DigitalStage1 = addressableValues[0].GetBoolean(report) ?? DigitalStage1;
            AnalogStage1 = addressableValues[1].GetFloat(report) ?? AnalogStage1;

            if (AnalogStage1 == 0)
                AnalogStage1 = DigitalStage1 ? 1.0f : 0f; // if analog is off, try to supply it via digital

            if (!DigitalStage1)
                DigitalStage1 = AnalogStage1 > 0; // if digital is off, check analog is 0
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }
        public bool IsReadDirty => false;
        public void CleanReadDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}