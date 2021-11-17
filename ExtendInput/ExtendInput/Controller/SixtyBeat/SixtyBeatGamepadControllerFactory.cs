using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controller.SixtyBeat
{
    public class SixtyBeatGamepadControllerFactory : IControllerFactory
    {
        public Dictionary<string, dynamic>[] DeviceWhitelist => null;
        public IController NewDevice(IDevice device)
        {
            SixtyBeatAudioDevice _device = device as SixtyBeatAudioDevice;

            if (_device == null)
                return null;

            {
                SixtyBeatGamepadController ctrl = new SixtyBeatGamepadController(_device);
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
