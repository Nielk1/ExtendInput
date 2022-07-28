using System;

namespace ExtendInput.Controls
{
    public class ControlRocker : IControl
    {
        //public int StateCount { get; private set; }
        public EDPadDirection Direction { get; set; }
        public ControlRocker(/*int StateCount*/)
        {
            //this.StateCount = StateCount;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "e":
                    if (Direction == EDPadDirection.East) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.NorthEast) return (T)Convert.ChangeType(1, typeof(T));
                    if (Direction == EDPadDirection.SouthEast) return (T)Convert.ChangeType(1, typeof(T));
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
            ControlRocker newData = new ControlRocker();

            newData.Direction = this.Direction;

            return newData;
        }

        public bool IsWriteDirty => false;
        public void CleanWriteDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            throw new NotImplementedException();
        }
    }
}
