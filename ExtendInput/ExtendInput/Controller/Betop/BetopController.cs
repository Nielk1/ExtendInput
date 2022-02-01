using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;

namespace ExtendInput.Controller.Betop
{
    public class BetopController : IController
    {
        enum ControllerSubtype
        {
            Unknown,
            Asura3Wired,
            Asura3Dongle,
        }
        private ControllerSubtype SubType = ControllerSubtype.Unknown;

        public EConnectionType ConnectionType => EConnectionType.Unknown;


        public const int VENDOR_BETOP = 0x20bc;
        public const int PRODUCT_BETOP_ASURA3 = 0x5036;
        public const int PRODUCT_BETOP_ASURA3_DONGLE = 0x5037;


        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }
        public string Name => "Betop Gamepad";
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
        public IDevice DeviceHackRef => devices.First();
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


        private HidDevice deviceVendor => devices.Where(dr => dr.Usages?.Contains(0xff000003u) ?? false).FirstOrDefault();


        private AccesMode AccessMode;
        bool Initalized;
        public BetopController(string UniqueKey, AccesMode AccessMode, HidDevice device)
        {
            ConnectionUniqueID = UniqueKey;
            this.AccessMode = AccessMode;
            devices = new HashSet<HidDevice>();

            if (device.ProductId == PRODUCT_BETOP_ASURA3)
            {
                SubType = ControllerSubtype.Asura3Wired;
                ControllerTypeCode = new string[] { "DEVICE_ASURA3", "DEVICE_GAMEPAD" };
                ConnectionTypeCode = new string[] { "CONNECTION_WIRE_USB", "CONNECTION_WIRE" };
            }
            else if (device.ProductId == PRODUCT_BETOP_ASURA3_DONGLE)
            {
                SubType = ControllerSubtype.Asura3Dongle;
                ControllerTypeCode = new string[] { "DEVICE_ASURA3", "DEVICE_GAMEPAD" };
                ConnectionTypeCode = new string[] { "CONNECTION_DONGLE_ASURA3", "CONNECTION_DONGLE" };
            }
            else
            {
                SubType = ControllerSubtype.Unknown;
                ControllerTypeCode = new string[] { "DEVICE_UNKNOWN" };
                ConnectionTypeCode = new string[] { "CONNECTION_UNKNOWN" };
            }



            State.Controls["cluster_left"] = new ControlDPad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair(ButtonProperties.CMB_Bumper);
            State.Controls["triggers"] = new ControlButtonPair(ButtonProperties.CMB_Trigger);
            State.Controls["menu"] = new ControlButtonPair(ButtonProperties.CMB_Button);
            State.Controls["home"] = new ControlButton(ButtonProperties.CMB_Button);
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);

            if (device.ProductId == PRODUCT_BETOP_ASURA3 && (AccessMode == AccesMode.FullControl || AccessMode == AccesMode.SafeWriteOnly))
            {
                State.Controls["menu2"] = new ControlButtonPair(ButtonProperties.CMB_Button);
                State.Controls["grip"] = new ControlButtonPair(ButtonProperties.CMB_Button);
            }

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
        bool AbortStatusThread = false;
        Thread CheckControllerStatusThread;

        public void Dispose()
        {
            AbortStatusThread = true;
        }

        public void AddDevice(HidDevice device)
        {
            lock (InitalizationLock)
            {
                devices.Add(device);
                device.DeviceReport += OnReport;

                if (Initalized)
                {
                    EnableConfigMode();
                    device.StartReading();
                }
            }
        }

        private void SendConnectivity(byte param)
        {
            if (deviceVendor == null)
                return;
            byte[] data = new byte[64];
            data[0] = 0x05;
            data[1] = 0x00;
            data[2] = 0x00;
            data[3] = 0xB0;
            data[4] = param;
            deviceVendor.WriteReport(data);
        }

        // Config Mode is needed to get raw controller data from the vendor specific endpoint, it is enabled automatically be XInput switch mode
        private void EnableConfigMode()//byte param1, byte param2)
        {
            if (deviceVendor == null)
                return;

            // rumble toggling has this already applied, but we can just request it if it's alyready active anyway

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
            bool success = deviceVendor.WriteReport(data);
            //Console.ForegroundColor = success ? ConsoleColor.DarkGray : ConsoleColor.DarkRed;
            //Console.WriteLine($"{BitConverter.ToString(data)}");
            //Console.ResetColor();
        }

        // This enables raw key events for config mode
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
            bool success = deviceVendor.WriteReport(data);
            //Console.ForegroundColor = success ? ConsoleColor.DarkGray : ConsoleColor.DarkRed;
            //Console.WriteLine($"{BitConverter.ToString(data)}");
            //Console.ResetColor();
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

            //Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");

