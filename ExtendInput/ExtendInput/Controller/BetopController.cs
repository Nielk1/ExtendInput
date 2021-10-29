using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
{
    public class BetopController : IController
    {
        public EConnectionType ConnectionType => EConnectionType.Unknown;


        public const int VENDOR_BETOP = 0x20bc;
        public const int PRODUCT_BETOP_ASURA3 = 0x5036;


        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }
        public string Name => "Betop Gamepad";
        public string[] NameDetails
        {
            get
            {
                //return new string[] { _device?.DevicePath ?? _device2.DevicePath };
                return new string[] { _device.DevicePath };
            }
        }
        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;
        public IDevice DeviceHackRef => _device;
        private HidDevice _device;
        private HidDevice _device2;
        int reportUsageLock = 0;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        ControllerState State = new ControllerState();
        public string ConnectionUniqueID
        {
            get
            {
                //if (!string.IsNullOrWhiteSpace(SerialNumber))
                //    return SerialNumber;
                //return _device?.UniqueKey ?? _device2.UniqueKey;
                return _device.UniqueKey;
            }
        }
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


        bool Initalized;
        public BetopController(HidDevice deviceVendor, HidDevice deviceGamepad)
        {
            ConnectionTypeCode = new string[] { "CONNECTION_WIRE_35MM_PHONE_TRRS", "CONNECTION_WIRE" };
            ControllerTypeCode = new string[] { "DEVICE_SIXTYBEAT_GAMEPAD", "DEVICE_GAMEPAD" };

            /*State.Controls["cluster_left"] = new ControlButtonQuad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair(ButtonProperties.CMB_Bumper);
            State.Controls["triggers"] = new ControlButtonPair(ButtonProperties.CMB_Bumper);
            State.Controls["menu"] = new ControlButtonPair(ButtonProperties.CMB_Button);
            State.Controls["home"] = new ControlButton(ButtonProperties.CMB_Button);
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);*/

            CheckControllerStatusThread = new Thread(() =>
            {
                for (; ; )
                {
                    SendConnectivity(0x02);
                    if (AbortStatusThread)
                        return;
                    Thread.Sleep(1000);
                }
            });

            //if (device.DevicePath.Contains("&col05"))
            {
                _device = deviceVendor;
                Initalized = false;

                _device.DeviceReport += OnReport;

                _device.StartReading();

                CheckControllerStatusThread.Start();

                EnableKeyEvent();
                //QC();
            }
            // ignore this sub-device when we have full control and thus can ask the controller to send us its more raw data
            /*//if (device.DevicePath.Contains("&col04"))
            {
                _device2 = deviceGamepad;

                _device2.DeviceReport += OnReport;

                _device2.StartReading();
            }*/
        }
        bool AbortStatusThread = false;
        Thread CheckControllerStatusThread;

        public void Dispose()
        {
            AbortStatusThread = true;
        }

        private void SendConnectivity(byte param)
        {
            byte[] data = new byte[64];
            data[0] = 0x05;
            data[1] = 0x00;
            data[2] = 0x00;
            data[3] = 0xB0;
            data[4] = param;
            _device.WriteReport(data);
        }

        private void EnableConfigMode(byte param1, byte param2)
        {
            requestId++; // might be Asura3 only
            byte[] data = new byte[64];
            data[0] = 0x05;
            //data[1] = 0x00;
            //data[2] = 0x00;
            data[1] = (byte)(requestId & 0xFF); // might be Asura3 only
            data[2] = (byte)((requestId >> 8) & 0xFF); // might be Asura3 only
            data[3] = 0x80;
            data[4] = 0x03;
            data[5] = 0x02;
            _device.WriteReport(data);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"{BitConverter.ToString(data)}");
            Console.ResetColor();
        }

        private void EnableKeyEvent()
        {
            requestId++; // might be Asura3 only
            byte[] data = new byte[64];
            data[0] = 0x05;
            //data[1] = 0x00;
            //data[2] = 0x00;
            data[1] = (byte)(requestId & 0xFF); // might be Asura3 only
            data[2] = (byte)((requestId >> 8) & 0xFF); // might be Asura3 only
            data[3] = 0xA0;
            data[4] = 0x03;
            data[5] = 0x02;
            _device.WriteReport(data);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"{BitConverter.ToString(data)}");
            Console.ResetColor();
        }

        /*private void QC()
        {
            requestId++; // might be Asura3 only
            byte[] data = new byte[64];
            data[0] = 0x05;
            //data[1] = 0x00;
            //data[2] = 0x00;
            data[1] = (byte)(requestId & 0xFF); // might be Asura3 only
            data[2] = (byte)((requestId >> 8) & 0xFF); // might be Asura3 only
            data[3] = 0xDC;
            data[4] = 0x01;
            //data[5] = 0x02;
            _device.WriteReport(data);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"{BitConverter.ToString(data)}");
            Console.ResetColor();
        }*/

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
        private void OnReport(IReport rawReportData)
        {
            //if (!(reportData is GenericBytesReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.HID) return;
            HidReport reportData = (HidReport)rawReportData;

            // this logic appears to be for a 0x20BC/0x5044

            if (reportData.ReportId == 0x05)
            {
                lock (ReportLock)
                {
                    int _v21 = 2;
                    int reportLength = reportData.ReportBytes.Reverse().SkipWhile(dr => dr == 0x00).Count();

                    if (reportLength > 0
                     && reportData.ReportBytes[2] != 0xBC)
                        Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");

                    if (reportLength == 0 || reportData.ReportBytes[2] == 0xBC)
                    {
                        counter++;
                        if (state == 2)
                        {
                            if (counter > 4)
                            {
                                state = 3;
                                _v21 = 1;
                                //_10040290(_v21);
                                Console.WriteLine($"_10040290({_v21})");
                            }
                        }
                        else
                        {
                            if (state == 4 && counter > 4)
                            {
                                state = 5;
                                //_10040290(_v21);
                                Console.WriteLine($"_10040290({_v21})");
                            }
                        }
                    }
                    else
                    {
                        counter = 0;
                    }

                    /*{
                        byte[] data = new byte[64];
                        data[0] = 0x05;
                        data[1] = 0x00;
                        data[2] = 0x00;
                        data[3] = 0xB0;
                        data[4] = 0x02;
                        _device.WriteReport(data);
                    }*/

                    /*if (reportLength >= 0)
                    {
                        if (reportData.ReportBytes[2] == 0x8C)
                        {
                            local_ac = 2;
                        }
                        else if (reportData.ReportBytes[2] == 0xF1)
                        {
                            switch (new string(reportData.ReportBytes.Skip(5).Take(3).Select(dr => (char)dr).ToArray()))
                            {
                                case "GGF":
                                    local_b4 = reportData.ReportBytes.Skip(8).Take(8).ToArray();
                                    Console.WriteLine("GGF fin");
                                    break;
                                case "GDP":
                                    if (state == 1)
                                        state = 2;

                                    if (BitConverter.ToUInt16(reportData.ReportBytes, 0) == requestId)
                                        //_10006FD0(BitConverter.ToUInt32(reportData.ReportBytes, 10), reportData.ReportBytes[5] - 8);
                                        Console.WriteLine($"_10006FD0({BitConverter.ToUInt32(reportData.ReportBytes, 10)}, {reportData.ReportBytes[5] - 8})");
                                    break;
                                case "GDT":
                                    if (state == 3)
                                        state = 4;
                                    if (BitConverter.ToUInt16(reportData.ReportBytes, 0) == requestId)
                                        //_10006FD0(BitConverter.ToUInt32(reportData.ReportBytes, 10), reportData.ReportBytes[5] - 8);
                                        Console.WriteLine($"_10006FD0({BitConverter.ToUInt32(reportData.ReportBytes, 10)}, {reportData.ReportBytes[5] - 8});");
                                    break;
                            }
                        }
                    }*/
                    /*if (local_ac == 0)
                    {
                        EnableConfigMode(0x03, 0x02);
                    }*/
                    /*else if (local_b4[0] == 0)
                    {
                        //write(_020000F005474746, 8);
                        {
                            byte[] data = new byte[64];
                            data[0] = 0x05;
                            data[1] = 0x00;
                            data[2] = 0x00;
                            data[3] = 0xF0;
                            data[4] = 0x05;
                            data[5] = 0x47;
                            data[6] = 0x47;
                            data[7] = 0x46;
                            _device.WriteReport(data);
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            Console.WriteLine($"{BitConverter.ToString(data)}");
                            Console.ResetColor();
                        }
                        state = 1;
                    }
                    else
                    {
                        if (state == 1)
                        {
                            if (local_74 == 0)
                            {
                                seq++;
                                //_020000F005474450[1...2] = seq;
                                //write(_020000F005474450, 8);
                                {
                                    byte[] data = new byte[64];
                                    data[0] = 0x05;
                                    data[1] = (byte)(seq & 0xFF);
                                    data[2] = (byte)((seq >> 8) & 0xFF);
                                    data[3] = 0xF0;
                                    data[4] = 0x05;
                                    data[5] = 0x47;
                                    data[6] = 0x44;
                                    data[7] = 0x50;
                                    _device.WriteReport(data);
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.WriteLine($"{BitConverter.ToString(data)}");
                                    Console.ResetColor();
                                }
                            }
                        }
                        else if (state == 3)
                        {
                            if (local_5c == 0)
                            {
                                seq++;
                                //_020000F005474454[1...2] = seq;
                                //write(_020000F005474746, 8);
                                {
                                    byte[] data = new byte[64];
                                    data[0] = 0x05;
                                    data[1] = (byte)(seq & 0xFF);
                                    data[2] = (byte)((seq >> 8) & 0xFF);
                                    data[3] = 0xF0;
                                    data[4] = 0x05;
                                    data[5] = 0x47;
                                    data[6] = 0x47;
                                    data[7] = 0x46;
                                    _device.WriteReport(data);
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.WriteLine($"{BitConverter.ToString(data)}");
                                    Console.ResetColor();
                                }
                            }
                        }
                        else if (state == 5)
                        {
                            //write(_020000800301, 6);
                            {
                                byte[] data = new byte[64];
                                data[0] = 0x05;
                                data[1] = 0x00;
                                data[2] = 0x00;
                                data[3] = 0x80;
                                data[4] = 0x03;
                                data[5] = 0x01;
                                _device.WriteReport(data);
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($"{BitConverter.ToString(data)}");
                                Console.ResetColor();

                            }
                            //Sleep(1000);
                            //state = _1000da70();
                            //break;
                        }
                    }*/
                }
            }
            else
            {
                Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
            }

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();
                    /*
                    byte SBJoystick_rawRightY = reverseByte(reportData.ReportBytes[2]);
                    byte SBJoystick_rawRightX = reverseByte(reportData.ReportBytes[3]);
                    byte SBJoystick_rawLeftY = reverseByte(reportData.ReportBytes[4]);
                    byte SBJoystick_rawLeftX = reverseByte(reportData.ReportBytes[5]);

                    (StateInFlight.Controls["stick_left"] as ControlStick).X = (float)(((double)SBJoystick_rawLeftX + (double)SBJoystick_rawLeftX) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_left"] as ControlStick).Y = (float)(((double)SBJoystick_rawLeftY + (double)SBJoystick_rawLeftY) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_right"] as ControlStick).X = (float)(((double)SBJoystick_rawRightX + (double)SBJoystick_rawRightX) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_right"] as ControlStick).Y = (float)(((double)SBJoystick_rawRightY + (double)SBJoystick_rawRightY) / 240.0 + -1.0);

                    (StateInFlight.Controls["cluster_left"] as ControlButtonQuad).ButtonN = (reportData.ReportBytes[0] & 0x08) == 0x08;
                    (StateInFlight.Controls["cluster_left"] as ControlButtonQuad).ButtonE = (reportData.ReportBytes[6] & 0x20) == 0x20;
                    (StateInFlight.Controls["cluster_left"] as ControlButtonQuad).ButtonS = (reportData.ReportBytes[6] & 0x40) == 0x40;
                    (StateInFlight.Controls["cluster_left"] as ControlButtonQuad).ButtonW = (reportData.ReportBytes[6] & 0x80) == 0x80;

                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonN = (reportData.ReportBytes[1] & 0x10) == 0x10;
                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonE = (reportData.ReportBytes[1] & 0x20) == 0x20;
                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonS = (reportData.ReportBytes[1] & 0x02) == 0x02;
                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonW = (reportData.ReportBytes[1] & 0x01) == 0x01;

                    (StateInFlight.Controls["stick_right"] as ControlStick).Click = (reportData.ReportBytes[6] & 0x10) == 0x10;
                    (StateInFlight.Controls["stick_left"] as ControlStick).Click = (reportData.ReportBytes[6] & 0x08) == 0x08;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Right.Digital = (reportData.ReportBytes[1] & 0x80) == 0x80;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Left.Digital = (reportData.ReportBytes[1] & 0x40) == 0x40;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Digital = (reportData.ReportBytes[0] & 0x01) == 0x01;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Digital = (reportData.ReportBytes[0] & 0x04) == 0x04;
                    (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.Digital = (reportData.ReportBytes[1] & 0x08) == 0x08;
                    (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.Digital = (reportData.ReportBytes[0] & 0x02) == 0x02;

                    (StateInFlight.Controls["home"] as ControlButton).Digital = (reportData.ReportBytes[1] & 0x04) == 0x04;
                    */


                    //Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");


                    // bring OldState in line with new State
                    State = StateInFlight;

                    ControllerStateUpdate?.Invoke(this, State);
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


        public void Initalize()
        {
            //Log("Initalize");
            //if (PollingState == EPollingState.Active) return;

            //PollingState = EPollingState.Active;
            //Log($"Polling state set to Active", ConsoleColor.Yellow);
            _device.StartReading();
        }

        public void DeInitalize()
        {
            //Log("DeInitalize");
            //if (PollingState == EPollingState.Inactive) return;
            //if (PollingState == EPollingState.SlowPoll) return;
            ////if (PollingState == EPollingState.RunOnce) return;
            //if (PollingState == EPollingState.RunUntilReady) return;

            // dongles switch back to slow poll instead of going inactive
            //if (ConnectionType == EConnectionType.Dongle)
            //{
            //    PollingState = EPollingState.SlowPoll;
            //    Log($"Polling state set to SlowPoll", ConsoleColor.Yellow);
            //}
            //else
            //{
                _device.StopReading();

            //    PollingState = EPollingState.Inactive;
            //    Log($"Polling state set to Inactive", ConsoleColor.Yellow);
                _device.CloseDevice();
            //}
        }

        public void SetActiveAlternateController(string ControllerID) { }

    }
}