﻿using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;

namespace ExtendInput.Controls
{
    public interface IControlButtonGenesis3 : IControl
    {
        bool ButtonA { get; set; }
        bool ButtonB { get; set; }
        bool ButtonC { get; set; }
    }
    [GenericControl("ButtonGenesis3")]
    public class ControlButtonGenesis3 : IControlButtonGenesis3, IGenericControl
    {
        public bool ButtonA { get; set; }
        public bool ButtonB { get; set; }
        public bool ButtonC { get; set; }

        public ControlButtonGenesis3() { }



        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlButtonGenesis3(string factoryName, AddressableValue[] addressableValues)
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
            ControlButtonGenesis3 newData = new ControlButtonGenesis3();

            newData.ButtonA = this.ButtonA;
            newData.ButtonB = this.ButtonB;
            newData.ButtonC = this.ButtonC;

            return newData;
        }

        public void SetGenericValue(IReport report)
        {
            ButtonA = addressableValues[0].GetBoolean(report) ?? ButtonA;
            ButtonB = addressableValues[1].GetBoolean(report) ?? ButtonB;
            ButtonC = addressableValues[2].GetBoolean(report) ?? ButtonC;
        }
    }
}