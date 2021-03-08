using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.Controller
{
    public class DualShock4Controller : IController
    {
        #region Device IDs
        // Sony
        public const int VENDOR_SONY = 0x054C;
        public const int PRODUCT_SONY_DONGLE = 0x0BA0;
        public const int PRODUCT_SONY_DONGLE_DFU = 0x0BA1; // wierd broken state, being flashed?
        public const int PRODUCT_SONY_DS4V1 = 0x05C4; // and BT
        public const int PRODUCT_SONY_DS4V2 = 0x09CC; // and BT

        // Brook
        public const int VENDOR_BROOK = 0x0C12;
        public const int PRODUCT_BROOK_MARS = 0x0E20;
        #endregion Device IDs

        #region Identity Hashes
        private string IDENTITY_SHA256_895X = @"2e2415ca56598006b94f1274d15e64a1b99385c53e91102ae7708b8919fe124f";
        #endregion Identity Hashes

        #region String Definitions
        private const string ATOM_CONNECTION_WIRE = "WIRE";
        private const string ATOM_CONNECTION_USB_WIRE = "USB_WIRE";
        private const string ATOM_CONNECTION_BT = "BT";
        private const string ATOM_CONNECTION_DONGLE = "DONGLE";
        private const string ATOM_CONNECTION_DS4_DONGLE = "DS4_DONGLE";
        private const string ATOM_CONNECTION_UNKKNOWN = "UNKNOWN";

        private readonly string[] _CONNECTION_WIRE = new string[] { ATOM_CONNECTION_USB_WIRE, ATOM_CONNECTION_WIRE };
        private readonly string[] _CONNECTION_BT = new string[] { ATOM_CONNECTION_BT };
        private readonly string[] _CONNECTION_DONGLE = new string[] { ATOM_CONNECTION_DS4_DONGLE, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_UNKKNOWN = new string[] { ATOM_CONNECTION_UNKKNOWN };

        private const string ATOM_CONTROLLER_GAMEPAD = "GAMEPAD";
        private const string ATOM_CONTROLLER_DS4 = "DS4";
        private const string ATOM_CONTROLLER_DS4V1 = "DS4V1";
        private const string ATOM_CONTROLLER_DS4V2 = "DS4V2";
        private const string ATOM_CONTROLLER_DS4_895X = "DS4_895X";
        private const string ATOM_CONTROLLER_DS4_8951 = "DS4_8951";
        private const string ATOM_CONTROLLER_DS4_8952 = "DS4_8952";
        private const string ATOM_CONTROLLER_BROOKMARS = "BROOKMARS";
        private const string ATOM_CONTROLLER_UNKKNOWN = "UNKNOWN";

        private readonly string[] _CONTROLLER_DS4V1 = new string[] { ATOM_CONTROLLER_DS4V1, ATOM_CONTROLLER_DS4, ATOM_CONTROLLER_GAMEPAD };
        private readonly string[] _CONTROLLER_DS4V2 = new string[] { ATOM_CONTROLLER_DS4V2, ATOM_CONTROLLER_DS4, ATOM_CONTROLLER_GAMEPAD };
        private readonly string[] _CONTROLLER_DS4_895X = new string[] { ATOM_CONTROLLER_DS4_895X, ATOM_CONTROLLER_DS4, ATOM_CONTROLLER_GAMEPAD };
        private readonly string[] _CONTROLLER_DS4_8951 = new string[] { ATOM_CONTROLLER_DS4_8951, ATOM_CONTROLLER_DS4, ATOM_CONTROLLER_GAMEPAD };
        private readonly string[] _CONTROLLER_DS4_8952 = new string[] { ATOM_CONTROLLER_DS4_8952, ATOM_CONTROLLER_DS4, ATOM_CONTROLLER_GAMEPAD };
        private readonly string[] _CONTROLLER_BROOKMARS = new string[] { ATOM_CONTROLLER_BROOKMARS, ATOM_CONTROLLER_DS4, ATOM_CONTROLLER_GAMEPAD };
        private readonly string[] _CONTROLLER_UNKKNOWN = new string[] { ATOM_CONTROLLER_UNKKNOWN };
        #endregion String Definitions

        #region Device Property Defaults
        private const float DS4_PAD_MAX_X = 1919f;
        private const float DS4_PAD_MAX_Y = 942f;

        private const float NO8951_PAD_MAX_X = 1918f;
        private const float NO8951_PAD_MAX_Y = 940f;

        private const float NO8952_PAD_MAX_X = 940f;
        private const float NO8952_PAD_MAX_Y = 940f;
        #endregion Device Property Defaults

        enum DS4SubType
        {
            None, // DS4 dongle with nothing connected
            Unknown,
            SonyDS4V1, // DS4V1 that has shown a non-zero temperture or is assumed correct due to coming through the offical dongle
            SonyDS4V2, // DS4V2 that has shown a non-zero temperture or is assumed correct due to coming through the offical dongle
            UnknownDS4V1, // DS4V1 when it first connects
            UnknownDS4V2, // DS4V2 when it first connects
            BrookMars, // wired only controller, so it is detected immediately
            No895X, // Has the No895X identifier
            No8951, // The touch pad is bigger than the No8952, so that's how this is found based on input
            No8952, // This has a readable extra button the No8951 does not, so that's how this is found based on input
        }
        private DS4SubType ControllerSubType = DS4SubType.None;



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

        /// <summary>
        /// Bitmask for checking QuirkExtraButtonByte6Bit3RingBuffer to control how many samples are needed
        /// </summary>
        private byte QUIRK_EXTRA_BUTTON_BYTE6_BIT3_BT_OBSCURE_RINGBUFFER_CHECK = 0x0F;


        /// <summary>
        /// An extra button exists on some controllers hidden in counting bits.
        /// This byte acts as a ring buffer to hold input bits, if all are 1 then some are human triggered and thus the button is pressed.
        /// </summary>
        private byte QuirkExtraButtonByte6Bit3RingBuffer = 0x00;

        /// <summary>
        /// Current max possible X value for touch pad
        /// </summary>
        private float TouchPadMaxX = 1919f;

        /// <summary>
        /// Current max possible Y value for touch pad
        /// </summary>
        private float TouchPadMaxY = 942f;
        

        private bool ControlsCreated = false;

        private string SerialNumber = null;

        public EConnectionType ConnectionType { get; private set; }
        public EPollingState PollingState { get; private set; }

        public string[] ConnectionTypeCode
        {
            get
            {
                switch (ConnectionType)
                {
                    case EConnectionType.USB:       return _CONNECTION_WIRE;
                    case EConnectionType.Bluetooth: return _CONNECTION_BT;
                    case EConnectionType.Dongle:    return _CONNECTION_DONGLE;
                    default:                        return _CONNECTION_UNKKNOWN;
                }
            }
        }
        public string[] ControllerTypeCode
        {
            get
            {
                switch (ControllerSubType)
                {
                    case DS4SubType.None:         return _CONTROLLER_UNKKNOWN;
                    case DS4SubType.SonyDS4V1:
                    case DS4SubType.UnknownDS4V1: return _CONTROLLER_DS4V1;
                    case DS4SubType.SonyDS4V2:
                    case DS4SubType.UnknownDS4V2: return _CONTROLLER_DS4V2;
                    case DS4SubType.BrookMars:    return _CONTROLLER_BROOKMARS;
                    case DS4SubType.No895X:       return _CONTROLLER_DS4_895X;
                    case DS4SubType.No8951:       return _CONTROLLER_DS4_8951;
                    case DS4SubType.No8952:       return _CONTROLLER_DS4_8952;
                    default:                      return _CONTROLLER_UNKKNOWN;
                }
            }
        }
        public string Name
        {
            get
            {
                UInt16 VID = (UInt16)_device.VendorId;
                UInt16 PID = (UInt16)_device.ProductId;

                string DeviceName = GetDeviceName(VID, PID);

                switch (ControllerSubType)
                {
                    case DS4SubType.None:
                    case DS4SubType.BrookMars:
                        return DeviceName;
                    case DS4SubType.SonyDS4V1:
                    case DS4SubType.SonyDS4V2:
                    case DS4SubType.UnknownDS4V1:
                    case DS4SubType.UnknownDS4V2:
                    case DS4SubType.No895X:
                    case DS4SubType.No8951:
                    case DS4SubType.No8952:
                    case DS4SubType.Unknown:
                    default:
                        return $"{DeviceName} [{SerialNumber ?? "No ID"}]";
                }
            }
        }


        private bool HaveSeenNonZeroRawTemp;

        public bool SensorsEnabled;
        private HidDevice _device;
        int reportUsageLock = 0;
        private byte last_touch_timestamp;
        private bool touch_last_frame;
        //private DateTime tmp = DateTime.Now;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;

        // The controller state can drastically change, where it picks up or loses items based on passive detection of quirky controllers, this makes it safe
        private ReaderWriterLockSlim StateMutationLock = new ReaderWriterLockSlim();

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
            this.ConnectionType = ConnectionType;

            _device = device;

            touch_last_frame = false;
            PollingState = EPollingState.Inactive;

            _device.DeviceReport += OnReport;

            ResetControllerInfo();

            // if we are a dongle we must slow poll instead of sit inactive
            if (ConnectionType == EConnectionType.Dongle && PollingState != EPollingState.SlowPoll)
            {
                // open the device overlapped read so we don't get stuck waiting for a report when we write to it
                ////_device.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
                //_device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

                PollingState = EPollingState.SlowPoll;

                // Start reading before the controller is active if it's a dongle since the dongle has to be asked if it has a controller or not
                _device.StartReading();
            }
        }

        public void Dispose()
        {
            // If we're a dongle and are slow-polling, our read thread is still active and must be stopped
            if (PollingState == EPollingState.SlowPoll)
            {
                _device.StopReading();

                PollingState = EPollingState.Inactive;
                _device.CloseDevice();
            }
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
            bool LEDInstead = false;
            switch(ControllerSubType)
            {
                case DS4SubType.No895X:
                case DS4SubType.No8951:
                case DS4SubType.No8952:
                    LEDInstead = true;
                    break;
            }

            byte[] report;
            int offset = 0;
            if (ConnectionType == EConnectionType.Bluetooth)
            {
                report = new byte[78]
                {
                    0x11, 0xC0, 0x20,
                    (byte)(LEDInstead ? 0x02 : 0x01), 0x00, 0x00,
                    0xff, 0xff,
                    0x00, 0x00, // LED must be black when doing LEDInstead
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
                    0x05,
                    (byte)(LEDInstead ? 0x02 : 0x01), 0x00, 0x00,
                    0xff, 0xff,
                    0x00, 0x00, 0x00, 0x0f, // LED must be black when doing LEDInstead
                    0x0f,

                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00,
                };
            }

            if (_device.WriteReport(report))
            {
                Thread.Sleep(250);
                report[1 + offset + 0] = (byte)(LEDInstead ? 0x02 : 0x01);
                report[1 + offset + 3] = 0x00;
                report[1 + offset + 4] = 0x00;
                _device.WriteReport(report);
            }
        }

        private string GetControllerIdentityHash()
        {
            byte[] RawData = new byte[0x100 + 0x10 + 0x100 + 0x100 + 0x100];
            for (; ; )
            {
                byte[] DataRead = new byte[64];
                bool success = _device.ReadFeatureData(out DataRead, 0xf1);
                if (!success)
                    return null;

                Array.Copy(DataRead, 4, RawData, 56 * DataRead[2], Math.Min(56, RawData.Length - (56 * DataRead[2])));

                if (DataRead[2] == 0x12)
                    break;
            }

            //byte[] signature = new byte[0x100]; Array.Copy(RawData, 0, signature, 0, 0x100);
            byte[] identity = new byte[0x10 + 0x100 + 0x100]; Array.Copy(RawData, 0x100, identity, 0, 0x10 + 0x100 + 0x100);
            //byte[] serial_num = new byte[0x10]; Array.Copy(RawData, 0x100, serial_num, 0, 0x10);
            //byte[] n = new byte[0x100]; Array.Copy(RawData, 0x100 + 0x10, n, 0, 0x100);
            //byte[] e = new byte[0x100]; Array.Copy(RawData, 0x100 + 0x10 + 0x100, e, 0, 0x100);
            //byte[] casig = new byte[0x100]; Array.Copy(RawData, 0x100 + 0x10 + 0x100 + 0x100, casig, 0, 0x100);

            byte[] sha_identity = SHA256.Create().ComputeHash(identity);
            return BitConverter.ToString(sha_identity).Replace("-", string.Empty).ToLowerInvariant();
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

        private DS4SubType GetControllerInitialTypeCode(UInt16 VID, UInt16 PID)
        {
            bool FromDongle = false;
            if (_device != null && VID == VENDOR_SONY && PID == PRODUCT_SONY_DONGLE) // we are an offical dongle
            {
                byte[] FeatureBuffer;
                _device.ReadFeatureData(out FeatureBuffer, 0xE3);
                VID = BitConverter.ToUInt16(FeatureBuffer, 1);
                PID = BitConverter.ToUInt16(FeatureBuffer, 3);
                FromDongle = true;
            }

            switch (VID)
            {
                case VENDOR_SONY:
                    switch (PID)
                    {
                        case PRODUCT_SONY_DS4V1:
                            return FromDongle ? DS4SubType.SonyDS4V1 : DS4SubType.UnknownDS4V1; // assume it's offical if it's on the dongle for now to reduce code complexity, since we will be blocked from accessing a lot of fancy stuff
                        case PRODUCT_SONY_DS4V2:
                            return FromDongle ? DS4SubType.SonyDS4V2 : DS4SubType.UnknownDS4V2; // assume it's offical if it's on the dongle for now to reduce code complexity, since we will be blocked from accessing a lot of fancy stuff
                    }
                    break;
                case VENDOR_BROOK:
                    switch (PID)
                    {
                        case PRODUCT_BROOK_MARS:
                            return DS4SubType.BrookMars;
                    }
                    break;
            }
            return FromDongle ? DS4SubType.None : DS4SubType.Unknown;
        }


        private Regex MacAsSerialNumber = new Regex("^[0-9a-fA-F]{12}$");
        private string GetSerialNumber()
        {
            switch (ControllerSubType)
            {
                case DS4SubType.None:
                case DS4SubType.SonyDS4V1:
                case DS4SubType.SonyDS4V2:
                case DS4SubType.UnknownDS4V1:
                case DS4SubType.UnknownDS4V2:
                    // try asking the device for its MAC
                    {
                        byte[] FeatureBuffer;
                        string Serial = null;
                        try
                        {
                            // get MAC of controller via Dongle
                            bool success = _device.ReadFeatureData(out FeatureBuffer, 0x12);
                            if (success)
                            {
                                Serial = string.Join(":", FeatureBuffer.Skip(1).Take(6).Reverse().Select(dr => $"{dr:X2}").ToArray());
                                if (Serial != "00:00:00:00:00:00")
                                {
                                    return Serial;
                                }
                            }
                        }
                        catch { }
                    }

                    // try using the local Serial Number from HID interface if we didn't get one yet
                    {
                        string SerialNumber = _device.ReadSerialNumber();
                        if (!string.IsNullOrWhiteSpace(SerialNumber))
                        {
                            // TODO confirm this is in the right order and we don't need to reverse it
                            if (MacAsSerialNumber.IsMatch(SerialNumber))
                                SerialNumber = $"{SerialNumber.Substring(0, 2)}:{SerialNumber.Substring(2, 2)}:{SerialNumber.Substring(4, 2)}:{SerialNumber.Substring(6, 2)}:{SerialNumber.Substring(8, 2)}:{SerialNumber.Substring(10, 2)}".ToUpperInvariant();
                            if (SerialNumber != "00:00:00:00:00:00")
                            {
                                return SerialNumber;
                            }
                        }
                    }

                    return null;
            }
            return null;
        }

        private string GetDeviceName(UInt16 VID, UInt16 PID)
        {
            switch (ControllerSubType)
            {
                case DS4SubType.None:
                    if (VID == VENDOR_SONY)
                    {
                        switch (PID)
                        {
                            case PRODUCT_SONY_DONGLE:
                                return "DUALSHOCK®4 USB Wireless Adaptor";
                            case PRODUCT_SONY_DONGLE_DFU:
                                return "DUALSHOCK®4 USB Wireless Adaptor in DFU mode!!!";
                        }
                        return $"Sony Device <{PID:X4}>";
                    }
                    return $"Unknown Device <{VID:X4},{PID:X4}>";
                case DS4SubType.SonyDS4V1:
                    return $"Sony DUALSHOCK®4 Controller V1";
                case DS4SubType.SonyDS4V2:
                    return $"Sony DUALSHOCK®4 Controller V2";
                case DS4SubType.UnknownDS4V1:
                    return $"Sony DUALSHOCK®4 Controller V1 (Possible Unoffical)";
                case DS4SubType.UnknownDS4V2:
                    return $"Sony DUALSHOCK®4 Controller V2 (Possible Unoffical)";
                case DS4SubType.BrookMars:
                    return "Brook Mars Wired Controller";
                case DS4SubType.No895X:
                    return "Model No. 895?";
                case DS4SubType.No8951:
                    return "Model No. 8951";
                case DS4SubType.No8952:
                    return "Model No. 8952";
                case DS4SubType.Unknown:
                default:
                    return $"Unknown Device <{VID:X4},{PID:X4}>";
            }
        }

        bool DisconnectedBit = false;
        private void OnReport(byte[] reportData)
        {
            if (PollingState == EPollingState.Inactive) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                StateMutationLock.EnterUpgradeableReadLock();
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
                        HasStateData = (reportData[1] & 0x80) == 0x80;
                    }

                    if (HasStateData)
                    {
                        (StateInFlight.Controls["stick_left"] as ControlStick).X = ControllerMathTools.QuickStickToFloat(reportData[1 + baseOffset + 0]);
                        (StateInFlight.Controls["stick_left"] as ControlStick).Y = ControllerMathTools.QuickStickToFloat(reportData[1 + baseOffset + 1]);

                        bool Finger1BrookMarsTest = (reportData[1 + baseOffset + 34] & 0x80) != 0x80;
                        if (_device.VendorId == VENDOR_BROOK && _device.ProductId == PRODUCT_BROOK_MARS && Finger1BrookMarsTest)
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

                        if (   (ControllerSubType == DS4SubType.No895X)
                            || (ControllerSubType == DS4SubType.No8952))
                        {
                            QuirkExtraButtonByte6Bit3RingBuffer = (byte)((QuirkExtraButtonByte6Bit3RingBuffer << 1) | ((reportData[1 + baseOffset + 6] & 0x04) == 0x04 ? 1 : 0));
                            bool SeeButton = (QuirkExtraButtonByte6Bit3RingBuffer & QUIRK_EXTRA_BUTTON_BYTE6_BIT3_BT_OBSCURE_RINGBUFFER_CHECK) == QUIRK_EXTRA_BUTTON_BYTE6_BIT3_BT_OBSCURE_RINGBUFFER_CHECK;
                            if (ControllerSubType == DS4SubType.No895X && SeeButton)
                            {
                                StateInFlight.Controls["clear"] = new ControlButton();
                                ChangeControllerSubType(DS4SubType.No8952);
                            }
                        }

                        (StateInFlight.Controls["home"] as ControlButton).Button0 = (reportData[1 + baseOffset + 6] & 0x1) == 0x1;

                        if (_device.VendorId == VENDOR_BROOK || _device.ProductId == PRODUCT_BROOK_MARS)
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
                        if (reportData[1 + baseOffset + 11] != 0)
                        {
                            // we see the temp is not 0, which means any suspect DS4s can now be converted to confirmed DS4s
                            if (ControllerSubType == DS4SubType.UnknownDS4V1) ChangeControllerSubType(DS4SubType.SonyDS4V1);
                            if (ControllerSubType == DS4SubType.UnknownDS4V2) ChangeControllerSubType(DS4SubType.SonyDS4V2);
                                
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
                            ResetControllerInfo();
                        }

                        // Brook Mars has emulated touch with stick, we don't care about that since we'd do it in software
                        if (_device.VendorId == VENDOR_BROOK && _device.ProductId == PRODUCT_BROOK_MARS)
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

                                // No8951 sends some invalid touch data if you tap the pad a lot
                                if (   (ControllerSubType == DS4SubType.No895X)
                                    || (ControllerSubType == DS4SubType.No8951))
                                {
                                    if (Finger1 && (F1X > TouchPadMaxX || F1Y > TouchPadMaxY))
                                        Finger1Valid = false;
                                    if (Finger2 && (F2X > TouchPadMaxX || F2Y > TouchPadMaxY))
                                        Finger2Valid = false;

                                    // we don't have to check if the finger is valid here since ANY value over the max of the 8951 proves this is an 8952, valid or invalid, since only the 8951 has this strange problem
                                    if (ControllerSubType == DS4SubType.No895X && (F1X > NO8952_PAD_MAX_X || F2X > NO8952_PAD_MAX_X))
                                    {
                                        ChangeControllerSubType(DS4SubType.No8951);
                                    }
                                }

                                if (Finger1Valid || Finger2Valid)
                                {
                                    byte TimeDelta = touch_last_frame ? ControllerMathTools.GetOverflowedDelta(last_touch_timestamp, touch_timestamp) : (byte)0;

                                    //Console.WriteLine($"{TimeDelta} {(tmp_now - tmp).Milliseconds}");
                                    (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(0, Finger1Valid && Finger1, (F1X / TouchPadMaxX) * 2f - 1f, (F1Y / TouchPadMaxY) * 2f - 1f, TimeDelta);
                                    (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(1, Finger2Valid && Finger2, (F2X / TouchPadMaxX) * 2f - 1f, (F2Y / TouchPadMaxY) * 2f - 1f, TimeDelta);

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
                    StateMutationLock.ExitUpgradeableReadLock();
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }


                // TODO: change how this works because we don't want to lock, we need to actually change the polling rate in the device
                if (ConnectionType == EConnectionType.Dongle && DisconnectedBit)
                    Thread.Sleep(_SLOW_POLL_MS); // if we're a dongle and we're not connected we might only be partially initalized, so slow roll our read
            }
        }

        object UpdateLocalDataLock = new object();
        Thread UpdateLocalDataThread = null;
        bool UpdateLocalDataPoison = false;

        private void ResetControllerInfo()
        {
            ControllerSubType = GetControllerInitialTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId);

            QuirkExtraButtonByte6Bit3RingBuffer = 0x00;
            SerialNumber = null;

            if (!ControlsCreated)
            {
                StateMutationLock.EnterWriteLock();
                try
                {
                    State.Controls["quad_left"] = new ControlDPad();
                    State.Controls["quad_left"] = new ControlDPad();
                    State.Controls["quad_right"] = new ControlButtonQuad();
                    State.Controls["bumpers"] = new ControlButtonPair();
                    State.Controls["bumpers2"] = new ControlButtonPair();
                    State.Controls["triggers"] = new ControlTriggerPair(HasStage2: false);
                    State.Controls["menu"] = new ControlButtonPair();
                    State.Controls["home"] = new ControlButton();
                    State.Controls["stick_left"] = new ControlStick(HasClick: true);
                    State.Controls["stick_right"] = new ControlStick(HasClick: true);
                    if (this.ControllerSubType == DS4SubType.BrookMars)
                    {
                        State.Controls["touch_center"] = new ControlButton();
                    }
                    else
                    {
                        State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
                    }
                    // According to this the normalized domain of the DS4 gyro is 1024 units per rad/s: https://gamedev.stackexchange.com/a/87178
                    State.Controls["motion"] = new ControlMotion();
                    ControlsCreated = true;
                }
                finally
                {
                    StateMutationLock.ExitWriteLock();
                }
            }
            ControllerMetadataUpdate?.Invoke();

            lock (UpdateLocalDataLock)
            {
                UpdateLocalDataPoison = true;
                while (UpdateLocalDataThread?.IsAlive ?? false)
                    Thread.Sleep(100); // we will actually lock up here while waiting for this thread to find a stop point, so this is far from the best way to handle this
                UpdateLocalDataPoison = false;

                UpdateLocalDataThread = new Thread(() =>
                {
                    if (UpdateLocalDataPoison)
                        return;
                    string SerialNumber_ = GetSerialNumber();
                    if (UpdateLocalDataPoison)
                        return;
                    SerialNumber = SerialNumber_;

                    if (!string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        string ControllerData = StoredDataHandler.GetMacData(SerialNumber);
                        DS4SubType ControllerSubTypeRead = DS4SubType.Unknown;
                        if (!string.IsNullOrWhiteSpace(ControllerData) && Enum.TryParse<DS4SubType>(ControllerData, out ControllerSubTypeRead))
                            ControllerSubType = ControllerSubTypeRead;

                        if ((ControllerSubType == DS4SubType.UnknownDS4V1)
                         || (ControllerSubType == DS4SubType.UnknownDS4V2))
                        {
                            string IdentityHash = GetControllerIdentityHash();
                            if (IdentityHash == IDENTITY_SHA256_895X)
                            {
                                ControllerSubType = DS4SubType.No895X;
                                StoredDataHandler.SetMacData(SerialNumber, ControllerSubType.ToString());
                            }
                        }

                        if (ControllerSubType == DS4SubType.No8951)
                            ChangeControllerSubType(ControllerSubType);
                        if (ControllerSubType == DS4SubType.No8952)
                            ChangeControllerSubType(ControllerSubType);
                    }

                    ControllerMetadataUpdate?.Invoke();
                });
                UpdateLocalDataThread.Start();
            }
        }

        private void ChangeControllerSubType(DS4SubType NewControllerSubType)
        {
            if (ConnectionType == EConnectionType.Dongle) // what are we doing here, get out, dongle assumed as true DS4s
                return;

            ControllerSubType = NewControllerSubType;
            if (!string.IsNullOrWhiteSpace(SerialNumber))
                StoredDataHandler.SetMacData(SerialNumber, ControllerSubType.ToString()); // TODO make this a thread event to prevent a hang?

            // note we entered an upgradable read lock if we called this from read locked code
            StateMutationLock.EnterWriteLock();
            try
            {
                if (ControllerSubType == DS4SubType.No8951)
                {
                    State.Controls["clear"] = new ControlButton();
                }
                else
                {
                    State.Controls["clear"] = null;
                }
                if (ControllerSubType == DS4SubType.BrookMars)
                {
                    State.Controls["touch_center"] = new ControlButton();
                }
                else
                {
                    State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
                }
            }
            finally
            {
                StateMutationLock.ExitWriteLock();
            }

            // Pad Size changes
            switch (ControllerSubType)
            {
                case DS4SubType.No8951:
                    TouchPadMaxX = NO8951_PAD_MAX_X;
                    TouchPadMaxY = NO8951_PAD_MAX_Y;
                    break;
                case DS4SubType.No8952:
                    TouchPadMaxX = NO8952_PAD_MAX_X;
                    TouchPadMaxY = NO8952_PAD_MAX_Y;
                    break;
                default:
                    TouchPadMaxX = DS4_PAD_MAX_X;
                    TouchPadMaxY = DS4_PAD_MAX_Y;
                    break;
            }

            ControllerMetadataUpdate?.Invoke();
        }
    }
}
