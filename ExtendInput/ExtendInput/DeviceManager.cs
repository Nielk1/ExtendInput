﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExtendInput.Controller;
using ExtendInput.Providers;

namespace ExtendInput
{
    public class DeviceManager
    {
        List<ICoreDeviceProvider> CoreDeviceProviders;
        List<IDeviceProvider> DeviceProviders;

        public event ControllerChangeEventHandler ControllerAdded;
        public event DeviceChangeEventHandler ControllerRemoved;

        public DeviceManager()
        {
            CoreDeviceProviders = new List<ICoreDeviceProvider>();
            DeviceProviders = new List<IDeviceProvider>();

            foreach (Type item in typeof(ICoreDeviceProvider).GetTypeInfo().Assembly.GetTypes())
            {
                //if (!item.IsClass) continue;
                if (item.GetInterfaces().Contains(typeof(ICoreDeviceProvider)))
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

                            ICoreDeviceProvider plugin = (ICoreDeviceProvider)Activator.CreateInstance(item, paramList);
                            CoreDeviceProviders.Add(plugin);

                            break;
                        }
                        catch { }
                    }
                }
            }

            foreach (Type item in typeof(IDeviceProvider).GetTypeInfo().Assembly.GetTypes())
            {
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

            foreach (ICoreDeviceProvider deviceProvider in CoreDeviceProviders)
            {
                deviceProvider.DeviceAdded += DeviceAdded;
                deviceProvider.DeviceRemoved += DeviceRemoved;
            }
        }

        private void DeviceAdded(object sender, IDevice e)
        {
            foreach (IDeviceProvider factory in DeviceProviders)
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
            foreach (ICoreDeviceProvider provider in CoreDeviceProviders)
            {
                provider.ScanNow();
            }
        }
    }

    public delegate void ControllerChangeEventHandler(object sender, IController e);
    public delegate void DeviceChangeEventHandler(object sender, IDevice e);
}
