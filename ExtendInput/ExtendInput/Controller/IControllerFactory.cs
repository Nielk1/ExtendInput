using ExtendInput.DeviceProvider;
using System.Collections.Generic;

namespace ExtendInput.Controller
{
    public interface IControllerFactory
    {
        Dictionary<string, dynamic>[] DeviceWhitelist { get; }

        IController NewDevice(IDevice device);
        string RemoveDevice(string UniqueKey);
    }
}
