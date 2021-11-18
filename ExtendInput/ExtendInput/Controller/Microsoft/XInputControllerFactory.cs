using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controller.Microsoft
{
    public class XInputControllerFactory : IControllerFactory
    {
        public Dictionary<string, dynamic>[] DeviceWhitelist => null;
        private Dictionary<string, XInputController> Controllers = new Dictionary<string, XInputController>();
        public IController NewDevice(IDevice device)
        {
            XInputDevice _device = device as XInputDevice;

            if (_device == null)
                return null;

            {
                lock (Controllers)
                {
                    XInputController ctrl = new XInputController(_device);
                    Controllers[_device.UniqueKey] = ctrl;
                    ctrl.HalfInitalize();
                    return ctrl;
                }
            }
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                //Console.WriteLine($"Removing {UniqueKey}");
                if (Controllers.ContainsKey(UniqueKey))
                {
                    XInputController ctrl = Controllers[UniqueKey];
                    string UniqueControllerId = ctrl.ConnectionUniqueID;

                    ctrl.DeInitalize();
                    ctrl.Dispose();
                    Controllers.Remove(UniqueKey);

                    return UniqueControllerId;
                }
            }
            return null;
        }
    }
}
