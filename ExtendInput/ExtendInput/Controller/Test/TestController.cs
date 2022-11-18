using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

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
                //return new string[] { _device?.DevicePath ?? _device2.DevicePath };
                return new string[] { devices.First().DevicePath };
            }
        }
        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;
        //public IDevice DeviceHackRef => devices.First();
        private HashSet<HidDevice> devices;
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
        private HidDevice deviceVendor => devices.Where(dr => dr.Usages?.Contains(0xff000003u) ?? false).FirstOrDefault();


        private AccessMode AccessMode;
        bool Initalized;
        public TestController(string UniqueKey, AccessMode AccessMode, HidDevice device)
        {
            ConnectionUniqueID = UniqueKey;
            this.AccessMode = AccessMode;
            devices = new HashSet<HidDevice>();

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
            State.Controls["bumpers_left"] = new ControlButton();
            State.Controls["bumpers_right"] = new ControlButton();
            State.Controls["triggers_left"] = new ControlTrigger();
            State.Controls["triggers_right"] = new ControlTrigger();
            State.Controls["menu_left"] = new ControlButton();
            State.Controls["menu_right"] = new ControlButton();
            State.Controls["home"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStickWithClick();
            State.Controls["stick_right"] = new ControlStickWithClick();

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

            AddDevice(device);

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
        }
        //bool AbortStatusThread = false;
        //Thread CheckControllerStatusThread;

        public void Dispose()
        {
            //AbortStatusThread = true;
        }

        public void AddDevice(HidDevice device)
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
        }
        public ControllerState GetState()
        {
            return State;
        }

        object ReportLock = new object();
        int counter = 0;
        int state = 0;
        UInt32 local_ac = 0;
        byte[] local_b4 = new byte[8];
        UInt32 local_74 = 0;
        UInt32 local_5c = 0;
        UInt16 requestId = 0;


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

            Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");

            //if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            //{
            //    try
            //    {
            //        {
            //            {
            //                switch(reportData.ReportId)
            //                {
            //                    case 0x03:
            //                        if (!ConfigModeKeyData)
            //                        {
            //                            State.StartStateChange();
            //                            try
            //                            {
            //                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[2] & 0x10) == 0x10;
            //                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[2] & 0x02) == 0x02;
            //                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[2] & 0x01) == 0x01;
            //                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[2] & 0x08) == 0x08;

            //                                switch(reportData.ReportBytes[1])
            //                                {
            //                                    case 0x0f: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None; break;
            //                                    case 0x00: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North; break;
            //                                    case 0x01: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast; break;
            //                                    case 0x02: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East; break;
            //                                    case 0x03: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast; break;
            //                                    case 0x04: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South; break;
            //                                    case 0x05: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest; break;
            //                                    case 0x06: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West; break;
            //                                    case 0x07: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest; break;
            //                                }
            //                                (State.Controls["bumper_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[2] & 0x40) == 0x40;
            //                                (State.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[2] & 0x80) == 0x80;

            //                                (State.Controls["trigger_left"] as IControlTrigger).AnalogStage1 = (float)(reportData.ReportBytes[8] > 0 ? reportData.ReportBytes[8] : (reportData.ReportBytes[3] & 0x01) == 0x01 ? byte.MaxValue : 0) / byte.MaxValue;
            //                                (State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)(reportData.ReportBytes[9] > 0 ? reportData.ReportBytes[9] : (reportData.ReportBytes[3] & 0x02) == 0x02 ? byte.MaxValue : 0) / byte.MaxValue;

            //                                (State.Controls["stick_left"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[4]);
            //                                (State.Controls["stick_left"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[5]);
            //                                (State.Controls["stick_left"] as IControlStickWithClick).Click = (reportData.ReportBytes[3] & 0x20) == 0x20;
            //                                (State.Controls["stick_right"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[6]);
            //                                (State.Controls["stick_right"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[7]);
            //                                (State.Controls["stick_right"] as IControlStickWithClick).Click = (reportData.ReportBytes[3] & 0x40) == 0x40;

            //                                (State.Controls["menu_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[3] & 0x04) == 0x04;
            //                                (State.Controls["menu_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[3] & 0x08) == 0x08;

            //                                (State.Controls["home"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[3] & 0x10) == 0x10;
            //                            }
            //                            finally
            //                            {
            //                                State.EndStateChange(true);
            //                            }
            //                        }
            //                        break;
            //                    case 0x05:
            //                        switch(reportData.ReportBytes[2])
            //                        {
            //                            case 0x3D:
            //                                {
            //                                    byte DataLen = reportData.ReportBytes[3];
            //                                    StringBuilder bld = new StringBuilder();
            //                                    for (int i = 0; i < DataLen - 2; i += 2)
            //                                    {
            //                                        switch (reportData.ReportBytes[4 + i])
            //                                        {
            //                                            case 0x10: // light
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} light");
            //                                                break;
            //                                            case 0x20: // lightdir
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} lightdir");
            //                                                break;
            //                                            case 0x30: // lightcolor
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} lightcolor");
            //                                                break;
            //                                            case 0x40: // lightlevel
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} lightlevel");
            //                                                break;
            //                                            case 0x50: // freqlevel
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} freqlevel");
            //                                                break;
            //                                            case 0x60: // viblevel
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} viblevel");
            //                                                break;
            //                                            case 0x70: // viblight
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} viblight");
            //                                                break;
            //                                            case 0x80: // battery
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} battery");
            //                                                break;
            //                                            case 0x90: // leftsense
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} leftsense  |{new string('=', reportData.ReportBytes[4 + i + 1] - 1)}{new string('-', 7 - reportData.ReportBytes[4 + i + 1])}|");
            //                                                break;
            //                                            case 0xA0: // rightsense
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} rightsense |{new string('=', reportData.ReportBytes[4 + i + 1] - 1)}{new string('-', 7 - reportData.ReportBytes[4 + i + 1])}|");
            //                                                break;
            //                                            case 0xB0: // ltlevel
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} ltlevel");
            //                                                break;
            //                                            case 0xC0: // rtlevel
            //                                                bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} rtlevel");
            //                                                break;
            //                                        }
            //                                    }
            //                                    Console.WriteLine(bld.ToString());
            //                                }
            //                                break;
            //                            case 0xAD:
            //                                {
            //                                    State.StartStateChange();
            //                                    try
            //                                    {
            //                                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[4] == 0x01);
            //                                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[6] == 0x01);
            //                                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[5] == 0x01);
            //                                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[3] == 0x01);
            //                                        {
            //                                            bool buttonUp = (reportData.ReportBytes[15] == 0x01);
            //                                            bool buttonRight = (reportData.ReportBytes[18] == 0x01);
            //                                            bool buttonDown = (reportData.ReportBytes[16] == 0x01);
            //                                            bool buttonLeft = (reportData.ReportBytes[17] == 0x01);
            //                                            int padH = 0;
            //                                            int padV = 0;
            //                                            if (buttonUp) padV++;
            //                                            if (buttonDown) padV--;
            //                                            if (buttonRight) padH++;
            //                                            if (buttonLeft) padH--;
            //                                            if (padH > 0)
            //                                                if (padV > 0)
            //                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast;
            //                                                else if (padV < 0)
            //                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast;
            //                                                else
            //                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East;
            //                                            else if (padH < 0)
            //                                                if (padV > 0)
            //                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest;
            //                                                else if (padV < 0)
            //                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest;
            //                                                else
            //                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West;
            //                                            else
            //                                                if (padV > 0)
            //                                                (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North;
            //                                            else if (padV < 0)
            //                                                (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South;
            //                                            else
            //                                                (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None;
            //                                        }

            //                                        (State.Controls["bumper_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[7] == 0x01);
            //                                        (State.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[8] == 0x01);

            //                                        (State.Controls["trigger_left"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[9] / byte.MaxValue;
            //                                        (State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[10] / byte.MaxValue;

            //                                        (State.Controls["stick_left"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[21]);
            //                                        (State.Controls["stick_left"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[22]);
            //                                        (State.Controls["stick_left"] as IControlStickWithClick).Click = reportData.ReportBytes[11] == 0x01;
            //                                        (State.Controls["stick_right"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[23]);
            //                                        (State.Controls["stick_right"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[24]);
            //                                        (State.Controls["stick_right"] as IControlStickWithClick).Click = reportData.ReportBytes[12] == 0x01;

            //                                        (State.Controls["menu_left"] as IControlButton).DigitalStage1 = reportData.ReportBytes[14] == 0x01;
            //                                        (State.Controls["menu_right"] as IControlButton).DigitalStage1 = reportData.ReportBytes[13] == 0x01;

            //                                        (State.Controls["menu2_left"] as IControlButton).DigitalStage1 = reportData.ReportBytes[28] == 0x01;
            //                                        (State.Controls["menu2_right"] as IControlButton).DigitalStage1 = reportData.ReportBytes[27] == 0x01;

            //                                        (State.Controls["grip_left"] as IControlButton).DigitalStage1 = reportData.ReportBytes[25] == 0x01;
            //                                        (State.Controls["grip_right"] as IControlButton).DigitalStage1 = reportData.ReportBytes[26] == 0x01;

            //                                        (State.Controls["home"] as IControlButton).DigitalStage1 = reportData.ReportBytes[29] == 0x01;
            //                                    }
            //                                    finally
            //                                    {
            //                                        State.EndStateChange(true);
            //                                    }
            //                                }
            //                                break;
            //                        }
            //                        break;
            //                }
            //            }
            //            //else
            //            //{
            //            //
            //            //}
            //        }

            //        /*
            //        byte SBJoystick_rawRightY = reverseByte(reportData.ReportBytes[2]);
            //        byte SBJoystick_rawRightX = reverseByte(reportData.ReportBytes[3]);
            //        byte SBJoystick_rawLeftY = reverseByte(reportData.ReportBytes[4]);
            //        byte SBJoystick_rawLeftX = reverseByte(reportData.ReportBytes[5]);

            //        (StateInFlight.Controls["stick_left"] as IControlStick).X = (float)(((double)SBJoystick_rawLeftX + (double)SBJoystick_rawLeftX) / 240.0 + -1.0);
            //        (StateInFlight.Controls["stick_left"] as IControlStick).Y = (float)(((double)SBJoystick_rawLeftY + (double)SBJoystick_rawLeftY) / 240.0 + -1.0);
            //        (StateInFlight.Controls["stick_right"] as IControlStick).X = (float)(((double)SBJoystick_rawRightX + (double)SBJoystick_rawRightX) / 240.0 + -1.0);
            //        (StateInFlight.Controls["stick_right"] as IControlStick).Y = (float)(((double)SBJoystick_rawRightY + (double)SBJoystick_rawRightY) / 240.0 + -1.0);

            //        (StateInFlight.Controls["cluster_left"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[0] & 0x08) == 0x08;
            //        (StateInFlight.Controls["cluster_left"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[6] & 0x20) == 0x20;
            //        (StateInFlight.Controls["cluster_left"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[6] & 0x40) == 0x40;
            //        (StateInFlight.Controls["cluster_left"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[6] & 0x80) == 0x80;

            //        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[1] & 0x10) == 0x10;
            //        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[1] & 0x20) == 0x20;
            //        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[1] & 0x02) == 0x02;
            //        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[1] & 0x01) == 0x01;

            //        (StateInFlight.Controls["stick_right"] as IControlStick).Click = (reportData.ReportBytes[6] & 0x10) == 0x10;
            //        (StateInFlight.Controls["stick_left"] as IControlStick).Click = (reportData.ReportBytes[6] & 0x08) == 0x08;
            //        (StateInFlight.Controls["menu"] as ControlButtonPair).Right.Digital = (reportData.ReportBytes[1] & 0x80) == 0x80;
            //        (StateInFlight.Controls["menu"] as ControlButtonPair).Left.Digital = (reportData.ReportBytes[1] & 0x40) == 0x40;
            //        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Digital = (reportData.ReportBytes[0] & 0x01) == 0x01;
            //        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Digital = (reportData.ReportBytes[0] & 0x04) == 0x04;
            //        (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.Digital = (reportData.ReportBytes[1] & 0x08) == 0x08;
            //        (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.Digital = (reportData.ReportBytes[0] & 0x02) == 0x02;

            //        (StateInFlight.Controls["home"] as ControlButton).Digital = (reportData.ReportBytes[1] & 0x04) == 0x04;
            //        */


            //        //Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");


            //        // bring OldState in line with new State
            //        //State = StateInFlight;

            //        //ControllerStateUpdate?.Invoke(this, State);
            //    }
            //    finally
            //    {
            //        Interlocked.Exchange(ref reportUsageLock, 0);
            //    }
            //}
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

                foreach (var device in devices)
                    device.StartReading();

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
            }
        }

        public void DeInitalize()
        {
            lock (InitalizationLock)
            {
                if (!Initalized)
                    return;

                //AbortStatusThread = true;

                foreach (var device in devices)
                {
                    device.StopReading();
                    device.CloseDevice();
                }

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