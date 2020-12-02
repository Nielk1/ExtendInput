using System;

namespace ExtendInput.Controls
{
    public class ControlDPad : IControl
    {
        //public int StateCount { get; private set; }
        public EDPadDirection Direction { get; private set; }
        public EDPadDirection? PendingDirection { get; set; }
        public ControlDPad(/*int StateCount*/)
        {
            //this.StateCount = StateCount;
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

        public void ProcessPendingInputs()
        {
            Direction = PendingDirection ?? Direction;
            PendingDirection = null;
        }
    }
}
