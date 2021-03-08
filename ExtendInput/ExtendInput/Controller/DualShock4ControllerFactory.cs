using ExtendInput.DeviceProvider;
using System.Linq;

namespace ExtendInput.Controller
{
    public class DualShock4ControllerFactory : IControllerFactory
    {
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

            string bt_hid_id = @"00001124-0000-1000-8000-00805f9b34fb";

            string devicePath = _device.DevicePath.ToString();

            EConnectionType ConType = EConnectionType.Unknown;
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

            {
                DualShock4Controller ctrl = new DualShock4Controller(_device, ConType);
                return ctrl;
            }
        }
    }
}
