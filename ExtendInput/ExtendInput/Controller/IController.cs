using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;

namespace ExtendInput.Controller
{
    public enum EConnectionType
    {
        Unknown,
        USB,
        Bluetooth,
        Dongle,
        Virtual,
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
        /// This device will poll once and then switch to inactive
        /// </summary>
        RunOnce,

        /// <summary>
        /// This device will poll until it gathers needed information, then stop
        /// </summary>
        RunUntilReady,

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

    public delegate void ControllerNameUpdateEvent(IController sender);
    public delegate void ControllerStateUpdateEvent(IController sender, ControlCollection controls);
    public interface IController : IDisposable
    {
        event ControllerNameUpdateEvent ControllerMetadataUpdate;
        event ControllerStateUpdateEvent ControllerStateUpdate;

        EConnectionType ConnectionType { get; }
        string[] ConnectionTypeCode { get; }
        string[] ControllerTypeCode { get; }
        bool HasSelectableAlternatives { get; }
        Dictionary<string, string> Alternates { get; }
        string Name { get; }
        string[] NameDetails { get; }

        /// <summary>
        /// Connection ID, set my OS, may have source prefixed
        /// </summary>
        string ConnectionUniqueID { get; }

        /// <summary>
        /// Device ID, based on MAC or Serial Number, may be null or change
        /// </summary>
        string DeviceUniqueID { get; }

        IDevice DeviceHackRef { get; }
        bool HasMotion { get; }

        /// <summary>
        /// Is the device ready to be used?
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Is the device present? A dongle with no device connected is not present.
        /// </summary>
        bool IsPresent { get; }

        /// <summary>
        /// Is the device virtual? Virtual devices have no hardware and are used to artificially output controller input.
        /// </summary>
        bool IsVirtual { get; }

        Dictionary<string, dynamic> DeviceProperties { get; }

        void DeInitalize();

        //[Obsolete("Need a proxy for the event instead of giving the State object this way", false)]
        //ControllerState GetState();
        void Initalize();
        void Identify();
        void SetActiveAlternateController(string ControllerID);

        /// <summary>
        /// Possible temp function, not sure yet
        /// </summary>
        /// <param name="control"></param>
        /// <param name="state"></param>
        bool SetControlState(string control, string state);
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

        public static short ProcSignedByteNybble(short b)
        {
            if ((b & 0x0800) == 0x0800)
                //b |= (short)0xf000;
                b |= -4096;
            return b;
        }
    }
}