            if (reportData.ReportId == 0x05)
            {
                /*int reportLength = reportData.ReportBytes.Reverse().SkipWhile(dr => dr == 0x00).Count();
                if (reportLength > 0
                     && reportData.ReportBytes[2] != 0xBC)
                    Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");*/

                //lock (ReportLock)
                {
                    /*int _v21 = 2;
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

                    if(reportData.ReportBytes[2] == 0x8C)
                    {
                        
                    }*/


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
                //Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
            }

            switch (reportData.ReportId)
            {
                case 0x05:
                    switch (reportData.ReportBytes[2])
                    {
                        case 0x3D: // config data came in, so obviously we have it enabled
                            ConfigMode = true;
                            break;
                        case 0x8C: // ACK for config mode enable
                            ConfigMode = true;
                            EnableKeyEvent();
                            break;
                        case 0xAC: // ACK for KeyData for ConfigMode
                            ConfigModeKeyData = true;
                            break;
                        case 0xAD: // KeyData for ConfigMode
                            ConfigModeKeyData = true;
                            break;
                    }
                    break;
            }

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    //ControllerState StateInFlight = (ControllerState)State.Clone();

                    //if(_device.VendorId == VENDOR_BETOP && _device.ProductId == PRODUCT_BETOP_ASURA3)
                    {
                        //if(_device2 != null) // device2 is the vendor specific device that handles config events
                        {
                            switch(reportData.ReportId)
                            {
                                case 0x03:
                                    if (!ConfigModeKeyData)
                                    {
                                        ControllerState StateInFlight = (ControllerState)State.Clone();

                                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonN = (reportData.ReportBytes[2] & 0x10) == 0x10;
                                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonE = (reportData.ReportBytes[2] & 0x02) == 0x02;
                                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonS = (reportData.ReportBytes[2] & 0x01) == 0x01;
                                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonW = (reportData.ReportBytes[2] & 0x08) == 0x08;

                                        switch(reportData.ReportBytes[1])
                                        {
                                            case 0x0f: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.None; break;
                                            case 0x00: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.North; break;
                                            case 0x01: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.NorthEast; break;
                                            case 0x02: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.East; break;
                                            case 0x03: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.SouthEast; break;
                                            case 0x04: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.South; break;
                                            case 0x05: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.SouthWest; break;
                                            case 0x06: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.West; break;
                                            case 0x07: (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.NorthWest; break;
                                        }
                                        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.DigitalStage1 = (reportData.ReportBytes[2] & 0x40) == 0x40;
                                        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.DigitalStage1 = (reportData.ReportBytes[2] & 0x80) == 0x80;

                                        (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.AnalogStage1 = (float)(reportData.ReportBytes[8] > 0 ? reportData.ReportBytes[8] : (reportData.ReportBytes[3] & 0x01) == 0x01 ? byte.MaxValue : 0) / byte.MaxValue;
                                        (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.AnalogStage1 = (float)(reportData.ReportBytes[9] > 0 ? reportData.ReportBytes[9] : (reportData.ReportBytes[3] & 0x02) == 0x02 ? byte.MaxValue : 0) / byte.MaxValue;

                                        (StateInFlight.Controls["stick_left"] as ControlStick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[4]);
                                        (StateInFlight.Controls["stick_left"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[5]);
                                        (StateInFlight.Controls["stick_left"] as ControlStick).Click = (reportData.ReportBytes[3] & 0x20) == 0x20;
                                        (StateInFlight.Controls["stick_right"] as ControlStick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[6]);
                                        (StateInFlight.Controls["stick_right"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[7]);
                                        (StateInFlight.Controls["stick_right"] as ControlStick).Click = (reportData.ReportBytes[3] & 0x40) == 0x40;

                                        (StateInFlight.Controls["menu"] as ControlButtonPair).Left.DigitalStage1 = (reportData.ReportBytes[3] & 0x04) == 0x04;
                                        (StateInFlight.Controls["menu"] as ControlButtonPair).Right.DigitalStage1 = (reportData.ReportBytes[3] & 0x08) == 0x08;

                                        (StateInFlight.Controls["home"] as ControlButton).DigitalStage1 = (reportData.ReportBytes[3] & 0x10) == 0x10;

                                        State = StateInFlight;

                                        ControllerStateUpdate?.Invoke(this, State);
                                    }
                                    break;
                                case 0x05:
                                    switch(reportData.ReportBytes[2])
                                    {
                                        case 0x3D:
                                            {
                                                byte DataLen = reportData.ReportBytes[3];
                                                StringBuilder bld = new StringBuilder();
                                                for (int i = 0; i < DataLen - 2; i += 2)
                                                {
                                                    switch (reportData.ReportBytes[4 + i])
                                                    {
                                                        case 0x10: // light
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} light");
                                                            break;
                                                        case 0x20: // lightdir
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} lightdir");
                                                            break;
                                                        case 0x30: // lightcolor
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} lightcolor");
                                                            break;
                                                        case 0x40: // lightlevel
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} lightlevel");
                                                            break;
                                                        case 0x50: // freqlevel
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} freqlevel");
                                                            break;
                                                        case 0x60: // viblevel
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} viblevel");
                                                            break;
                                                        case 0x70: // viblight
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} viblight");
                                                            break;
                                                        case 0x80: // battery
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} battery");
                                                            break;
                                                        case 0x90: // leftsense
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} leftsense  |{new string('=', reportData.ReportBytes[4 + i + 1] - 1)}{new string('-', 7 - reportData.ReportBytes[4 + i + 1])}|");
                                                            break;
                                                        case 0xA0: // rightsense
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} rightsense |{new string('=', reportData.ReportBytes[4 + i + 1] - 1)}{new string('-', 7 - reportData.ReportBytes[4 + i + 1])}|");
                                                            break;
                                                        case 0xB0: // ltlevel
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} ltlevel");
                                                            break;
                                                        case 0xC0: // rtlevel
                                                            bld.AppendLine($"{reportData.ReportBytes[4 + i + 1]:X2} rtlevel");
                                                            break;
                                                    }
                                                }
                                                Console.WriteLine(bld.ToString());
                                            }
                                            break;
                                        case 0xAD:
                                            {
                                                ControllerState StateInFlight = (ControllerState)State.Clone();

                                                (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonN = (reportData.ReportBytes[4] == 0x01);
                                                (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonE = (reportData.ReportBytes[6] == 0x01);
                                                (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonS = (reportData.ReportBytes[5] == 0x01);
                                                (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonW = (reportData.ReportBytes[3] == 0x01);
                                                {
                                                    bool buttonUp = (reportData.ReportBytes[15] == 0x01);
                                                    bool buttonRight = (reportData.ReportBytes[18] == 0x01);
                                                    bool buttonDown = (reportData.ReportBytes[16] == 0x01);
                                                    bool buttonLeft = (reportData.ReportBytes[17] == 0x01);
                                                    int padH = 0;
                                                    int padV = 0;
                                                    if (buttonUp) padV++;
                                                    if (buttonDown) padV--;
                                                    if (buttonRight) padH++;
                                                    if (buttonLeft) padH--;
                                                    if (padH > 0)
                                                        if (padV > 0)
                                                            (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.NorthEast;
                                                        else if (padV < 0)
                                                            (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.SouthEast;
                                                        else
                                                            (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.East;
                                                    else if (padH < 0)
                                                        if (padV > 0)
                                                            (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.NorthWest;
                                                        else if (padV < 0)
                                                            (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.SouthWest;
                                                        else
                                                            (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.West;
                                                    else
                                                        if (padV > 0)
                                                        (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.North;
                                                    else if (padV < 0)
                                                        (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.South;
                                                    else
                                                        (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.None;
                                                }

                                                (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.DigitalStage1 = (reportData.ReportBytes[7] == 0x01);
                                                (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.DigitalStage1 = (reportData.ReportBytes[8] == 0x01);

                                                (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.AnalogStage1 = (float)reportData.ReportBytes[9] / byte.MaxValue;
                                                (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.AnalogStage1 = (float)reportData.ReportBytes[10] / byte.MaxValue;

                                                (StateInFlight.Controls["stick_left"] as ControlStick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[21]);
                                                (StateInFlight.Controls["stick_left"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[22]);
                                                (StateInFlight.Controls["stick_left"] as ControlStick).Click = reportData.ReportBytes[11] == 0x01;
                                                (StateInFlight.Controls["stick_right"] as ControlStick).X = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[23]);
                                                (StateInFlight.Controls["stick_right"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat(reportData.ReportBytes[24]);
                                                (StateInFlight.Controls["stick_right"] as ControlStick).Click = reportData.ReportBytes[12] == 0x01;

                                                (StateInFlight.Controls["menu"] as ControlButtonPair).Left.DigitalStage1 = reportData.ReportBytes[14] == 0x01;
                                                (StateInFlight.Controls["menu"] as ControlButtonPair).Right.DigitalStage1 = reportData.ReportBytes[13] == 0x01;

                                                (StateInFlight.Controls["menu2"] as ControlButtonPair).Left.DigitalStage1 = reportData.ReportBytes[28] == 0x01;
                                                (StateInFlight.Controls["menu2"] as ControlButtonPair).Right.DigitalStage1 = reportData.ReportBytes[27] == 0x01;

                                                (StateInFlight.Controls["grip"] as ControlButtonPair).Left.DigitalStage1 = reportData.ReportBytes[25] == 0x01;
                                                (StateInFlight.Controls["grip"] as ControlButtonPair).Right.DigitalStage1 = reportData.ReportBytes[26] == 0x01;

                                                (StateInFlight.Controls["home"] as ControlButton).DigitalStage1 = reportData.ReportBytes[29] == 0x01;


                                                State = StateInFlight;

                                                ControllerStateUpdate?.Invoke(this, State);
                                            }
                                            break;
                                    }
                                    break;
                            }
                        }
                        //else
                        //{
                        //
                        //}
                    }

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
                    //State = StateInFlight;

                    //ControllerStateUpdate?.Invoke(this, State);
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

                foreach (var device in devices)
                    device.StartReading();

                EnableConfigMode();
                CheckControllerStatusThread?.Abort();
                AbortStatusThread = false;
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
                CheckControllerStatusThread.Start();

                Initalized = true;
            }
        }

        public void DeInitalize()
        {
            lock (InitalizationLock)
            {
                if (!Initalized)
                    return;

                AbortStatusThread = true;

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

    }
}