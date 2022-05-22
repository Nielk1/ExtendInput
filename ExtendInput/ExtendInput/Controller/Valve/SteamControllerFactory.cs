using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ExtendInput.Controller.Valve
{
    public class SteamControllerFactory : IControllerFactory
    {
        //Dictionary<string, WeakReference<SemaphoreSlim>> SharedDongleLocks = new Dictionary<string, WeakReference<SemaphoreSlim>>();
        Dictionary<Guid, WeakReference<SemaphoreSlim>> SharedDongleLocks = new Dictionary<Guid, WeakReference<SemaphoreSlim>>();
        private Dictionary<string, SteamController> Controllers = new Dictionary<string, SteamController>();

        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", SteamController.VendorId }, { "PID", SteamController.ProductIdDongle } },
            new Dictionary<string, dynamic>(){ { "VID", SteamController.VendorId }, { "PID", SteamController.ProductIdWired } },
            new Dictionary<string, dynamic>(){ { "VID", SteamController.VendorId }, { "PID", SteamController.ProductIdBT } },
            new Dictionary<string, dynamic>(){ { "VID", SteamController.VendorId }, { "PID", SteamController.ProductIdChell } },
        };

        public IController NewDevice(IDevice device)
        {
            HidDevice _device = device as HidDevice;

            if (_device == null)
                return null;

            if (_device.VendorId != SteamController.VendorId)
                return null;

            if (!new int[] {
                SteamController.ProductIdDongle,
                SteamController.ProductIdWired,
                SteamController.ProductIdBT,
                SteamController.ProductIdChell,
            }.Contains(_device.ProductId))
                return null;

            {
                string devicePath = _device.DevicePath.ToString();

                uint[] Usages = device.Properties.ContainsKey("Usages") ? device.Properties["Usages"] as uint[] : null;

                EConnectionType ConType = EConnectionType.Unknown;
                SteamController.EControllerType CtrlType = SteamController.EControllerType.Gordon;
                switch (_device.ProductId)
                {
                    case SteamController.ProductIdBT:
                        if (!Usages.Contains(0xff000001u)) // skip anything that isn't the controller's custom HID device
                            return null;
                        ConType = EConnectionType.Bluetooth;
                        break;
                    case SteamController.ProductIdWired:
                        if (!Usages.Contains(0xff000001u)) // skip anything that isn't the controller's custom HID device
                            return null;
                        ConType = EConnectionType.USB;
                        break;
                    case SteamController.ProductIdDongle:
                        if (!Usages.Contains(0xff000001u)) // skip anything that isn't the controller's custom HID device
                            return null;
                        ConType = EConnectionType.Dongle;
                        break;
                    case SteamController.ProductIdChell:
                        //if (!devicePath.Contains("mi_02")) return null; // skip odd 2nd device
                        if (!Usages.Contains(0xff000001u)) // TODO: confirm this is the correct vendor and page for the Chell SC
                            return null;
                        ConType = EConnectionType.USB;
                        CtrlType = SteamController.EControllerType.Chell;
                        break;
                }

                SemaphoreSlim SharedDongleLock = null;
                if (ConType == EConnectionType.Dongle)
                {
                    string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(_device.DevicePath);
                    Guid? ContrainerID = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId);
                    if (ContrainerID.HasValue)
                        lock (SharedDongleLocks)
                        {
                            if (!SharedDongleLocks.ContainsKey(ContrainerID.Value) || !SharedDongleLocks[ContrainerID.Value].TryGetTarget(out SharedDongleLock))
                            {
                                SharedDongleLock = new SemaphoreSlim(1);
                                SharedDongleLocks.Add(ContrainerID.Value, new WeakReference<SemaphoreSlim>(SharedDongleLock));
                            }

                            Console.WriteLine(ContrainerID.Value.ToString());

                            // clear any dead refs
                            // TODO consider switching to a permit counting storage method instead as WeakRefs only eject on higher memory pressure
                            foreach (Guid key in SharedDongleLocks.Keys.ToList()) // ToList to clone the key list so we can modify it
                                if (!SharedDongleLocks[key].TryGetTarget(out _))
                                    SharedDongleLocks.Remove(key);
                        }
                }

                SteamController ctrl = new SteamController(_device, SharedDongleLock, ConType, CtrlType);
                lock (Controllers)
                    Controllers[ctrl.ConnectionUniqueID] = ctrl;
                //ctrl.HalfInitalize();
                return ctrl;
            }
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                //Console.WriteLine($"Removing {UniqueKey}");
                if (Controllers.ContainsKey(UniqueKey))
                {
                    SteamController ctrl = Controllers[UniqueKey];
                    string UniqueControllerId = ctrl.ConnectionUniqueID;

                    // TODO deal with parent/child map?

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
