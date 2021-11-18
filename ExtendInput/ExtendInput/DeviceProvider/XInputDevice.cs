﻿using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.DeviceProvider
{
    public class XInputDevice : IDevice
    {
        public string DevicePath { get { return $"SharpDX.XInput.Controller({internalDevice.UserIndex})"; } }// internalDevice.DevicePath; } }
        public int ProductId
        {
            get
            {
                try
                {
                    XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                    if (XInputNative.XInputGetCapabilitiesEx(1, (int)internalDevice.UserIndex, 0, ref data) == 0)
                        return data.PID;
                }
                catch { }
                return -1;
            }
        }
        public int VendorId
        {
            get
            {
                try
                {
                    XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                    if (XInputNative.XInputGetCapabilitiesEx(1, (int)internalDevice.UserIndex, 0, ref data) == 0)
                        return data.VID;
                }
                catch { }
                return -1;
            }
        }
        public int Revision
        {
            get
            {
                try
                {
                    XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                    if (XInputNative.XInputGetCapabilitiesEx(1, (int)internalDevice.UserIndex, 0, ref data) == 0)
                        return data.REV;
                }
                catch { }
                return -1;
            }
        }
        public uint XID
        {
            get
            {
                try
                {
                    XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                    if (XInputNative.XInputGetCapabilitiesEx(1, (int)internalDevice.UserIndex, 0, ref data) == 0)
                        return data.XID;
                }
                catch { }
                return 0xFFFFFFFF;
            }
        }

        public bool IsConnected => internalDevice.IsConnected;
        public byte UserIndex => (byte)internalDevice.UserIndex;

        public Dictionary<string, dynamic> Properties { get; private set; }

        [Obsolete("Refactor any uses of this out")]
        public SharpDX.XInput.Controller internalDeviceHackRef => internalDevice;
        private SharpDX.XInput.Controller internalDevice; // this should be private but we have hackery to fix
        //private bool IsOpen = false;

        public XInputDevice(SharpDX.XInput.Controller internalDevice)
        {
            Properties = new Dictionary<string, dynamic>();

            this.internalDevice = internalDevice;
        }

        public bool WriteReport(byte[] data)
        {
            //try
            {
                //GetStream().Write(data);
                //return true;
            }
            //catch
            {
                return false;
            }
        }

        public bool WriteFeatureData(byte[] data)
        {
            //try
            //{
                //int maxLen = internalDevice.GetMaxFeatureReportLength();
                //GetStream().SetFeature(data);
            //    return true;
            //}
            //catch
            {
                return false;
            }
        }

        public bool ReadFeatureData(out byte[] data, byte reportId = 0)
        {
            data = new byte[0];
            /*data = new byte[internalDevice.GetMaxFeatureReportLength()];
            try
            {
                data[0] = reportId;
                byte[] buffer = new byte[data.Length];
                GetStream().GetFeature(data);
                return true;
            }
            catch*/
            {
                return false;
            }
        }

        public void Dispose()
        {
            //if (MonitorDeviceEvents) MonitorDeviceEvents = false;
            //if (IsOpen) CloseDevice();
        }

        public string ReadSerialNumber()
        {
            return string.Empty;// internalDevice.GetSerialNumber();
        }

        bool reading = false;
        object readingLock = new object();
        Thread readingThread = null;

        private QueueWorker<Wrapper<XInputReport>> sendingQueue = null;
        public void StartReading()
        {
            lock (readingLock)
            {
                if (DeviceReport == null)
                    reading = false;

                if (reading)
                    return;

                reading = true;

                sendingQueue = new QueueWorker<Wrapper<XInputReport>>(1, (report) =>
                {
                    if (report != null)
                    {
                        DeviceReportEvent threadSafeEvent = DeviceReport;
                        threadSafeEvent?.Invoke(report.Value);
                    }
                }, $"ReportThread {DevicePath}");

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
                            //XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                            //if (XInputNative.XInputGetCapabilitiesEx(1, (int)internalDevice.UserIndex, 0, ref data) == 0)

                            XInputNative.XInputState data = new XInputNative.XInputState();
                            if (XInputNative.XInputGetStateEx((int)internalDevice.UserIndex, ref data) == 0)
                            {
                                sendingQueue.EnqueueTask(new XInputReport()
                                {
                                    Connected = internalDevice.IsConnected,
                                    wButtons = (UInt16)data.Gamepad.wButtons,
                                    bLeftTrigger = data.Gamepad.bLeftTrigger,
                                    bRightTrigger = data.Gamepad.bRightTrigger,
                                    sThumbLX = data.Gamepad.sThumbLX,
                                    sThumbLY = data.Gamepad.sThumbLY,
                                    sThumbRX = data.Gamepad.sThumbRX,
                                    sThumbRY = data.Gamepad.sThumbRY,
                                });
                            }

                            /*var State = internalDevice.GetState();

                            sendingQueue.EnqueueTask(new XInputReport()
                            {
                                Connected = internalDevice.IsConnected,
                                wButtons = (UInt16)State.Gamepad.Buttons,
                                bLeftTrigger = State.Gamepad.LeftTrigger,
                                bRightTrigger = State.Gamepad.RightTrigger,
                                sThumbLX = State.Gamepad.LeftThumbX,
                                sThumbLY = State.Gamepad.LeftThumbY,
                                sThumbRX = State.Gamepad.RightThumbX,
                                sThumbRY = State.Gamepad.RightThumbY,
                            });*/
                        }
                        catch
                        {
                            reading = false;
                        }
                    }
                    reading = false;
                });
                readingThread.Start();
            }
        }

        public void NotifyOfConnectEvent()
        {
            DeviceReportEvent threadSafeEvent = DeviceReport;

            threadSafeEvent?.Invoke(new XInputReport()
            {
                Connected = internalDevice.IsConnected,
            });
        }

        public void StopReading()
        {
            lock (readingLock)
            {
                reading = false;
            }
        }

        //public string UniqueKey => $"XInputDevice {DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(this.DevicePath)}";
        public string UniqueKey => $"XInputDevice {this.DevicePath}";

        bool IEquatable<IDevice>.Equals(IDevice other)
        {
            return this.UniqueKey == other.UniqueKey;
        }

        public event DeviceReportEvent DeviceReport;
    }
}
