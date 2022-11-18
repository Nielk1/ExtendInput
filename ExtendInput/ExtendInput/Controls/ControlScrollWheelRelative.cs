using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlScrollWheelRelative : IControl
    {
        int Delta { get; set; }
        float Divisions { get; set; }
        bool HasClick { get; set; }
        bool Click { get; set; }
    }

    //[GenericControl("SpinnerRelative")]
    public class ControlScrollWheelRelative : IControlScrollWheelRelative, IControlSpinnerRelative
    {
        public int Delta { get; set; }
        public float Divisions { get; set; }
        public bool HasClick { get; set; }
        public bool Click { get; set; }
        public ControlScrollWheelRelative() { }


        //private AddressableValue[] addressableValues;
        //private string factoryName;
        //public ControlSpinnerRelative(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
        //{
        //    this.factoryName = factoryName;
        //    this.addressableValues = addressableValues;
        //}

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "":
                    return (T)Convert.ChangeType(Delta, typeof(T));
                case "Delta":
                    return (T)Convert.ChangeType(Delta, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            switch (key)
            {
                case "":
                    return typeof(bool);
                case "Delta":
                    return typeof(int);
                default:
                    return typeof(bool);
            }
        }

        public object Clone()
        {
            ControlScrollWheelRelative newData = new ControlScrollWheelRelative();

            newData.Delta = this.Delta;
            newData.Divisions = this.Divisions;
            newData.HasClick = this.HasClick;
            newData.Click = this.Click;

            return newData;
        }

        //public void ProcessReportForGenericController(IReport report)
        //{
        //    Delta = addressableValues[0].GetInteger(report) ?? Delta;
        //}

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }
        public bool IsReadDirty => Delta != 0;
        public void CleanReadDirty()
        {
            Delta = 0;
        }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}
