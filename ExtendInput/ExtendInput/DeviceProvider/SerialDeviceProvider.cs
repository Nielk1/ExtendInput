using ExtendInput.Controller;
using HidSharp;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;

namespace ExtendInput.DeviceProvider
{
    [DeviceProvider(TypeString = "SER", TypeCode = "SER", SupportsAutomaticDetection = true, SupportsManualyQuery = true, RequiresManualConfiguration = false)]
    public class SerialDeviceProvider : IDeviceProvider
    {
        public event DeviceAddedEventHandler DeviceAdded;
        public event DeviceRemovedEventHandler DeviceRemoved;

        //Dictionary<HidSharp.SerialDevice, IDevice> KnownDevices = new Dictionary<HidSharp.SerialDevice, IDevice>();
        Dictionary<string, IDevice> KnownDevices = new Dictionary<string, IDevice>();
        object lock_device_list = new object();

        HashSet<(UInt16, UInt16?)> Whitelist = new HashSet<(ushort, ushort?)>();

        public SerialDeviceProvider()
        {
            HidSharp.DeviceList.Local.Changed += DeviceListChanged; // abusing HidSharp's serial port scanner for now, will need to change this later
        }

        public void Dispose()
        {
        }

        public void ScanNow()
        {
            lock (lock_device_list)
            {
                try
                {
                    //HashSet<HidSharp.SerialDevice> AllCurrentDevices = new HashSet<HidSharp.SerialDevice>(HidSharp.DeviceList.Local.GetSerialDevices());
                    string[] AllCurrentDevices = SerialPort.GetPortNames();

                    //foreach (HidSharp.SerialDevice device in KnownDevices.Keys.ToList())
                    foreach (string portName in KnownDevices.Keys.ToList())
                    {
                        //if (!AllCurrentDevices.Contains(device))
                        if (!AllCurrentDevices.Contains(portName))
                        {
                            string FriendlyName = string.Empty;
                            //try
                            //{
                            //    FriendlyName = device.GetFriendlyName();
                            //}
                            //catch (IOException) { }
                            //Debug.WriteLine($"Device Removed: {device.DevicePath.PadRight(100)} \"{FriendlyName}\"\r\n                {device.DevicePath.PadRight(100)} \"{device}\"");
                            Debug.WriteLine($"Device Removed: {portName.PadRight(100)} \"{FriendlyName}\"\r\n                {portName.PadRight(100)} \"{portName}\"");

                            DeviceRemovedEventHandler threadSafeEventHandler = DeviceRemoved;
                            //threadSafeEventHandler?.Invoke(this, KnownDevices[device].UniqueKey);
                            threadSafeEventHandler?.Invoke(this, KnownDevices[portName].UniqueKey);
                            //KnownDevices.Remove(device);
                            KnownDevices.Remove(portName);
                        }
                    }

                    //foreach (HidSharp.SerialDevice device in AllCurrentDevices.ToList())
                    foreach (string portName in AllCurrentDevices.ToList())
                    {
                        //if ((Whitelist.Contains(((UInt16)device.VendorID, null)) || Whitelist.Contains(((UInt16)device.VendorID, (UInt16?)device.ProductID))) && !KnownDevices.ContainsKey(device))
                        {
                            //var Properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(device.DevicePath.Replace(@"\\.\COM", String.Empty));
                            //var Properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(@"USB\VID_10C4&PID_EA60\01ED7C86");
                            //var Properties2 = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(@"COM12");

                            string FriendlyName = string.Empty;
                            //try
                            //{
                            //    FriendlyName = device.GetFriendlyName();
                            //}
                            //catch (IOException) { }
                            //Debug.WriteLine($"Device Added: {device.DevicePath.PadRight(100)} \"{FriendlyName}\"\r\n              {device.DevicePath.PadRight(100)} \"{device}\"");
                            Debug.WriteLine($"Device Added: {portName.PadRight(100)} \"{FriendlyName}\"\r\n              {portName.PadRight(100)} \"{portName}\"");

                            //KnownDevices[device] = new SerialDevice(device);
                            KnownDevices[portName] = new SerialDevice(new SerialPort(portName));
                            DeviceAddedEventHandler threadSafeEventHandler = DeviceAdded;
                            //threadSafeEventHandler?.Invoke(this, KnownDevices[device]);
                            threadSafeEventHandler?.Invoke(this, KnownDevices[portName]);
                        }
                    }
                }
                catch { }
            }
        }

        private void DeviceListChanged(object sender, HidSharp.DeviceListChangedEventArgs e)
        {
            ScanNow();
        }

        /*public static IEnumerable<HidDevice> Enumerate(int vendorId, params int[] productIds)
        {
            return HidSharp.DeviceList.Local.GetHidDevices(vendorId).Where(dr => productIds.Contains(dr.ProductID)).Select(dr => new HidDevice(dr));
        }*/

        /*public void X()
        {
            HidSharp.DeviceList.Local.Changed
        }*/

        public IDeviceManualTriggerContext ManualTrigger(DeviceManualTriggerContextOption Option)
        {
            throw new NotImplementedException();
        }

        public void RegisterWhitelist(Dictionary<string, dynamic>[] deviceWhitelist)
        {
            if (deviceWhitelist == null)
                return;
            foreach(var white in deviceWhitelist)
            {
                UInt16? VID = white.ContainsKey("VID") ? (UInt16?)white["VID"] : null;
                UInt16? PID = white.ContainsKey("PID") ? (UInt16?)white["PID"] : null;
                if (VID.HasValue)
                {
                    Whitelist.Add((VID.Value, PID));
                }
            }
        }
    }
}
