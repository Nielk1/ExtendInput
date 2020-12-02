using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ExtendInput.DeviceProvider
{
    [CoreDeviceProvider(TypeString = "Audio", SupportsAutomaticDetection = false, SupportsManualyQuery = true, RequiresManualConfiguration = true)]
    public class AudioDeviceProvider : IDeviceProvider
    {

        public event DeviceChangeEventHandler DeviceAdded;
        public event DeviceChangeEventHandler DeviceRemoved;

        public AudioDeviceProvider()
        {
        }

        public void ScanNow()
        {
            //lock (lock_device_list)
            {
                try
                {
                    
                }
                catch { }
            }
        }
    }
}