using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
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
        class ControllerSubTypeAttribute : Attribute
        {
            public string[] Tokens { get; private set; }
            public Int16 PadMaxX { get; private set; }
            public Int16 PadMaxY { get; private set; }
            public Int16 PhysicalWidth { get; private set; }
            public Int16 PhysicalHeight { get; private set; }
            public string Name { get; private set; }
            public string IdentitySha256 { get; private set; }
            public int USB_VID { get; private set; }
            public int USB_PID { get; private set; }
            public int BT_VID { get; private set; }
            public int BT_PID { get; private set; }
            public bool NoTemperture { get; private set; }
            public bool NoMac { get; private set; }
            public bool BT_UseLedBitForRumble { get; private set; }
            public bool BT_BlackLedIgnored { get; private set; }
            public bool USB_FlagsIgnored { get; private set; }
            public bool PadIsClickOnly { get; private set; }
            public bool ExtraButton { get; private set; }
            public bool AllowManualSelect { get; private set; }
            public bool AllowMacSave { get; private set; }

            public ControllerSubTypeAttribute(
                string[] Token,
                string Name,
                string IdentitySha256 = null,
                Int16 PadMaxX = -1,
                Int16 PadMaxY = -1,
                Int16 PhysicalWidth = -1,
                Int16 PhysicalHeight = -1,
                int USB_VID = -1,
                int USB_PID = -1,
                int BT_VID = -1,
                int BT_PID = -1,
                bool NoTemperture = false,
                bool NoMac = false,
                bool BT_UseLedBitForRumble = false,
                bool BT_BlackLedIgnored = false,
                bool USB_FlagsIgnored = false,
                bool PadIsClickOnly = false,
                bool ExtraButton = false,
                bool AllowMacSave = false,
                bool AllowManualSelect = false)
            {
                this.Tokens = Token;
                this.PadMaxX = PadMaxX >= 0 ? PadMaxX : PAD_MAX_X;
                this.PadMaxY = PadMaxY >= 0 ? PadMaxY : PAD_MAX_Y;
                this.PhysicalWidth = PhysicalWidth >= 0 ? PhysicalWidth : this.PadMaxX;
                this.PhysicalHeight = PhysicalHeight >= 0 ? PhysicalHeight : this.PadMaxY;
                this.Name = Name;
                this.IdentitySha256 = IdentitySha256;
                this.USB_VID = USB_VID;
                this.USB_PID = USB_PID;
                this.BT_VID = BT_VID;
                this.BT_PID = BT_PID;
                this.NoTemperture = NoTemperture;
                this.NoMac = NoMac;
                this.BT_UseLedBitForRumble = BT_UseLedBitForRumble;
                this.BT_BlackLedIgnored = BT_BlackLedIgnored;
                this.USB_FlagsIgnored = USB_FlagsIgnored;
                this.PadIsClickOnly = PadIsClickOnly;
                this.ExtraButton = ExtraButton;
                this.AllowManualSelect = AllowManualSelect;
                this.AllowMacSave = AllowMacSave;
            }
        }

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

        public const Int16 PAD_MAX_X = 1919;
        public const Int16 PAD_MAX_Y = 941;

        #region Identity Hashes
        private const string AUTH_IDENTITY_SHA256_2E2415CA = @"2e2415ca56598006b94f1274d15e64a1b99385c53e91102ae7708b8919fe124f";
        #endregion Identity Hashes

        #region String Definitions
        private const string ATOM_CONNECTION_WIRE = "CONNECTION_WIRE";
        private const string ATOM_CONNECTION_USB_WIRE = "CONNECTION_WIRE_USB";
        private const string ATOM_CONNECTION_BT = "CONNECTION_BT";
        private const string ATOM_CONNECTION_DONGLE = "CONNECTION_DONGLE";
        private const string ATOM_CONNECTION_DS4_DONGLE = "CONNECTION_DONGLE_DS4";
        private const string ATOM_CONNECTION_UNKKNOWN = "CONNECTION_UNKNOWN";

        private readonly string[] _CONNECTION_WIRE = new string[] { ATOM_CONNECTION_USB_WIRE, ATOM_CONNECTION_WIRE };
        private readonly string[] _CONNECTION_BT = new string[] { ATOM_CONNECTION_BT };
        private readonly string[] _CONNECTION_DONGLE = new string[] { ATOM_CONNECTION_DS4_DONGLE, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_UNKKNOWN = new string[] { ATOM_CONNECTION_UNKKNOWN };
        #endregion String Definitions

        enum DS4SubType
        {
            [ControllerSubType(
                Token: new string[] { "DEVICE_NONE" },
                Name: null,
                NoMac: true)]
            None = -1, // DS4 dongle with nothing connected

            [ControllerSubType(
                Token: new string[] { "DEVICE_UNKNOWN" },
                Name: null)]
            Unknown = 0,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4V1", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                Name: "Sony DUALSHOCK®4 Controller V1",
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V1)]
            SonyDS4V1 = 1,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4V2", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PhysicalWidth: 50,
                PhysicalHeight: 25,
                Name: "Sony DUALSHOCK®4 Controller V2",
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V2,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2)]
            SonyDS4V2,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4V1", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PhysicalWidth: 50,
                PhysicalHeight: 25,
                Name: "Sony DUALSHOCK®4 Controller V1 (Possible Unoffical)",
                NoTemperture: true,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V1,
                AllowManualSelect: true)]
            UnknownDS4V1,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4V2", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                Name: "Sony DUALSHOCK®4 Controller V2 (Possible Unoffical)",
                NoTemperture: true,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V2,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2,
                AllowManualSelect: true)]
            UnknownDS4V2,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_BROOKMARS", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                Name: "Brook Mars Wired Controller",
                //NoTemperture: true,
                USB_VID: VENDOR_BROOK, USB_PID: PRODUCT_BROOK_MARS,
                NoMac: true,
                PadIsClickOnly: true)]
            BrookMars = 100, // wired only controller, so it is detected immediately

            // These controllers have specific strange properties:
            // 1. These controllers all share the same Private/Pulic Key, which means Sony could blacklist them all from the PS4 if they wanted
            // 2. Their PID sometimes changes between USB and BT
            // 3. Their MAC is sometimes not available under any method under USB, but not all cases
            // 4. On USB, it is impossible to bit filter rumble and LED changes out, they always apply every write no matter what
            // 5. On BT, rumble writes ignore the rumble flag and instead apply if the LED flag is set. LED sets of 0x000000 are ignored to facilitate this
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_2E2415CA", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                Name: "Quirks Pad 2E2415CA",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                USB_FlagsIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            PartialDetection2E2415CA = 200,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_8951", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1918,
                PadMaxY: 940,
                Name: "Model No. PS4-8951",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2,
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                USB_FlagsIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            No8951,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_8952", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 940,
                PadMaxY: 940,
                PhysicalWidth: 23,
                PhysicalHeight: 40,
                Name: "Model No. PS4-8952",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2,
                ExtraButton: true,
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                USB_FlagsIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            No8952,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_SZ4002B", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1900,
                PadMaxY: 940,
                Name: "Senze SZ-4002B",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V1,
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                USB_FlagsIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            SZ4002B,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_SZ4003B", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1918,
                PadMaxY: 940,
                Name: "Senze SZ-4003B",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2,
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                USB_FlagsIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            SZ4003B,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_YIYANG498", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1918,
                PadMaxY: 940,
                Name: "Yiyang 498", // May actually be a Senze SZ-4006B?   Maybe Senze rebrands them
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1, //USB_REV: 0x0100
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V1, //BT_REV: 0x0000
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: false,
                AllowManualSelect: true,
                AllowMacSave: true)]
            Yiyang498,

            //   0,0 __________________________ 1918,0 ~ exact
            //      |                          |
            //      |                          |
            // 0,230|__________________________|1928,230 ~ aprox
            //       \                        /
            //        \                      /
            //         \                    /
            //  180,940 \__________________/ 1738,940 ~ aprox
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_SZ4011B", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1918,
                PadMaxY: 940,
                Name: "Senze SZ-4011B",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                //USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1, //USB_REV: 0x0100
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2, //BT_REV: 0x0000
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            SZ4011B,

            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_STK4003", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1918,
                PadMaxY: 940,
                Name: "Saitake STK-4003",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1, //USB_REV: 0x0100
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2, //BT_REV: 0x0000
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                USB_FlagsIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            STK4003,

            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_Gamory2075B", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1918,
                PadMaxY: 940,
                Name: "Gamory 2075B",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V2, //BT_REV: 0x0000
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: true,
                AllowManualSelect: true,
                AllowMacSave: true)]
            Gamory2075B,

            //  0,0 __________________________ 1918,0
            //      \                        /
            //       \                      /
            //        \                    /
            // 223,940 \__________________/ 1705,940
            [ControllerSubType(
                Token: new string[] { "DEVICE_DS4_P4GamepadQ300", "DEVICE_DS4", "DEVICE_GAMEPAD" },
                PadMaxX: 1918,
                PadMaxY: 940,
                Name: "P4 Gamepad Q300",
                NoTemperture: true,
                IdentitySha256: AUTH_IDENTITY_SHA256_2E2415CA,
                USB_VID: VENDOR_SONY, USB_PID: PRODUCT_SONY_DS4V1, //USB_REV: 0x0100
                BT_VID: VENDOR_SONY, BT_PID: PRODUCT_SONY_DS4V1, //BT_REV: 0x0000
                BT_UseLedBitForRumble: true,
                BT_BlackLedIgnored: false,
                AllowManualSelect: true,
                AllowMacSave: true)]
            P4GamepadQ300,
        }
        private DS4SubType ControllerSubType = DS4SubType.None;
        private ControllerSubTypeAttribute ControllerAttribute = null;
        private string IdentityHash = null;
        private bool HaveSeenNonZeroRawTemp = false;
        private List<DS4SubType> ManualSelectionList = new List<DS4SubType>();


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
        private Int16 TouchPadMaxX = 1919;

        /// <summary>
        /// Current max possible Y value for touch pad
        /// </summary>
        private Int16 TouchPadMaxY = 942;
        

        private bool ControlsCreated = false;

        private string SerialNumber = null;

        public EConnectionType ConnectionType { get; private set; }
        public EPollingState PollingState { get; private set; }

        //private ControllerSubTypeAttribute NO8952Attr = DS4SubType.No8952.GetAttribute<ControllerSubTypeAttribute>();

        public string UniqueID
        {
            get
            {
                //if(!string.IsNullOrWhiteSpace(SerialNumber))
                //    return SerialNumber;
                return _device.UniqueKey;
            }
        }

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
                return ControllerAttribute?.Tokens ?? new string[] { "DEVICE_UNKNOWN" };

            }
        }

        public bool HasSelectableAlternatives
        {
            get
            {
                lock (ManualSelectionList)
                {
                    return ManualSelectionList.Count > 0;
                }
            }
        }

        public Dictionary<string, string> Alternates
        {
            get
            {
                lock (ManualSelectionList)
                {
                    return ManualSelectionList.ToDictionary(dr => dr.ToString(), dx => GetNameForSubType(dx));
                }
            }
        }

        public string[] NameDetails
        {
            get
            {
                if (ControllerSubType == DS4SubType.None)
                {
                    UInt16 VID = (UInt16)_device.VendorId;
                    UInt16 PID = (UInt16)_device.ProductId;
                    if (VID == VENDOR_SONY && PID == PRODUCT_SONY_DONGLE)
                    {
                        return new string[] { $"No Controller" };
                    }
                }

                if (ControllerAttribute?.NoMac ?? false)
                    return null;

                return new string[] { $"[{SerialNumber ?? "No ID"}]" };
            }
        }

        public string Name
        {
            get
            {
                string retVal = GetNameForSubType(ControllerSubType);
                if (!string.IsNullOrWhiteSpace(retVal))
                    return retVal;
                UInt16 VID = (UInt16)_device.VendorId;
                UInt16 PID = (UInt16)_device.ProductId;
                if (ControllerSubType == DS4SubType.None)
                {
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
                }
                return $"Unknown Device <{VID:X4},{PID:X4}>";
            }
        }

        private string GetNameForSubType(DS4SubType ControllerSubType)
        {
            return ControllerSubType.GetAttribute<ControllerSubTypeAttribute>()?.Name;
        }



        public bool SensorsEnabled;
        private HidDevice _device;
        int reportUsageLock = 0;
        private byte last_touch_timestamp;
        private bool touch_last_frame;
        //private DateTime tmp = DateTime.Now;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

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
            else
            {
                PollingState = EPollingState.RunOnce;
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
            byte[] report;
            int offset = 0;
            if (ConnectionType == EConnectionType.Bluetooth)
            {
                if(ControllerAttribute.BT_UseLedBitForRumble && !ControllerAttribute.BT_BlackLedIgnored)
                    return; // these devices can't seperate LED color from rumble on BT, TODO once we have ReadOnly/SafeWrite/FullControl done, allow this on FullControl mode

                report = new byte[78]
                {
                    _REPORT_STATE_1, 0xC0, 0x20,
                    (byte)(ControllerAttribute.BT_UseLedBitForRumble ? 0x02 : 0x01), 0x00, 0x00,
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
                if (ControllerAttribute?.USB_FlagsIgnored ?? false)
                    return; // these devices can't seperate LED color from rumble on USB, TODO once we have ReadOnly/SafeWrite/FullControl done, allow this on FullControl mode

                report = new byte[32]
                {
                    0x05,
                    0x01, 0x00, 0x00,
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
                Thread.Sleep(500);
                report[1 + offset + 0] = (byte)((ConnectionType == EConnectionType.Bluetooth && ControllerAttribute.BT_UseLedBitForRumble) ? 0x02 : 0x01);
                report[1 + offset + 3] = 0x00;
                report[1 + offset + 4] = 0x00;
                _device.WriteReport(report);
            }
        }

        private string GetControllerAuthIdentityHash()
        {
            if (_device.VendorId == VENDOR_SONY && _device.ProductId == PRODUCT_SONY_DONGLE)
                return null;

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

        private DS4SubType GetControllerInitialTypeCode(UInt16 VID, UInt16 PID, bool HaveSeenNonZeroRawTemp, string IdentityHash)
        {
            const int ID_MATCH               = 0x100000;
            const int HAS_NO_ID              = 0x080000;
            const int BATTERY_STATUS         = 0x040000;
            const int HAS_MATCHING_AUTH_HASH = 0x020000;
            const int HAS_NO_AUTH_HASH       = 0x010000;
            const int TYPE_AUTH              = 0x00FFFF;

            bool FromDongle = false;
            if (_device != null && VID == VENDOR_SONY && PID == PRODUCT_SONY_DONGLE) // we are an offical dongle
            {
                byte[] FeatureBuffer;
                _device.ReadFeatureData(out FeatureBuffer, 0xE3);
                VID = BitConverter.ToUInt16(FeatureBuffer, 1);
                PID = BitConverter.ToUInt16(FeatureBuffer, 3);
                if (VID == 0x0000 && PID == 0x0000)
                    return DS4SubType.None;
                FromDongle = true;
            }

            List<Tuple<int, DS4SubType>> Candidates = new List<Tuple<int, DS4SubType>>();
            foreach (DS4SubType subType in Enum.GetValues(typeof(DS4SubType)))
            {
                int Rank = 0;//(int)subType;
                ControllerSubTypeAttribute attr = subType.GetAttribute<ControllerSubTypeAttribute>();
                // controller VID/PID is correct or target doesn't use one
                if ((ConnectionType == EConnectionType.USB       && attr.USB_VID == VID && attr.USB_PID == PID) // USB type matches
                 || (ConnectionType == EConnectionType.Bluetooth && attr.BT_VID  == VID && attr.BT_PID  == PID) // BT types matches
                 || (ConnectionType == EConnectionType.Dongle    && attr.BT_VID  == VID && attr.BT_PID  == PID)) // BT types matches (via dongle)
                {
                    Rank += ID_MATCH;
                }
                else if (attr.USB_VID == -1 && attr.USB_PID == -1 && attr.BT_VID == -1 && attr.BT_PID == -1)
                {
                    Rank += HAS_NO_ID;
                }
                // we have some sort of match from above
                if (Rank > 0)
                {
                    // temp matches expectation
                    if ((attr.NoTemperture && !HaveSeenNonZeroRawTemp) || (!attr.NoTemperture && HaveSeenNonZeroRawTemp))
                    {
                        Rank += BATTERY_STATUS;
                    }
                    if ((attr.NoTemperture && !HaveSeenNonZeroRawTemp) || !attr.NoTemperture)
                    {
                        // No target IdentityHash or we have a matching IdentityHash
                        if (IdentityHash == null)// && attr.IdentitySha256 == null)
                        {
                            Rank += HAS_NO_AUTH_HASH;
                            Rank += TYPE_AUTH - (int)subType;
                            Candidates.Add(new Tuple<int, DS4SubType>(Rank, subType));
                        }
                        else if (attr.IdentitySha256 == IdentityHash)
                        {
                            Rank += HAS_MATCHING_AUTH_HASH;
                            Rank += TYPE_AUTH - (int)subType;
                            Candidates.Add(new Tuple<int, DS4SubType>(Rank, subType));
                        }
                        else if (IdentityHash != null)
                        {
                            Rank += TYPE_AUTH - (int)subType;
                            Candidates.Add(new Tuple<int, DS4SubType>(Rank, subType));
                        }
                    }
                }
            }

            return Candidates.OrderByDescending(dr => dr.Item1).FirstOrDefault()?.Item2 ?? (FromDongle ? DS4SubType.None : DS4SubType.Unknown);

            /*switch (VID)
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
            return FromDongle ? DS4SubType.None : DS4SubType.Unknown;*/
        }


        private Regex MacAsSerialNumber = new Regex("^[0-9a-fA-F]{12}$");
        private string GetSerialNumber()
        {
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


        bool DisconnectedBit = false;
        private void OnReport(byte[] reportData)
        {
            if (PollingState == EPollingState.Inactive) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                bool DongleConnectionStatusChanged = false;
                bool ControllerTempDataAppeared = false;
                StateMutationLock.EnterReadLock();
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    //OldState = State; // shouldn't this be a clone?
                    //if (_attached == false) { return; }

                    int baseOffset = 0;
                    bool HasStateData = true;
                    if (ConnectionType == EConnectionType.Bluetooth && new byte[] {
                        _REPORT_STATE_1,
                        _REPORT_STATE_2,
                        _REPORT_STATE_3,
                        _REPORT_STATE_4,
                        _REPORT_STATE_5,
                        _REPORT_STATE_6,
                        _REPORT_STATE_7,
                        _REPORT_STATE_8,
                        _REPORT_STATE_9
                    }.Contains(reportData[0]))
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
                        //(StateInFlight.Controls["bumpers2"] as ControlButtonPair).Right.Button0 = (reportData[1 + baseOffset + 5] & 8) == 8;
                        //(StateInFlight.Controls["bumpers2"] as ControlButtonPair).Left.Button0 = (reportData[1 + baseOffset + 5] & 4) == 4;
                        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Button0 = (reportData[1 + baseOffset + 5] & 2) == 2;
                        (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Button0 = (reportData[1 + baseOffset + 5] & 1) == 1;

                        // counter
                        // bld.Append((reportData[1 + baseOffset + 6] & 0xfc).ToString().PadLeft(3, '0'));

                        /*if (ConnectionType == EConnectionType.Bluetooth)
                        {
                            if (_device.VendorId == VENDOR_SONY && _device.ProductId == PRODUCT_SONY_DS4V2)
                            {
                                if ((ControllerSubType == DS4SubType.PartialDetection2E2415CA)
                                 || (ControllerSubType == DS4SubType.No8952))
                                {
                                    QuirkExtraButtonByte6Bit3RingBuffer = (byte)((QuirkExtraButtonByte6Bit3RingBuffer << 1) | ((reportData[1 + baseOffset + 6] & 0x04) == 0x04 ? 1 : 0));
                                    bool SeeButton = (QuirkExtraButtonByte6Bit3RingBuffer & QUIRK_EXTRA_BUTTON_BYTE6_BIT3_BT_OBSCURE_RINGBUFFER_CHECK) == QUIRK_EXTRA_BUTTON_BYTE6_BIT3_BT_OBSCURE_RINGBUFFER_CHECK;
                                    if (ControllerSubType == DS4SubType.PartialDetection2E2415CA && SeeButton)
                                    {
                                        StateInFlight.Controls["clear"] = new ControlButton();
                                        ChangeControllerSubType(DS4SubType.No8952);
                                    }
                                }
                            }
                        }*/

                        if (ControllerAttribute.ExtraButton)
                        {
                            if ((StateInFlight.Controls["clear"] as ControlButton) != null)
                                switch (ConnectionType)
                                {
                                    case EConnectionType.Bluetooth:
                                    case EConnectionType.Dongle:
                                        QuirkExtraButtonByte6Bit3RingBuffer = (byte)((QuirkExtraButtonByte6Bit3RingBuffer << 1) | ((reportData[1 + baseOffset + 6] & 0x04) == 0x04 ? 1 : 0));
                                        (StateInFlight.Controls["clear"] as ControlButton).Button0 = (QuirkExtraButtonByte6Bit3RingBuffer & QUIRK_EXTRA_BUTTON_BYTE6_BIT3_BT_OBSCURE_RINGBUFFER_CHECK) == QUIRK_EXTRA_BUTTON_BYTE6_BIT3_BT_OBSCURE_RINGBUFFER_CHECK;
                                        break;
                                    case EConnectionType.USB:
                                        (StateInFlight.Controls["clear"] as ControlButton).Button0 = (reportData[1 + baseOffset + 6] & 0x04) == 0x04;
                                        break;
                                }
                        }

                        (StateInFlight.Controls["home"] as ControlButton).Button0 = (reportData[1 + baseOffset + 6] & 0x1) == 0x1;

                        if (ControllerAttribute.PadIsClickOnly)
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
                            HaveSeenNonZeroRawTemp = true;
                            // we see the temp is not 0, which means any suspect DS4s can now be converted to confirmed DS4s
                            ControllerTempDataAppeared = true;    
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
                            DongleConnectionStatusChanged = true;
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

                                // Some third party controllers give bad data.
                                // It appears to be caused by the data coming in too fast and getting summed togeather.
                                {
                                    if (Finger1 && (F1X > TouchPadMaxX || F1Y > TouchPadMaxY))
                                        Finger1Valid = false;
                                    if (Finger2 && (F2X > TouchPadMaxX || F2Y > TouchPadMaxY))
                                        Finger2Valid = false;

                                    // we don't have to check if the finger is valid here since ANY value over the max of the 8951 proves this is an 8952, valid or invalid, since only the 8951 has this strange problem
                                    /*if (ControllerSubType == DS4SubType.PartialDetection2E2415CA && (F1X > NO8952Attr.PadMaxX || F2X > NO8952Attr.PadMaxY))
                                    {
                                        if (ConnectionType == EConnectionType.Bluetooth)
                                        {
                                            if (_device.VendorId == VENDOR_SONY && _device.ProductId == PRODUCT_SONY_DS4V2)
                                            {
                                                // TODO: see if we can figure out how to tell these apart, though manual override should be possible
                                                ChangeControllerSubType(DS4SubType.No8951);
                                                //ChangeControllerSubType(DS4SubType.SZ4003B);
                                            }
                                            else if (_device.VendorId == VENDOR_SONY && _device.ProductId == PRODUCT_SONY_DS4V1)
                                            {
                                                // TODO: see if we can figure out how to tell these apart, though manual override should be possible
                                                ChangeControllerSubType(DS4SubType.SZ4002B);
                                                //ChangeControllerSubType(DS4SubType.Yiyang498);
                                            }
                                        }
                                        else if (ConnectionType == EConnectionType.USB && !string.IsNullOrWhiteSpace(SerialNumber))
                                        {
                                            if (_device.VendorId == VENDOR_SONY && _device.ProductId == PRODUCT_SONY_DS4V1)
                                            {
                                                ChangeControllerSubType(DS4SubType.SZ4003B);
                                            }
                                        }
                                    }*/
                                }

                                if (Finger1Valid || Finger2Valid)
                                {
                                    if (Finger1 && F1X > TouchPadMaxX) F1X = TouchPadMaxX;
                                    if (Finger1 && F1Y > TouchPadMaxY) F1Y = TouchPadMaxY;
                                    if (Finger2 && F2X > TouchPadMaxX) F2X = TouchPadMaxX;
                                    if (Finger2 && F2Y > TouchPadMaxY) F2Y = TouchPadMaxY;

                                    byte TimeDelta = touch_last_frame ? ControllerMathTools.GetOverflowedDelta(last_touch_timestamp, touch_timestamp) : (byte)0;

                                    //Console.WriteLine($"{TimeDelta} {(tmp_now - tmp).Milliseconds}");
                                    (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(0, Finger1Valid && Finger1, (1.0f * F1X / TouchPadMaxX) * 2f - 1f, (1.0f * F1Y / TouchPadMaxY) * 2f - 1f, TimeDelta);
                                    (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(1, Finger2Valid && Finger2, (1.0f * F2X / TouchPadMaxX) * 2f - 1f, (1.0f * F2Y / TouchPadMaxY) * 2f - 1f, TimeDelta);

                                    last_touch_timestamp = touch_timestamp;
                                }
                                //tmp = tmp_now;
                            }

                            touch_last_frame = TouchDataCount > 0;
                        }

                        // bring OldState in line with new State
                        OldState = State;
                        State = StateInFlight;

                        ControllerStateUpdate?.Invoke(this, State);

                        if (PollingState == EPollingState.RunOnce)
                        {
                            _device.StopReading();
                            PollingState = EPollingState.Inactive;
                            _device.CloseDevice();
                        }
                    }
                }
                finally
                {
                    StateMutationLock.ExitReadLock();

                    if (ControllerTempDataAppeared)
                    {
                        if (ControllerSubType == DS4SubType.UnknownDS4V1) ChangeControllerSubType(DS4SubType.SonyDS4V1);
                        if (ControllerSubType == DS4SubType.UnknownDS4V2) ChangeControllerSubType(DS4SubType.SonyDS4V2);
                    }

                    if (DongleConnectionStatusChanged)
                        ResetControllerInfo();

                    Interlocked.Exchange(ref reportUsageLock, 0);
                }

                // TODO: change how this works because we don't want to lock, we need to actually change the polling rate in the device
                if (ConnectionType == EConnectionType.Dongle && DisconnectedBit)
                    Thread.Sleep(_SLOW_POLL_MS); // if we're a dongle and we're not connected we might only be partially initalized, so slow roll our read
            }
        }

        object UpdateLocalDataLock = new object();
        bool UpdateLocalDataPoison = false;

        private void ResetControllerInfo()
        {
            HaveSeenNonZeroRawTemp = false;
            IdentityHash = null;
            SerialNumber = null;
            QuirkExtraButtonByte6Bit3RingBuffer = 0x00;

            ControllerSubType = GetControllerInitialTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId, HaveSeenNonZeroRawTemp, IdentityHash);
            ControllerAttribute = ControllerSubType.GetAttribute<ControllerSubTypeAttribute>();

            if (!ControlsCreated)
            {
                StateMutationLock.EnterWriteLock();
                try
                {
                    State.Controls["quad_left"] = new ControlDPad();
                    State.Controls["quad_left"] = new ControlDPad();
                    State.Controls["quad_right"] = new ControlButtonQuad();
                    State.Controls["bumpers"] = new ControlButtonPair();
                    //State.Controls["bumpers2"] = new ControlButtonPair();
                    State.Controls["triggers"] = new ControlTriggerPair(HasStage2: false);
                    State.Controls["menu"] = new ControlButtonPair();
                    State.Controls["home"] = new ControlButton();
                    State.Controls["stick_left"] = new ControlStick(HasClick: true);
                    State.Controls["stick_right"] = new ControlStick(HasClick: true);
                    if (ControllerAttribute?.PadIsClickOnly ?? false)
                    {
                        State.Controls["touch_center"] = new ControlButton();
                    }
                    else
                    {
                        State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
                        (State.Controls["touch_center"] as ControlTouch).PhysicalWidth = ControllerAttribute.PhysicalWidth;
                        (State.Controls["touch_center"] as ControlTouch).PhysicalHeight = ControllerAttribute.PhysicalHeight;
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
            ControllerMetadataUpdate?.Invoke(this);

            //lock (UpdateLocalDataLock)
            {
                //UpdateLocalDataPoison = true;
                //while (UpdateLocalDataThread?.IsAlive ?? false)
                //    Thread.Sleep(100); // we will actually lock up here while waiting for this thread to find a stop point, so this is far from the best way to handle this
                //UpdateLocalDataPoison = false;

                //if(UpdateLocalDataThread == null)
                {
                    //UpdateLocalDataThread = new Thread(() =>
                    //{
                        lock (UpdateLocalDataLock)
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
                            }
                            if (   (ControllerSubType == DS4SubType.UnknownDS4V1)
                                || (ControllerSubType == DS4SubType.UnknownDS4V2))
                            {
                                IdentityHash = GetControllerAuthIdentityHash();
                                //if (IdentityHash == AUTH_IDENTITY_SHA256_2E2415CA)
                                //{
                                //    ControllerSubType = DS4SubType.PartialDetection2E2415CA;
                                //    StoredDataHandler.SetMacData(SerialNumber, ControllerSubType.ToString());
                                //}
                            
                                //ControllerSubType = GetControllerInitialTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId, HaveSeenNonZeroRawTemp, IdentityHash);
                                //ControllerAttribute = ControllerSubType.GetAttribute<ControllerSubTypeAttribute>();
                                //if (ControllerAttribute.AllowMacSave)
                                //    StoredDataHandler.SetMacData(SerialNumber, ControllerSubType.ToString());
                            }
                            ControllerSubType = GetControllerInitialTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId, HaveSeenNonZeroRawTemp, IdentityHash);
                            ChangeControllerSubType(ControllerSubType);

                            ControllerMetadataUpdate?.Invoke(this);
                        }
                    //});
                    //UpdateLocalDataThread.Name = "DualShock4Controller:UpdateLocalDataThread";
                }
                //UpdateLocalDataThread.Start();
            }
        }

        public void SetActiveAlternateController(string ControllerID)
        {
            ChangeControllerSubType((DS4SubType)Enum.Parse(typeof(DS4SubType), ControllerID));
        }

        private void UpdateAlternateSubTypes()
        {
            lock (ManualSelectionList)
            {
                ManualSelectionList.Clear();
                if (ControllerAttribute?.AllowManualSelect ?? false)
                {
                    foreach (DS4SubType subType in Enum.GetValues(typeof(DS4SubType)))
                    {
                        ControllerSubTypeAttribute attr = subType.GetAttribute<ControllerSubTypeAttribute>();
                        if (attr.AllowManualSelect)
                        {
                            // controller VID/PID is correct or target doesn't use one
                            if ((ConnectionType == EConnectionType.USB && attr.USB_VID == _device.VendorId && attr.USB_PID == _device.ProductId) // USB type matches
                             || (ConnectionType == EConnectionType.Bluetooth && attr.BT_VID == _device.VendorId && attr.BT_PID == _device.ProductId) // BT types matches
                             || (ConnectionType == EConnectionType.Dongle && attr.BT_VID == _device.VendorId && attr.BT_PID == _device.ProductId) // BT types matches (via dongle)
                             || (attr.USB_VID == -1 && attr.USB_PID == -1 && attr.BT_VID == -1 && attr.BT_PID == -1)) // subtype can't possibly match by ID
                            {
                                // temp matches expectation
                                if ((attr.NoTemperture && !HaveSeenNonZeroRawTemp) || !attr.NoTemperture)
                                {
                                    // No target IdentityHash or we have a matching IdentityHash
                                    if (IdentityHash == null || attr.IdentitySha256 == IdentityHash)
                                    {
                                        ManualSelectionList.Add(subType);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ChangeControllerSubType(DS4SubType NewControllerSubType)
        {
            ControllerAttribute = null;

            //if (ConnectionType == EConnectionType.Dongle) // what are we doing here, get out, dongle assumed as true DS4s
            //    return;

            ControllerSubType = NewControllerSubType;
            ControllerAttribute = ControllerSubType.GetAttribute<ControllerSubTypeAttribute>();
            if (!string.IsNullOrWhiteSpace(SerialNumber) && ControllerAttribute.AllowMacSave)
                StoredDataHandler.SetMacData(SerialNumber, ControllerSubType.ToString()); // TODO make this a thread event to prevent a hang?

            StateMutationLock.EnterWriteLock();
            try
            {
                if (ControllerAttribute.ExtraButton)
                {
                    State.Controls["clear"] = new ControlButton();
                }
                else
                {
                    State.Controls["clear"] = null;
                }
                if (ControllerAttribute.PadIsClickOnly)
                {
                    State.Controls["touch_center"] = new ControlButton();
                }
                else
                {
                    State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
                    (State.Controls["touch_center"] as ControlTouch).PhysicalWidth = ControllerAttribute.PhysicalWidth;
                    (State.Controls["touch_center"] as ControlTouch).PhysicalHeight = ControllerAttribute.PhysicalHeight;
                }
            }
            finally
            {
                StateMutationLock.ExitWriteLock();
            }

            TouchPadMaxX = ControllerAttribute.PadMaxX;
            TouchPadMaxY = ControllerAttribute.PadMaxY;

            UpdateAlternateSubTypes();

            ControllerMetadataUpdate?.Invoke(this);
        }
    }
}
