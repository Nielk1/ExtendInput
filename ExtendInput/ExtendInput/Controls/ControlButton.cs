using System;

namespace ExtendInput.Controls
{
    [Flags]
    public enum ButtonDataType : byte
    {
        //FILTER = 0x03, // 00000011

        NoExist = 0x00, // ------00
        Exist   = 0x01, // ------01
        Digital = 0x01, // ------01
        Analog  = 0x03, // ------11
    }

    [Flags]
    public enum ButtonEventType : byte
    {
        //FILTER = 0x03, // 00000011

        KeyState = 0x00, // ------00
        KeyDown  = 0x01, // ------01
        KeyUp    = 0x03, // ------11
    }

    [Flags]
    public enum ButtonTravelDirection : byte
    {
        //FILTER = 0x01, // 00000001

        Push = 0x00, // -------0
        Pull = 0x01, // -------1
    }

    [Flags]
    public enum ButtonTravelMotion : byte
    {
        //FILTER = 0x01, // 00000001

        Linear = 0x00, // -------0
        Angled = 0x01, // -------1
    }

    [Flags]
    public enum ButtonAccuracy : byte
    {
        //FILTER = 0x01, // 00000001

        Lo = 0x00, // -------0
        Hi = 0x01, // -------1
    }

    [Flags]
    public enum ButtonProperties : UInt16
    {
        // Direction
        FILTER_DIRECTION = 0x001, /**/ // 00000000 00000001
        DirectionPush = 0x000, /*****/ // -------- -------0 // Button is pulled
        DirectionPull = 0x001, /*****/ // -------- -------1 // Button is pushed

        // Travel
        FILTER_TRAVEL = 0x002, /**/ // 00000000 00000010
        TravelLinear = 0x000, /***/ // -------- ------0- // Button travels in a line
        TravelAngled = 0x002, /***/ // -------- ------1- // Button pivots on one side

        // Accuracy
        FILTER_ACCURACY = 0x004, // 00000000 00000100
        AccuracyLo = 0x000, /**/ // -------- -----0-- // Button accuracy is low (or digital)
        AccuracyHi = 0x004, /**/ // -------- -----1-- // Button accuracy is high (high fidelity control)

        // Stage1
        FILTER_STAGE1ANALOG = 0x008, // 00000000 00001000
        FILTER_STAGE1___ALL = 0x008, // 00000000 00001000
        Stage1Digital = 0x000, /***/ // -------- ----0--- // Button's main operation is digital
        Stage1Analog_ = 0x008, /***/ // -------- ----1--- // Button's main operation is analog

        // Stage2
        FILTER_STAGE2EXISTS = 0x010, // 00000000 00010000
        FILTER_STAGE2ANALOG = 0x020, // 00000000 00100000
        FILTER_STAGE2___ALL = 0x030, // 00000000 00110000
        Stage2NoExist = 0x000, /***/ // -------- --00---- // Button's bottom has no special function
        Stage2Digital = 0x010, /***/ // -------- --01---- // Button's bottom has a digital operation
        Stage2Analog_ = 0x030, /***/ // -------- --11---- // Button's bottom has an analog operation

        // Stage0
        FILTER_STAGE0EXISTS = 0x040, // 00000000 01000000
        FILTER_STAGE0ANALOG = 0x080, // 00000000 10000000
        FILTER_STAGE0___ALL = 0x0C0, // 00000000 11000000
        Stage0NoExist = 0x000, /***/ // -------- 00------ // Button has no capsense
        Stage0Digital = 0x040, /***/ // -------- 01------ // Button has capsense that is off/on
        Stage0Analog_ = 0x0C0, /***/ // -------- 11------ // Button has capsense with degree of contact

        // Hardware Events
        FILTER_HWEVENTS = 0x300, /**/ // 00000011 00000000
        EventKeyState = 0x000, /****/ // ------00 -------- // Button is normal with an up and down state or we can figure it out somehow
        EventKeyDown_ = 0x100, /****/ // ------01 -------- // button pulses on press
        EventKeyUp___ = 0x200, /****/ // ------10 -------- // button pulses on release

