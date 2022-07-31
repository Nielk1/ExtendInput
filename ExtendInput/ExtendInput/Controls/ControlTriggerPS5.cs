using System;

namespace ExtendInput.Controls
{
    public enum EEffectTriggerForceFeedbackPS5
    {
        STATE_PS5_TRIGGER_NONE,
        STATE_PS5_TRIGGER_FEEDBACK,  // start,      resistance
        STATE_PS5_TRIGGER_WEAPON,    // start, end, resistance
        STATE_PS5_TRIGGER_VIBRATION, // start,                  amplitude, frequency
    }
    public interface IControlTriggerPS5 : IControl
    {
        //string[] States { get; }
        //string State { get; set; }

        EEffectTriggerForceFeedbackPS5 Effect { get; set; }
        // Fdb // Wep // Vibr  // Note
        byte Start      { get; set; } // 0-9 // 2-7 // 0-9   // 0-9
        byte End        { get; set; } // X   // 3-8 // X     // technicly 0-9, but 9 puts the break point too late
        byte Strength   { get; set; } // 0-8 // 0-8 // X     // 0-8
        byte Amplitude  { get; set; } // X   // X   // 0-8   // 0-8
        byte Frequency  { get; set; } // X   // X   // 0-255 // 0-255
        byte TriggerStop { get; set; }
        byte TriggerStatus { get; set; }
    }

    public class ControlTriggerPS5 : ControlTrigger, IControlTriggerPS5, IControlTrigger
    {
        //private static string[] _states = new string[] { "STATE_PS5_TRIGGER_NONE", "STATE_PS5_TRIGGER_FEEDBACK", "STATE_PS5_TRIGGER_WEAPON", "STATE_PS5_TRIGGER_VIBRATION" };
        //private static string[] _statesEmpty = new string[0];
        //public string[] States
        //{
        //    get
        //    {
        //        if (AccessMode == AccessMode.FullControl)
        //            return _states;
        //        return _statesEmpty;
        //    }
        //}

        //private string _state;

        //public string State // State is null if not settable, only apply mode if valid and able to
        //{
        //    get => _state;
        //    set
        //    {
        //        if (AccessMode == AccessMode.FullControl && States.Contains(value) && _state != value)
        //        {
        //            _state = value;
        //            IsWriteDirty = true;
        //        }
        //    }
        //}
        public new bool IsWriteDirty { get; private set; }

        public EEffectTriggerForceFeedbackPS5 Effect { get; set; }
        // Fdb // Wep // Vibr  // Note
        public byte Start { get; set; } // 0-9 // 2-7 // 0-9   // 0-9
        public byte End { get; set; } // X   // 3-8 // X     // technicly 0-9, but 9 puts the break point too late
        public byte Strength { get; set; } // 0-8 // 0-8 // X     // 0-8
        public byte Amplitude { get; set; } // X   // X   // 0-8   // 0-8
        public byte Frequency { get; set; } // X   // X   // 0-255 // 0-255
        public byte TriggerStop { get; set; }
        public byte TriggerStatus { get; set; }


        private AccessMode AccessMode;
        public ControlTriggerPS5(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
            if (AccessMode == AccessMode.FullControl)
            {
            }
            else
            {
            }
        }

        /*public void SetEffect(EEffectTriggerForceFeedback Effect, params float[] Paramaters)
        {
            if (AccessMode != AccessMode.FullControl)
                return;

            switch (Effect)
            {
                case EEffectTriggerForceFeedback.Feedback:
                    this.Effect = EEffectTriggerForceFeedback.Feedback;
                    this.Start = (byte)Math.Round(Paramaters[0] * 9f);
                    this.End = 9;
                    this.Resistance = (byte)Math.Round(Paramaters[1] * 8f);
                    this.Amplitude = 0;
                    this.Frequency = 0;
                    break;
                case EEffectTriggerForceFeedback.Weapon:
                    this.Effect = EEffectTriggerForceFeedback.Weapon;
                    this.Start = (byte)Math.Round(Paramaters[0] * 9f);
                    this.End = (byte)Math.Round(Paramaters[1] * 9f);
                    this.Resistance = (byte)Math.Round(Paramaters[2] * 8f);
                    this.Amplitude = 0;
                    this.Frequency = 0;
                    break;
                case EEffectTriggerForceFeedback.Vibration:
                    this.Effect = EEffectTriggerForceFeedback.Vibration;
                    this.Start = (byte)Math.Round(Paramaters[0] * 9f);
                    this.End = 9;
                    this.Resistance = 0;
                    this.Amplitude = (byte)Math.Round(Paramaters[1] * 8f);
                    this.Frequency = (byte)Math.Round(Paramaters[2] * 255f);
                    break;
                case EEffectTriggerForceFeedback.None:
                default:
                    this.Effect = EEffectTriggerForceFeedback.None;
                    this.Start = 0;
                    this.End = 0;
                    this.Resistance = 0;
                    this.Amplitude = 0;
                    this.Frequency = 0;
                    break;
            }
        }*/


        public override T Value<T>(string key)
        {
            switch (key)
            {
                default:
                    return base.Value<T>(key);
            }
        }
        public override Type Type(string key)
        {
            switch (key)
            {
                default:
                    return base.Type(key);
            }
        }

        public override object Clone()
        {
            ControlTriggerPS5 newData = new ControlTriggerPS5(AccessMode);

            newData.AnalogStage1 = this.AnalogStage1;
            newData.Effect = Effect;
            newData.Start = Start;
            newData.End = End;
            newData.Strength = Strength;
            newData.Amplitude = Amplitude;
            newData.Frequency = Frequency;
            newData.TriggerStop = TriggerStop;
            newData.TriggerStatus = TriggerStatus;
            newData.IsWriteDirty = this.IsWriteDirty; // need to preserve this stuff

            return newData;
        }

