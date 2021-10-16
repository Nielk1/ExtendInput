using System;

namespace ExtendInput.Controls
{
    public enum ButtonDataType : byte
    {
        //FILTER = 0x03, // 00000011

        NoExist = 0x00, // ------00
        Digital = 0x01, // ------01
        Analog  = 0x03, // ------11
    }
    public enum ButtonEventType : byte
    {
        //FILTER = 0x03, // 00000011

        Hold    = 0x00, // ------00
        Press   = 0x01, // ------01
        Release = 0x03, // ------11
    }
    public enum ButtonTravelDirection : byte
    {
        //FILTER = 0x01, // 00000001

        Push = 0x00, // -------0
        Pull = 0x01, // -------1
    }
    public enum ButtonTravelMotion : byte
    {
        //FILTER = 0x01, // 00000001

        Linear = 0x00, // -------0
        Angled = 0x01, // -------1
    }
    public enum ButtonAccuracy : byte
    {
        //FILTER = 0x01, // 00000001

        Lo = 0x00, // -------0
        Hi = 0x01, // -------1
    }
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
        EventHold___ = 0x000, /*****/ // ------00 -------- // Button is normal with an up and down state
        EventPress__ = 0x100, /*****/ // ------01 -------- // button pulses on press
        EventRelease = 0x200, /*****/ // ------10 -------- // button pulses on release

        // Combo
        CMB_Bumper /***********/ = DirectionPull | TravelLinear | Stage0NoExist | Stage1Digital | Stage2NoExist | AccuracyLo,
        CMB_Trigger /**********/ = DirectionPull | TravelAngled | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyHi,
        CMB_Peddle /***********/ = DirectionPush | TravelAngled | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyHi,
        CMB_2StageTrigger /****/ = DirectionPull | TravelAngled | Stage0NoExist | Stage1Analog_ | Stage2Digital | AccuracyLo,
        CMB_2StageVRTrigger /**/ = DirectionPull | TravelAngled | Stage0Digital | Stage1Analog_ | Stage2Digital | AccuracyLo,
        CMB_Button /***********/ = DirectionPush | TravelLinear | Stage0NoExist | Stage1Digital | Stage2NoExist | AccuracyLo,
        CMB_PressureButton /***/ = DirectionPush | TravelLinear | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyLo,
        CMB_AnalogButton /*****/ = DirectionPush | TravelLinear | Stage0NoExist | Stage1Analog_ | Stage2NoExist | AccuracyLo,
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


        public static ButtonTravelDirection GetDirection(ref this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.DirectionPull) ? ButtonTravelDirection.Pull : ButtonTravelDirection.Push;
        }
        public static void SetDirection(ref this ButtonProperties type, ButtonTravelDirection value)
        {
            type.SetFlag(value == ButtonTravelDirection.Pull ? ButtonProperties.DirectionPull : ButtonProperties.DirectionPush, ButtonProperties.FILTER_DIRECTION);
        }

        public static ButtonTravelMotion GetTravel(ref this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.DirectionPull) ? ButtonTravelMotion.Angled : ButtonTravelMotion.Linear;
        }
        public static void SetTravel(ref this ButtonProperties type, ButtonTravelMotion value)
        {
            type.SetFlag(value == ButtonTravelMotion.Angled ? ButtonProperties.TravelAngled : ButtonProperties.TravelLinear, ButtonProperties.FILTER_TRAVEL);
        }

        public static ButtonAccuracy GetAccuracy(ref this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.DirectionPull) ? ButtonAccuracy.Hi : ButtonAccuracy.Lo;
        }
        public static void SetAccuracy(ref this ButtonProperties type, ButtonAccuracy value)
        {
            type.SetFlag(value == ButtonAccuracy.Hi ? ButtonProperties.AccuracyHi : ButtonProperties.AccuracyLo, ButtonProperties.FILTER_ACCURACY);
        }

        public static ButtonDataType GetHasStage0(ref this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.FILTER_STAGE0EXISTS) ? type.HasFlag(ButtonProperties.FILTER_STAGE0ANALOG) ? ButtonDataType.Analog : ButtonDataType.Digital : ButtonDataType.NoExist;
        }
        public static void SetHasStage0(ref this ButtonProperties type, ButtonDataType value)
        {
            type.SetFlag(value == ButtonDataType.NoExist ? ButtonProperties.Stage0NoExist : value == ButtonDataType.Analog ? ButtonProperties.Stage0Analog_ : ButtonProperties.Stage0Digital, ButtonProperties.FILTER_STAGE0___ALL);
        }

        public static ButtonDataType GetHasStage1(ref this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.FILTER_STAGE1ANALOG) ? ButtonDataType.Analog : ButtonDataType.Digital;
        }
        public static void SetHasStage1(ref this ButtonProperties type, ButtonDataType value)
        {
            if (value == ButtonDataType.NoExist) throw new ArgumentException("Stage1 must exist");
            type.SetFlag(value == ButtonDataType.Analog ? ButtonProperties.Stage1Analog_ : ButtonProperties.Stage1Digital, ButtonProperties.FILTER_STAGE1___ALL);
        }

        public static ButtonDataType GetHasStage2(ref this ButtonProperties type)
        {
            return type.HasFlag(ButtonProperties.FILTER_STAGE2EXISTS) ? type.HasFlag(ButtonProperties.FILTER_STAGE2ANALOG) ? ButtonDataType.Analog : ButtonDataType.Digital : ButtonDataType.NoExist;
        }
        public static void SetHasStage2(ref this ButtonProperties type, ButtonDataType value)
        {
            type.SetFlag(value == ButtonDataType.NoExist ? ButtonProperties.Stage2NoExist : value == ButtonDataType.Analog ? ButtonProperties.Stage2Analog_ : ButtonProperties.Stage2Digital, ButtonProperties.FILTER_STAGE2___ALL);
        }
    }

    public class ControlButton : IControl
    {
        public ButtonProperties Properties;

        public ButtonDataType HasStage1
        {
            get { return Properties.GetHasStage1(); }
            set { Properties.SetHasStage1(value); }
        }
        public float Analog { get; set; }
        public bool Digital
        {
            get { return Analog > 0; }
            set { Analog = value ? 1.0f : 0.0f; }
        }


        public ButtonDataType HasStage2
        {
            get { return Properties.GetHasStage2(); }
            set { Properties.SetHasStage2(value); }
        }
        public float AnalogStage2 { get; set; }
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
        public float AnalogStage0 { get; set; }
        public bool DigitalStage0
        {
            get { return AnalogStage0 > 0; }
            set { AnalogStage0 = value ? 1.0f : 0.0f; }
        }



        public ControlButton(ButtonProperties Properties)
        {
            this.Properties = Properties;
        }

        public T Value<T>(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "click")
                return (T)Convert.ChangeType(Digital, typeof(T));
            return default;
        }
        public Type Type(string key)
        {
            return typeof(bool);
        }

        public object Clone()
        {
            ControlButton newData = new ControlButton(this.Properties);

            newData.Digital = this.Digital;

            return newData;
        }
    }
}
