using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtendInput.Controller;
using ExtendInput.DeviceProvider;
using System.Diagnostics;

namespace ExtendInput
{
    public enum AccesMode
    {
        ReadOnly,
        SafeWriteOnly,
        FullControl,
    }
    public class DeviceManager
    {
        List<IDeviceProvider> DeviceProviders;
        List<IControllerFactory> ControllerFactories;

        public event ControllerChangeEventHandler ControllerAdded;
        public event DeviceChangeEventHandler ControllerRemoved;

        private AccesMode AccessMode;

        public DeviceManager(AccesMode AccessMode = AccesMode.SafeWriteOnly)
        {
            DeviceProviders = new List<IDeviceProvider>();
            ControllerFactories = new List<IControllerFactory>();
            this.AccessMode = AccessMode;

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
                            DeviceProviders.Add(plugin);

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
                            ControllerFactories.Add(plugin);

                            break;
                        }
                        catch { }
                    }
                }
            }

            foreach (IDeviceProvider deviceProvider in DeviceProviders)
            {
                deviceProvider.DeviceAdded += DeviceAdded;
                deviceProvider.DeviceRemoved += DeviceRemoved;
            }
        }

        private void DeviceAdded(object sender, IDevice e)
        {
            foreach (IControllerFactory factory in ControllerFactories)
            {
                IController d = factory.NewDevice(e);
                if (d != null)
                {
                    ControllerChangeEventHandler threadSafeEventHandler = ControllerAdded;
                    threadSafeEventHandler?.Invoke(this, d);

                    Debug.WriteLine($"New Device<{e.GetType()}>({e.UniqueKey}) with Properties:{string.Join(string.Empty, e.Properties?.Select(dr => $"\r\n[{dr.Key}]={dr.Value}"))}\r\n");
                }
            }
        }

        private void DeviceRemoved(object sender, IDevice e)
        {
            DeviceChangeEventHandler threadSafeEventHandler = ControllerRemoved;
            // TODO Must Dispose controller here, which means we need to keep a device to controller map.  Should probably make the factories do it
            threadSafeEventHandler?.Invoke(this, e);
        }

        public void ScanNow()
        {
            foreach (IDeviceProvider provider in DeviceProviders)
            {
                provider.ScanNow();
            }
        }

        public List<IDeviceProvider> GetManualDeviceProviders()
        {
            return DeviceProviders.Where(dr => (Attribute.GetCustomAttribute(dr.GetType(), typeof(DeviceProviderAttribute)) as DeviceProviderAttribute)?.RequiresManualConfiguration ?? false).ToList();
        }
    }

    public delegate void ControllerChangeEventHandler(object sender, IController e);
    public delegate void DeviceChangeEventHandler(object sender, IDevice e);
}
