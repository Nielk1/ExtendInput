using ExtendInput.Controls;
using ExtendInput.DeviceProvider;

namespace ExtendInput.Controller
{
    public enum EConnectionType
    {
        Unknown,
        USB,
        Bluetooth,
        Dongle,
    }
    public delegate void ControllerNameUpdateEvent();
    public interface IController
    {
        event ControllerNameUpdateEvent ControllerNameUpdated;

        EConnectionType ConnectionType { get; }
        IDevice DeviceHackRef { get; }
        bool HasMotion { get; }

        void DeInitalize();
        ControllerState GetState();
        void Initalize();
        void Identify();
        string GetName();
    }

    public static class ControllerMathTools
    {
        /// <summary>
        /// Convert stick byte to float, assumes stick center is 0x80, min is 0x00, and max is 0xff
        /// </summary>
        /// <param name="val">raw byte value</param>
        /// <returns>float value from -1.0f to 1.0f</returns>
        public static float QuickStickToFloat(byte val)
        {
            float r = (val - 128) / 127f;
            if (r < -1.0f)
                return -1.0f;
            return r;
        }
    }
}
