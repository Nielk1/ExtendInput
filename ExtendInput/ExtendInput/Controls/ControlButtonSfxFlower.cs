using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;

namespace ExtendInput.Controls
{
    public interface IControlButtonSfxFlower : IControl
    {
        bool ButtonN { get; set; }
        bool ButtonNE { get; set; }
        bool ButtonE { get; set; }
        bool ButtonSE { get; set; }
        bool ButtonS { get; set; }
        bool ButtonSW { get; set; }
        bool ButtonW { get; set; }
        bool ButtonNW { get; set; }
    }
    [GenericControl("ButtonSfxFlower")]
    public class ControlControlButtonSfxFlower : IControlButtonSfxFlower, IControlButtonQuad, IGenericControl
    {
        public bool ButtonN { get; set; }
        public bool ButtonNE { get; set; }
        public bool ButtonE { get; set; }
        public bool ButtonSE { get; set; }
        public bool ButtonS { get; set; }
        public bool ButtonSW { get; set; }
        public bool ButtonW { get; set; }
        public bool ButtonNW { get; set; }

        public ControlControlButtonSfxFlower() { }



        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlControlButtonSfxFlower(string factoryName, AddressableValue[] addressableValues)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "n":  return (T)Convert.ChangeType(ButtonN,  typeof(T));
                case "ne": return (T)Convert.ChangeType(ButtonNE, typeof(T));
                case "e":  return (T)Convert.ChangeType(ButtonE,  typeof(T));
                case "se": return (T)Convert.ChangeType(ButtonSE, typeof(T));
                case "s":  return (T)Convert.ChangeType(ButtonS,  typeof(T));
                case "sw": return (T)Convert.ChangeType(ButtonSW, typeof(T));
                case "w":  return (T)Convert.ChangeType(ButtonW,  typeof(T));
                case "nw": return (T)Convert.ChangeType(ButtonNW, typeof(T));
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
            ControlControlButtonSfxFlower newData = new ControlControlButtonSfxFlower();

            newData.ButtonN  = this.ButtonN;
            newData.ButtonNE = this.ButtonNE;
            newData.ButtonE  = this.ButtonE;
            newData.ButtonSE = this.ButtonSE;
            newData.ButtonS  = this.ButtonS;
            newData.ButtonSW = this.ButtonSW;
            newData.ButtonW  = this.ButtonW;
            newData.ButtonNW = this.ButtonNW;

            return newData;
        }

        public void SetGenericValue(IReport report)
        {
            ButtonN  = addressableValues[0].GetBoolean(report) ?? ButtonN;
            ButtonNE = addressableValues[1].GetBoolean(report) ?? ButtonNE;
            ButtonE  = addressableValues[2].GetBoolean(report) ?? ButtonE;
            ButtonSE = addressableValues[3].GetBoolean(report) ?? ButtonSE;
            ButtonS  = addressableValues[4].GetBoolean(report) ?? ButtonS;
            ButtonSW = addressableValues[5].GetBoolean(report) ?? ButtonSW;
            ButtonW  = addressableValues[6].GetBoolean(report) ?? ButtonW;
            ButtonNW = addressableValues[7].GetBoolean(report) ?? ButtonNW;
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }
    }
}
