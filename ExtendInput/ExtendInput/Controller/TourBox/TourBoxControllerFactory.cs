using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.TourBox
{
    public class TourBoxControllerFactory : IControllerFactory
    {
        private AccessMode AccessMode;
        //private object DeviceControllerMapLock = new object(); // use the locking of Controllers with these for now
        private Dictionary<string, Guid> DeviceToControllerKeyMap = new Dictionary<string, Guid>();
        private Dictionary<Guid, HashSet<string>> ControllerToDeviceKeyMap = new Dictionary<Guid, HashSet<string>>();
        public TourBoxControllerFactory(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
        }

        Dictionary<string, TourBoxController> Controllers = new Dictionary<string, TourBoxController>();
        public class ControllerPair
        {
            public bool bVendor;
            public bool bGamepad;
            // TOOD: if these are both weak references, are we literally just praying we get both before a race condition eliminates one?
            public WeakReference<HidDevice> Vendor;
            public WeakReference<HidDevice> Gamepad;
        }

        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
        };

        public IController NewDevice(IDevice device)
        {
            if (this.AccessMode != AccessMode.FullControl)
                return null; // Serial port based devices are single reader/writer, so abort unless we're in full control mode

            SerialDevice deviceSerial = device as SerialDevice;

            // We dont understand this device type
            if (deviceSerial == null)
                return null;

            if (device.VendorId == TourBoxController.VENDOR_SILICON_LABS && device.ProductId == TourBoxController.PRODUCT_CP210X_UART_BRIDGE)
            {
                lock (Controllers)
                {
                    TourBoxController ctrl = null;
                    if (Controllers.ContainsKey(deviceSerial.UniqueKey))
                    {
                        // TODO handle subdevices, such as the audio device
                        //ctrl = Controllers[ContrainerID.Value];
                        //ctrl.AddDevice(_device);
                    }
                    else
                    {
                        Controllers[deviceSerial.UniqueKey] = new TourBoxController(deviceSerial);
                        ctrl = Controllers[deviceSerial.UniqueKey];
                    }

                    return ctrl;
                }
            }

            return null;
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                if (!Controllers.ContainsKey(UniqueKey))
                    return null;
                TourBoxController ctrl = Controllers[UniqueKey];
                string UniqueControllerId = ctrl.ConnectionUniqueID;

                ctrl.DeInitalize();
                ctrl.Dispose();
                Controllers.Remove(UniqueKey);

                return UniqueControllerId;
            }
        }
    }
}
