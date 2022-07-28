using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;

namespace ExtendInput.Controls
{
    public interface IControlDPad : IControl
    {
        EDPadDirection Direction { get; set; }
    }
    [GenericControl("DPad")]
    public class ControlDPad : IControlDPad, IGenericControl
    {
        //public int StateCount { get; private set; }
        public EDPadDirection Direction { get; set; }
        public ControlDPad(/*int StateCount*/)
        {
            //this.StateCount = StateCount;
        }



        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlDPad(string factoryName, AddressableValue[] addressableValues)
        {
            this.factoryName = factoryName;
            this.addressableValues = addressableValues;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "n":
                    if (Direction == EDPadDirection.North) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.NorthEast) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.NorthWest) return (T)Convert.ChangeType(1, typeof(T));
                    return default;
                case "e":
                    if (Direction == EDPadDirection.East) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.NorthEast) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.SouthEast) return (T)Convert.ChangeType(1, typeof(T));
                    return default;
                case "s":
                    if (Direction == EDPadDirection.South) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.SouthEast) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.SouthWest) return (T)Convert.ChangeType(1, typeof(T));
                    return default;
                case "w":
                    if (Direction == EDPadDirection.West) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.NorthWest) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.SouthWest) return (T)Convert.ChangeType(1, typeof(T));
                    return default;
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
            ControlDPad newData = new ControlDPad();

            newData.Direction = this.Direction;

            return newData;
        }

        public void SetGenericValue(IReport report)
        {
            switch(factoryName)
            {
                case "stick":
                    {
                        float? padH = addressableValues[0].GetFloat(report);
                        float? padV = addressableValues[1].GetFloat(report);
                        if(padH.HasValue && padV.HasValue)
                        {
                            if (padH > 0.5f)
                                if (padV > 0.5f)
                                    Direction = EDPadDirection.SouthEast;
                                else if (padV < -0.5f)
                                    Direction = EDPadDirection.NorthEast;
                                else
                                    Direction = EDPadDirection.East;
                            else if (padH < -0.5f)
                                if (padV > 0.5f)
                                    Direction = EDPadDirection.SouthWest;
                                else if (padV < -0.5f)
                                    Direction = EDPadDirection.NorthWest;
                                else
                                    Direction = EDPadDirection.West;
                            else
                                if (padV > 0.5f)
                                Direction = EDPadDirection.South;
                            else if (padV < -0.5f)
                                Direction = EDPadDirection.North;
                            else
                                Direction = EDPadDirection.None;
                            return;
                        }
                    }
                    break;
                default:
                    {
                        byte? DirectionValue = addressableValues[0].GetByte(report);
                        if (DirectionValue.HasValue)
                        {
                            if (DirectionValue == addressableValues[1].GetByte(report)) Direction = EDPadDirection.North;
                            else if (DirectionValue == addressableValues[2].GetByte(report)) Direction = EDPadDirection.NorthEast;
                            else if (DirectionValue == addressableValues[3].GetByte(report)) Direction = EDPadDirection.East;
                            else if (DirectionValue == addressableValues[4].GetByte(report)) Direction = EDPadDirection.SouthEast;
                            else if (DirectionValue == addressableValues[5].GetByte(report)) Direction = EDPadDirection.South;
                            else if (DirectionValue == addressableValues[6].GetByte(report)) Direction = EDPadDirection.SouthWest;
                            else if (DirectionValue == addressableValues[7].GetByte(report)) Direction = EDPadDirection.West;
                            else if (DirectionValue == addressableValues[8].GetByte(report)) Direction = EDPadDirection.NorthWest;
                            else Direction = EDPadDirection.None;
                        }
                        return;
                    }
            }
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}
