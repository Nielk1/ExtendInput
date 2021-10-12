using ExtendInput.DeviceProvider;
using System.Collections.Generic;
using System.Linq;

namespace ExtendInput.Controller
{
    public class FlydigiControllerFactory : IControllerFactory
    {
        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_DONGLE_1 } },
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_DONGLE_2 } },
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_DONGLE_3 } },
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_USB } },
        };
        public IController NewDevice(IDevice device)
        {
            HidDevice _device = device as HidDevice;

            if (_device == null)
                return null;

            // if we are in full control mode, check for xbox here and if it is a flydigi controller send the command that changes the mode here and return null

            if (_device.VendorId == FlydigiController.VENDOR_FLYDIGI)
            {
                if (!new int[] {
                    FlydigiController.PRODUCT_FLYDIGI_DONGLE_1,
                    FlydigiController.PRODUCT_FLYDIGI_DONGLE_2,
                    FlydigiController.PRODUCT_FLYDIGI_DONGLE_3,
                    FlydigiController.PRODUCT_FLYDIGI_USB,
                }.Contains(_device.ProductId))
                    return null;
            }
            else
            {
                return null;
            }

            string bt_hid_id = @"00001124-0000-1000-8000-00805f9b34fb";

            string devicePath = _device.DevicePath.ToString();

            EConnectionType ConType = EConnectionType.Unknown;
            switch (_device.VendorId)
            {
                case FlydigiController.VENDOR_FLYDIGI:
                    if (_device.Properties.ContainsKey("ProductName")
                          && (((_device.Properties["ProductName"] as string)?.ToLowerInvariant()?.Contains("flydigi") ?? false) || ((_device.Properties["ProductName"] as string)?.ToLowerInvariant()?.Contains("feizhi") ?? false))
                          && devicePath.Contains("mi_02"))
                    {
                        //if (devicePath.Contains(bt_hid_id))
                        //{
                        //    ConType = EConnectionType.Bluetooth;
                        //}
                        //else
                        //{
                        //    ConType = EConnectionType.USB;
                        //}
                    }
                    else
                    {
                        return null;
                    }
                    break;
            }

            FlydigiController ctrl = new FlydigiController(_device, ConType);
            return ctrl;
        }
    }
}
