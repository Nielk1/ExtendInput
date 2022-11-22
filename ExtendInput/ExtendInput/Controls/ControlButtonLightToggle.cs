using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public class ControlButtonLightToggle : IControlButtonWithStateLight, IControlButton
    {
        public bool DigitalStage1 { get; set; }

        private static string[] _states = new string[] { "STATE_LIGHT_OFF", "STATE_LIGHT_ON" };
        private static string[] _statesEmpty = new string[0];
        public string[] States
        {
            get
            {
                if (AccessMode == AccessMode.FullControl)
                    return _states;
                return _statesEmpty;
            }
        }

        private string _state;

        public string State // State is null if not settable, only apply mode if valid and able to
        {
            get => _state;
            set
            {
                if (AccessMode == AccessMode.FullControl && States.Contains(value) && _state != value)
                {
                    _state = value;
                    IsWriteDirty = true;
                }
            }
        }

        private AccessMode AccessMode;
        public ControlButtonLightToggle(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
            if (AccessMode == AccessMode.FullControl)
            {
                _state = States[0];
            }
            else
            {
                _state = null;
            }
        }

        public T Value<T>(string key)
        {
            switch (key)
            {
                case "":
                    return (T)Convert.ChangeType(DigitalStage1, typeof(T));
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
            ControlButtonLightToggle newData = new ControlButtonLightToggle(this.AccessMode);

            newData.DigitalStage1 = this.DigitalStage1;
            newData.State = this.State;
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
                case "State":
                    State = value;
                    IsWriteDirty = true;
                    return true;
            }
            return false;
        }
    }
}