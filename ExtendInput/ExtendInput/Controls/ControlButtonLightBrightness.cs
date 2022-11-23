using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace ExtendInput.Controls
{
    public class ControlButtonLightBrightness : IControlButton
    {
        public bool DigitalStage1 { get; set; }
        public float Brightness
        {
            get => _Brightness;
            set
            {
                if (AccessMode == AccessMode.FullControl && _Brightness != value)
                {
                    _Brightness = value;
                    IsWriteDirty = true;
                }
            }
        }
        private float _Brightness;
        public int Levels { get; private set; }

        private AccessMode AccessMode;
        public ControlButtonLightBrightness(AccessMode AccessMode, float Brightness = 1.0f, int Levels = 256)
        {
            this.AccessMode = AccessMode;
            this.Brightness = Brightness;
            this.Levels = Levels;
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "":
                    return (T)Convert.ChangeType(DigitalStage1, typeof(T));
                case "Brightness":
                    return (T)Convert.ChangeType(Brightness, typeof(T));
                case "Levels":
                    return (T)Convert.ChangeType(Levels, typeof(T));
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
            ControlButtonLightBrightness newData = new ControlButtonLightBrightness(this.AccessMode);

            newData.DigitalStage1 = this.DigitalStage1;
            newData.Brightness = this.Brightness;
            newData.Levels = this.Levels;
            newData.IsWriteDirty = this.IsWriteDirty; // need to preserve this stuff

            return newData;
        }

        public bool IsWriteDirty { get; private set; }
        public void CleanWriteDirty() { IsWriteDirty = false; }
        public bool IsReadDirty => false;
        public void CleanReadDirty() { }

        public bool SetProperty(string property, string value, params string[] paramaters)
        {
            switch (property)
            {
                case "Brightness":
                    {
                        float outVal;
                        if (float.TryParse(value, out outVal))
                        {
                            if (outVal < 0.0f)
                                return false;
                            if (outVal > 1.0f)
                                return false;
                            Brightness = outVal;
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