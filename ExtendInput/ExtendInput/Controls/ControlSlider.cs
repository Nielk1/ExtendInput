﻿using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlSlider : IControl
    {
        float AnalogStage1 { get; set; }

    }
    [GenericControl("Slider")]
    public class ControlSlider : IControlSlider, IGenericControl
    {
        public float AnalogStage1 { get; set; }

        public ControlSlider() { }


        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlSlider(string factoryName, AddressableValue[] addressableValues)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
        }

        public virtual T Value<T>(string key)
        {
            switch (key)
            {
                case "":
                    return (T)Convert.ChangeType(AnalogStage1, typeof(T));
                default:
                    return default;
            }
        }
        public virtual Type Type(string key)
        {
            return typeof(float);
        }

        public virtual object Clone()
        {
            ControlSlider newData = new ControlSlider();

            newData.AnalogStage1 = this.AnalogStage1;

            return newData;
        }

        public void SetGenericValue(IReport report)
        {
            AnalogStage1 = addressableValues[0].GetFloat(report) ?? AnalogStage1;
        }
    }
}