        // Combo
        CMB_Bumper /***********/ = DirectionPull | TravelLinear | Stage0NoExist | Stage1Digital | Stage2NoExist | AccuracyLo,
        CMB_Trigger /**********/ = DirectionPull | TravelAngled | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyHi,
        CMB_Peddle /***********/ = DirectionPush | TravelAngled | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyHi,
        CMB_2StageTrigger /****/ = DirectionPull | TravelAngled | Stage0NoExist | Stage1Analog_ | Stage2Digital | AccuracyLo,
        CMB_2StageVRTrigger /**/ = DirectionPull | TravelAngled | Stage0Digital | Stage1Analog_ | Stage2Digital | AccuracyLo,
        CMB_Button /***********/ = DirectionPush | TravelLinear | Stage0NoExist | Stage1Digital | Stage2NoExist | AccuracyLo,
        CMB_PressureButton /***/ = DirectionPush | TravelLinear | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyLo,
        CMB_AnalogButton /*****/ = DirectionPush | TravelLinear | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyLo,
        
        TOOL_QuickButtonConfig = FILTER_STAGE1___ALL | FILTER_STAGE2___ALL | FILTER_STAGE0___ALL,
    }

    public static class Extensions
    {
        public static ButtonProperties SetFlag(ref this ButtonProperties type, ButtonProperties value)
        {
            type |= value;
            return type;
        }
        public static ButtonProperties RemoveFlag(ref this ButtonProperties type, ButtonProperties value)
        {
            type &= ~value;
            return type;
        }
        public static ButtonProperties SetFlag(ref this ButtonProperties type, ButtonProperties value, ButtonProperties filter)
        {
            type = (type & ~filter) | value;
            return type;
        }


        public static ButtonTravelDirection GetDirection(this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.DirectionPull) ? ButtonTravelDirection.Pull : ButtonTravelDirection.Push;
        }
        public static void SetDirection(this ButtonProperties type, ButtonTravelDirection value)
        {
            type.SetFlag(value == ButtonTravelDirection.Pull ? ButtonProperties.DirectionPull : ButtonProperties.DirectionPush, ButtonProperties.FILTER_DIRECTION);
        }

