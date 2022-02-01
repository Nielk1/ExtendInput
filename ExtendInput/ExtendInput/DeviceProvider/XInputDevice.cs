using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.DeviceProvider
{
    public class XInputDevice : IDevice
    {
        public string DevicePath { get { return $"SharpDX.XInput.Controller({UserIndex})"; } }
        public int ProductId
        {
            get
            {
                try
                {
                    XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                    if (XInputNative.XInputGetCapabilitiesEx(1, UserIndex + 1, 0, ref data) == 0)
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
                    if (XInputNative.XInputGetCapabilitiesEx(1, UserIndex + 1, 0, ref data) == 0)
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
                    if (XInputNative.XInputGetCapabilitiesEx(1, UserIndex + 1, 0, ref data) == 0)
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
                    if (XInputNative.XInputGetCapabilitiesEx(1, UserIndex + 1, 0, ref data) == 0)
                        return data.XID;
                }
                catch { }
                return 0xFFFFFFFF;
            }
        }

        //public bool IsConnected { get; private set; }
        public byte UserIndex { get; private set; }

        public Dictionary<string, dynamic> Properties { get; private set; }


        public XInputDevice(byte UserIndex)
        {
            Properties = new Dictionary<string, dynamic>();
            this.UserIndex = UserIndex;
            //this.IsConnected = true;
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

        private void CreateSendingQueue()
        {
            if (sendingQueue != null)
                return;
            sendingQueue = new QueueWorker<Wrapper<XInputReport>>(1, (report) =>
            {
                if (report != null)
                {
                    DeviceReportEvent threadSafeEvent = DeviceReport;
                    threadSafeEvent?.Invoke(report.Value);
                }
            }, $"ReportThread {DevicePath}");
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

                CreateSendingQueue();

                Stopwatch InputTimer = new Stopwatch();

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
                            InputTimer.Restart();

                            //XInputNative.XInputCapabilitiesEx data = new XInputNative.XInputCapabilitiesEx();
                            //if (XInputNative.XInputGetCapabilitiesEx(1, (int)internalDevice.UserIndex, 0, ref data) == 0)

                            XInputNative.XInputState data = new XInputNative.XInputState();
                            if (XInputNative.XInputGetStateEx(UserIndex + 1, ref data) == 0)
                            {
                                //if (!IsConnected)
                                //{
                                //    // TODO notify of unplug event
                                //    IsConnected = true;
                                //}
                                sendingQueue.EnqueueTask(new XInputReport()
                                {
                                    //Connected = IsConnected,
                                    wButtons = (UInt16)data.Gamepad.wButtons,
                                    bLeftTrigger = data.Gamepad.bLeftTrigger,
                                    bRightTrigger = data.Gamepad.bRightTrigger,
                                    sThumbLX = data.Gamepad.sThumbLX,
                                    sThumbLY = data.Gamepad.sThumbLY,
                                    sThumbRX = data.Gamepad.sThumbRX,
                                    sThumbRY = data.Gamepad.sThumbRY,
                                });
                            }
                            /*else if (IsConnected)
                            {
                                IsConnected = false;
                                sendingQueue.EnqueueTask(new XInputReport()
                                {
                                    Connected = IsConnected,
                                });
                            }
                            else
                            {
                                // poll every 1 second if the device's state is disconnected
                                // might need to move polling rate out, which would require a temporary polling rate for an unpopulated node
                                int PollingRate = 1000;
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
                            }*/

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

                            InputTimer.Stop();
                            if (InputTimer.ElapsedMilliseconds < 10)
                            {
                                Thread.Sleep((int)(10 - InputTimer.ElapsedMilliseconds));
                            }
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

        public void StopReading()
        {
            lock (readingLock)
            {
                reading = false;
            }
        }

        public void SetVibration(UInt16 Left, UInt16 Right)
        {
            XInputNative.XInputVibration pVibration = new XInputNative.XInputVibration()
            {
                LeftMotorSpeed = Left,
                RightMotorSpeed = Right,
            };
            XInputNative.XInputSetState(UserIndex + 1, ref pVibration);
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
