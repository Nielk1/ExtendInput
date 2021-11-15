using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.DeviceProvider
{
    public class HidDevice : IDevice
    {
        public static string GetUniqueKey(string DevicePath) => $"HidDevice::{DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(DevicePath)}";

        public string DevicePath { get { return internalDevice.DevicePath; } }
        public int ProductId { get { return internalDevice.ProductID; } }
        public int VendorId { get { return internalDevice.VendorID; } }
        public int RevisionNumber { get { return internalDevice.ReleaseNumberBcd; } }

        public Dictionary<string, dynamic> Properties { get; private set; }


        public uint[] Usages { get; private set; }

        private HidSharp.HidDevice internalDevice;
        private bool IsOpen = false;
        private HidSharp.HidStream stream;

        [Obsolete("Design to get rid of this")]
        public HidSharp.HidDevice HackDevice { get { return internalDevice; } }

        public HidDevice(HidSharp.HidDevice internalDevice)
        {
            Properties = new Dictionary<string, dynamic>();

            Properties["MaxFeatureReportLength"] = internalDevice.GetMaxFeatureReportLength();
            Properties["MaxInputReportLength"] = internalDevice.GetMaxInputReportLength();
            Properties["MaxOutputReportLength"] = internalDevice.GetMaxOutputReportLength();

            try
            {
                Properties["Manufacturer"] = internalDevice.GetManufacturer();
            }
            catch { }
            try
            {
                Properties["ProductName"] = internalDevice.GetProductName();
            }
            catch { }

            try
            {
                bool FoundAny = false;
                HashSet<uint> Usages = new HashSet<uint>();
                foreach(var descriptor in internalDevice.GetReportDescriptor().DeviceItems)
                {
                    try
                    {
                        foreach (uint usage in descriptor.Usages.GetAllValues())
                        {
                            Usages.Add(usage);
                            FoundAny = true;
                        }
                    }
                    catch { }
                }
                if (FoundAny)
                {
                    this.Usages = Usages.ToArray();
                    Properties["Usages"] = this.Usages;
                }
            }
            catch { }

            this.internalDevice = internalDevice;
        }
        private HidSharp.HidStream GetStream()
        {
            if (!IsOpen || stream == null)
            {
                stream = internalDevice.Open();
            }
            return stream;
        }

        public bool WriteReport(byte[] data)
        {
            try
            {
                GetStream().Write(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WriteFeatureData(byte[] data)
        {
            try
            {
                //int maxLen = internalDevice.GetMaxFeatureReportLength();
                GetStream().SetFeature(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ReadFeatureData(out byte[] data, byte reportId = 0)
        {
            data = new byte[internalDevice.GetMaxFeatureReportLength()];
            try
            {
                data[0] = reportId;
                //byte[] buffer = new byte[data.Length];
                GetStream().GetFeature(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ReadFeatureDataCustom(ref byte[] data, byte reportId = 0)
        {
            try
            {
                data[0] = reportId;
                //byte[] buffer = new byte[data.Length];
                GetStream().GetFeature(data);
                return true;
            }
            catch
            {
                return false;
            }
        }



        public void OpenDevice()
        {
            OpenDevice(DeviceMode.NonOverlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
        }

        public void OpenDevice(DeviceMode readMode, DeviceMode writeMode, ShareMode shareMode)
        {
            if (IsOpen) return;

            //HidSharp.OpenConfiguration config = new HidSharp.OpenConfiguration();
            //config.SetOption(HidSharp.OpenOption.Priority, HidSharp.OpenOption.Priority.)
            stream = internalDevice.Open();

            //_deviceReadMode = readMode;
            //_deviceWriteMode = writeMode;
            //_deviceShareMode = shareMode;

            IsOpen = true;
        }

        public void CloseDevice()
        {
            if (!IsOpen) return;
            stream.Close();
            IsOpen = false;
        }

        public void Dispose()
        {
            //if (MonitorDeviceEvents) MonitorDeviceEvents = false;
            if (IsOpen) CloseDevice();
        }

        public string ReadSerialNumber()
        {
            try
            {
                return internalDevice.GetSerialNumber();
            }
            catch { }
            return null;
        }

        bool reading = false;
        object readingLock = new object();
        Thread readingThread = null;
        public void StartReading()
        {
            lock (readingLock)
            {
                if (DeviceReport == null)
                    reading = false;

                if (reading)
                    return;

                reading = true;

                readingThread = new Thread(() =>
                {
                    while (reading)
                    {
                        if (DeviceReport == null)
                        {
                            break;
                        }

                        try
                        {
                            HidSharp.HidStream _stream = GetStream();
                            lock (_stream)
                            {
                                byte[] data = _stream.Read();

                                DeviceReportEvent threadSafeEvent = DeviceReport;
                                new Thread(() =>
                                {
                                    threadSafeEvent?.Invoke(new HidReport() { ReportId = data[0], ReportType = HidReportType.Input, ReportBytes = data.Skip(1).ToArray() });
                                }).Start();

                            }
                            if (PollingRate > 0)
                            {
                                int SleepTime = 0;
                                while (SleepTime < (PollingRate / 1000))
                                {
                                    Thread.Sleep(1000);
                                    SleepTime++;
                                }
                                Thread.Sleep(PollingRate % 1000);
                            }
                        }
                        catch (System.TimeoutException)
                        { }
                        catch (Exception) // for example System.IO.IOException: 'Operation failed after some time.'
                        {
                            reading = false;
                        }
                    }
                    reading = false;
                });
                readingThread.Start();
            }
        }

        public void StopReading()
        {
            lock (readingLock)
            {
                reading = false;
            }
        }

        public string UniqueKey => GetUniqueKey(this.DevicePath);

        public int PollingRate { get; internal set; }

        bool IEquatable<IDevice>.Equals(IDevice other)
        {
            return this.UniqueKey == other.UniqueKey;
        }

        public event DeviceReportEvent DeviceReport;
    }

    public delegate void DeviceReportEvent(IReport report);

    public enum DeviceMode
    {
        NonOverlapped = 0,
        Overlapped = 1
    }

    [Flags]
    public enum ShareMode
    {
        Exclusive = 0,
        ShareRead = NativeMethods.FILE_SHARE_READ,
        ShareWrite = NativeMethods.FILE_SHARE_WRITE
    }

    internal static class NativeMethods
    {
        internal const short FILE_SHARE_READ = 0x1;
        internal const short FILE_SHARE_WRITE = 0x2;
    }
}
