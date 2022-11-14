using HidSharp;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.DeviceProvider
{
    public class SerialDevice : IDevice
    {
        public static string GetUniqueKey(string DevicePath) => $"SerialDevice::{DevicePath}";

        public string DevicePath { get { return internalDevice.PortName; } }

        private int? _productId;
        private int? _vendorId;
        private int? _revisionNumber;
        public int ProductId { get { return _productId ?? 0; } }
        public int VendorId { get { return _vendorId ?? 0; } }
        public int RevisionNumber { get { return _revisionNumber ?? 0; } }

        public Dictionary<string, dynamic> Properties { get; private set; }


        public uint[] Usages { get; private set; }

        //private HidSharp.SerialDevice internalDevice;
        private SerialPort internalDevice;
        //private bool IsOpen = false;
        //private HidSharp.SerialStream stream;

        [Obsolete("Design to get rid of this")]
        //public HidSharp.SerialDevice HackDevice { get { return internalDevice; } }
        public SerialPort HackDevice { get { return internalDevice; } }

        //public SerialDevice(HidSharp.SerialDevice internalDevice)
        public SerialDevice(SerialPort internalDevice)
        {
            Properties = new Dictionary<string, dynamic>();

            const string Win32_SerialPort = "Win32_SerialPort";
            //SelectQuery q = new SelectQuery(Win32_SerialPort, $"DeviceID = '{internalDevice.DevicePath.Replace(@"\\.\", String.Empty)}'");
            SelectQuery q = new SelectQuery(Win32_SerialPort, $"DeviceID = '{internalDevice.PortName}'");
            ManagementObjectSearcher s = new ManagementObjectSearcher(q);
            foreach (object cur in s.Get())
            {
                ManagementObject mo = (ManagementObject)cur;
                //foreach (PropertyData prop in mo.Properties)
                //    Properties[prop.Name] = prop.Value;
                object pnpId = mo.GetPropertyValue("PNPDeviceID");
                if(!string.IsNullOrWhiteSpace(pnpId as string))
                {
                    Properties["PNPDeviceID"] = pnpId;

                    _vendorId = DevPKey.PnpDevicePropertyAPI.GetDeviceVendorId(pnpId as string);
                    _productId = DevPKey.PnpDevicePropertyAPI.GetDeviceProductId(pnpId as string);
                    _revisionNumber = DevPKey.PnpDevicePropertyAPI.GetDeviceRevisionNumber(pnpId as string);
                }
                break;
            }

            /*Properties["MaxFeatureReportLength"] = internalDevice.GetMaxFeatureReportLength();
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
            //try
            //{
            //    //Properties["Descriptor"] = internalDevice.GetRawReportDescriptor();
            //    Console.WriteLine($"{internalDevice.VendorID:X4}:{internalDevice.ProductID:X4} {BitConverter.ToString(internalDevice.GetRawReportDescriptor()).Replace("-", " ")}");
            //}
            //catch { }

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
            catch { }*/

            internalDevice.DataReceived += InternalDevice_DataReceived;

            this.internalDevice = internalDevice;
        }
        //private HidSharp.SerialStream GetStream()
        //{
        //    if (!IsOpen || stream == null)
        //    {
        //        stream = internalDevice.Open();
        //    }
        //    return stream;
        //}

        public bool WriteReport(byte[] data)
        {
            try
            {
                //GetStream().Write(data, 0, data.Length);
                if (!internalDevice.IsOpen)
                    internalDevice.Open();
                internalDevice.Write(data, 0, data.Length);
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
                //GetStream().SetFeature(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ReadFeatureData(out byte[] data, byte reportId = 0)
        {
            /*data = new byte[internalDevice.GetMaxFeatureReportLength()];
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
            }*/
            data = null;
            return false;
        }

        public bool ReadFeatureDataCustom(ref byte[] data, byte reportId = 0)
        {
            /*try
            {
                data[0] = reportId;
                //byte[] buffer = new byte[data.Length];
                GetStream().GetFeature(data);
                return true;
            }
            catch
            {
                return false;
            }*/
            return false;
        }



        public void OpenDevice()
        {
            OpenDevice(DeviceMode.NonOverlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
        }

        public void OpenDevice(DeviceMode readMode, DeviceMode writeMode, ShareMode shareMode)
        {
            //if (IsOpen) return;
            if (internalDevice.IsOpen) return;

            //HidSharp.OpenConfiguration config = new HidSharp.OpenConfiguration();
            //config.SetOption(HidSharp.OpenOption.Priority, HidSharp.OpenOption.Priority.)
            //stream = internalDevice.Open();
            if (!internalDevice.IsOpen)
                internalDevice.Open();

            //_deviceReadMode = readMode;
            //_deviceWriteMode = writeMode;
            //_deviceShareMode = shareMode;

            //IsOpen = true;
        }

        public void CloseDevice()
        {
            //if (!IsOpen) return;
            if (!internalDevice.IsOpen) return;
            //stream.Close();
            internalDevice.Close();
            //IsOpen = false;
        }

        public void Dispose()
        {
            //if (MonitorDeviceEvents) MonitorDeviceEvents = false;
            //if (IsOpen) CloseDevice();
            if (internalDevice.IsOpen) CloseDevice();
        }

        public string ReadSerialNumber()
        {
            //try
            //{
            //    return internalDevice.GetSerialNumber();
            //}
            //catch { }
            return null;
        }

        bool reading = false;
        object readingLock = new object();
        //Thread readingThread = null;

        private QueueWorker<Wrapper<SerialReport>> sendingQueue = null;

        public void StartReading()
        {
            lock (readingLock)
            {
                if (DeviceReport == null)
                    reading = false;

                if (reading)
                    return;

                reading = true;

                sendingQueue = new QueueWorker<Wrapper<SerialReport>>(1, (report) =>
                {
                    if (report != null)
                    {
                        DeviceReportEvent threadSafeEvent = DeviceReport;
                        threadSafeEvent?.Invoke(report.Value);
                    }
                }, $"ReportThread {GetUniqueKey(DevicePath)}");

                OpenDevice();

                /*readingThread = new Thread(() =>
                {
                    while (reading)
                    {
                        if (DeviceReport == null)
                        {
                            break;
                        }

                        try
                        {
                            HidSharp.SerialStream _stream = GetStream();
                            lock (_stream)
                            {
                                byte[] buffer = new byte[32];
                                int length = _stream.Read(buffer, 0, buffer.Length);

                                //DeviceReportEvent threadSafeEvent = DeviceReport;
                                ////new Thread(() =>
                                ////{
                                ////    threadSafeEvent?.Invoke(new HidReport() { ReportId = data[0], ReportType = HidReportType.Input, ReportBytes = data.Skip(1).ToArray() });
                                ////}).Start();
                                //
                                //ThreadPool.QueueUserWorkItem((stateInfo) =>
                                //{
                                //    threadSafeEvent?.Invoke(new HidReport() { ReportId = data[0], ReportType = HidReportType.Input, ReportBytes = data.Skip(1).ToArray() });
                                //});

                                sendingQueue.EnqueueTask(new SerialReport() { ReportBytes = buffer.Take(length).ToArray() });
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
                readingThread.Start();*/
            }
        }

        private void InternalDevice_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (internalDevice)
            {
                byte[] buffer = new byte[internalDevice.BytesToRead];
                internalDevice.Read(buffer, 0, internalDevice.BytesToRead);
                try
                {
                    sendingQueue.EnqueueTask(new SerialReport() { ReportBytes = buffer });
                }
                catch (System.TimeoutException)
                { }
                catch (Exception) // for example System.IO.IOException: 'Operation failed after some time.'
                {
                    reading = false;
                    internalDevice.Close();
                }
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
}
