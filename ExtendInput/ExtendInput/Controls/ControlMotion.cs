using System;

namespace ExtendInput.Controls
{
    /// <summary>
    /// This motion data will change drasticly in later versions
    /// </summary>
    public class ControlMotion : IControl
    {
        public Int16 AccelerometerX { get; set; }
        public Int16 AccelerometerY { get; set; }
        public Int16 AccelerometerZ { get; set; }
        public Int16 AngularVelocityX { get; set; }
        public Int16 AngularVelocityY { get; set; }
        public Int16 AngularVelocityZ { get; set; }
        public Int16 OrientationW { get; set; }
        public Int16 OrientationX { get; set; }
        public Int16 OrientationY { get; set; }
        public Int16 OrientationZ { get; set; }

        public bool DataStuck { get; set; }
        

        public ControlMotion()
        {
        }

        public T Value<T>(string key)
        {
            if (key == "accelerometer:x")
                return (T)Convert.ChangeType(AccelerometerX, typeof(T));
            if (key == "accelerometer:y")
                return (T)Convert.ChangeType(AccelerometerY, typeof(T));
            if (key == "accelerometer:z")
                return (T)Convert.ChangeType(AccelerometerZ, typeof(T));
            if (key == "angularVelocity:x")
                return (T)Convert.ChangeType(AngularVelocityX, typeof(T));
            if (key == "angularVelocity:y")
                return (T)Convert.ChangeType(AngularVelocityY, typeof(T));
            if (key == "angularVelocity:z")
                return (T)Convert.ChangeType(AngularVelocityZ, typeof(T));
            if (key == "orientation:w")
                return (T)Convert.ChangeType(OrientationW, typeof(T));
            if (key == "orientation:x")
                return (T)Convert.ChangeType(OrientationX, typeof(T));
            if (key == "orientation:y")
                return (T)Convert.ChangeType(OrientationY, typeof(T));
            if (key == "orientation:z")
                return (T)Convert.ChangeType(OrientationZ, typeof(T));

            return default;
        }
        public Type Type(string key)
        {
            switch (key)
            {
                case "accelerometer:x":
                case "accelerometer:y":
                case "accelerometer:z":
                case "angularVelocity:x":
                case "angularVelocity:y":
                case "angularVelocity:z":
                case "orientation:w":
                case "orientation:x":
                case "orientation:y":
                case "orientation:z":
                    return typeof(Int16);
            }

            return default;
        }

        public object Clone()
        {
            ControlMotion newData = new ControlMotion();

            newData.AccelerometerX = this.AccelerometerX;
            newData.AccelerometerY = this.AccelerometerY;
            newData.AccelerometerZ = this.AccelerometerZ;
            newData.AngularVelocityX = this.AngularVelocityX;
            newData.AngularVelocityY = this.AngularVelocityY;
            newData.AngularVelocityZ = this.AngularVelocityZ;
            newData.OrientationW = this.OrientationW;
            newData.OrientationX = this.OrientationX;
            newData.OrientationY = this.OrientationY;
            newData.OrientationZ = this.OrientationZ;

            return newData;
        }
    }
}
