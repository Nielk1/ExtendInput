using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtendInput.Controller.Sony
{
    public class DualSenseControllerFactory : IControllerFactory
    {
        private AccessMode AccessMode;
        private Dictionary<string, Guid> DeviceToControllerKeyMap = new Dictionary<string, Guid>();
        Dictionary<Guid, DualSenseController> Controllers = new Dictionary<Guid, DualSenseController>();
        private Dictionary<Guid, HashSet<string>> ControllerToDeviceKeyMap = new Dictionary<Guid, HashSet<string>>();
        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", DualSenseController.VendorId }, { "PID", DualSenseController.ProductId } },
        };

        public DualSenseControllerFactory(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
        }

        public IController NewDevice(IDevice device)
        {
            HidDevice _device = device as HidDevice;

            if (_device == null)
                return null;

            if (_device.VendorId != DualSenseController.VendorId)
                return null;

            if (!new int[] {
                DualSenseController.ProductId,
            }.Contains(_device.ProductId))
                return null;

            string bt_hid_id = @"00001124-0000-1000-8000-00805f9b34fb";

            string devicePath = _device.DevicePath.ToString();

            EConnectionType ConType = EConnectionType.Unknown;
            //switch (_device.ProductId)
            {
                //case DualSenseController.ProductId:
                    if (devicePath.Contains(bt_hid_id))
                    {
                        ConType = EConnectionType.Bluetooth;
                    }
                    else
                    {
                        ConType = EConnectionType.USB;
                    }
                    //break;
            }

            {
                string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(_device.DevicePath);
                Guid? ContrainerID = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId);
                if (ContrainerID.HasValue)
                    lock (Controllers)
                    {
                        DualSenseController ctrl = null;
                        if (Controllers.ContainsKey(ContrainerID.Value))
                        {
                            // TODO handle subdevices, such as the audio device
                            //ctrl = Controllers[ContrainerID.Value];
                            //ctrl.AddDevice(_device);
                        }
                        else
                        {
                            Controllers[ContrainerID.Value] = new DualSenseController(_device, AccessMode, ConType);
                            ctrl = Controllers[ContrainerID.Value];
                        }

                        DeviceToControllerKeyMap[device.UniqueKey] = ContrainerID.Value;
                        if (!ControllerToDeviceKeyMap.ContainsKey(ContrainerID.Value))
                            ControllerToDeviceKeyMap[ContrainerID.Value] = new HashSet<string>();
                        ControllerToDeviceKeyMap[ContrainerID.Value].Add(device.UniqueKey);
                        return ctrl;
                    }
            }

            return null;
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                if (!DeviceToControllerKeyMap.ContainsKey(UniqueKey))
                    return null;

                Guid DeviceParent = DeviceToControllerKeyMap[UniqueKey];

                foreach (string DeviceKey in ControllerToDeviceKeyMap[DeviceParent])
                    if (DeviceToControllerKeyMap.ContainsKey(DeviceKey))
                        DeviceToControllerKeyMap.Remove(DeviceKey);

                DualSenseController ctrl = Controllers[DeviceParent];
                string UniqueControllerId = ctrl.ConnectionUniqueID;

                ctrl.DeInitalize();
                ctrl.Dispose();
                Controllers.Remove(DeviceParent);
                ControllerToDeviceKeyMap.Remove(DeviceParent);

                return UniqueControllerId;
            }
        }
    }
}
