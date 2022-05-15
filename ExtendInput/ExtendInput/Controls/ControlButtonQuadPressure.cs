using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;

namespace ExtendInput.Controls
{
    public interface IControlButtonQuadPressure : IControl
    {
        bool ButtonN { get; set; }
        float AButtonN { get; set; }
        bool ButtonE { get; set; }
        float AButtonE { get; set; }
        bool ButtonS { get; set; }
        float AButtonS { get; set; }
        bool ButtonW { get; set; }
        float AButtonW { get; set; }
    }
    [GenericControl("ButtonQuadPressure")]
    public class ControlButtonQuadPressure : IControlButtonQuad, IGenericControl
    {
        public bool ButtonN { get; set; }
        public float AButtonN { get; set; }
        public bool ButtonE { get; set; }
        public float AButtonE { get; set; }
        public bool ButtonS { get; set; }
        public float AButtonS { get; set; }
        public bool ButtonW { get; set; }
        public float AButtonW { get; set; }

        public ControlButtonQuadPressure() { }



        private AddressableValue[] addressableValues;
        public ControlButtonQuadPressure(AddressableValue[] addressableValues)
        {
            this.addressableValues = addressableValues;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "n":
                    return (T)Convert.ChangeType(AButtonN, typeof(T));
                case "e":
                    return (T)Convert.ChangeType(AButtonE, typeof(T));
                case "s":
                    return (T)Convert.ChangeType(AButtonS, typeof(T));
                case "w":
                    return (T)Convert.ChangeType(AButtonW, typeof(T));
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
            ControlButtonQuadPressure newData = new ControlButtonQuadPressure();

            newData.ButtonN = this.ButtonN;
            newData.ButtonE = this.ButtonE;
            newData.ButtonS = this.ButtonS;
            newData.ButtonW = this.ButtonW;
            newData.AButtonN = this.AButtonN;
            newData.AButtonE = this.AButtonE;
            newData.AButtonS = this.AButtonS;
            newData.AButtonW = this.AButtonW;

            return newData;
        }

        public void SetGenericValue(IReport report)
        {
            ButtonN = addressableValues[0].GetBoolean(report) ?? ButtonN;
            ButtonE = addressableValues[1].GetBoolean(report) ?? ButtonE;
            ButtonS = addressableValues[2].GetBoolean(report) ?? ButtonS;
            ButtonW = addressableValues[3].GetBoolean(report) ?? ButtonW;
            AButtonN = addressableValues[4].GetFloat(report) ?? AButtonN;
            AButtonE = addressableValues[5].GetFloat(report) ?? AButtonE;
            AButtonS = addressableValues[6].GetFloat(report) ?? AButtonS;
            AButtonW = addressableValues[7].GetFloat(report) ?? AButtonW;

            if (AButtonN == 0) AButtonN = ButtonN ? 1.0f : 0f; // if analog is off, try to supply it via digital
            if (!ButtonN) ButtonN = AButtonN > 0; // if digital is off, check analog is 0

            if (AButtonE == 0) AButtonE = ButtonE ? 1.0f : 0f; // if analog is off, try to supply it via digital
            if (!ButtonE) ButtonE = AButtonE > 0; // if digital is off, check analog is 0

            if (AButtonS == 0) AButtonS = ButtonS ? 1.0f : 0f; // if analog is off, try to supply it via digital
            if (!ButtonS) ButtonS = AButtonS > 0; // if digital is off, check analog is 0

            if (AButtonW == 0) AButtonW = ButtonW ? 1.0f : 0f; // if analog is off, try to supply it via digital
            if (!ButtonW) ButtonW = AButtonW > 0; // if digital is off, check analog is 0
        }
    }
}
