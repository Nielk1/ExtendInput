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
                    int FoundDevices = 0;
                    for (byte i = 0; i < MAX_SLOT; i++)
                    {
                        // XInputDevices hang around forever once they are initalized, so we only need to handle the first discovery event
                        // this might change in the future, need to talk it over
                        if (DeviceCache[i] == null)
                        {
                            XInputNative.XInputState data = new XInputNative.XInputState();
                            if (XInputNative.XInputGetState(i, ref data) == 0)
                            {
                                if (DeviceCache[i] == null)
                                    DeviceCache[i] = new XInputDevice(i);
                                DeviceAddedEventHandler threadSafeEventHandler = DeviceAdded;
                                threadSafeEventHandler?.Invoke(this, DeviceCache[i]);
                            }
                        }
                        else
                        {
                            FoundDevices++;
                        }
                    }
                    if (FoundDevices == MAX_SLOT)
                    {
                        // Since XInput nodes only spawn when they are detected the first time, if all 4 devices exist it's time to stop scanning
                        AbortStatusThread = true;
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