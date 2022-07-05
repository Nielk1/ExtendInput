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
        public event DeviceAddedEventHandler DeviceAdded;
        public event DeviceRemovedEventHandler DeviceRemoved;

        Dictionary<HidSharp.HidDevice, IDevice> KnownDevices = new Dictionary<HidSharp.HidDevice, IDevice>();
        object lock_device_list = new object();

        HashSet<(UInt16, UInt16?)> Whitelist = new HashSet<(ushort, ushort?)>();

        public HidDeviceProvider()
        {
            HidSharp.DeviceList.Local.Changed += DeviceListChanged;
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
                    HashSet<HidSharp.Device> AllCurrentDevices = new HashSet<HidSharp.Device>(HidSharp.DeviceList.Local.GetHidDevices());

                    foreach (HidSharp.HidDevice device in KnownDevices.Keys.ToList())
                    {
                        if (!AllCurrentDevices.Contains(device))
                        {
                            string FriendlyName = string.Empty;
                            try
                            {
                                FriendlyName = device.GetFriendlyName();
                            }
                            catch (IOException) { }
                            Debug.WriteLine($"Device Removed: {device.DevicePath.PadRight(100)} \"{FriendlyName}\"\r\n                {device.DevicePath.PadRight(100)} \"{device}\"");

                            DeviceRemovedEventHandler threadSafeEventHandler = DeviceRemoved;
                            threadSafeEventHandler?.Invoke(this, KnownDevices[device].UniqueKey);
                            KnownDevices.Remove(device);
                        }
                    }

                    foreach (HidSharp.HidDevice device in AllCurrentDevices.ToList())
                    {
                        if ((Whitelist.Contains(((UInt16)device.VendorID, null)) || Whitelist.Contains(((UInt16)device.VendorID, (UInt16?)device.ProductID))) && !KnownDevices.ContainsKey(device))
                        {
                            string FriendlyName = string.Empty;
                            try
                            {
                                FriendlyName = device.GetFriendlyName();
                            }
                            catch (IOException) { }
                            Debug.WriteLine($"Device Added: {device.DevicePath.PadRight(100)} \"{FriendlyName}\"\r\n              {device.DevicePath.PadRight(100)} \"{device}\"");

                            KnownDevices[device] = new HidDevice(device);
                            DeviceAddedEventHandler threadSafeEventHandler = DeviceAdded;
                            threadSafeEventHandler?.Invoke(this, KnownDevices[device]);
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

    public delegate void DeviceAddedEventHandler(object sender, IDevice e);
    public delegate void DeviceRemovedEventHandler(object sender, string UniqueKey);
    public interface IDeviceProvider : IDisposable
    {
        event DeviceAddedEventHandler DeviceAdded;
        event DeviceRemovedEventHandler DeviceRemoved;

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
