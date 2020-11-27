﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput
{
    public interface IControl : ICloneable
    {
        T Value<T>(string key);
        Type Type(string key);
    }
    public class ControlCollection /*: ICloneable*/// where T : IControl
    {
        private Dictionary<string, IControl> Data = new Dictionary<string, IControl>();

        /*public string[] Keys
        {
            get
            {
                return Data.Keys.ToArray();
            }
        }*/
        public IControl this[string key]
        {
            get
            {
                if (Data.ContainsKey(key))
                    return Data[key];
                return default;
            }
            set
            {
                Data[key] = value;
            }
        }

        public object Clone()
        {
            ControlCollection newData = new ControlCollection();

            foreach (var key in Data.Keys)
            {
                newData[key] = (IControl)Data[key].Clone();
            }

            return newData;
        }
    }

    public enum EDPadDirection
    {
        None,
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest,
    }
    public class ControlTriggerPair : IControl
    {
        public bool HasStage2 { get; private set; }
        public float L_Analog { get; set; }
        public float R_Analog { get; set; }
        public bool L_Stage2 { get; set; }
        public bool R_Stage2 { get; set; }

        public ControlTriggerPair(bool HasStage2)
        {
            this.HasStage2 = HasStage2;
        }
        public T Value<T>(string key)
        {
            switch (key)
            {
                case "l:analog":
                    return (T)Convert.ChangeType(L_Analog, typeof(T));
                case "r:analog":
                    return (T)Convert.ChangeType(R_Analog, typeof(T));
                case "l:stage2":
                    return (T)Convert.ChangeType(L_Stage2, typeof(T));
                case "r:stage2":
                    return (T)Convert.ChangeType(R_Stage2, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            switch (key)
            {
                case "l:analog":
                    return typeof(float);
                case "r:analog":
                    return typeof(float);
                case "l:stage2":
                    return typeof(bool);
                case "r:stage2":
                    return typeof(bool);
                default:
                    return default;
            }
        }
        public object Clone()
        {
            ControlTriggerPair newData = new ControlTriggerPair(this.HasStage2);

            newData.L_Analog = this.L_Analog;
            newData.R_Analog = this.R_Analog;

            newData.L_Stage2 = this.L_Stage2;
            newData.R_Stage2 = this.R_Stage2;

            return newData;
        }
    }
    public class ControlTrigger : IControl
    {
        public bool HasStage2 { get; private set; }
        public float Analog { get; set; }
        public bool Stage2 { get; set; }

        public ControlTrigger(bool HasStage2)
        {
            this.HasStage2 = HasStage2;
        }
        public T Value<T>(string key)
        {
            switch (key)
            {
                case "analog":
                    return (T)Convert.ChangeType(Analog, typeof(T));
                case "stage2":
                    return (T)Convert.ChangeType(Stage2, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            switch (key)
            {
                case "analog":
                    return typeof(float);
                case "stage2":
                    return typeof(bool);
                default:
                    return default;
            }
        }
        public object Clone()
        {
            ControlTrigger newData = new ControlTrigger(this.HasStage2);

            newData.Analog = this.Analog;

            newData.Stage2 = this.Stage2;

            return newData;
        }
    }
    public class ControlDPad : IControl
    {
        //public int StateCount { get; private set; }
        public EDPadDirection Direction { get; set; }
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
    }
    public class ControlButtonQuad : IControl
    {
        public bool ButtonN { get; set; }
        public bool ButtonE { get; set; }
        public bool ButtonS { get; set; }
        public bool ButtonW { get; set; }

        public ControlButtonQuad()
        {
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "n":
                    return (T)Convert.ChangeType(ButtonN, typeof(T));
                case "e":
                    return (T)Convert.ChangeType(ButtonE, typeof(T));
                case "s":
                    return (T)Convert.ChangeType(ButtonS, typeof(T));
                case "w":
                    return (T)Convert.ChangeType(ButtonW, typeof(T));
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
            ControlButtonQuad newData = new ControlButtonQuad();

            newData.ButtonN = this.ButtonN;
            newData.ButtonE = this.ButtonE;
            newData.ButtonS = this.ButtonS;
            newData.ButtonW = this.ButtonW;

            return newData;
        }
    }
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
    }
    public class ControlButtonPair : IControl
    {
        public bool Left { get; set; }
        public bool Right { get; set; }

        public ControlButtonPair()
        {
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "l":
                    return (T)Convert.ChangeType(Left, typeof(T));
                case "r":
                    return (T)Convert.ChangeType(Right, typeof(T));
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
            ControlButtonPair newData = new ControlButtonPair();

            newData.Left = this.Left;
            newData.Right = this.Right;

            return newData;
        }
    }
    public class ControlButton : IControl
    {
        public bool Button0 { get; set; }
        public T Value<T>(string key)
        {
            return (T)Convert.ChangeType(Button0, typeof(T));
            //return default;
        }
        public Type Type(string key)
        {
            return typeof(bool);
        }

        public object Clone()
        {
            ControlButton newData = new ControlButton();

            newData.Button0 = this.Button0;

            return newData;
        }
    }
    public class ControlStick : IControl
    {
        public bool HasClick { get; private set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool Click { get; internal set; }

        public ControlStick(bool HasClick)
        {
            this.HasClick = HasClick;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "x":
                    return (T)Convert.ChangeType(X, typeof(T));
                case "y":
                    return (T)Convert.ChangeType(Y, typeof(T));
                case "click":
                    return (T)Convert.ChangeType(Click, typeof(T));
                default:
                    return default;
            }
        }
        public Type Type(string key)
        {
            switch (key)
            {
                case "x":
                    return typeof(float);
                case "y":
                    return typeof(float);
                case "click":
                    return typeof(bool);
                default:
                    return default;
            }
        }

        public object Clone()
        {
            ControlStick newData = new ControlStick(this.HasClick);

            newData.X = this.X;
            newData.Y = this.Y;
            newData.Click = this.Click;

            return newData;
        }
    }
    public class ControlTouch : IControl
    {
        public bool HasClick { get; private set; }
        public int TouchCount { get; private set; }
        public float[] X { get; private set; }
        public float[] Y { get; private set; }
        public bool[] Touch { get; private set; }
        public bool Click { get; set; }

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

    public class ControllerState : ICloneable
    {
        public ControlCollection Controls { get; private set; }

        public ControllerState()
        {
            Controls = new ControlCollection();
        }
        
        public object Clone()
        {
            ControllerState newState = new ControllerState();

            newState.Controls = (ControlCollection)this.Controls.Clone();

            return newState;
        }
    }
}
