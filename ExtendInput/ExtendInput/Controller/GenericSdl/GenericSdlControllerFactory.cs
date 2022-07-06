using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controller.GenericSdl
{
    public class GenericSdlControllerFactory : IControllerFactory
    {
        private AccessMode AccessMode;
        private Dictionary<string, GenericSdlController> Controllers = new Dictionary<string, GenericSdlController>();
        public Dictionary<string, dynamic>[] DeviceWhitelist => null;

        public GenericSdlControllerFactory(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
        }

        public IController NewDevice(IDevice device)
        {
            SdlDevice _device = device as SdlDevice;

            if (_device == null)
                return null;

            lock (Controllers)
            {
                GenericSdlController ctrl = null;
                if (Controllers.ContainsKey(device.UniqueKey))
                {
                    // TODO handle subdevices, such as the audio device
                    //ctrl = Controllers[ContrainerID.Value];
                    //ctrl.AddDevice(_device);
                }
                else
                {
                    Controllers[device.UniqueKey] = new GenericSdlController(_device, AccessMode);
                    ctrl = Controllers[device.UniqueKey];
                }

                return ctrl;
            }

            return null;
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                if (!Controllers.ContainsKey(UniqueKey))
                    return null;
                GenericSdlController ctrl = Controllers[UniqueKey];
                string UniqueControllerId = ctrl.ConnectionUniqueID;

                ctrl.DeInitalize();
                ctrl.Dispose();
                Controllers.Remove(UniqueKey);

                return UniqueControllerId;
            }
        }
    }
}
