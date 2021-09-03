using System;

namespace ExtendInput.Controls
{
    public enum ButtonProperties : byte
    {
        // Filters
        FILTER_DIRECTION    = 0x01, // 00000001
        FILTER_TRAVEL       = 0x02, // 00000010
        FILTER_ACCURACY     = 0x04, // 00000100
        FILTER_STAGE1ANALOG = 0x08, // 00001000
        FILTER_STAGE2EXISTS = 0x10, // 00010000
        FILTER_STAGE2ANALOG = 0x20, // 00100000
        FILTER_STAGE0EXISTS = 0x40, // 01000000
        FILTER_STAGE0ANALOG = 0x80, // 10000000

        // Direction
        DirectionPush = 0x00, // -------0 // Button is pulled
        DirectionPull = 0x01, // -------1 // Button is pushed
        // Travel
        TravelLinear  = 0x00, // ------0- // Button travels in a line
        TravelAngled  = 0x02, // ------1- // Button pivots on one side
        // Accuracy
        AccuracyLo    = 0x00, // -----0-- // Button accuracy is low (or digital)
        AccuracyHi    = 0x04, // -----1-- // Button accuracy is high (high fidelity control)
        // Stage1
        Stage1Digital = 0x00, // ----0--- // Button's main operation is digital
        Stage1Analog_ = 0x08, // ----1--- // Button's main operation is analog
        // Stage2
        Stage2NoExist = 0x00, // --00---- // Button's bottom has no special function
        Stage2Digital = 0x10, // --01---- // Button's bottom has a digital operation
        Stage2Analog_ = 0x30, // --11---- // Button's bottom has an analog operation
        // Stage0
        Stage0NoExist = 0x00, // 00------ // Button has no capsense
        Stage0Digital = 0x40, // 01------ // Button has capsense that is off/on
        Stage0Analog_ = 0xC0, // 11------ // Button has capsense with degree of contact

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

    public class ControlButton : IControl
    {
        public bool Button0 { get; set; }
        public T Value<T>(string key)
        {
            if (string.IsNullOrEmpty(key) || key == "click")
                return (T)Convert.ChangeType(Button0, typeof(T));
            return default;
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
}
