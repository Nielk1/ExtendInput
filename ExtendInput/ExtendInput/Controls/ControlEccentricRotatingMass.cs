using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlEccentricRotatingMass : IControl
    {
        float? Power { get; set; }
    }

    [GenericControl("EccentricRotatingMass")]
    public class ControlEccentricRotatingMass : IControlEccentricRotatingMass, IGenericControl
    {
        private float? _power;
        public float? Power // State is null if not settable, only apply mode if valid and able to
        {
            get => _power;
            set
            {
                if (AccessMode == AccessMode.FullControl)
                {
                    _power = value;
                    IsWriteDirty = true;
                }
            }
        }

        private AccessMode AccessMode;
        public ControlEccentricRotatingMass(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
            if (AccessMode == AccessMode.FullControl)
            {
                Power = 0;
            }
            else
            {
                Power = null;
            }
        }

        public bool IsHeavy { get; private set; }

        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlEccentricRotatingMass(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
        {
            this.AccessMode = accessMode;
            if (AccessMode == AccessMode.FullControl)
            {
                Power = 0;
            }
            else
            {
                Power = null;
            }

            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
            if (properties != null)
            {
                if (properties.ContainsKey("w"))
                {
                    switch (properties["w"])
                    {
                        case "h": IsHeavy = true; break;
                        case "l": IsHeavy = false; break;
                    }
                }
            }
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "":
                    return (T)Convert.ChangeType(Power, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            return typeof(float);
        }

        public object Clone()
        {
            ControlEccentricRotatingMass newData = new ControlEccentricRotatingMass(this.AccessMode) { IsHeavy = this.IsHeavy };

            newData.Power = this.Power;
            newData.IsWriteDirty = this.IsWriteDirty; // need to preserve this stuff

            return newData;
        }

        public void SetGenericValue(IReport report)
        {
            // this is written tot he controller, not read from it, so there's no reason for generic read logic
            //Power = addressableValues[0].GetFloat(report) ?? Power;
        }

        public bool IsWriteDirty { get; private set; }
        public void CleanWriteDirty() { IsWriteDirty = false; }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            switch (property)
            {
                case "Power":
                    {
                        float parsed;
                        if (float.TryParse(value, out parsed))
                        {
                            Power = parsed;
                            IsWriteDirty = true;
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }
    }
}
