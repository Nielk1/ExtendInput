using ExtendInput.Controls;
using ExtendInput.Providers;

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

    public interface IDeviceProvider
    {
        IController NewDevice(IDevice device);
    }
}
