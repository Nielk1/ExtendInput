using ExtendInput.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ExtendInput.DeviceProvider
{
    [DeviceProvider(TypeString = "60beat Audio", SupportsAutomaticDetection = false, SupportsManualyQuery = true, RequiresManualConfiguration = true)]
    public class SixtyBeatAudioDeviceProvider : IDeviceProvider
    {

        public event DeviceChangeEventHandler DeviceAdded;
        public event DeviceChangeEventHandler DeviceRemoved;

        public SixtyBeatAudioDeviceProvider()
        {
        }

        public void ScanNow()
        {
        }

        public void ManualTrigger()
        {
            DeviceChangeEventHandler threadSafeEventHandler = DeviceAdded;
            threadSafeEventHandler?.Invoke(this, new SixtyBeatAudioDevice());
        }
    }
}