using System;

namespace ExtendInput.Controls
{
    public class ControlTouch : IControl
    {
        public bool HasClick { get; private set; }
        public int TouchCount { get; private set; }
        public float[] X { get; private set; }
        public float[] Y { get; private set; }
        public bool[] Touch { get; private set; }
        public bool Click { get; set; }

        public int PhysicalWidth { get; set; }
        public int PhysicalHeight { get; set; }

        public ControlTouch(int TouchCount, bool HasClick)
        {
            this.TouchCount = TouchCount;
            this.HasClick = HasClick;

            this.X = new float[TouchCount];
            this.Y = new float[TouchCount];
            this.Touch = new bool[TouchCount];
            this.Click = false;
        }

        public T Value<T>(string key)
        {
            if(key == "click")
                return (T)Convert.ChangeType(Click, typeof(T));

            for (int i = 0; i < TouchCount; i++)
            {
                if (key == $"{i}:x")
                    return (T)Convert.ChangeType(X[i], typeof(T));

                if (key == $"{i}:y")
                    return (T)Convert.ChangeType(Y[i], typeof(T));

                if (key == $"{i}:touch")
                    return (T)Convert.ChangeType(Touch[i], typeof(T));
            }

            return default;
        }
        public Type Type(string key)
        {
            if (key == "click")
                return typeof(bool);

            for (int i = 0; i < TouchCount; i++)
            {
                if (key == $"{i}:x")
                    return typeof(float);

                if (key == $"{i}:y")
                    return typeof(float);

                if (key == $"{i}:touch")
                    return typeof(bool);
            }

            return default;
        }

        public object Clone()
        {
            ControlTouch newData = new ControlTouch(this.TouchCount, this.HasClick);

            newData.Click = this.Click;
            newData.PhysicalWidth = this.PhysicalWidth;
            newData.PhysicalHeight = this.PhysicalHeight;

            for (int i = 0; i < this.TouchCount; i++)
            {
                // taking advantage of the fact it's an array, so the private setter doesn't stop us
                newData.Touch[i] = this.Touch[i];
                newData.X[i] = this.X[i];
                newData.Y[i] = this.Y[i];
            }

            return newData;
        }

        public void AddTouch(int idx, bool touch, float x, float y, byte timedeltams)
        {
            //Console.WriteLine($"{idx}\t{touch}\t{x}\t{y}\t{timedelta}");

            Touch[idx] = touch;
            X[idx] = x;
            Y[idx] = y;
        }
    }
}
