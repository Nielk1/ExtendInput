﻿using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;

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
        public ControlStick(string factoryName, AddressableValue[] addressableValues)
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
    }
}
