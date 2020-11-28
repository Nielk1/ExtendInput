using ExtendInput.Providers;

namespace ExtendInput.Controller
{
    public interface IControllerFactory
    {
        IController NewDevice(IDevice device);
    }
}