        public new bool SetProperty(string property, string value, params string[] paramaters)
        {
            switch (property)
            {
                case "Effect":
                    {
                        EEffectTriggerForceFeedbackPS5 parsed;
                        if (Enum.TryParse<EEffectTriggerForceFeedbackPS5>(value, out parsed))
                        {
                            Effect = parsed;
                            IsWriteDirty = true;
                            return true;
                        }
                    }
                    return false;
                case "Start":
                    {
                        byte parsed;
                        if(byte.TryParse(value, out parsed))
                        {
                            Start = parsed;
                            IsWriteDirty = true;
                            return true;
                        }
                    }
                    return false;
                case "End":
                    {
                        byte parsed;
                        if (byte.TryParse(value, out parsed))
                        {
                            End = parsed;
                            IsWriteDirty = true;
                            return true;
                        }
                    }
                    return false;
                case "Resistance":
                    {
                        byte parsed;
                        if (byte.TryParse(value, out parsed))
                        {
                            Strength = parsed;
                            IsWriteDirty = true;
                            return true;
                        }
                    }
                    return false;
                case "Amplitude":
                    {
                        byte parsed;
                        if (byte.TryParse(value, out parsed))
                        {
                            Amplitude = parsed;
                            IsWriteDirty = true;
                            return true;
                        }
                    }
                    return false;
                case "Frequency":
                    {
                        byte parsed;
                        if (byte.TryParse(value, out parsed))
                        {
                            Frequency = parsed;
                            IsWriteDirty = true;
                            return true;
                        }
                    }
                    return false;
            }
            return false;
        }
    }

    [ControlConverter(typeof(ControlTriggerPS5), typeof(IControlTrigger2Stage))]
    public class ControlTriggerPS5_to_IControlTrigger2Stage_ControlConverter : IControlConverter
    {
        public bool CanAlwaysConvert => false;
        public bool CanConvert(IControl Control)
        {
            ControlTriggerPS5 ctrl = Control as ControlTriggerPS5;
            if (ctrl == null)
                return false;

            //if (ctrl.Mode != Weapon)
            //    return false;

            //return if we are past the trigger stop

            return false;
        }
        public IControl Convert(IControl Control)
        {
            return null;
        }
    }

    //[ControlConverter(typeof(ControlTriggerPS5), typeof(IControlTriggerForceFeedback))]
    //public class ControlTriggerPS5_to_ControlTriggerForceFeedback_ControlConverter : IControlConverter
    //{
    //    public bool CanAlwaysConvert => false;
    //    public bool CanConvert(IControl Control) => false;
    //    public IControl Convert(IControl Control)
    //    {
    //        if (Control is ControlTriggerPS5)
    //            return new ControlTriggerPS5_to_ControlTriggerForceFeedback((ControlTriggerPS5)Control);
    //        return null;
    //    }
    //}

    //public class ControlTriggerPS5_to_ControlTriggerForceFeedback : IControlTriggerForceFeedback
    //{
    //    private ControlTriggerPS5 Wrapped;

    //    public float AnalogStage1 => Wrapped.AnalogStage1;
    //    public EEffectTriggerForceFeedback Effect
    //    {
    //        get { return Wrapped.Effect; }
    //        set { Wrapped.Effect = value; }
    //    }
    //    public float Start
    //    {
    //        get { return Wrapped.Start / 9f; }
    //        set { Wrapped.Start = (byte)Math.Round(value * 9f); }
    //    }
    //    public float End
    //    {
    //        get { return Wrapped.End / 9f; }
    //        set { Wrapped.End = (byte)Math.Round(value * 9f); }
    //    }
    //    public float Resistance
    //    {
    //        get { return Wrapped.Resistance / 8f; }
    //        set { Wrapped.Resistance = (byte)Math.Round(value * 8f); }
    //    }
    //    public float Amplitude
    //    {
    //        get { return Wrapped.Amplitude / 8f; }
    //        set { Wrapped.Amplitude = (byte)Math.Round(value * 8f); }
    //    }
    //    public float Frequency
    //    {
    //        get { return Wrapped.Frequency / 255f; }
    //        set { Wrapped.Frequency = (byte)Math.Round(value * 255f); }
    //    }

    //    public bool IsWriteDirty => Wrapped.IsWriteDirty;

    //    public void CleanWriteDirty()
    //    {
    //        Wrapped.CleanWriteDirty();
    //    }


    //    public ControlTriggerPS5_to_ControlTriggerForceFeedback(ControlTriggerPS5 Wrapped)
    //    {
    //        this.Wrapped = Wrapped;
    //    }

    //    // Probably won't be called
    //    public object Clone()
    //    {
    //        ControlTriggerPS5_to_ControlTriggerForceFeedback newData = new ControlTriggerPS5_to_ControlTriggerForceFeedback(Wrapped);
    //        return newData;
    //    }

    //    /*public void SetEffect(EEffectTriggerForceFeedback Effect, params float[] Paramaters)
    //    {
    //        if (Effect == EEffectTriggerForceFeedback.Vibration)
    //        {
    //            // Skip Resistance for this effect because it doesn't exist for DualSense
    //            Wrapped.SetEffect(Effect, Paramaters[0], Paramaters[2], Paramaters[3]);
    //            return;
    //        }
    //        Wrapped.SetEffect(Effect, Paramaters);
    //    }*/

    //    public Type Type(string key)
    //    {
    //        return typeof(float);
    //    }

    //    public T Value<T>(string key)
    //    {
    //        return default;
    //    }
    //}
}
