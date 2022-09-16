using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;

namespace ExtendInput.Controls
{
    public interface IControlButtonQuadSlider : IControl
    {
        bool ButtonN { get; set; }
        bool ButtonE { get; set; }
        bool ButtonS { get; set; }
        bool ButtonW { get; set; }

        float X { get; set; }
        float Y { get; set; }
    }
    [GenericControl("ButtonQuadSlider")]
    public class ControlButtonQuadSlider : IControlButtonQuadSlider, IControlButtonQuad, IGenericControl
    {
        public bool ButtonN { get; set; }
        public bool ButtonE { get; set; }
        public bool ButtonS { get; set; }
        public bool ButtonW { get; set; }

        public float X { get; set; }
        public float Y { get; set; }

        public bool MovableButtonN { get; private set; }
        public bool MovableButtonE { get; private set; }
        public bool MovableButtonS { get; private set; }
        public bool MovableButtonW { get; private set; }

        public ControlButtonQuadSlider(bool MovableButtonN, bool MovableButtonE, bool MovableButtonS, bool MovableButtonW)
        {
            this.MovableButtonN = MovableButtonN;
            this.MovableButtonE = MovableButtonE;
            this.MovableButtonS = MovableButtonS;
            this.MovableButtonW = MovableButtonW;
        }



        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlButtonQuadSlider(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
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
                case "x":
                    return (T)Convert.ChangeType(X, typeof(T));
                case "y":
                    return (T)Convert.ChangeType(Y, typeof(T));
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
            ControlButtonQuadSlider newData = new ControlButtonQuadSlider(MovableButtonN, MovableButtonE, MovableButtonS, MovableButtonW);

            newData.ButtonN = this.ButtonN;
            newData.ButtonE = this.ButtonE;
            newData.ButtonS = this.ButtonS;
            newData.ButtonW = this.ButtonW;

            newData.X = this.X;
            newData.Y = this.Y;

            return newData;
        }

        public void ProcessReportForGenericController(IReport report)
        {
            X = addressableValues[0].GetFloat(report) ?? X;
            Y = addressableValues[1].GetFloat(report) ?? Y;

            ButtonN = addressableValues[2].GetBoolean(report) ?? ButtonN;
            ButtonE = addressableValues[3].GetBoolean(report) ?? ButtonE;
            ButtonS = addressableValues[4].GetBoolean(report) ?? ButtonS;
            ButtonW = addressableValues[5].GetBoolean(report) ?? ButtonW;

            MovableButtonN = addressableValues[6].GetBoolean(report) ?? MovableButtonN;
            MovableButtonE = addressableValues[7].GetBoolean(report) ?? MovableButtonE;
            MovableButtonS = addressableValues[8].GetBoolean(report) ?? MovableButtonS;
            MovableButtonW = addressableValues[9].GetBoolean(report) ?? MovableButtonW;
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}
