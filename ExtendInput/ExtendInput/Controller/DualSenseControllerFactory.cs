using ExtendInput.DeviceProvider;
using System.Collections.Generic;
using System.Linq;

namespace ExtendInput.Controller
{
    public class DualSenseControllerFactory : IControllerFactory
    {
        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", DualSenseController.VendorId }, { "PID", DualSenseController.ProductId } },
        };
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

            DualSenseController ctrl = new DualSenseController(_device, ConType);
            ctrl.HalfInitalize();
            return ctrl;
        }

        public string RemoveDevice(string UniqueKey)
        {
            // TODO IMPLEMENT
            return null;
        }
    }
}
