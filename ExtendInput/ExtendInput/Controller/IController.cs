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
    public enum EPollingState
    {
        /// <summary>
        /// Device is entirely inactive
        /// </summary>
        Inactive,

        /// <summary>
        /// Device must be polled to check if child is connected
        /// </summary>
        SlowPoll,

        /// <summary>
        /// Device is active and being polled constantly
        /// </summary>
        Active,

        /// <summary>
        /// Device sends events rather than polling
        /// </summary>
        Push,
    }

    public enum EInterfaceLevel
    {
        /// <summary>
        /// Only read controller state, don't write anything
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Only write safe things to the controller, such as transient states like rumble
        /// </summary>
        SafeWriteOnly,

        /// <summary>
        /// All writes are allowed, not just transient states, this may also trigger exclusive mode
        /// </summary>
        FullControl,
    }

    public delegate void ControllerNameUpdateEvent();
    public interface IController
    {
        event ControllerNameUpdateEvent ControllerNameUpdated;

        EConnectionType ConnectionType { get; }
        string[] ConnectionTypeCode { get; }
        string[] ControllerTypeCode { get; }

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

        /// <summary>
        /// Get delta between two values accounting for overflow
        /// </summary>
        /// <param name="prev">Previous Value</param>
        /// <param name="cur">Current Value</param>
        /// <param name="overflow">Value of overflow, defaults to max for type</param>
        /// <returns></returns>
        public static byte GetOverflowedDelta(byte prev, byte cur, byte overflow = byte.MaxValue)
        {
            uint _cur = cur;
            while (_cur < prev)
                _cur += (uint)overflow + 1;
            return (byte)(_cur - prev);
        }
    }
}
