using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;

namespace ExtendInput.Controls
{
    public interface IControlDualStrikePivot : IControl
    {
        float X { get; set; }
        float Y { get; set; }
    }
    [GenericControl("DualStrikePivot")]
    public class ControlDualStrikePivot : IControlDualStrikePivot, IGenericControl
    {
        public float X { get; set; }
        public float Y { get; set; }
        public bool XMin { get; set; }
        public bool XMax { get; set; }
        public bool YMin { get; set; }
        public bool YMax { get; set; }

        public ControlDualStrikePivot() { }


        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlDualStrikePivot(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
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
                case "xmax":
                    return (T)Convert.ChangeType(XMax, typeof(T));
                case "xmin":
                    return (T)Convert.ChangeType(XMin, typeof(T));
                case "ymax":
                    return (T)Convert.ChangeType(YMax, typeof(T));
                case "ymin":
                    return (T)Convert.ChangeType(YMin, typeof(T));
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
                case "xmax":
                    return typeof(bool);
                case "xmin":
                    return typeof(bool);
                case "ymax":
                    return typeof(bool);
                case "ymin":
                    return typeof(bool);
                default:
                    return default;
            }
        }

        public virtual object Clone()
        {
            ControlDualStrikePivot newData = new ControlDualStrikePivot();

            newData.X = this.X;
            newData.Y = this.Y;
            newData.XMax = this.XMax;
            newData.XMin = this.XMin;
            newData.YMax = this.YMax;
            newData.YMin = this.YMin;

            return newData;
        }

        public void ProcessReportForGenericController(IReport report)
        {
            X = addressableValues[0].GetFloat(report) ?? X;
            Y = addressableValues[1].GetFloat(report) ?? Y;
            if (addressableValues[2].GetBoolean(report) ?? false)
            {
                if (X > 0)
                {
                    XMax = true;
                    XMin = false;
                }
                else
                {
                    XMax = false;
                    XMin = true;
                }
            }
            else
            {
                XMax = false;
                XMin = false;
            }
            if (addressableValues[3].GetBoolean(report) ?? false)
            {
                if (Y > 0)
                {
                    YMax = true;
                    YMin = false;
                }
                else
                {
                    YMax = false;
                    YMin = true;
                }
            }
            else
            {
                YMax = false;
                YMin = false;
            }
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
