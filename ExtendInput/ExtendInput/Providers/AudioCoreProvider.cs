using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ExtendInput.Providers
{
    [CoreDeviceProvider(TypeString = "Audio", SupportsAutomaticDetection = false, SupportsManualyQuery = true, RequiresManualConfiguration = true)]
    public class AudioCoreProvider : ICoreDeviceProvider
    {

        public event DeviceChangeEventHandler DeviceAdded;
        public event DeviceChangeEventHandler DeviceRemoved;

        public AudioCoreProvider()
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