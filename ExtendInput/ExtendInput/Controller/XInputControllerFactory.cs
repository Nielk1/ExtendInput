using ExtendInput.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controller
{
    public class XInputControllerFactory : IControllerFactory
    {
        public IController NewDevice(IDevice device)
        {
            XInputDevice _device = device as XInputDevice;

            if (_device == null)
                return null;

            {
                XInputController ctrl = new XInputController(_device);
                ctrl.HalfInitalize();
                return ctrl;
            }
        }
    }
}
