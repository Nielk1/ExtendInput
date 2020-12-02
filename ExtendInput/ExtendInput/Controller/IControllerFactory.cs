using ExtendInput.DeviceProvider;

namespace ExtendInput.Controller
{
    public interface IControllerFactory
    {
        IController NewDevice(IDevice device);
    }
}
