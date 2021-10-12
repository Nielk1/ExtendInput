using ExtendInput.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ExtendInput.DeviceProvider
{
    [DeviceProvider(TypeString = "HID", TypeCode = "HID", SupportsAutomaticDetection = true, SupportsManualyQuery = true, RequiresManualConfiguration = false)]
    public class HidDeviceProvider : IDeviceProvider
    {
        public event DeviceChangeEventHandler DeviceAdded;
        public event DeviceChangeEventHandler DeviceRemoved;

        HashSet<HidSharp.HidDevice> KnownDevices = new HashSet<HidSharp.HidDevice>();
        object lock_device_list = new object();

        HashSet<(UInt16, UInt16?)> Whitelist = new HashSet<(ushort, ushort?)>();

        public HidDeviceProvider()
        {
            HidSharp.DeviceList.Local.Changed += DeviceListChanged;
        }

        public void ScanNow()
        {
            lock (lock_device_list)
            {
                try
                {
                    HashSet<HidSharp.Device> AllCurrentDevices = new HashSet<HidSharp.Device>(HidSharp.DeviceList.Local.GetHidDevices());

                    foreach (HidSharp.HidDevice device in KnownDevices.ToList())
                    {
                        if (!AllCurrentDevices.Contains(device))
                        {
                            string FriendlyName = string.Empty;
                            try
                            {
                                FriendlyName = device.GetFriendlyName();
                            }
                            catch (IOException) { }
                            Debug.WriteLine($"Device Removed: {device.DevicePath.PadRight(100)} \"{FriendlyName}\"");
                            Debug.WriteLine($"Device Removed: {device.DevicePath.PadRight(100)} \"{device}\"");

                            KnownDevices.Remove(device);
                            DeviceChangeEventHandler threadSafeEventHandler = DeviceRemoved;
                            threadSafeEventHandler?.Invoke(this, new HidDevice(device));
                        }
                    }

                    foreach (HidSharp.HidDevice device in AllCurrentDevices.ToList())
                    {
                        if ((Whitelist.Contains(((UInt16)device.VendorID, null)) || Whitelist.Contains(((UInt16)device.VendorID, (UInt16?)device.ProductID))) && !KnownDevices.Contains(device))
                        {
                            string FriendlyName = string.Empty;
                            try
                            {
                                FriendlyName = device.GetFriendlyName();
                            }
                            catch(IOException) { }
                            Debug.WriteLine($"Device Added: {device.DevicePath.PadRight(100)} \"{FriendlyName}\"");
                            Debug.WriteLine($"Device Added: {device.DevicePath.PadRight(100)} \"{device}\"");

                            KnownDevices.Add(device);
                            DeviceChangeEventHandler threadSafeEventHandler = DeviceAdded;
                            threadSafeEventHandler?.Invoke(this, new HidDevice(device));
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
                UInt16? VID = white.ContainsKey("VID") ? white["VID"] as UInt16? : null;
                UInt16? PID = white.ContainsKey("PID") ? white["PID"] as UInt16? : null;
                if (VID.HasValue)
                {
                    Whitelist.Add((VID.Value, PID));
                }
            }
        }
    }

    public delegate void DeviceChangeEventHandler(object sender, IDevice e);
    public interface IDeviceProvider
    {
        event DeviceChangeEventHandler DeviceAdded;
        event DeviceChangeEventHandler DeviceRemoved;

        void ScanNow();
        IDeviceManualTriggerContext ManualTrigger(DeviceManualTriggerContextOption Option);
        void RegisterWhitelist(Dictionary<string, dynamic>[] deviceWhitelist);
    }
    public class DeviceProviderAttribute : Attribute
    {
        public string TypeString { get; set; }
        public string TypeCode { get; set; }
        public bool SupportsAutomaticDetection { get; set; }
        public bool SupportsManualyQuery { get; set; }
        public bool RequiresManualConfiguration { get; set; }
    }
}
