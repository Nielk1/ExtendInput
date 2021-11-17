using System;
using ExtendInput.DeviceProvider;
using System.Linq;
using System.Collections.Generic;

namespace ExtendInput.Controller
{
    public class DualShock4ControllerFactory : IControllerFactory
    {
        private readonly Guid CONTAINER_ID_REWASD_VIRTUAL_DS4 = new Guid(0xfbc4667d, 0xf0d7, 0x58dc, 0x84, 0x32, 0x19, 0xf7, 0x0a, 0x66, 0x0d, 0xb2);
        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", DualShock4Controller.VENDOR_SONY }, { "PID", DualShock4Controller.PRODUCT_SONY_DONGLE } },
            new Dictionary<string, dynamic>(){ { "VID", DualShock4Controller.VENDOR_SONY }, { "PID", DualShock4Controller.PRODUCT_SONY_DS4V1 } },
            new Dictionary<string, dynamic>(){ { "VID", DualShock4Controller.VENDOR_SONY }, { "PID", DualShock4Controller.PRODUCT_SONY_DS4V2 } },
            new Dictionary<string, dynamic>(){ { "VID", DualShock4Controller.VENDOR_BROOK }, { "PID", DualShock4Controller.PRODUCT_BROOK_MARS } },
        };
        public IController NewDevice(IDevice device)
        {
            HidDevice _device = device as HidDevice;

            if (_device == null)
                return null;

            if (_device.VendorId == DualShock4Controller.VENDOR_SONY)
            {
                if (!new int[] {
                    DualShock4Controller.PRODUCT_SONY_DONGLE,
                    DualShock4Controller.PRODUCT_SONY_DS4V1,
                    DualShock4Controller.PRODUCT_SONY_DS4V2,
                }.Contains(_device.ProductId))
                    return null;
            }
            else if (_device.VendorId == DualShock4Controller.VENDOR_BROOK)
            {
                if (_device.ProductId == DualShock4Controller.PRODUCT_BROOK_MARS)
                {
                    if (_device.DevicePath.Contains(@"&col01"))
                    {
                        DualShock4Controller ctrl = new DualShock4Controller(_device, EConnectionType.USB);
                        return ctrl;
                    }
                }
                return null;
            }
            else
            {
                return null;
            }

            string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(_device.DevicePath);
            bool IsVigem = DevPKey.PnpDevicePropertyAPI.GetDeviceUINumber(deviceInstanceId).HasValue;
            bool IsReWasd = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId) == CONTAINER_ID_REWASD_VIRTUAL_DS4;

            string bt_hid_id = @"00001124-0000-1000-8000-00805f9b34fb";

            string devicePath = _device.DevicePath.ToString();

            EConnectionType ConType = EConnectionType.Unknown;
            DualShock4Controller.DS4VirtualType VirtualType = DualShock4Controller.DS4VirtualType.NotVirtual;
            if (IsVigem)
            {
                ConType = EConnectionType.Virtual;
                VirtualType = DualShock4Controller.DS4VirtualType.ViGEm;
            }
            else if (IsReWasd)
            {
                ConType = EConnectionType.Virtual;
                VirtualType = DualShock4Controller.DS4VirtualType.reWASD;
            }
            else
            {
                switch (_device.ProductId)
                {
                    case DualShock4Controller.PRODUCT_SONY_DS4V1:
                    case DualShock4Controller.PRODUCT_SONY_DS4V2:
                        if (devicePath.Contains(bt_hid_id))
                        {
                            ConType = EConnectionType.Bluetooth;
                        }
                        else
                        {
                            ConType = EConnectionType.USB;
                        }
                        break;
                    case DualShock4Controller.PRODUCT_SONY_DONGLE:
                        ConType = EConnectionType.Dongle;
                        break;
                }
            }

            {
                DualShock4Controller ctrl = new DualShock4Controller(_device, ConType, VirtualType);
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
