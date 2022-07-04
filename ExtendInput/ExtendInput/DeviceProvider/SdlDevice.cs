using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SDL2;
using System.Runtime.InteropServices;

namespace ExtendInput.DeviceProvider
{
    public class SdlDevice : IDevice
    {
        private const string nativeLibName = "SDL2";
        /* gamecontroller refers to an SDL_GameController* */
        [DllImport(nativeLibName, EntryPoint = "SDL_GameControllerPath", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr INTERNAL_SDL_GameControllerPath(
            IntPtr gamecontroller
        );
        public static string SDL_GameControllerPath(
            IntPtr gamecontroller
        )
        {
            return SDL.UTF8_ToManaged(
                INTERNAL_SDL_GameControllerPath(gamecontroller)
            );
        }
        [DllImport(nativeLibName, EntryPoint = "SDL_GameControllerPathForIndex", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr INTERNAL_SDL_GameControllerPathForIndex(
            int joystick_index
        );
        public static string SDL_GameControllerPathForIndex(
            int joystick_index
        )
        {
            return SDL.UTF8_ToManaged(
                INTERNAL_SDL_GameControllerPathForIndex(joystick_index)
            );
        }


        static char SDL_GetJoystickGUIDInfo(Guid guid)
        {
            byte[] guidB = guid.ToByteArray();

            /* If the GUID fits the form of BUS 0000 VENDOR 0000 PRODUCT 0000, return the data */
            if (/* guidB[0:1] is device bus type */
                guidB[2] == 0x00 && guidB[3] == 0x00 &&
                /* guidB[4:5] is vendor ID */
                guidB[6] == 0x00 && guidB[7] == 0x00 &&
                /* guidB[8:9] is product ID */
                guidB[10] == 0x00 && guidB[11] == 0x00
            /* guidB[12:13] is product version */
            )
            {
                return (char)guidB[14];
            }
            return '\0';
        }



        public string DevicePath
        {
            get
            {
                IntPtr device_handle = SDL.SDL_JoystickFromInstanceID(instance_id);
                if (device_handle != IntPtr.Zero)
                {
                    try
                    {
                        return SDL_GameControllerPath(device_handle);
                    }
                    finally
                    {
                        SDL.SDL_JoystickClose(device_handle);
                    }
                }
                return null;
            }
        }
        private int instance_id { get; set; }

        public int ProductId
        {
            get
            {
                IntPtr device_handle = SDL.SDL_JoystickFromInstanceID(instance_id);
                if (device_handle != IntPtr.Zero)
                {
                    try
                    {
                        return SDL.SDL_GameControllerGetProduct(device_handle);
                    }
                    finally
                    {
                        SDL.SDL_JoystickClose(device_handle);
                    }
                }
                return -1;
            }
        }
        public int VendorId
        {
            get
            {
                IntPtr device_handle = SDL.SDL_JoystickFromInstanceID(instance_id);
                if (device_handle != IntPtr.Zero)
                {
                    try
                    {
                        return SDL.SDL_GameControllerGetVendor(device_handle);
                    }
                    finally
                    {
                        SDL.SDL_JoystickClose(device_handle);
                    }
                }
                return -1;
            }
        }
        public int Revision
        {
            get
            {
                IntPtr device_handle = SDL.SDL_JoystickFromInstanceID(instance_id);
                if (device_handle != IntPtr.Zero)
                {
                    try
                    {
                        return SDL.SDL_GameControllerGetProductVersion(device_handle);
                    }
                    finally
                    {
                        SDL.SDL_JoystickClose(device_handle);
                    }
                }
                return -1;
            }
        }

        //public bool IsConnected { get; private set; }

        public Dictionary<string, dynamic> Properties { get; private set; }


        public SdlDevice(int instance_id)
        {
            Properties = new Dictionary<string, dynamic>();
            this.instance_id = instance_id;
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

        /*public void SetVibration(UInt16 Left, UInt16 Right)
        {
            XInputNative.XInputVibration pVibration = new XInputNative.XInputVibration()
            {
                LeftMotorSpeed = Left,
                RightMotorSpeed = Right,
            };
            XInputNative.XInputSetState(UserIndex + 1, ref pVibration);
        }*/

        //public string UniqueKey => $"XInputDevice {DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(this.DevicePath)}";
        public string UniqueKey => $"SDL2:{this.instance_id}";

        bool IEquatable<IDevice>.Equals(IDevice other)
        {
            return this.UniqueKey == other.UniqueKey;
        }

        public event DeviceReportEvent DeviceReport;
    }
}
