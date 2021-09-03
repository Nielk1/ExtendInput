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
    [DeviceProvider(TypeString = "60beat Audio", SupportsAutomaticDetection = false, SupportsManualyQuery = true, RequiresManualConfiguration = true)]
    public class SixtyBeatAudioDeviceProvider : IDeviceProvider
    {

        public event DeviceChangeEventHandler DeviceAdded;
        public event DeviceChangeEventHandler DeviceRemoved;

        public SixtyBeatAudioDeviceProvider()
        {
            StringBuilder blder = new StringBuilder();

            /*for (int n = -1; n < WaveIn.DeviceCount; n++)
            {
                var caps = WaveIn.GetCapabilities(n);
                blder.AppendLine($"{n}: {caps.ProductName}");
            }*/

            /*var objSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_SoundDevice");

            var objCollection = objSearcher.Get();*/
            //foreach (var d in objCollection)
            {
                //blder.AppendLine("=====DEVICE====");
                //foreach (var p in d.Properties)
                {
                    //blder.AppendLine($"\t{p.Name}:{p.Value}");
                    /*if(p.Name == @"DeviceID")
                    {
                        Console.AppendLine($"\tDeviceIDProc:{DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId((string)p.Value)}");
                    }
                    if (p.Name == @"DeviceID")
                    {
                        blder.AppendLine($"\t{p.Name}:{p.Value}");

                        Dictionary<DevPKey.Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(p.Value.ToString());
                    }*/
                }
                //blder.AppendLine(d.Properties["DeviceID"].Value.ToString());
                //Dictionary<DevPKey.Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(d.Properties["DeviceID"].Value.ToString());
            }

            /*var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            foreach (var endpoint in enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active))
            {
                //blder.AppendLine(endpoint.FriendlyName);
                blder.AppendLine($"{endpoint.DeviceTopology.DeviceId}    {endpoint.DeviceFriendlyName}    {endpoint.FriendlyName}");
                for (int i = 0; i < endpoint.Properties.Count; i++)
                {
                    try
                    {
                        //if(endpoint.Properties[i].Key.formatId == DevPKey.Native.PnpDevicePropertyAPINative.)
                        if (endpoint.Properties[i].Value is byte[])
                        {
                            blder.AppendLine($"\t{{{endpoint.Properties[i].Key.formatId}}}, {endpoint.Properties[i].Key.propertyId} = {Encoding.Unicode.GetString((byte[])(endpoint.Properties[i].Value))}");
                        }
                        else
                        {
                            blder.AppendLine($"\t{{{endpoint.Properties[i].Key.formatId}}}, {endpoint.Properties[i].Key.propertyId} = {endpoint.Properties[i].Value}");
                        }
                    }
                    catch { }
                }
            }*/

            /*ManagementObjectSearcher mo = new ManagementObjectSearcher("select * from Win32_SoundDevice");

            foreach (ManagementObject soundDevice in mo.Get())
            {
                blder.AppendLine((string)soundDevice.GetPropertyValue("DeviceId"));
                blder.AppendLine((string)soundDevice.GetPropertyValue("Manufacturer"));
                Dictionary<DevPKey.Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties((string)soundDevice.GetPropertyValue("DeviceId"));
                // etc                       
            }*/

            //create enumerator
            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            //cycle through all audio devices
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                // these happen to enumate the same order
                NAudio.CoreAudioApi.MMDevice dev = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active)[i];
                //Guid scratch = (Guid)(dev.Properties[new NAudio.CoreAudioApi.PropertyKey(DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Device_ContainerId.fmtid, (int)DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Device_ContainerId.pid)].Value);
                //blder.AppendLine($"{i}\t{scratch}\t{dev}");

                string DeviceID = dev.Properties[new NAudio.CoreAudioApi.PropertyKey(DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Audio_InstanceId.fmtid, (int)DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Audio_InstanceId.pid)].Value.ToString();
                Dictionary<DevPKey.Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(DeviceID);

                /*for (int j = 0; j < dev.Properties.Count; j++)
                {
                    try
                    {
                        //if (dev.Properties[j].Key.formatId == DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Device_UNKNOWN.fmtid)
                        //    Console.WriteLine("Fount");

                        if (dev.Properties[j].Value is byte[])
                        {
                            string Val = Encoding.Unicode.GetString(dev.Properties[j].Value as byte[]);
                            if (Val.TrimEnd('\0').All(c => !char.IsControl(c)))
                            {
                                blder.AppendLine($"\t{{{dev.Properties[j].Key.formatId}}}, {dev.Properties[j].Key.propertyId}\t{Val}");
                                continue;
                            }

                            Val = Encoding.ASCII.GetString(dev.Properties[j].Value as byte[]);
                            if (Val.TrimEnd('\0').All(c => !char.IsControl(c)))
                            {
                                blder.AppendLine($"\t{{{dev.Properties[j].Key.formatId}}}, {dev.Properties[j].Key.propertyId}\t{Val}");
                                continue;
                            }

                            Val = BitConverter.ToString(dev.Properties[j].Value as byte[]);
                            blder.AppendLine($"\t{{{dev.Properties[j].Key.formatId}}}, {dev.Properties[j].Key.propertyId}\t{Val}");
                            continue;
                        }
                        blder.AppendLine($"\t{{{dev.Properties[j].Key.formatId}}}, {dev.Properties[j].Key.propertyId}\t{dev.Properties[j].Value}");
                    }
                    catch { }
                }
                //Dictionary<DevPKey.Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(scratch.ToString());
                */
            }
            /*for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                // these happen to enumate the same order
                NAudio.CoreAudioApi.MMDevice dev = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Render, NAudio.CoreAudioApi.DeviceState.Active)[i];
                blder.AppendLine($"{i}\t{dev.Properties[new NAudio.CoreAudioApi.PropertyKey(DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Device_ContainerId.fmtid, (int)DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Device_ContainerId.pid)].Value}\t{dev}");
            }*/
            //clean up
            enumerator.Dispose();

            Console.WriteLine(blder.ToString());
        }

        public void ScanNow()
        {
        }

        public void ManualTrigger()
        {
            DeviceChangeEventHandler threadSafeEventHandler = DeviceAdded;
            threadSafeEventHandler?.Invoke(this, new SixtyBeatAudioDevice(null));
        }
    }
}