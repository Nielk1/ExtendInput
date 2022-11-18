using System;

namespace ExtendInput.Controls
{
    public class ControlButtonGrid : IControl
    {
        public bool[,] Button { get; set; }
        private int Width;
        private int Height;

        public ControlButtonGrid(int width, int height)
        {
            Width = width;
            Height = height;
            Button = new bool[Width, Height];
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "width":
                    return (T)Convert.ChangeType(Width, typeof(T));
                case "height":
                    return (T)Convert.ChangeType(Height, typeof(T));
                default:
                try
                {
                    string[] parts = key.Split(':');
                    return (T)Convert.ChangeType(Button[int.Parse(parts[0]), int.Parse(parts[1])], typeof(T));
                }
                catch
                {
                    return default;
                }
            }
        }
        public Type Type(string key)
        {
            return typeof(bool);
        }

        public object Clone()
        {
            ControlButtonGrid newData = new ControlButtonGrid(Width, Height);

            for (int w = 0; w < Width; w++)
                for (int h = 0; h < Height; h++)
                    newData.Button[w, h] = this.Button[w, h];

            return newData;
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
