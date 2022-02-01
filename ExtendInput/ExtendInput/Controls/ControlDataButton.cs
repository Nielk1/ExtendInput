using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public enum ButtonTypeStage0 : byte
    {
        None,
        Touch, // Can detect touch
        Proximity, // Can detect proximity
    }

    public enum ButtonTypeStage1 : byte
    {
        Digital, // 0/1
        Pressure, // general pressure
        Analog, // clear large travel
    }

    public enum ButtonTypeStage2 : byte
    {
        None,
        Digital, // 0/1 button at bottom of trigger/analog
        Pressure, // FSR at bottom of button
    }
    public enum ButtonTravelMotion : byte
    {
        Linear,
        Angled,
    }

    public enum ButtonTravelDirection : byte
    {
        Push, // push away from user (not away from controller)
        Pull, // pull toward user (not toward controller)
    }
    public enum ButtonEvents : byte
    {
        State,
        DownOnly, // button fires a pulse when pressed
        UpOnly, // button fires a pulse when released
        DownAndUpOnly, // button fires a pulse when pressed or released, but we can't tell which is whichpt
    }
    public class ControlDataButtonBasic
    {
        // Physical Actuation
        public ButtonTravelMotion Motion { get; set; }
        public ButtonTravelDirection Direction { get; set; }

        // Sensor Logic
        public ButtonTypeStage1 TypeStage1 { get; set; }

        // State
        public bool Stage1 { get; set; }
    }
    public class ControlDataButtonRich
    {
        // Physical Actuation
        public ButtonTravelMotion Motion { get; set; }
        public ButtonTravelDirection Direction { get; set; }

        // Sensor Logic
        public ButtonTypeStage0 TypeStage0 { get; set; }
        public ButtonTypeStage1 TypeStage1 { get; set; }
        public ButtonTypeStage2 TypeStage2 { get; set; }

        // State
        public float Stage0 { get; set; }
        public float Stage1 { get; set; }
        public float Stage2 { get; set; }
    }
}
