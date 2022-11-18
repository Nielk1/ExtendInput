using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;

namespace ExtendInput.Controls
{
    public interface IControlDPadPressure : IControl
    {
        EDPadDirection Direction { get; set; }
        float AButtonN { get; set; }
        float AButtonE { get; set; }
        float AButtonS { get; set; }
        float AButtonW { get; set; }
    }
    [GenericControl("DPadPressure")]
    public class ControlDPadPressure : IControlDPadPressure, IGenericControl
    {
        //public int StateCount { get; private set; }
        public EDPadDirection Direction { get; set; }
        public float AButtonN { get; set; }
        public float AButtonE { get; set; }
        public float AButtonS { get; set; }
        public float AButtonW { get; set; }
        public ControlDPadPressure(/*int StateCount*/)
        {
            //this.StateCount = StateCount;
        }



        private AddressableValue[] addressableValues;
        private string factoryName;
        public ControlDPadPressure(AccessMode accessMode, string factoryName, AddressableValue[] addressableValues, Dictionary<string, dynamic> properties)
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
            ControlDPadPressure newData = new ControlDPadPressure();

            newData.Direction = this.Direction;
            newData.AButtonN = this.AButtonN;
            newData.AButtonE = this.AButtonE;
            newData.AButtonS = this.AButtonS;
            newData.AButtonW = this.AButtonW;

            return newData;
        }

        public void ProcessReportForGenericController(IReport report)
        {
            byte? DirectionValue = addressableValues[0].GetByte(report);
            if(DirectionValue.HasValue)
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

            AButtonN = addressableValues[9].GetFloat(report) ?? AButtonN;
            AButtonE = addressableValues[10].GetFloat(report) ?? AButtonE;
            AButtonS = addressableValues[11].GetFloat(report) ?? AButtonS;
            AButtonW = addressableValues[12].GetFloat(report) ?? AButtonW;

            if(Direction == EDPadDirection.None)
            {
                float padH = AButtonE - AButtonW;
                float padV = AButtonN - AButtonS;
                if(padH != 0 && padV != 0)
                {
                    if (padH > 0)
                        if (padV > 0)
                            Direction = EDPadDirection.NorthEast;
                        else if (padV < 0)
                            Direction = EDPadDirection.SouthEast;
                        else
                            Direction = EDPadDirection.East;
                    else if (padH < 0)
                        if (padV > 0)
                            Direction = EDPadDirection.NorthWest;
                        else if (padV < 0)
                            Direction = EDPadDirection.SouthWest;
                        else
                            Direction = EDPadDirection.West;
                    else
                        if (padV > 0)
                        Direction = EDPadDirection.North;
                    else if (padV < 0)
                        Direction = EDPadDirection.South;
                    else
                        Direction = EDPadDirection.None;
                }
            }
            else
            {
                float padH = AButtonE - AButtonW;
                float padV = AButtonN - AButtonS;
                if (padH == 0 && padV == 0)
                {
                    switch (Direction)
                    {
                        case EDPadDirection.North:     AButtonN = 1.0f; AButtonE = 0.0f; AButtonS = 0.0f; AButtonW = 0.0f; break;
                        case EDPadDirection.NorthEast: AButtonN = 1.0f; AButtonE = 1.0f; AButtonS = 0.0f; AButtonW = 0.0f; break;
                        case EDPadDirection.East:      AButtonN = 0.0f; AButtonE = 1.0f; AButtonS = 0.0f; AButtonW = 0.0f; break;
                        case EDPadDirection.SouthEast: AButtonN = 0.0f; AButtonE = 1.0f; AButtonS = 1.0f; AButtonW = 0.0f; break;
                        case EDPadDirection.South:     AButtonN = 0.0f; AButtonE = 0.0f; AButtonS = 1.0f; AButtonW = 0.0f; break;
                        case EDPadDirection.SouthWest: AButtonN = 0.0f; AButtonE = 0.0f; AButtonS = 1.0f; AButtonW = 1.0f; break;
                        case EDPadDirection.West:      AButtonN = 0.0f; AButtonE = 0.0f; AButtonS = 0.0f; AButtonW = 1.0f; break;
                        case EDPadDirection.NorthWest: AButtonN = 1.0f; AButtonE = 0.0f; AButtonS = 0.0f; AButtonW = 0.0f; break;
                    }
                }
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
