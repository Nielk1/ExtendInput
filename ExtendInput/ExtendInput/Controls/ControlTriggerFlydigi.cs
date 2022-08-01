using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public enum EEffectTriggerForceFeedbackFlydigi
    {
        STATE_FLYDIGI_TRIGGER_NONE,
        STATE_FLYDIGI_TRIGGER_FEEDBACK,  // start,      resistance
        STATE_FLYDIGI_TRIGGER_VIBRATION, // start,      resistance, amplitude, frequency // PS5 doesn't have resistance here
        STATE_FLYDIGI_TRIGGER_WEAPON,    // start, end, resistance
    }
    public interface IControlTriggerFlydigi : IControl
    {
        //string[] States { get; }
        //string State { get; set; }

        EEffectTriggerForceFeedbackFlydigi Effect { get; set; }
        // Fdb // Wep // Vibr  // Note
        byte Start      { get; set; }
        byte End        { get; set; }
        byte Strength   { get; set; }
        byte Amplitude  { get; set; }
        byte Frequency  { get; set; }
        //byte TriggerStop { get; set; }
        //byte TriggerStatus { get; set; }
    }

    public class ControlTriggerFlydigi : ControlTrigger, IControlTriggerFlydigi, IControlTrigger
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

        public EEffectTriggerForceFeedbackFlydigi Effect { get; set; }
        public byte Start { get; set; }
        public byte End { get; set; }
        public byte Strength { get; set; }
        public byte Amplitude { get; set; }
        public byte Frequency { get; set; }
        //public byte TriggerStop { get; set; }
        //public byte TriggerStatus { get; set; }


        private AccessMode AccessMode;
        public ControlTriggerFlydigi(AccessMode AccessMode)
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
            ControlTriggerFlydigi newData = new ControlTriggerFlydigi(AccessMode);

            newData.AnalogStage1 = this.AnalogStage1;
            newData.Effect = this.Effect;
            newData.Start = this.Start;
            newData.End = this.End;
            newData.Strength = this.Strength;
            newData.Amplitude = this.Amplitude;
            newData.Frequency = this.Frequency;
            //newData.TriggerStop = this.TriggerStop;
            //newData.TriggerStatus = this.TriggerStatus;
            newData.IsWriteDirty = this.IsWriteDirty; // need to preserve this stuff

            return newData;
        }
        public new void CleanWriteDirty() { IsWriteDirty = false; }

        public new bool SetProperty(string property, string value, params string[] paramaters)
        {
            switch (property)
            {
                case "Effect":
                    {
                        EEffectTriggerForceFeedbackFlydigi parsed;
                        if (Enum.TryParse<EEffectTriggerForceFeedbackFlydigi>(value, out parsed))
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
                case "Strength":
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

    /*[ControlConverter(typeof(ControlTriggerPS5), typeof(IControlTrigger2Stage))]
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
    }*/

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
