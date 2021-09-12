using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ExtendInput.Controller
{
    class ControllerSubTypeAttribute : Attribute
    {
        public string[] Tokens { get; private set; }
        public string Name { get; private set; }
        public byte? DeviceIdFromFeature { get; private set; }
        public byte? ReportFESubType { get; private set; } // 2nd data byte
        public byte? FixedValueFromByte28 { get; private set; } // 27th data byte
        public byte? VersionFromByte30 { get; private set; } // 29th data byte
        public byte? FixedValueFromByte31 { get; private set; } // 30th data byte
        public int[] ExpectedDongle { get; private set; }
        public ControllerSubTypeAttribute(
            string[] Token,
            string Name,
            int DeviceIdFromFeature = -1,
            int FixedValueFromByte28 = -1,
            int VersionFromByte30 = -1,
            int FixedValueFromByte31 = -1,
            int ReportFESubType = -1,
            int[] ExpectedDongle = null)
        {
            this.Tokens = Token;
            this.Name = Name;
            this.DeviceIdFromFeature = DeviceIdFromFeature > -1 ? (byte?)DeviceIdFromFeature : null;
            this.FixedValueFromByte28 = FixedValueFromByte28 > -1 ? (byte?)FixedValueFromByte28 : null;
            this.VersionFromByte30 = VersionFromByte30 > -1 ? (byte?)VersionFromByte30 : null;
            this.FixedValueFromByte31 = FixedValueFromByte31 > -1 ? (byte?)FixedValueFromByte31 : null;
            this.ReportFESubType = ReportFESubType > -1 ? (byte?)ReportFESubType : null;
            this.ExpectedDongle = ExpectedDongle;
        }
    }

    public class FlydigiController : IController
    {
        public const int VENDOR_FLYDIGI = 0x04B4;
        public const int PRODUCT_FLYDIGI_DONGLE_1 = 0x2410;
        public const int PRODUCT_FLYDIGI_DONGLE_2 = 0x2411;
        public const int PRODUCT_FLYDIGI_DONGLE_3 = 0x2411;
        public const int PRODUCT_FLYDIGI_USB = 0x2411;
        public const int REVISION_FLYDIGI_DONGLE_1 = 0x0303;
        public const int REVISION_FLYDIGI_DONGLE_2 = 0x0303;
        public const int REVISION_FLYDIGI_DONGLE_3 = 0x0401;
        public const int REVISION_FLYDIGI_USB = 0x0303;

        #region String Definitions
        private const string ATOM_CONNECTION_WIRE = "CONNECTION_WIRE";
        private const string ATOM_CONNECTION_USB_WIRE = "CONNECTION_WIRE_USB";
        private const string ATOM_CONNECTION_BT = "CONNECTION_BT";
        private const string ATOM_CONNECTION_DONGLE = "CONNECTION_DONGLE";
        private const string ATOM_CONNECTION_DONGLE_1 = "CONNECTION_DONGLE_1";
        private const string ATOM_CONNECTION_DONGLE_2 = "CONNECTION_DONGLE_2";
        private const string ATOM_CONNECTION_DONGLE_3 = "CONNECTION_DONGLE_3";
        private const string ATOM_CONNECTION_UNKKNOWN = "CONNECTION_UNKNOWN";

        private readonly string[] _CONNECTION_WIRE = new string[] { ATOM_CONNECTION_USB_WIRE, ATOM_CONNECTION_WIRE };
        private readonly string[] _CONNECTION_BT = new string[] { ATOM_CONNECTION_BT };
        private readonly string[] _CONNECTION_DONGLE = new string[] { ATOM_CONNECTION_DONGLE_1, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_UNKKNOWN = new string[] { ATOM_CONNECTION_UNKKNOWN };
        #endregion String Definitions
        
        enum FlyDigiSubType
        {
            [ControllerSubType(
                Token: new string[] { "DEVICE_NONE" },
                Name: null)]
            None = -1, // Dongle with nothing connected

            [ControllerSubType(
                Token: new string[] { "DEVICE_UNKNOWN" },
                Name: null)]
            Unknown = 0,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_X9" },
                Name: "Flydigi X9",
                DeviceIdFromFeature: 0x10,
                ReportFESubType: 0x55)]
            X9,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_X8" },
                Name: "Flydigi X8",
                DeviceIdFromFeature: 0x11,
                FixedValueFromByte28: 0x00,
                VersionFromByte30: 0x32,
                FixedValueFromByte31: 0x1B,
                ReportFESubType: 0x66)]
            X8,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_APEX" },
                Name: "Flydigi APEX",
                DeviceIdFromFeature: 0x12,
                FixedValueFromByte28: 0x01,
                VersionFromByte30: 0x32,
                ExpectedDongle: new int[] { (PRODUCT_FLYDIGI_DONGLE_1 << 8) | REVISION_FLYDIGI_DONGLE_1 })]
            APEX,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_APEX_2" },
                Name: "Flydigi APEX 2",
                DeviceIdFromFeature: 0x13,
                FixedValueFromByte28: 0x01,
                VersionFromByte30: 0x36,
                ExpectedDongle: new int[] { (PRODUCT_FLYDIGI_DONGLE_2 << 8) | REVISION_FLYDIGI_DONGLE_2 })]
            APEX_2,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_F1" },
                Name: "Flydigi F1",
                DeviceIdFromFeature: 0x14,
                ReportFESubType: 0x66)]
            F1,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_F1" },
                Name: "Flydigi F1 WIRED",
                DeviceIdFromFeature: 0x15)]
            F1_WIRED,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WEE1" },
                Name: "Flydigi WEE1",
                DeviceIdFromFeature: 0x20)]
            WEE1,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WEE2" },
                Name: "Flydigi WEE2",
                DeviceIdFromFeature: 0x21)]
            WEE2,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_Q1" },
                Name: "Flydigi Q1",
                DeviceIdFromFeature: 0x30)]
            Q1,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_D1" },
                Name: "Flydigi D1",
                DeviceIdFromFeature: 0x31)]
            D1,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_BT" },
                Name: "Flydigi WASP BT",
                DeviceIdFromFeature: 0x40)]
            WASP_BT,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_N" },
                Name: "Flydigi WASP N",
                DeviceIdFromFeature: 0x41)]
            WASP_N,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_X" },
                Name: "Flydigi WASP X",
                DeviceIdFromFeature: 0x42)]
            WASP_X,
            
            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_2" },
                Name: "Flydigi WASP 2",
                DeviceIdFromFeature: 0x43)]
            WASP_2,
        }
        private FlyDigiSubType ControllerSubType = FlyDigiSubType.None;
        private ControllerSubTypeAttribute ControllerAttribute = null;
        private byte? DetectedDeviceId = null;
        private bool ControlsCreated = false;

        private HidDevice _device;
        int reportUsageLock = 0;

        private const int _SLOW_POLL_MS = 1000;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        // The controller state can drastically change, where it picks up or loses items based on passive detection of quirky controllers, this makes it safe
        private ReaderWriterLockSlim StateMutationLock = new ReaderWriterLockSlim();

        public bool HasMotion => true;

        public bool IsReady => true;
        public bool IsPresent => true;
        public bool IsVirtual => false;

        public ControllerState GetState()
        {
            return State;
        }

        public EConnectionType ConnectionType { get; private set; }
        public EPollingState PollingState { get; private set; }

        public string ConnectionUniqueID
        {
            get
            {
                //if (!string.IsNullOrWhiteSpace(SerialNumber))
                //    return SerialNumber;
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

        public string[] ConnectionTypeCode
        {
            get
            {
                switch (ConnectionType)
                {
                    case EConnectionType.USB: return _CONNECTION_WIRE;
                    case EConnectionType.Bluetooth: return _CONNECTION_BT;
                    case EConnectionType.Dongle: return _CONNECTION_DONGLE;
                    default: return _CONNECTION_UNKKNOWN;
                }
            }
        }
        public string[] ControllerTypeCode
        {
            get
            {
                return new string[] { "DEVICE_FLYDIGI" };

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
                if (ControllerSubType == FlyDigiSubType.None)
                {
                    if (VID == VENDOR_FLYDIGI)
                    {
                        //switch (PID)
                        //{
                        //    case PRODUCT_SONY_DONGLE:
                        //        return "DUALSHOCK®4 USB Wireless Adaptor";
                        //    case PRODUCT_SONY_DONGLE_DFU:
                        //        return "DUALSHOCK®4 USB Wireless Adaptor in DFU mode!!!";
                        //}
                        return $"Flydigi Device <{PID:X4}>";
                    }
                    return $"Unknown Device <{VID:X4},{PID:X4}>";
                }
                if (VID == VENDOR_FLYDIGI)
                    return $"Flydigi Device <{PID:X4}>";
                return $"Unknown Device <{VID:X4},{PID:X4}>";
            }
        }

        private string GetNameForSubType(FlyDigiSubType ControllerSubType)
        {
            return ControllerSubType.GetAttribute<ControllerSubTypeAttribute>()?.Name;
        }


        public string[] NameDetails
        {
            get
            {
                return null;
            }
        }
        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;

        public IDevice DeviceHackRef => _device;

        ControllerState State = new ControllerState();
        ControllerState OldState = null;


        public FlydigiController(HidDevice device, EConnectionType ConnectionType = EConnectionType.Unknown)
        {
            this.ConnectionType = ConnectionType;

            _device = device;

            PollingState = EPollingState.Inactive;

            _device.DeviceReport += OnReport;

            ResetControllerInfo();

            // Dongle capable of controller type detection
            if (_device.VendorId == VENDOR_FLYDIGI)
            {
                if ((_device.ProductId == PRODUCT_FLYDIGI_DONGLE_1 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_1) // 1 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_2 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_2) // 2 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3)) // 3 dot dongle
                {
                    this.ConnectionType = EConnectionType.Dongle;
                }
                else if ((_device.ProductId == PRODUCT_FLYDIGI_USB && _device.RevisionNumber == REVISION_FLYDIGI_USB))
                {
                    this.ConnectionType = EConnectionType.USB;
                }

                if ((_device.ProductId == PRODUCT_FLYDIGI_DONGLE_2 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_2) // 2 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3) // 3 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_USB && _device.RevisionNumber == REVISION_FLYDIGI_USB)) // controller that supports USB
                {
                    this.PollingState = EPollingState.RunUntilReady; // we need to read until we get device info
                    getDeviceInfoInAndroid();
                }
                else
                {
                    this.PollingState = EPollingState.RunOnce; // we are either a controller or a dumb 
                }
            }
            _device.StartReading();
        }
        public void Dispose()
        {
        }

        public void Initalize()
        {
            if (PollingState == EPollingState.Active) return;

            PollingState = EPollingState.Active;
            _device.StartReading();
        }

        public void DeInitalize()
        {
            if (PollingState == EPollingState.Inactive) return;
            if (PollingState == EPollingState.SlowPoll) return;
            //if (PollingState == EPollingState.RunOnce) return;
            if (PollingState == EPollingState.RunUntilReady) return;

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
        }

        private void OnReport(IReport rawReportData)
        {
            if (PollingState == EPollingState.Inactive) return;
            //if (!(reportData is HidReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.HID) return;
            HidReport reportData = (HidReport)rawReportData;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    if (reportData.ReportId == 0x04)
                    {
                        switch (reportData.ReportBytes[0])
                        {
                            case 0xFE:
                                {
                                    StateMutationLock.EnterReadLock();
                                    try
                                    {
                                        // Clone the current state before altering it since the OldState is likely a shared reference
                                        ControllerState StateInFlight = (ControllerState)State.Clone();

                                        byte ControllerID = reportData.ReportBytes[27];

                                        bool buttonC = (reportData.ReportBytes[6] & 0x01) == 0x01;
                                        bool buttonZ = (reportData.ReportBytes[6] & 0x02) == 0x02;
                                        bool buttonM1 = (reportData.ReportBytes[6] & 0x04) == 0x04;
                                        bool buttonM2 = (reportData.ReportBytes[6] & 0x08) == 0x08;
                                        bool buttonM3 = (reportData.ReportBytes[6] & 0x10) == 0x10;
                                        bool buttonM4 = (reportData.ReportBytes[6] & 0x20) == 0x20;
                                        bool buttonM5 = (reportData.ReportBytes[6] & 0x40) == 0x40;
                                        bool buttonM6 = (reportData.ReportBytes[6] & 0x80) == 0x80;
                                        bool buttonPair = (reportData.ReportBytes[7] & 0x01) == 0x01;
                                        bool buttonHome = (reportData.ReportBytes[7] & 0x08) == 0x08;
                                        bool buttonBack = (reportData.ReportBytes[7] & 0x10) == 0x10;
                                        ////////////////////////////////////////////////////
                                        bool buttonUp = (reportData.ReportBytes[8] & 0x01) == 0x01;
                                        bool buttonRight = (reportData.ReportBytes[8] & 0x02) == 0x02;
                                        bool buttonDown = (reportData.ReportBytes[8] & 0x04) == 0x04;
                                        bool buttonLeft = (reportData.ReportBytes[8] & 0x08) == 0x08;
                                        ////////////////////////////////////////////////////
                                        bool buttonA = (reportData.ReportBytes[8] & 0x10) == 0x10;
                                        bool buttonB = (reportData.ReportBytes[8] & 0x20) == 0x20;
                                        bool buttonSelect = (reportData.ReportBytes[8] & 0x40) == 0x40;
                                        bool buttonX = (reportData.ReportBytes[8] & 0x80) == 0x80;
                                        bool buttonY = (reportData.ReportBytes[9] & 0x01) == 0x01;
                                        bool buttonStart = (reportData.ReportBytes[9] & 0x02) == 0x02;
                                        bool buttonL1 = (reportData.ReportBytes[9] & 0x04) == 0x04;
                                        bool buttonR1 = (reportData.ReportBytes[9] & 0x08) == 0x08;
                                        bool buttonL2 = (reportData.ReportBytes[9] & 0x10) == 0x10;
                                        bool buttonR2 = (reportData.ReportBytes[9] & 0x20) == 0x20;
                                        bool buttonL3 = (reportData.ReportBytes[9] & 0x40) == 0x40;
                                        bool buttonR3 = (reportData.ReportBytes[9] & 0x80) == 0x80;
                                        ////////////////////////////////////////////////////
                                        byte LStickX = reportData.ReportBytes[16];
                                        byte LStickY = reportData.ReportBytes[18];
                                        byte RStickX = reportData.ReportBytes[20];
                                        byte RStickY = reportData.ReportBytes[21];
                                        byte SStickX = reportData.ReportBytes[22];
                                        byte SStickY = reportData.ReportBytes[23];
                                        ////////////////////////////////////////////////////
                                        byte WheelX = reportData.ReportBytes[16]; // or trigger
                                        byte WheelY = reportData.ReportBytes[18]; // or trigger
                                        byte StickRX = reportData.ReportBytes[20];
                                        byte StickRY = reportData.ReportBytes[21];

                                        /*if ((ControllerID == 0x01 && _device.Attributes.ProductId == ProductId_X8_Apex1) || (settings.NoApex2 ?? false)) // is APEX1 reportID and is APEX1 VID
                                        {
                                            ds4Controller.SetSliderValue(DualShock4Slider.LeftTrigger, report.Data[22]);
                                            ds4Controller.SetSliderValue(DualShock4Slider.RightTrigger, report.Data[23]);
                                        }
                                        else
                                        {
                                            ds4Controller.SetSliderValue(DualShock4Slider.LeftTrigger, (byte)(buttonL2 ? 0xff : 0x00));
                                            ds4Controller.SetSliderValue(DualShock4Slider.RightTrigger, (byte)(buttonR2 ? 0xff : 0x00));
                                        }*/

                                        int padH = 0;
                                        int padV = 0;
                                        if (buttonUp) padV++;
                                        if (buttonDown) padV--;
                                        if (buttonRight) padH++;
                                        if (buttonLeft) padH--;
                                        /*
                                        if (padH > 0)
                                            if (padV > 0)
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.Northeast);
                                            else if (padV < 0)
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.Southeast);
                                            else
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.East);
                                        else if (padH < 0)
                                            if (padV > 0)
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.Northwest);
                                            else if (padV < 0)
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.Southwest);
                                            else
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.West);
                                        else
                                            if (padV > 0)
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.North);
                                            else if (padV < 0)
                                            //ds4Controller.SetDPadDirection(DualShock4DPadDirection.South);
                                            else
                                                //ds4Controller.SetDPadDirection(DualShock4DPadDirection.None);
                                        */

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
                                    finally
                                    {
                                        StateMutationLock.ExitReadLock();

                                        //if (ControllerTempDataAppeared)
                                        //{
                                        //    if (ControllerSubType == DS4SubType.UnknownDS4V1) ChangeControllerSubType(DS4SubType.SonyDS4V1);
                                        //    if (ControllerSubType == DS4SubType.UnknownDS4V2) ChangeControllerSubType(DS4SubType.SonyDS4V2);
                                        //}
                                        //
                                        //if (DongleConnectionStatusChanged)
                                        //    ResetControllerInfo();

                                        Interlocked.Exchange(ref reportUsageLock, 0);
                                        //Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                                        
                                        Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                                    }
                                }
                                break;
                            case 0xFF:
                                {
                                    if (reportData.ReportBytes[1] == 0xF0)
                                    {
                                        if ((reportData.ReportBytes[14] & 0xFF) == 0xEC)
                                        {
                                            int FirmwareRevision = reportData.ReportBytes[8] & 0xF;
                                            int FirmwareBuild = reportData.ReportBytes[8] >> 4;
                                            int FirmwareMinor = reportData.ReportBytes[9] & 0xF;
                                            int FirmwareMajor = reportData.ReportBytes[9] >> 4;
                                            int BatteryReading = reportData.ReportBytes[10];
                                            int BatteryRangeMin = 0x62;
                                            int BatteryRangeMax = 0x72;
                                            if (BatteryReading < BatteryRangeMin)
                                            {
                                                BatteryReading = BatteryRangeMin;
                                            }
                                            else if (BatteryReading > BatteryRangeMax)
                                            {
                                                BatteryReading = BatteryRangeMax;
                                            }
                                            // reportData.ReportBytes[10] of 0 means unknown
                                            int batteryPercent = (int)((float)(BatteryReading - BatteryRangeMin) / (float)(BatteryRangeMax - BatteryRangeMin) * 100f);
                                            Console.WriteLine("Controller firmware version: V" + FirmwareMajor + "." + FirmwareMinor + "." + FirmwareBuild + "." + FirmwareRevision);
                                            Console.WriteLine("Controller Power: " + batteryPercent);

                                            DetectedDeviceId = reportData.ReportBytes[2];

                                            if (PollingState == EPollingState.RunUntilReady)
                                                PollingState = EPollingState.SlowPoll;
                                            Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                                    }
                                }
                                break;
                            default:
                                Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                                break;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }


                // TODO: change how this works because we don't want to lock, we need to actually change the polling rate in the device
                if (PollingState == EPollingState.SlowPoll)
                    Thread.Sleep(_SLOW_POLL_MS); // if we're a dongle and we're not connected we might only be partially initalized, so slow roll our read
            }
                        Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
        }

        private void getDeviceInfoInAndroid()
        {
            byte[] array = new byte[14];
            array[0] = 5;
            array[1] = 0xEC;
            _device.WriteReport(array);
        }

        object UpdateLocalDataLock = new object();
        bool UpdateLocalDataPoison = false;

        private void ResetControllerInfo()
        {
            ControllerSubType = GetControllerInitialTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId, (UInt16)_device.RevisionNumber);
            ControllerAttribute = ControllerSubType.GetAttribute<ControllerSubTypeAttribute>();

            if (!ControlsCreated)
            {
                StateMutationLock.EnterWriteLock();
                try
                {
                    // universal fixed controls that all Flydigi controllers have, these won't change
                    State.Controls["quad_left"] = new ControlDPad();
                    State.Controls["quad_right"] = new ControlButtonQuad();
                    State.Controls["bumpers"] = new ControlButtonPair();
                    //State.Controls["triggers"] = new ControlTriggerPair(HasStage2: false);
                    State.Controls["menu"] = new ControlButtonPair();
                    State.Controls["home"] = new ControlButton();
                    State.Controls["stick_left"] = new ControlStick(HasClick: true);
                    State.Controls["stick_right"] = new ControlStick(HasClick: true);
                    State.Controls["stick_right"] = new ControlStick(HasClick: true);
                    //if (ControllerAttribute?.PadIsClickOnly ?? false)
                    //{
                    //    State.Controls["touch_center"] = new ControlButton();
                    //}
                    //else
                    //{
                    //    State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
                    //    (State.Controls["touch_center"] as ControlTouch).PhysicalWidth = ControllerAttribute.PhysicalWidth;
                    //    (State.Controls["touch_center"] as ControlTouch).PhysicalHeight = ControllerAttribute.PhysicalHeight;
                    //}
                    // According to this the normalized domain of the DS4 gyro is 1024 units per rad/s: https://gamedev.stackexchange.com/a/87178
                    //State.Controls["motion"] = new ControlMotion();
                    ControlsCreated = true;
                }
                finally
                {
                    StateMutationLock.ExitWriteLock();
                }
            }
            ControllerMetadataUpdate?.Invoke(this);

            {
                lock (UpdateLocalDataLock)
                {
                    if (UpdateLocalDataPoison)
                        return;
                    //string SerialNumber_ = GetSerialNumber();
                    //if (UpdateLocalDataPoison)
                    //    return;
                    //SerialNumber = SerialNumber_;

                    //if (!string.IsNullOrWhiteSpace(SerialNumber))
                    //{
                    //    string ControllerData = StoredDataHandler.GetMacData(SerialNumber);
                    //    DS4SubType ControllerSubTypeRead = DS4SubType.Unknown;
                    //    if (!string.IsNullOrWhiteSpace(ControllerData) && Enum.TryParse<DS4SubType>(ControllerData, out ControllerSubTypeRead))
                    //        ControllerSubType = ControllerSubTypeRead;
                    //}
                    //switch (ControllerSubType)
                    //{
                    //    case FlyDigiSubType.Unknown:
                    //    case FlyDigiSubType.UnknownDS4V1:
                    //    case FlyDigiSubType.UnknownDS4V2:
                    //        IdentityHash = GetControllerAuthIdentityHash();
                    //        ControllerSubType = GetControllerInitialTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId, HaveSeenNonZeroRawTemp, IdentityHash);
                    //        break;
                    //}
                    //ChangeControllerSubType(ControllerSubType);

                    ControllerMetadataUpdate?.Invoke(this);
                }
            }
        }

        private FlyDigiSubType GetControllerInitialTypeCode(UInt16 VID, UInt16 PID, UInt16 REV)
        {
            /*const int ID_MATCH = 0x100000;
            const int HAS_NO_ID = 0x080000;
            const int BATTERY_STATUS = 0x040000;
            const int HAS_MATCHING_AUTH_HASH = 0x020000;
            const int HAS_NO_AUTH_HASH = 0x010000;
            const int TYPE_AUTH = 0x00FFFF;

            bool FromDongle = false;
            if (_device != null && VID == VENDOR_FLYDIGI && (  (PID == PRODUCT_FLYDIGI_DONGLE_1 && REV == REVISION_FLYDIGI_DONGLE_1)
                                                            || (PID == PRODUCT_FLYDIGI_DONGLE_2 && REV == REVISION_FLYDIGI_DONGLE_2)
                                                            || (PID == PRODUCT_FLYDIGI_DONGLE_3 && REV == REVISION_FLYDIGI_DONGLE_3))) // we are an offical dongle
            {
                byte[] FeatureBuffer;
                _device.ReadFeatureData(out FeatureBuffer, 0xE3);
                VID = BitConverter.ToUInt16(FeatureBuffer, 1);
                PID = BitConverter.ToUInt16(FeatureBuffer, 3);
                if (VID == 0x0000 && PID == 0x0000)
                    return FlyDigiSubType.None;
                FromDongle = true;
            }

            List<Tuple<int, FlyDigiSubType>> Candidates = new List<Tuple<int, FlyDigiSubType>>();
            foreach (FlyDigiSubType subType in Enum.GetValues(typeof(FlyDigiSubType)))
            {
                int Rank = 0;//(int)subType;
                ControllerSubTypeAttribute attr = subType.GetAttribute<ControllerSubTypeAttribute>();
                // controller VID/PID is correct or target doesn't use one
                if ((ConnectionType == EConnectionType.USB && attr.USB_VID == VID && attr.USB_PID == PID) // USB type matches
                 || (ConnectionType == EConnectionType.Bluetooth && attr.BT_VID == VID && attr.BT_PID == PID) // BT types matches
                 || (ConnectionType == EConnectionType.Dongle && attr.BT_VID == VID && attr.BT_PID == PID)) // BT types matches (via dongle)
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

            return Candidates.OrderByDescending(dr => dr.Item1).FirstOrDefault()?.Item2 ?? (FromDongle ? DS4SubType.None : DS4SubType.Unknown);*/

            return FlyDigiSubType.Unknown;
        }
        public void SetActiveAlternateController(string ControllerID) { }
    }
}
