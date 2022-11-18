using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;

namespace ExtendInput.Controls
{
    public interface IControlButtonGenesis6 : IControl
    {
        bool ButtonA { get; set; }
        bool ButtonB { get; set; }
        bool ButtonC { get; set; }
        bool ButtonX { get; set; }
        bool ButtonY { get; set; }
        bool ButtonZ { get; set; }
    }
    [GenericControl("ButtonGenesis6")]
    public class ControlButtonGenesis6 : IControlButtonGenesis6, IGenericControl
    {
        public bool ButtonA { get; set; }
        public bool ButtonB { get; set; }
        public bool ButtonC { get; set; }
        public bool ButtonX { get; set; }
        public bool ButtonY { get; set; }
        public bool ButtonZ { get; set; }

        public ControlButtonGenesis6() { }



        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlButtonGenesis6(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "a":
                    return (T)Convert.ChangeType(ButtonA, typeof(T));
                case "b":
                    return (T)Convert.ChangeType(ButtonB, typeof(T));
                case "c":
                    return (T)Convert.ChangeType(ButtonC, typeof(T));
                case "x":
                    return (T)Convert.ChangeType(ButtonX, typeof(T));
                case "y":
                    return (T)Convert.ChangeType(ButtonY, typeof(T));
                case "z":
                    return (T)Convert.ChangeType(ButtonZ, typeof(T));
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
            ControlButtonGenesis6 newData = new ControlButtonGenesis6();

            newData.ButtonA = this.ButtonA;
            newData.ButtonB = this.ButtonB;
            newData.ButtonC = this.ButtonC;
            newData.ButtonX = this.ButtonX;
            newData.ButtonY = this.ButtonY;
            newData.ButtonZ = this.ButtonZ;

            return newData;
        }

        public void ProcessReportForGenericController(IReport report)
        {
            ButtonA = addressableValues[0].GetBoolean(report) ?? ButtonA;
            ButtonB = addressableValues[1].GetBoolean(report) ?? ButtonB;
            ButtonC = addressableValues[2].GetBoolean(report) ?? ButtonC;
            ButtonX = addressableValues[3].GetBoolean(report) ?? ButtonX;
            ButtonY = addressableValues[4].GetBoolean(report) ?? ButtonY;
            ButtonZ = addressableValues[5].GetBoolean(report) ?? ButtonZ;
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
