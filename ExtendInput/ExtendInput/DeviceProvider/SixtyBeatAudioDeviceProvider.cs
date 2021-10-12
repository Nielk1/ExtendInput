using ExtendInput.Controller;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace ExtendInput.DeviceProvider
{
    [DeviceProvider(TypeString = "60beat Audio", TypeCode = "60BA", SupportsAutomaticDetection = false, SupportsManualyQuery = true, RequiresManualConfiguration = true)]
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

        public IDeviceManualTriggerContext ManualTrigger(DeviceManualTriggerContextOption Option = null)
        {
            if (Option != null)
            {
                DeviceChangeEventHandler threadSafeEventHandler = DeviceAdded;
                SixtyBeatAudioDevice device = SixtyBeatAudioDevice.Create(Option.Tag as string);
                if (device != null)
                    threadSafeEventHandler?.Invoke(this, device);
                return null;
            }

            SixtyBeatAudioDeviceManualTriggerContext ResponseData = new SixtyBeatAudioDeviceManualTriggerContext();
            ResponseData.Options = new List<DeviceManualTriggerContextOption>();

            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            //cycle through all audio devices
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                // these happen to enumate the same order
                NAudio.CoreAudioApi.MMDevice dev = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active)[i];

                string DeviceID = dev.Properties[new NAudio.CoreAudioApi.PropertyKey(DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Audio_InstanceId.fmtid, (int)DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Audio_InstanceId.pid)].Value.ToString();
                if (!SixtyBeatAudioDevice.DeviceKnown(DeviceID))
                    ResponseData.Options.Add(new DeviceManualTriggerContextOption(dev.FriendlyName, DeviceID));
            }
            enumerator.Dispose();

            return ResponseData;
        }

        public void RegisterWhitelist(Dictionary<string, dynamic>[] deviceWhitelist)
        { }
    }

    public class SixtyBeatAudioDeviceManualTriggerContext : IDeviceManualTriggerContext
    {
        public List<DeviceManualTriggerContextOption> Options { get; set; }
    }

    public class DeviceManualTriggerContextOption
    {
        public string Name { get; set; }
        public object Tag { get; set; }

        public DeviceManualTriggerContextOption(string Name, object Tag)
        {
            this.Name = Name;
            this.Tag = Tag;
        }
    }

    public interface IDeviceManualTriggerContext
    {
        List<DeviceManualTriggerContextOption> Options { get; set; }
    }
}