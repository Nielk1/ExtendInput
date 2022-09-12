using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;

namespace ExtendInput.Controls
{
    public interface IControlStick : IControl
    {
        float X { get; set; }
        float Y { get; set; }
    }
    [GenericControl("Stick")]
    public class ControlStick : IControlStick, IGenericControl
    {
        public float X { get; set; }
        public float Y { get; set; }

        public ControlStick() { }


        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlStick(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
        }

        public virtual T Value<T>(string key)
        {
            switch (key)
            {
                case "x":
                    return (T)Convert.ChangeType(X, typeof(T));
                case "y":
                    return (T)Convert.ChangeType(Y, typeof(T));
                default:
                    return default;
            }
        }
        public virtual Type Type(string key)
        {
            switch (key)
            {
                case "x":
                    return typeof(float);
                case "y":
                    return typeof(float);
                default:
                    return default;
            }
        }

        public virtual object Clone()
        {
            ControlStick newData = new ControlStick();

            newData.X = this.X;
            newData.Y = this.Y;

            return newData;
        }

        public void SetGenericValue(IReport report)
        {
            X = addressableValues[0].GetFloat(report) ?? X;
            Y = addressableValues[1].GetFloat(report) ?? Y;
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}
