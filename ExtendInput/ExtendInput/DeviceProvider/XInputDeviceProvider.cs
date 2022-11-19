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

        public event DeviceAddedEventHandler DeviceAdded;
        public event DeviceRemovedEventHandler DeviceRemoved;

        //HashSet<HidSharp.HidDevice> KnownDevices = new HashSet<HidSharp.HidDevice>();
        object lock_device_list = new object();
        XInputDevice[] DeviceCache = new XInputDevice[MAX_SLOT];
        bool[] ControllerActive = new bool[MAX_SLOT];

        bool AbortStatusThread = false;
        Thread CheckControllerStatusThread;

        public XInputDeviceProvider()
        {
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
                    for (byte i = 0; i < MAX_SLOT; i++)
                    {
                        XInputNative.XInputCapabilities data = new XInputNative.XInputCapabilities();

                        if (DeviceCache[i] == null)
                        {
                            if (XInputNative.XInputGetCapabilities(i, XInputNative.ControllType.XINPUT_FLAG_ALL, ref data) == 0)
                            {
                                if (DeviceCache[i] == null)
                                    DeviceCache[i] = new XInputDevice(i);
                                DeviceAddedEventHandler threadSafeEventHandler = DeviceAdded;
                                threadSafeEventHandler?.Invoke(this, DeviceCache[i]);
                            }
                        }
                        else
                        {
                            bool connected = XInputNative.XInputGetCapabilities(i, XInputNative.ControllType.XINPUT_FLAG_ALL, ref data) == 0;
                            if (!connected)
                            {
                                DeviceRemovedEventHandler threadSafeEventHandler = DeviceRemoved;
                                threadSafeEventHandler?.Invoke(this, DeviceCache[i].UniqueKey);
                                DeviceCache[i] = null;
                            }
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