using ExtendInput;
using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ExtendInput.Controller.Test
{
    public class TestController : IController
    {
        enum ControllerSubtype
        {
            Unknown,
            Test,
        }
        private ControllerSubtype SubType = ControllerSubtype.Unknown;

        public EConnectionType ConnectionType => EConnectionType.Unknown;


        public const int VENDOR_TEST = 0x3537;
        public const int PRODUCT_TEST_TEST = 0x1001;


        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }
        public string Name => "TEST Gamepad";
        public string[] NameDetails
        {
            get
            {
                return new string[] { _device?.DevicePath };
                //return new string[] { devices.First().DevicePath };
            }
        }
        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;
        //public IDevice DeviceHackRef => devices.First();
        //private HashSet<HidDevice> devices;
        int reportUsageLock = 0;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        ControllerState State = new ControllerState();
        public string ConnectionUniqueID { get; private set; }
        /*public string ConnectionUniqueID
        {
            get
            {
                //if (!string.IsNullOrWhiteSpace(SerialNumber))
                //    return SerialNumber;
                //return _device?.UniqueKey ?? _device2.UniqueKey;
                //return deviceVendor.UniqueKey;
                return devices.First().UniqueKey;
            }
        }*/
        public string DeviceUniqueID
        {
            get
            {
                return null;
            }
        }

        public bool HasMotion => false;
        public bool IsReady => true;
        public bool IsPresent => true;
        public bool IsVirtual => false;

        private bool ConfigMode = false;
        private bool ConfigModeKeyData = false;


        public Dictionary<string, dynamic> DeviceProperties => null;
        //private HidDevice deviceVendor => devices.Where(dr => dr.Usages?.Contains(0xff000003u) ?? false).FirstOrDefault();
        private HidDevice _device;


        private AccessMode AccessMode;
        bool Initalized;
        public TestController(string UniqueKey, AccessMode AccessMode, HidDevice device)
        {
            ConnectionUniqueID = UniqueKey;
            this.AccessMode = AccessMode;
            //devices = new HashSet<HidDevice>();
            _device = device;

            if (device.ProductId == PRODUCT_TEST_TEST)
            {
                SubType = ControllerSubtype.Test;
                ControllerTypeCode = new string[] { "DEVICE_TEST", "DEVICE_GAMEPAD" };
                ConnectionTypeCode = new string[] { "CONNECTION_WIRE_USB", "CONNECTION_WIRE" };
            }
            else
            {
                SubType = ControllerSubtype.Unknown;
                ControllerTypeCode = new string[] { "DEVICE_UNKNOWN" };
                ConnectionTypeCode = new string[] { "CONNECTION_UNKNOWN" };
            }



            State.Controls["cluster_left"] = new ControlDPad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumper_left"] = new ControlButton();
            State.Controls["bumper_right"] = new ControlButton();
            State.Controls["trigger_left"] = new ControlTrigger();
            State.Controls["trigger_right"] = new ControlTrigger();
            State.Controls["menu_left"] = new ControlButton();
            State.Controls["menu_right"] = new ControlButton();
            //State.Controls["home"] = new ControlButtonLightToggle(AccessMode) { State = "STATE_LIGHT_ON" };
            State.Controls["home"] = new ControlButtonLightBrightness(AccessMode, Brightness: 1.0f, Levels: 68);
            State.Controls["stick_left"] = new ControlStickWithClick();
            State.Controls["stick_right"] = new ControlStickWithClick();

            State.Controls["m1"] = new ControlButton();
            State.Controls["m2"] = new ControlButton();
            State.Controls["m"] = new ControlButton();
            State.Controls["share"] = new ControlButton();
            State.Controls["mute"] = new ControlButtonLightToggle(AccessMode) {  State = "STATE_LIGHT_ON" };

            State.Controls["motion"] = new ControlMotion();

            State.Controls["rumble_left"] = new ControlEccentricRotatingMass(AccessMode, IsHeavy: true);
            State.Controls["rumble_right"] = new ControlEccentricRotatingMass(AccessMode, IsHeavy: false);
            State.Controls["trumble_left"] = new ControlEccentricRotatingMass(AccessMode, IsHeavy: false);
            State.Controls["trumble_right"] = new ControlEccentricRotatingMass(AccessMode, IsHeavy: false);


            /*if (device.ProductId == PRODUCT_TEST_TEST && (AccessMode == AccessMode.FullControl || AccessMode == AccessMode.SafeWriteOnly))
            {
                State.Controls["menu2_left"] = new ControlButton();
                State.Controls["menu2_right"] = new ControlButton();
                State.Controls["grip_left"] = new ControlButton();
                State.Controls["grip_right"] = new ControlButton();
            }*/

            //CheckControllerStatusThread = new Thread(() =>
            //{
            //    for (; ; )
            //    {
            //        SendConnectivity(0x02);
            //        if (AbortStatusThread)
            //            return;
            //        Thread.Sleep(1000);
            //    }
            //});

            //AddDevice(device);

            //if (device.DevicePath.Contains("&col05"))
            /*{
                this.deviceVendor = deviceVendor;
                this.deviceGamepad = deviceGamepad;
                Initalized = false;

                this.deviceVendor.DeviceReport += OnReport;
                if (this.deviceGamepad != null) this.deviceGamepad.DeviceReport += OnReport;

                //_device.StartReading();
                //_device2?.StartReading();
                //
                //CheckControllerStatusThread.Start();
                //
                //EnableConfigMode();
                ////EnableKeyEvent();
                ////QC();
            }*/
            // ignore this sub-device when we have full control and thus can ask the controller to send us its more raw data
            /*//if (device.DevicePath.Contains("&col04"))
            {
                _device2 = deviceGamepad;

                _device2.DeviceReport += OnReport;

                _device2.StartReading();
            }*/
            Initalized = false;

            device.DeviceReport += OnReport;
            State.ControllerStateUpdate += State_ControllerStateUpdate;
        }
        //bool AbortStatusThread = false;
        //Thread CheckControllerStatusThread;

        public void Dispose()
        {
            //AbortStatusThread = true;
        }
















        bool OutputThreadActive = false;
        Thread OutputThread;
        private void StartOutputThread()
        {
            OutputThreadActive = true;
            // TODO: fix the speed of this thread, it runs too fast, might need per reportID/type throttles or something
            OutputThread = new Thread(() =>
            {
                for (; ; )
                {
                    if (!OutputThreadActive) break;
                    Thread.Sleep(1000 / 60);
                    if (!OutputThreadActive) break;
                    //if(WriteStateDirtyPossible)
                    {
                        IControlEccentricRotatingMass left1 = State.Controls["rumble_left"] as IControlEccentricRotatingMass;
                        IControlEccentricRotatingMass right1 = State.Controls["rumble_right"] as IControlEccentricRotatingMass;
                        IControlEccentricRotatingMass left2 = State.Controls["trumble_left"] as IControlEccentricRotatingMass;
                        IControlEccentricRotatingMass right2 = State.Controls["trumble_right"] as IControlEccentricRotatingMass;

                        if (left1.IsWriteDirty
                        || right1.IsWriteDirty
                        || left2.IsWriteDirty
                        || right2.IsWriteDirty)
                        {
                            bool success = _device.WriteReport(new byte[] { 0x00, 0x02, 0x08, (byte)(right1.Power * 255f), (byte)(left1.Power * 255f), (byte)(right2.Power * 255f), (byte)(left2.Power * 255f) });
                            left1.CleanWriteDirty();
                            right1.CleanWriteDirty();
                            left2.CleanWriteDirty();
                            right2.CleanWriteDirty();
                        }

                        //ControlButtonLightToggle HomeButton = State.Controls["home"] as ControlButtonLightToggle;
                        ControlButtonLightBrightness HomeButton = State.Controls["home"] as ControlButtonLightBrightness;
                        ControlButtonLightToggle MuteButton = State.Controls["mute"] as ControlButtonLightToggle;

                        if (HomeButton.IsWriteDirty || MuteButton.IsWriteDirty)
                        {
                            //bool success = _device.WriteReport(new byte[] { 0x00, 0x03, 0x00, (byte)(HomeButton.State == HomeButton.States[1] ? 0x01 : 0x00), (byte)(MuteButton.State == MuteButton.States[1] ? 0x01 : 0x00) });
                            bool success = _device.WriteReport(new byte[] { 0x00, 0x03, 0x00, (byte)(HomeButton.Brightness * (HomeButton.Levels - 1) + (HomeButton.Brightness > 0 ? 32 : 0)), (byte)(MuteButton.State == MuteButton.States[1] ? 0x01 : 0x00) });
                            HomeButton.CleanWriteDirty();
                            MuteButton.CleanWriteDirty();
                        }
                    }
                    if (!OutputThreadActive) break;
                }
            });
            OutputThread.Start();
        }
        private void StopOutputThread()
        {
            OutputThreadActive = false;
        }




















        /*public void AddDevice(HidDevice device)
        {
            lock (InitalizationLock)
            {
                devices.Add(device);
                device.DeviceReport += OnReport;

                if (Initalized)
                {
                    //EnableConfigMode();
                    device.StartReading();
                }
            }
        }*/
        public ControllerState GetState()
        {
            return State;
        }

        private void State_ControllerStateUpdate(ControlCollection controls)
        {
            ControllerStateUpdate?.Invoke(this, controls);
        }
        private void OnReport(IReport rawReportData)
        {
            //if (!(reportData is GenericBytesReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.HID) return;
            HidReport reportData = (HidReport)rawReportData;

            // this logic appears to be for a 0x20BC/0x5044

            //Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    switch (reportData.ReportId)
                    {
                        case 0x00:
                            State.StartStateChange();
                            try
                            {
                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[0] & 0x08) == 0x08;
                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[0] & 0x04) == 0x04;
                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[0] & 0x02) == 0x02;
                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[0] & 0x01) == 0x01;

                                switch (reportData.ReportBytes[2] & 0x0F)
                                {
                                    case 0x08: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None; break;
                                    case 0x00: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North; break;
                                    case 0x01: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast; break;
                                    case 0x02: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East; break;
                                    case 0x03: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast; break;
                                    case 0x04: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South; break;
                                    case 0x05: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest; break;
                                    case 0x06: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West; break;
                                    case 0x07: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest; break;
                                }
                                (State.Controls["bumper_left" ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[0] & 0x10) == 0x10;
                                (State.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[0] & 0x20) == 0x20;

                                // can't trust digital data as trigger internally is 12 bits, so any value is digital
                                //(State.Controls["trigger_left" ] as IControlTrigger).AnalogStage1 = (float)(reportData.ReportBytes[17] > 0 ? reportData.ReportBytes[17] : (reportData.ReportBytes[0] & 0x40) == 0x40 ? byte.MaxValue : 0) / byte.MaxValue;
                                //(State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)(reportData.ReportBytes[18] > 0 ? reportData.ReportBytes[18] : (reportData.ReportBytes[0] & 0x80) == 0x80 ? byte.MaxValue : 0) / byte.MaxValue;
                                //(State.Controls["trigger_left" ] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[17] / byte.MaxValue;
                                //(State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[18] / byte.MaxValue;
                                //UInt16 trigger_left  = (UInt16)(reportData.ReportBytes[22] + (reportData.ReportBytes[21] << 8));
                                UInt16 trigger_left  = BitConverter.ToUInt16(reportData.ReportBytes, 21);
                                //UInt16 trigger_right = (UInt16)(reportData.ReportBytes[24] + (reportData.ReportBytes[23] << 8));
                                UInt16 trigger_right = BitConverter.ToUInt16(reportData.ReportBytes, 23);
                                (State.Controls["trigger_left" ] as IControlTrigger).AnalogStage1 = (float)(trigger_left  > 0 ? trigger_left  : (reportData.ReportBytes[0] & 0x40) == 0x40 ? 0x3ff : 0) / 0x3ff;
                                (State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)(trigger_right > 0 ? trigger_right : (reportData.ReportBytes[0] & 0x80) == 0x80 ? 0x3ff : 0) / 0x3ff;

                                //(State.Controls["stick_left"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[3]);
                                //(State.Controls["stick_left"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[4]);
                                (State.Controls["stick_left"] as IControlStickWithClick).X = BitConverter.ToInt16(reportData.ReportBytes, 3) *  1.0f / Int16.MaxValue;
                                (State.Controls["stick_left"] as IControlStickWithClick).Y = BitConverter.ToInt16(reportData.ReportBytes, 5) * -1.0f / Int16.MaxValue;
                                (State.Controls["stick_left"] as IControlStickWithClick).Click = (reportData.ReportBytes[1] & 0x04) == 0x04;
                                //(State.Controls["stick_right"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[5]);
                                //(State.Controls["stick_right"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[6]);
                                (State.Controls["stick_right"] as IControlStickWithClick).X = BitConverter.ToInt16(reportData.ReportBytes, 7) *  1.0f / Int16.MaxValue;
                                (State.Controls["stick_right"] as IControlStickWithClick).Y = BitConverter.ToInt16(reportData.ReportBytes, 9) * -1.0f / Int16.MaxValue;
                                (State.Controls["stick_right"] as IControlStickWithClick).Click = (reportData.ReportBytes[1] & 0x08) == 0x08;

                                (State.Controls["menu_left" ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[1] & 0x01) == 0x01;
                                (State.Controls["menu_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[1] & 0x02) == 0x02;

                                //(State.Controls["home"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[1] & 0x10) == 0x10;
                                (State.Controls["home"] as ControlButtonLightBrightness).DigitalStage1 = (reportData.ReportBytes[1] & 0x10) == 0x10;

                                //(State.Controls["m1"   ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[24] & 0x01) == 0x01;
                                //(State.Controls["m2"   ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[24] & 0x02) == 0x02;
                                //(State.Controls["m"    ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[24] & 0x10) == 0x10;
                                //(State.Controls["share"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[24] & 0x20) == 0x20;
                                //(State.Controls["mute" ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[24] & 0x40) == 0x40;
                                (State.Controls["m1"   ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[30] & 0x01) == 0x01;
                                (State.Controls["m2"   ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[30] & 0x02) == 0x02;
                                (State.Controls["m"    ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[30] & 0x10) == 0x10;
                                (State.Controls["share"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[30] & 0x20) == 0x20;
                                (State.Controls["mute" ] as IControlButton).DigitalStage1 = (reportData.ReportBytes[30] & 0x40) == 0x40;

                                //(State.Controls["motion"] as ControlMotion).AccelerometerX = BitConverter.ToInt16(reportData.ReportBytes.Skip(34).Take(2).Reverse().ToArray(), 0);
                                //(State.Controls["motion"] as ControlMotion).AccelerometerY = BitConverter.ToInt16(reportData.ReportBytes.Skip(36).Take(2).Reverse().ToArray(), 0);
                                //(State.Controls["motion"] as ControlMotion).AccelerometerZ = BitConverter.ToInt16(reportData.ReportBytes.Skip(38).Take(2).Reverse().ToArray(), 0);
                                //(State.Controls["motion"] as ControlMotion).AngularVelocityX = BitConverter.ToInt16(reportData.ReportBytes.Skip(28).Take(2).Reverse().ToArray(), 0);
                                //(State.Controls["motion"] as ControlMotion).AngularVelocityZ = BitConverter.ToInt16(reportData.ReportBytes.Skip(30).Take(2).Reverse().ToArray(), 0);
                                //(State.Controls["motion"] as ControlMotion).AngularVelocityY = BitConverter.ToInt16(reportData.ReportBytes.Skip(32).Take(2).Reverse().ToArray(), 0);
                                (State.Controls["motion"] as ControlMotion).AccelerometerX = (Int16)Math.Max(Math.Min((Int32)BitConverter.ToInt16(reportData.ReportBytes.Skip(42).Take(2).Reverse().ToArray(), 0) * 2, Int16.MaxValue), Int16.MinValue);
                                (State.Controls["motion"] as ControlMotion).AccelerometerY = (Int16)Math.Max(Math.Min((Int32)BitConverter.ToInt16(reportData.ReportBytes.Skip(44).Take(2).Reverse().ToArray(), 0) * 2, Int16.MaxValue), Int16.MinValue);
                                (State.Controls["motion"] as ControlMotion).AccelerometerZ = (Int16)Math.Max(Math.Min((Int32)BitConverter.ToInt16(reportData.ReportBytes.Skip(40).Take(2).Reverse().ToArray(), 0) * 2, Int16.MaxValue), Int16.MinValue);
                                (State.Controls["motion"] as ControlMotion).AngularVelocityX = BitConverter.ToInt16(reportData.ReportBytes.Skip(36).Take(2).Reverse().ToArray(), 0);
                                (State.Controls["motion"] as ControlMotion).AngularVelocityY = BitConverter.ToInt16(reportData.ReportBytes.Skip(38).Take(2).Reverse().ToArray(), 0);
                                (State.Controls["motion"] as ControlMotion).AngularVelocityZ = BitConverter.ToInt16(reportData.ReportBytes.Skip(34).Take(2).Reverse().ToArray(), 0);
                            }
                            finally
                            {
                                State.EndStateChange(true);
                            }
                            break;
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }
            }
        }

        public void Identify()
        {

        }


        object InitalizationLock = new object();
        public void Initalize()
        {
            lock (InitalizationLock)
            {
                if (Initalized)
                    return;

                //foreach (var device in devices)
                //    device.StartReading();
                _device.StartReading();

                //EnableConfigMode();
                //CheckControllerStatusThread?.Abort();
                //AbortStatusThread = false;
                //CheckControllerStatusThread = new Thread(() =>
                //{
                //    for (; ; )
                //    {
                //        SendConnectivity(0x02);
                //        if (AbortStatusThread)
                //            return;
                //        Thread.Sleep(1000);
                //    }
                //});
                //CheckControllerStatusThread.Start();

                Initalized = true;
                StartOutputThread();
            }
        }

        public void DeInitalize()
        {
            lock (InitalizationLock)
            {
                if (!Initalized)
                    return;

                //AbortStatusThread = true;

                //foreach (var device in devices)
                //{
                //    device.StopReading();
                //    device.CloseDevice();
                //}
                _device.StopReading();
                StopOutputThread();
                _device.CloseDevice();

                ConfigMode = false;
                ConfigModeKeyData = false;

                Initalized = false;
            }
        }

        public void SetActiveAlternateController(string ControllerID) { }

        public void LockState()
        {
            State.StartStateChange();
        }
        public void UnlockState(bool Notify)
        {
            State.EndStateChange(Notify);
        }
        public IControl GetControl(string control)
        {
            return State.Controls[control];
        }
        public bool SetControlState(string control, string state, params object[] args)
        {
            return false;
        }

    }
}