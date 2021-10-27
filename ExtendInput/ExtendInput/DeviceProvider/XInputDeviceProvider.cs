using ExtendInput.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace ExtendInput.DeviceProvider
{
    [DeviceProvider(TypeString = "XInput", TypeCode = "XINPUT", SupportsAutomaticDetection = true, SupportsManualyQuery = true, RequiresManualConfiguration = false)]
    public class XInputDeviceProvider : IDeviceProvider
    {
        private const int MAX_SLOT = 4;

        public event DeviceChangeEventHandler DeviceAdded;
        public event DeviceChangeEventHandler DeviceRemoved;

        //HashSet<HidSharp.HidDevice> KnownDevices = new HashSet<HidSharp.HidDevice>();
        object lock_device_list = new object();
        SharpDX.XInput.Controller[] Controllers = new SharpDX.XInput.Controller[MAX_SLOT];
        bool[] ControllerActive = new bool[MAX_SLOT];

        bool AbortStatusThread = false;
        Thread CheckControllerStatusThread;

        public XInputDeviceProvider()
        {
            for (int i = 0; i < MAX_SLOT; i++)
            {
                Controllers[i] = new SharpDX.XInput.Controller((SharpDX.XInput.UserIndex)i);
                //XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                //if (XInputNative.XInputGetCapabilitiesEx(1, i, 0, ref data) == 0) { }
                //XInputNative.XInputBaseBusInformation data2 = new XInputNative.XInputBaseBusInformation();
                //if (XInputNative.XInputGetBaseBusInformation(i, ref data2) == 0) { }
            }
            CheckControllerStatusThread = new Thread(() =>
            {
                for (; ; )
                {
                    Thread.Sleep(1000);
                    ScanNow();
                    if (AbortStatusThread)
                        return;
                }
            });
            CheckControllerStatusThread.Start();
        }

        public void Dispose()
        {
            AbortStatusThread = true;
        }

        public void ScanNow()
        {
            lock (lock_device_list)
            {
                try
                {
                    for(int i=0;i< MAX_SLOT;i++)
                    {
                        if (ControllerActive[i] != Controllers[i].IsConnected)
                        {
                            if(Controllers[i].IsConnected)
                            {
                                DeviceChangeEventHandler threadSafeEventHandler = DeviceAdded;
                                threadSafeEventHandler?.Invoke(this, new XInputDevice(Controllers[i]));
                            }
                            else
                            {
                                DeviceChangeEventHandler threadSafeEventHandler = DeviceRemoved;
                                threadSafeEventHandler?.Invoke(this, new XInputDevice(Controllers[i]));
                            }
                            ControllerActive[i] = Controllers[i].IsConnected;
                        }
                    }
                }
                catch { }
            }
        }

        public IDeviceManualTriggerContext ManualTrigger(DeviceManualTriggerContextOption Option)
        {
            throw new NotImplementedException();
        }
        public void RegisterWhitelist(Dictionary<string, dynamic>[] deviceWhitelist)
        { }
    }
}