using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ExtendInput.Controller
{
    public class DualShock4Controller : IController
    {
        public const int VendorId = 0x054C;
        public const int ProductIdDongle = 0x0BA0;
        public const int ProductIdDongleBroken = 0x0BA1; // wierd broken state, being flashed?
        public const int ProductIdWired = 0x05C4; // and BT
        public const int ProductIdWiredV2 = 0x09CC; // and BT

        public const int BrookMarsVendorId = 0x0C12;
        public const int BrookMarsProductId = 0x0E20;

        private const byte _REPORT_STATE_1 = 0x11;
        private const byte _REPORT_STATE_2 = 0x12;
        private const byte _REPORT_STATE_3 = 0x13;
        private const byte _REPORT_STATE_4 = 0x14;
        private const byte _REPORT_STATE_5 = 0x15;
        private const byte _REPORT_STATE_6 = 0x16;
        private const byte _REPORT_STATE_7 = 0x17;
        private const byte _REPORT_STATE_8 = 0x18;
        private const byte _REPORT_STATE_9 = 0x19;

        private const int _SLOW_POLL_MS = 1000;

        private readonly string[] _CONNECTION_WIRE = new string[] { "USB_WIRE", "WIRE" };
        private readonly string[] _CONNECTION_BT = new string[] { "BT" };
        private readonly string[] _CONNECTION_DONGLE = new string[] { "DS4_DONGLE", "DONGLE" };
        private readonly string[] _CONNECTION_UNKKNOWN = new string[] { "UNKNOWN" };

        private readonly string[] _CONTROLLER_DS4V1 = new string[] { "DS4V1", "DS4", "GAMEPAD" };
        private readonly string[] _CONTROLLER_DS4V2 = new string[] { "DS4V2", "DS4", "GAMEPAD" };
        private readonly string[] _CONTROLLER_MARS = new string[] { "BROOKMARS", "MARS", "DS4", "GAMEPAD" };
        private readonly string[] _CONTROLLER_UNKKNOWN = new string[] { "UNKNOWN" };

        public EConnectionType ConnectionType { get; private set; }
        public EPollingState PollingState { get; private set; }

        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }


        private bool HaveSeenNonZeroRawTemp;

        public bool SensorsEnabled;
        private HidDevice _device;
        int reportUsageLock = 0;
        private byte last_touch_timestamp;
        private bool touch_last_frame;
        //private DateTime tmp = DateTime.Now;

        public event ControllerNameUpdateEvent ControllerNameUpdated;

        public bool HasMotion => true;

        public ControllerState GetState()
        {
            return State;
        }


        public IDevice DeviceHackRef => _device;

        ControllerState State = new ControllerState();
        ControllerState OldState = new ControllerState();


        public delegate void StateUpdatedEventHandler(object sender, ControllerState e);
        public event StateUpdatedEventHandler StateUpdated;

        public DualShock4Controller(HidDevice device, EConnectionType ConnectionType = EConnectionType.Unknown)
        {
            HaveSeenNonZeroRawTemp = false;

            this.ConnectionType = ConnectionType;

            switch(ConnectionType)
            {
                case EConnectionType.USB:
                    ConnectionTypeCode = _CONNECTION_WIRE;
                    break;
                case EConnectionType.Bluetooth:
                    ConnectionTypeCode = _CONNECTION_BT;
                    break;
                case EConnectionType.Dongle:
                    ConnectionTypeCode = _CONNECTION_DONGLE;
                    break;
                default:
                    ConnectionTypeCode = _CONNECTION_UNKKNOWN;
                    break;
            }
            ControllerTypeCode = GetControllerTypeCode((UInt16)device.VendorId, (UInt16)device.ProductId);

            State.Controls["quad_left"] = new ControlDPad();
            State.Controls["quad_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair();
            State.Controls["bumpers2"] = new ControlButtonPair();
            State.Controls["triggers"] = new ControlTriggerPair(HasStage2: false);
            State.Controls["menu"] = new ControlButtonPair();
            State.Controls["home"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);
            if (device.VendorId == BrookMarsVendorId || device.ProductId == BrookMarsProductId)
            {
                State.Controls["touch_center"] = new ControlButton();
            }
            else
            {
                State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
            }
            State.Controls["motion"] = new ControlMotion();

            // According to this the normalized domain of the DS4 gyro is 1024 units per rad/s: https://gamedev.stackexchange.com/a/87178

            _device = device;

            touch_last_frame = false;
            PollingState = EPollingState.Inactive;

            // if we are a dongle we must slow poll instead of sit inactive
            if (ConnectionType == EConnectionType.Dongle && PollingState != EPollingState.SlowPoll)
            {
                // open the device overlapped read so we don't get stuck waiting for a report when we write to it
                //_device.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
                _device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

                PollingState = EPollingState.SlowPoll;
                _device.StartReading();
            }

            _device.DeviceReport += OnReport;
        }

        public void Initalize()
        {
            if (PollingState == EPollingState.Active) return;

            touch_last_frame = false;

            PollingState = EPollingState.Active;
            _device.StartReading();
        }

        public void DeInitalize()
        {
            if (PollingState == EPollingState.Inactive) return;
            if (PollingState == EPollingState.SlowPoll) return;

            // dongles switch back to slow poll instead of going inactive
            if (ConnectionType == EConnectionType.Dongle)
            {
                PollingState = EPollingState.SlowPoll;
            }
            else
            {
                _device.StopReading();

                PollingState = EPollingState.Inactive;
                _device.CloseDevice();
            }
        }

        public async void Identify()
        {
            byte[] report;
            int offset = 0;
            if (ConnectionType == EConnectionType.Bluetooth)
            {
                report = new byte[78]
                {
                    0x11, 0xC0, 0x20, 0x05, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00,
                    0x00, 0x0f, 0x0f,

                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00
                };
                offset = 2;
            }
            else
            {
                report = new byte[32]
                {
                    0x05, 0x05, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00, 0x00, 0x0f,
                    0x0f,

                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00,
                };
            }

            if (_device.WriteReport(report))
            {
                Thread.Sleep(250);
                report[1 + offset + 0] = 0x01;
                report[1 + offset + 3] = 0x00;
                report[1 + offset + 4] = 0x00;
                _device.WriteReport(report);
                Thread.Sleep(2000);
                report[1 + offset + 0] = 0x04;
                report[1 + offset + 8] = 0x01;
                report[1 + offset + 9] = 0x01;
                _device.WriteReport(report);
            }
        }

        public bool CheckSensorDataStuck()
        {
            return (OldState != null &&
                (State.Controls["motion"] as ControlMotion).AccelerometerX == 0 &&
                (State.Controls["motion"] as ControlMotion).AccelerometerY == 0 &&
                (State.Controls["motion"] as ControlMotion).AccelerometerZ == 0 ||
                (State.Controls["motion"] as ControlMotion).AccelerometerX == (OldState.Controls["motion"] as ControlMotion).AccelerometerX &&
                (State.Controls["motion"] as ControlMotion).AccelerometerY == (OldState.Controls["motion"] as ControlMotion).AccelerometerY &&
                (State.Controls["motion"] as ControlMotion).AccelerometerZ == (OldState.Controls["motion"] as ControlMotion).AccelerometerZ ||
                (State.Controls["motion"] as ControlMotion).AngularVelocityX == (OldState.Controls["motion"] as ControlMotion).AngularVelocityX &&
                (State.Controls["motion"] as ControlMotion).AngularVelocityY == (OldState.Controls["motion"] as ControlMotion).AngularVelocityY &&
                (State.Controls["motion"] as ControlMotion).AngularVelocityZ == (OldState.Controls["motion"] as ControlMotion).AngularVelocityZ
            );
        }

        private string[] GetControllerTypeCode(UInt16 VID, UInt16 PID)
        {
            if (_device != null && VID == VendorId && PID == ProductIdDongle) // we are an offical dongle
            {
                byte[] FeatureBuffer;
                _device.ReadFeatureData(out FeatureBuffer, 0xE3);
                VID = BitConverter.ToUInt16(FeatureBuffer, 1);
                PID = BitConverter.ToUInt16(FeatureBuffer, 3);
            }

            switch (VID)
            {
                case VendorId:
                    switch (PID)
                    {
                        case ProductIdWired:
                            return _CONTROLLER_DS4V1;
                        case ProductIdWiredV2:
                            return _CONTROLLER_DS4V2;
                    }
                    break;
                case BrookMarsVendorId:
                    switch (PID)
                    {
                        case BrookMarsProductId:
                            return _CONTROLLER_MARS;
                    }
                    break;
            }
            return _CONTROLLER_UNKKNOWN;
        }

        private string GetDeviceName(UInt16 VID, UInt16 PID, out bool CanGetSN)
        {
            switch (VID)
            {
                case VendorId:
                    switch (PID)
                    {
                        case ProductIdDongle:
                            CanGetSN = false;
                            return "DUALSHOCK®4 USB Wireless Adaptor";
                        case ProductIdDongleBroken:
                            CanGetSN = false;
                            return "DUALSHOCK®4 USB Wireless Adaptor in DFU mode!!!";
                        case ProductIdWired:
                            CanGetSN = true;
                            if (!HaveSeenNonZeroRawTemp)
                                return $"Sony DUALSHOCK®4 Controller V1 (Possible Unoffical)";
                            return $"Sony DUALSHOCK®4 Controller V1";
                        case ProductIdWiredV2:
                            CanGetSN = true;
                            if (!HaveSeenNonZeroRawTemp)
                                return $"Sony DUALSHOCK®4 Controller V2 (Possible Unoffical)";
                            return $"Sony DUALSHOCK®4 Controller V2";
                        default:
                            CanGetSN = false;
                            return $"Sony Device <{PID:X4}>";
                    }
                case BrookMarsVendorId:
                    switch (PID)
                    {
                        case BrookMarsProductId:
                            CanGetSN = false;
                            return "Brook Mars Wired Controller";
                        default:
                            CanGetSN = false;
                            return $"Brook Device <{PID:X4}>";
                    }
                default:
                    CanGetSN = false;
                    return $"Unknown Device <{VID:X4},{PID:X4}>";
            }
        }

        Regex MacAsSerialNumber = new Regex("^[0-9a-fA-F]{12}$");
        private void GetHidSerialNumberIfNull(ref string Serial)
        {
            if (string.IsNullOrWhiteSpace(Serial))
            {
                string SerialNumber = _device.ReadSerialNumber();
                if (!string.IsNullOrWhiteSpace(SerialNumber))
                {
                    // TODO confirm this is in the right order and we don't need to reverse it
                    if (MacAsSerialNumber.IsMatch(SerialNumber))
                        SerialNumber = $"{SerialNumber.Substring(0, 2)}:{SerialNumber.Substring(2, 2)}:{SerialNumber.Substring(4, 2)}:{SerialNumber.Substring(6, 2)}:{SerialNumber.Substring(8, 2)}:{SerialNumber.Substring(10, 2)}".ToUpperInvariant();
                    Serial = SerialNumber;
                }
            }

            if (string.IsNullOrWhiteSpace(Serial))
                Serial = null;
        }

        public string GetName()
        {
            string DeviceName = null;
            bool CanGetSN = false;
            if (_device.VendorId == VendorId)
            {
                if (ConnectionType == EConnectionType.Dongle)
                {
                    if (_device.ProductId == ProductIdDongle) // we are an offical dongle
                    {
                        byte[] FeatureBuffer;
                        _device.ReadFeatureData(out FeatureBuffer, 0xE3);
                        UInt16 local_VID = BitConverter.ToUInt16(FeatureBuffer, 1);
                        UInt16 local_PID = BitConverter.ToUInt16(FeatureBuffer, 3);

                        if (local_VID == 0)
                        {
                            DeviceName = GetDeviceName(VendorId, ProductIdDongle, out CanGetSN);
                        }
                        else
                        {
                            DeviceName = GetDeviceName(local_VID, local_PID, out CanGetSN);
                        }

                        if (!CanGetSN)
                            return DeviceName;

                        string Serial = null;
                        try
                        {
                            // get MAC of controller via Dongle
                            _device.ReadFeatureData(out FeatureBuffer, 0x12);
                            Serial = string.Join(":", FeatureBuffer.Skip(1).Take(6).Reverse().Select(dr => $"{dr:X2}").ToArray());
                            if (Serial == "00:00:00:00:00:00")
                                Serial = null;
                        }
                        catch { }

                        GetHidSerialNumberIfNull(ref Serial);

                        return DeviceName += $" [{Serial ?? "No ID"}]";
                    }
                    else // unknown dongle so we don't know what to do, currently can't happen
                    {
                        DeviceName = GetDeviceName((UInt16)_device.VendorId, (UInt16)_device.ProductId, out _);
                        return DeviceName;
                    }
                }
                else // not a dongle
                {
                    DeviceName = GetDeviceName((UInt16)_device.VendorId, (UInt16)_device.ProductId, out CanGetSN);

                    if (!CanGetSN)
                        return DeviceName;

                    // for right now we can only get here here V1 and V2 controllers, so we can safely call the GetMac report
                    string Serial = null;
                    try
                    {
                        byte[] data;
                        _device.ReadFeatureData(out data, 0x12);
                        Serial = string.Join(":", data.Skip(1).Take(6).Reverse().Select(dr => $"{dr:X2}").ToArray());
                        if (Serial == "00:00:00:00:00:00")
                            Serial = null;
                    }
                    catch { }

                    GetHidSerialNumberIfNull(ref Serial);

                    return DeviceName += $" [{Serial ?? "No ID"}]";
                }
            }
            else // not a Sony device
            {
                DeviceName = GetDeviceName((UInt16)_device.VendorId, (UInt16)_device.ProductId, out CanGetSN);

                if (!CanGetSN)
                    return DeviceName;

                string Serial = null;
                GetHidSerialNumberIfNull(ref Serial);

                return DeviceName += $" [{Serial ?? "No ID"}]";
            }
        }

        bool DisconnectedBit = false;
        private void OnReport(byte[] reportData)
        {
            if (PollingState == EPollingState.Inactive) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    //OldState = State; // shouldn't this be a clone?
                    //if (_attached == false) { return; }

                    int baseOffset = 0;
                    bool HasStateData = true;
                    if (ConnectionType == EConnectionType.Bluetooth)
                    {
                        baseOffset = 2;
                        HasStateData = (reportData[1 + 1] & 0x80) == 0x80;
                    }

                    if (HasStateData)
                    {
                        (StateInFlight.Controls["stick_left"] as ControlStick).X = ControllerMathTools.QuickStickToFloat(reportData[1 + baseOffset + 0]);
                        (StateInFlight.Controls["stick_left"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat(reportData[1 + baseOffset + 1]);

                        bool Finger1BrookMarsTest = (reportData[1 + baseOffset + 34] & 0x80) != 0x80;
                        if (_device.VendorId == BrookMarsVendorId && _device.ProductId == BrookMarsProductId && Finger1BrookMarsTest)
                        {
                            int F1X = reportData[1 + baseOffset + 35]
                                  | ((reportData[1 + baseOffset + 36] & 0xF) << 8);
                            int F1Y = ((reportData[1 + baseOffset + 36] & 0xF0) >> 4)
                                     | (reportData[1 + baseOffset + 37] << 4);

                            (StateInFlight.Controls["stick_right"] as ControlStick).X = ControllerMathTools.QuickStickToFloat((byte)((F1X - 192) / 6));
                            (StateInFlight.Controls["stick_right"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat((byte)((F1Y - 86) / 3));
                        }
                        else
                        {
                            (StateInFlight.Controls["stick_right"] as ControlStick).X = ControllerMathTools.QuickStickToFloat(reportData[1 + baseOffset + 2]);
                            (StateInFlight.Controls["stick_right"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat(reportData[1 + baseOffset + 3]);
                        }

                        (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonN = (reportData[1 + baseOffset + 4] & 128) == 128;
                        (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonE = (reportData[1 + baseOffset + 4] & 64) == 64;
                        (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonS = (reportData[1 + baseOffset + 4] & 32) == 32;
                        (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonW = (reportData[1 + baseOffset + 4] & 16) == 16;

                        switch ((reportData[1 + baseOffset + 4] & 0x0f))
                        {
                            case 0: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.North; break;
                            case 1: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.NorthEast; break;
                            case 2: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.East; break;
                            case 3: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.SouthEast; break;
                            case 4: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.South; break;
                            case 5: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.SouthWest; break;
                            case 6: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.West; break;
                            case 7: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.NorthWest; break;
                            default: (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.None; break;
                        }

                        (StateInFlight.Controls["stick_right"] as ControlStick).Click = (reportData[1 + baseOffset + 5] & 128) == 128;
                        (StateInFlight.Controls["stick_left"] as ControlStick).Click = (reportData[1 + baseOffset + 5] & 64) == 64;
                        (StateInFlight.Controls["menu"] as ControlButtonPair).Right.Button0 = (reportData[1 + baseOffset + 5] & 32) == 32;
                        (StateInFlight.Controls["menu"] as ControlButtonPair).Left.Button0 = (reportData[1 + baseOffset + 5] & 16) == 16;
                        (StateInFlight.Controls["bumpers2"] as ControlButtonPair).Right.Button0 = (reportData[1 + baseOffset + 5] & 8) == 8;
                        (StateInFlight.Controls["bumpers2"] as ControlButtonPair).Left.Button0 = (reportData[1 + baseOffset + 5] & 4) == 4;
                        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Button0 = (reportData[1 + baseOffset + 5] & 2) == 2;
                        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Button0 = (reportData[1 + baseOffset + 5] & 1) == 1;

                        // counter
                        // bld.Append((reportData[1 + baseOffset + 6] & 0xfc).ToString().PadLeft(3, '0'));

                        (StateInFlight.Controls["home"] as ControlButton).Button0 = (reportData[1 + baseOffset + 6] & 0x1) == 0x1;

                        if (_device.VendorId == BrookMarsVendorId || _device.ProductId == BrookMarsProductId)
                        {
                            (StateInFlight.Controls["touch_center"] as ControlButton).Button0 = (reportData[1 + baseOffset + 6] & 0x2) == 0x2;
                        }
                        else
                        {
                            (StateInFlight.Controls["touch_center"] as ControlTouch).Click = (reportData[1 + baseOffset + 6] & 0x2) == 0x2;
                        }

                        (StateInFlight.Controls["triggers"] as ControlTriggerPair).Left.Analog = (float)reportData[1 + baseOffset + 7] / byte.MaxValue;
                        (StateInFlight.Controls["triggers"] as ControlTriggerPair).Right.Analog = (float)reportData[1 + baseOffset + 8] / byte.MaxValue;

                        // GyroTimestamp
                        //bld.Append(BitConverter.ToUInt16(reportData, 1 + baseOffset + 9).ToString().PadLeft(5));
                        // FIX: (timestamp * 16) / 3

                        // Battery Temperture
                        if (!HaveSeenNonZeroRawTemp)
                        {
                            if (reportData[1 + baseOffset + 11] != 0)
                            {
                                HaveSeenNonZeroRawTemp = true;
                                //ControllerTypeCode = GetControllerTypeCode((UInt16)device.VendorId, (UInt16)device.ProductId);
                                ControllerNameUpdated?.Invoke();
                            }
                        }

                        (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityX = BitConverter.ToInt16(reportData, 1 + baseOffset + 12);
                        (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityZ = BitConverter.ToInt16(reportData, 1 + baseOffset + 14);
                        (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityY = BitConverter.ToInt16(reportData, 1 + baseOffset + 16);
                        (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerX = BitConverter.ToInt16(reportData, 1 + baseOffset + 18);
                        (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerY = BitConverter.ToInt16(reportData, 1 + baseOffset + 20);
                        (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerZ = BitConverter.ToInt16(reportData, 1 + baseOffset + 22);

                        // ??
                        // bld.Append(reportData[1 + baseOffset + 27].ToString("X2"));

                        //State.Inputs.? = (reportData[1 + baseOffset + 29] & 128) == 128;
                        //State.Inputs.Mic = (reportData[1 + baseOffset + 29] & 64) == 64;
                        //State.Inputs.Headphone = (reportData[1 + baseOffset + 29] & 32) == 32;
                        //State.Inputs.PowerCable = (reportData[1 + baseOffset + 29] * 16) == 16;

                        //int bat = reportData[1 + baseOffset + 29] & 0x0f;
                        //bool plugged = (reportData[1 + baseOffset + 29] & 0x10) == 0x10;

                        // ??
                        // bld.Append(reportData[1 + baseOffset + 30].ToString("X2"));
                        // bld.Append(reportData[1 + baseOffset + 31].ToString("X2") + " ");

                        bool DisconnectedFlag = (reportData[1 + baseOffset + 30] & 0x04) == 0x04;
                        if (DisconnectedFlag != DisconnectedBit)
                        {
                            DisconnectedBit = DisconnectedFlag;
                            ControllerTypeCode = GetControllerTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId);
                            ControllerNameUpdated?.Invoke();
                        }

                        // Brook Mars has emulated touch with stick, we don't care about that since we'd do it in software
                        if (_device.VendorId == BrookMarsVendorId && _device.ProductId == BrookMarsProductId)
                        {

                        }
                        else
                        {
                            int TouchDataCount = reportData[1 + baseOffset + 32];

                            for (int FingerCounter = 0; FingerCounter < TouchDataCount; FingerCounter++)
                            {
                                byte touch_timestamp = reportData[1 + baseOffset + 33 + (FingerCounter * 9)]; // Touch Pad Counter
                                                                                                              //DateTime tmp_now = DateTime.Now;

                                bool Finger1 = (reportData[1 + baseOffset + 34 + (FingerCounter * 9)] & 0x80) != 0x80;
                                byte Finger1Index = (byte)(reportData[1 + baseOffset + 34 + (FingerCounter * 9)] & 0x7f);
                                int F1X = reportData[1 + baseOffset + 35 + (FingerCounter * 9)]
                                      | ((reportData[1 + baseOffset + 36 + (FingerCounter * 9)] & 0xF) << 8);
                                int F1Y = ((reportData[1 + baseOffset + 36 + (FingerCounter * 9)] & 0xF0) >> 4)
                                         | (reportData[1 + baseOffset + 37 + (FingerCounter * 9)] << 4);

                                bool Finger2 = (reportData[1 + baseOffset + 38 + (FingerCounter * 9)] & 0x80) != 0x80;
                                byte Finger2Index = (byte)(reportData[1 + baseOffset + 38 + (FingerCounter * 9)] & 0x7f);
                                int F2X = reportData[1 + baseOffset + 39 + (FingerCounter * 9)]
                                      | ((reportData[1 + baseOffset + 40 + (FingerCounter * 9)] & 0xF) << 8);
                                int F2Y = ((reportData[1 + baseOffset + 40 + (FingerCounter * 9)] & 0xF0) >> 4)
                                         | (reportData[1 + baseOffset + 41 + (FingerCounter * 9)] << 4);

                                bool Finger1Valid = true;
                                bool Finger2Valid = true;

                                if (!HaveSeenNonZeroRawTemp) // Battery temp is 0x00, which suggests it's a 3rd party controller
                                {
                                    if (Finger1 && (F1X > 1919f || F1Y > 942f))
                                        Finger1Valid = false;
                                    if (Finger2 && (F2X > 1919f || F2Y > 942f))
                                        Finger2Valid = false;
                                    //Console.WriteLine($"TS:{touch_timestamp}\t{Finger1}\t{Finger1Index}\t{F1X}\t{F1Y}\t{Finger2}\t{Finger2Index}\t{F2X}\t{F2Y}");
                                }
                                if (Finger1Valid || Finger2Valid)
                                {
                                    byte TimeDelta = touch_last_frame ? ControllerMathTools.GetOverflowedDelta(last_touch_timestamp, touch_timestamp) : (byte)0;

                                    //Console.WriteLine($"{TimeDelta} {(tmp_now - tmp).Milliseconds}");
                                    (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(0, Finger1Valid && Finger1, (F1X / 1919f) * 2f - 1f, (F1Y / 942f) * 2f - 1f, TimeDelta);
                                    (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(1, Finger2Valid && Finger2, (F2X / 1919f) * 2f - 1f, (F2Y / 942f) * 2f - 1f, TimeDelta);

                                    last_touch_timestamp = touch_timestamp;
                                }
                                //tmp = tmp_now;
                            }

                            touch_last_frame = TouchDataCount > 0;
                        }

                        // bring OldState in line with new State
                        OldState = State;
                        State = StateInFlight;

                        StateUpdated?.Invoke(this, State);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }


                // TODO: change how this works because we don't want to lock, we need to actually change the polling rate in the device
                if (ConnectionType == EConnectionType.Dongle && DisconnectedBit)
                    Thread.Sleep(_SLOW_POLL_MS); // if we're a dongle and we're not connected we might only be partially initalized, so slow roll our read
            }
        }
    }
}
