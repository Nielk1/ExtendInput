using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
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

    [GenericControl("Button")]
    public class ControlButton : IControlButton, IGenericControl
    {
        public bool DigitalStage1 { get; set; }

        public ControlButton() { }


        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlButton(string factoryName, AddressableValue[] addressableValues)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
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

        public void SetGenericValue(IReport report)
        {
            DigitalStage1 = addressableValues[0].GetBoolean(report) ?? DigitalStage1;
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}