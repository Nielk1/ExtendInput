using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtendInput.Controller;
using ExtendInput.DeviceProvider;

namespace ExtendInput
{
    public class DeviceManager
    {
        List<IDeviceProvider> CoreDeviceProviders;
        List<IControllerFactory> DeviceProviders;

        public event ControllerChangeEventHandler ControllerAdded;
        public event DeviceChangeEventHandler ControllerRemoved;

        public DeviceManager()
        {
            CoreDeviceProviders = new List<IDeviceProvider>();
            DeviceProviders = new List<IControllerFactory>();

            foreach (Type item in typeof(IDeviceProvider).GetTypeInfo().Assembly.GetTypes())
            {
                //if (!item.IsClass) continue;
                if (item.GetInterfaces().Contains(typeof(IDeviceProvider)))
                {
                    ConstructorInfo[] cons = item.GetConstructors();
                    foreach (ConstructorInfo con in cons)
                    {
                        try
                        {
                            ParameterInfo[] @params = con.GetParameters();
                            object[] paramList = new object[@params.Length];
                            // don't worry about paramaters for now
                            //for (int i = 0; i < @params.Length; i++)
                            //{
                            //    paramList[i] = ServiceProvider.GetService(@params[i].ParameterType);
                            //}

                            IDeviceProvider plugin = (IDeviceProvider)Activator.CreateInstance(item, paramList);
                            CoreDeviceProviders.Add(plugin);

                            break;
                        }
                        catch { }
                    }
                }
            }

            foreach (Type item in typeof(IControllerFactory).GetTypeInfo().Assembly.GetTypes())
            {
                if (item.GetInterfaces().Contains(typeof(IControllerFactory)))
                {
                    ConstructorInfo[] cons = item.GetConstructors();
                    foreach (ConstructorInfo con in cons)
                    {
                        try
                        {
                            ParameterInfo[] @params = con.GetParameters();
                            object[] paramList = new object[@params.Length];
                            // don't worry about paramaters for now
                            //for (int i = 0; i < @params.Length; i++)
                            //{
                            //    paramList[i] = ServiceProvider.GetService(@params[i].ParameterType);
                            //}

                            IControllerFactory plugin = (IControllerFactory)Activator.CreateInstance(item, paramList);
                            DeviceProviders.Add(plugin);

                            break;
                        }
                        catch { }
                    }
                }
            }

            foreach (IDeviceProvider deviceProvider in CoreDeviceProviders)
            {
                deviceProvider.DeviceAdded += DeviceAdded;
                deviceProvider.DeviceRemoved += DeviceRemoved;
            }
        }

        private void DeviceAdded(object sender, IDevice e)
        {
            foreach (IControllerFactory factory in DeviceProviders)
            {
                IController d = factory.NewDevice(e);
                if (d != null)
                {
                    ControllerChangeEventHandler threadSafeEventHandler = ControllerAdded;
                    threadSafeEventHandler?.Invoke(this, d);
                }
            }
        }

        private void DeviceRemoved(object sender, IDevice e)
        {
            DeviceChangeEventHandler threadSafeEventHandler = ControllerRemoved;
            threadSafeEventHandler?.Invoke(this, e);
        }

        public void ScanNow()
        {
            foreach (IDeviceProvider provider in CoreDeviceProviders)
            {
                provider.ScanNow();
            }
        }
    }

    public delegate void ControllerChangeEventHandler(object sender, IController e);
    public delegate void DeviceChangeEventHandler(object sender, IDevice e);
}