        public static ButtonTravelMotion GetTravel(this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.DirectionPull) ? ButtonTravelMotion.Angled : ButtonTravelMotion.Linear;
        }
        public static void SetTravel(this ButtonProperties type, ButtonTravelMotion value)
        {
            type.SetFlag(value == ButtonTravelMotion.Angled ? ButtonProperties.TravelAngled : ButtonProperties.TravelLinear, ButtonProperties.FILTER_TRAVEL);
        }

        public static ButtonAccuracy GetAccuracy(ref this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.DirectionPull) ? ButtonAccuracy.Hi : ButtonAccuracy.Lo;
        }
        public static void SetAccuracy(this ButtonProperties type, ButtonAccuracy value)
        {
            type.SetFlag(value == ButtonAccuracy.Hi ? ButtonProperties.AccuracyHi : ButtonProperties.AccuracyLo, ButtonProperties.FILTER_ACCURACY);
        }

        public static ButtonDataType GetHasStage0(this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.FILTER_STAGE0EXISTS) ? type.HasFlag(ButtonProperties.FILTER_STAGE0ANALOG) ? ButtonDataType.Analog : ButtonDataType.Digital : ButtonDataType.NoExist;
        }
        public static void SetHasStage0(this ButtonProperties type, ButtonDataType value)
        {
            type.SetFlag(value == ButtonDataType.NoExist ? ButtonProperties.Stage0NoExist : value == ButtonDataType.Analog ? ButtonProperties.Stage0Analog_ : ButtonProperties.Stage0Digital, ButtonProperties.FILTER_STAGE0___ALL);
        }

        public static ButtonDataType GetHasStage1(this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.FILTER_STAGE1ANALOG) ? ButtonDataType.Analog : ButtonDataType.Digital;
        }
        public static void SetHasStage1(this ButtonProperties type, ButtonDataType value)
        {
            if (value == ButtonDataType.NoExist) throw new ArgumentException("Stage1 must exist");
            type.SetFlag(value == ButtonDataType.Analog ? ButtonProperties.Stage1Analog_ : ButtonProperties.Stage1Digital, ButtonProperties.FILTER_STAGE1___ALL);
        }

        public static ButtonDataType GetHasStage2(this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.FILTER_STAGE2EXISTS) ? type.HasFlag(ButtonProperties.FILTER_STAGE2ANALOG) ? ButtonDataType.Analog : ButtonDataType.Digital : ButtonDataType.NoExist;
        }
        public static void SetHasStage2(this ButtonProperties type, ButtonDataType value)
        {
            type.SetFlag(value == ButtonDataType.NoExist ? ButtonProperties.Stage2NoExist : value == ButtonDataType.Analog ? ButtonProperties.Stage2Analog_ : ButtonProperties.Stage2Digital, ButtonProperties.FILTER_STAGE2___ALL);
        }

        public static int CountFlagCount(this ButtonStateFlags Flags, ButtonStateFlags Bits)
        {
            int value = (byte)(Flags & Bits);
            int count = 0;
            while (value != 0)
            {
                count++;
                value &= value - 1;
            }
            return count;
        }
    }

    public enum ButtonStateFlags : byte
    {
        Empty = 0x00, /************/ // --------

        // Digital Values
        Stage1DValue_ = 0x10, /****/ // -------1 // Button's main operation value if digital
        Stage2DValue_ = 0x20, /****/ // ------1- // Button's bottom value if digital
        Stage0DValue_ = 0x40, /****/ // -----1-- // Button capsense value if digital

        // Stage1
        FILTER_STAGE1ANALOG = 0x008, // 00001000
        FILTER_STAGE1___ALL = 0x008, // 00001000
        Stage1Digital = 0x000, /***/ // ----0--- // Button's main operation is digital
        Stage1Analog_ = 0x008, /***/ // ----1--- // Button's main operation is analog

        // Stage2
        FILTER_STAGE2EXISTS = 0x010, // 00010000
        FILTER_STAGE2ANALOG = 0x020, // 00100000
        FILTER_STAGE2___ALL = 0x030, // 00110000
        Stage2NoExist = 0x000, /***/ // --00---- // Button's bottom has no special function
        Stage2Digital = 0x010, /***/ // --01---- // Button's bottom has a digital operation
        Stage2Analog_ = 0x030, /***/ // --11---- // Button's bottom has an analog operation

        // Stage0
        FILTER_STAGE0EXISTS = 0x040, // 01000000
        FILTER_STAGE0ANALOG = 0x080, // 10000000
        FILTER_STAGE0___ALL = 0x0C0, // 11000000
        Stage0NoExist = 0x000, /***/ // 00------ // Button has no capsense
        Stage0Digital = 0x040, /***/ // 01------ // Button has capsense that is off/on
        Stage0Analog_ = 0x0C0, /***/ // 11------ // Button has capsense with degree of contact
    }

    public class ButtonState
    {
        public ButtonStateFlags Flags { get; set; }
        public float[] Analog { get; set; } // Array of float values in the order Stage1, Stage2, Stage0, but digital only values are skipped

        public float Stage1
        {
            get
            {
                return Flags.HasFlag(ButtonStateFlags.Stage1Analog_) ? Analog[0] : Flags.HasFlag(ButtonStateFlags.Stage1DValue_) ? 1.0f : 0.0f;
            }
            set
            {
                if (Flags.HasFlag(ButtonStateFlags.Stage1Analog_))
                    Analog[0] = value;
                Flags = Flags & ~ButtonStateFlags.Stage1DValue_ | (value > 0 ? ButtonStateFlags.Stage1DValue_ : ButtonStateFlags.Empty);
            }
        }
        public float Stage2
        {
            get
            {
                return Flags.HasFlag(ButtonStateFlags.Stage2Analog_) ? Analog[Flags.CountFlagCount(ButtonStateFlags.Stage1Analog_)] : Flags.HasFlag(ButtonStateFlags.Stage2DValue_) ? 1.0f : 0.0f;
            }
            set
            {
                if (Flags.HasFlag(ButtonStateFlags.Stage2Analog_))
                    Analog[Flags.CountFlagCount(ButtonStateFlags.Stage1Analog_)] = value;
                Flags = Flags & ~ButtonStateFlags.Stage2DValue_ | (value > 0 ? ButtonStateFlags.Stage2DValue_ : ButtonStateFlags.Empty);
            }
        }
        public float Stage0
        {
            get
            {
                return Flags.HasFlag(ButtonStateFlags.Stage0Analog_) ? Analog[Flags.CountFlagCount(ButtonStateFlags.Stage1Analog_ | ButtonStateFlags.Stage2Analog_)] : Flags.HasFlag(ButtonStateFlags.Stage0DValue_) ? 1.0f : 0.0f;
            }
            set
            {
                if (Flags.HasFlag(ButtonStateFlags.Stage0Analog_))
                    Analog[Flags.CountFlagCount(ButtonStateFlags.Stage1Analog_ | ButtonStateFlags.Stage2Analog_)] = value;
                Flags = Flags & ~ButtonStateFlags.Stage0DValue_ | (value > 0 ? ButtonStateFlags.Stage0DValue_ : ButtonStateFlags.Empty);
            }
        }
    }

    public class ControlButton : IControl
    {
        public ButtonProperties Properties { get; private set; }
        public ButtonState State { get; private set; }


        /*public ButtonState GetState()
        {
            // Clone the valid button config flags and inject analog button values
            ButtonStateFlags Flags = (ButtonStateFlags)(Properties & ButtonProperties.TOOL_QuickButtonConfig)
                                   | (DigitalStage1 ? ButtonStateFlags.Stage1DValue_ : ButtonStateFlags.Empty)
                                   | (DigitalStage2 ? ButtonStateFlags.Stage2DValue_ : ButtonStateFlags.Empty)
                                   | (DigitalStage0 ? ButtonStateFlags.Stage0DValue_ : ButtonStateFlags.Empty);

            // Create enough float array items to hold the analog values, store the analogs
            float[] AnalogData = new float[Flags.CountFlagCount(ButtonStateFlags.Stage1Analog_ | ButtonStateFlags.Stage2Analog_ | ButtonStateFlags.Stage0Analog_)];
            int idx = 0;
            if (Properties.HasFlag(ButtonStateFlags.Stage1Analog_)) AnalogData[idx++] = AnalogStage1;
            if (Properties.HasFlag(ButtonStateFlags.Stage2Analog_)) AnalogData[idx++] = AnalogStage2;
            if (Properties.HasFlag(ButtonStateFlags.Stage0Analog_)) AnalogData[idx++] = AnalogStage0;

            return new ButtonState()
            {
                Flags = Flags,
                Analog = AnalogData,
            };
        }*/


        public ButtonDataType HasStage1
        {
            get { return Properties.GetHasStage1(); }
            set { Properties.SetHasStage1(value); }
        }
        public float AnalogStage1
        {
            get { return State.Stage1; }
            set { State.Stage1 = value; }
        }
        public bool DigitalStage1
        {
            get { return AnalogStage1 > 0; }
            set { AnalogStage1 = value ? 1.0f : 0.0f; }
        }


        public ButtonDataType HasStage2
        {
            get { return Properties.GetHasStage2(); }
            set { Properties.SetHasStage2(value); }
        }
        public float AnalogStage2
        {
            get { return State.Stage2; }
            set { State.Stage2 = value; }
        }
        public bool DigitalStage2
        {
            get { return AnalogStage2 > 0; }
            set { AnalogStage2 = value ? 1.0f : 0.0f; }
        }


        public ButtonDataType HasStage0
        {
            get { return Properties.GetHasStage0(); }
            set { Properties.SetHasStage0(value); }
        }
        public float AnalogStage0
        {
            get { return State.Stage0; }
            set { State.Stage0 = value; }
        }
        public bool DigitalStage0
        {
            get { return AnalogStage0 > 0; }
            set { AnalogStage0 = value ? 1.0f : 0.0f; }
        }


        public ControlButton(ButtonProperties Properties)
        {
            this.Properties = Properties;
            //this.State.Flags = Properties.GetHasStage1() == ButtonDataType.Analog ? ButtonStageFlags.Stage1Analog_

            // Clone the valid button config flags and inject analog button values
            ButtonStateFlags Flags = (ButtonStateFlags)(Properties & ButtonProperties.TOOL_QuickButtonConfig);

            // Create enough float array items to hold the analog values, store the analogs
            float[] AnalogData = new float[Flags.CountFlagCount(ButtonStateFlags.Stage1Analog_ | ButtonStateFlags.Stage2Analog_ | ButtonStateFlags.Stage0Analog_)];

            this.State = new ButtonState()
            {
                Flags = Flags,
                Analog = AnalogData,
            };
        }

        public T Value<T>(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "click")
                return (T)Convert.ChangeType(DigitalStage1, typeof(T));
            return default;
        }
        public Type Type(string key)
        {
            return typeof(bool);
        }

        public object Clone()
        {
            ControlButton newData = new ControlButton(this.Properties);

            newData.DigitalStage1 = this.DigitalStage1;

            return newData;
        }
    }
}
