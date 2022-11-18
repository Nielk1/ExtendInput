using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlSpinnerRelative : IControl
    {
        int Delta { get; set; }
        float Divisions { get; set; }
    }

    //[GenericControl("SpinnerRelative")]
    public class ControlSpinnerRelative : IControlSpinnerRelative
    {
        public int Delta { get; set; }
        public float Divisions { get; set; }

        public ControlSpinnerRelative() { }


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
                case "Degrees":
                    return (T)Convert.ChangeType(Divisions, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            return typeof(int);
        }

        public object Clone()
        {
            ControlSpinnerRelative newData = new ControlSpinnerRelative();

            newData.Delta = this.Delta;
            newData.Divisions = this.Divisions;

            return newData;
        }

        //public void ProcessReportForGenericController(IReport report)
        //{
        //    Delta = addressableValues[0].GetInteger(report) ?? Delta;
        //}

        public bool IsWriteDirty => false;

        public bool IsReadDirty => Delta != 0;

        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }

        public void CleanReadDirty()
        {
            Delta = 0;
        }
    }
}
