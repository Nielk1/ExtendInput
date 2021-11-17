using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ExtendInput.Controller
{
    public class SteamControllerFactory : IControllerFactory
    {
        //Dictionary<string, WeakReference<SemaphoreSlim>> SharedDongleLocks = new Dictionary<string, WeakReference<SemaphoreSlim>>();
        Dictionary<Guid, WeakReference<SemaphoreSlim>> SharedDongleLocks = new Dictionary<Guid, WeakReference<SemaphoreSlim>>();

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

                EConnectionType ConType = EConnectionType.Unknown;
                SteamController.EControllerType CtrlType = SteamController.EControllerType.Gordon;
                switch (_device.ProductId)
                {
                    case SteamController.ProductIdBT:
                        if (!devicePath.Contains("col03")) return null; // skip anything that isn't the controller's custom HID device
                        ConType = EConnectionType.Bluetooth;
                        break;
                    case SteamController.ProductIdWired:
                        if (!devicePath.Contains("mi_02")) return null; // skip anything that isn't the controller's custom HID device
                        ConType = EConnectionType.USB;
                        break;
                    case SteamController.ProductIdDongle:
                        if (devicePath.Contains("mi_00")) return null; // skip the dongle itself
                        ConType = EConnectionType.Dongle;
                        break;
                    case SteamController.ProductIdChell:
                        if (!devicePath.Contains("mi_02")) return null; // skip odd 2nd device
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
                            foreach (Guid key in SharedDongleLocks.Keys.ToList()) // ToList to clone the key list so we can modify it
                                if (!SharedDongleLocks[key].TryGetTarget(out _))
                                    SharedDongleLocks.Remove(key);
                        }
                }

                SteamController ctrl = new SteamController(_device, SharedDongleLock, ConType, CtrlType);
                //ctrl.HalfInitalize();
                return ctrl;
            }
        }

        public string RemoveDevice(string UniqueKey)
        {
            // TODO IMPLEMENT
            return null;
        }
    }
}
