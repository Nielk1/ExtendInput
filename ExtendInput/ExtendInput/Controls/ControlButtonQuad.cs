using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
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
    [GenericControl("ButtonQuad")]
    public class ControlButtonQuad : IControlButtonQuad, IGenericControl
    {
        public bool ButtonN { get; set; }
        public bool ButtonE { get; set; }
        public bool ButtonS { get; set; }
        public bool ButtonW { get; set; }

        public ControlButtonQuad() { }



        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlButtonQuad(string factoryName, AddressableValue[] addressableValues)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
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

        public void SetGenericValue(IReport report)
        {
            ButtonN = addressableValues[0].GetBoolean(report) ?? ButtonN;
            ButtonE = addressableValues[1].GetBoolean(report) ?? ButtonE;
            ButtonS = addressableValues[2].GetBoolean(report) ?? ButtonS;
            ButtonW = addressableValues[3].GetBoolean(report) ?? ButtonW;
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}
