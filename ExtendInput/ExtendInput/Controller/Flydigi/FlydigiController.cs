﻿using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.Flydigi
{
    class ControllerSubTypeAttribute : Attribute
    {
        public string[] Tokens { get; private set; }
        public string Name { get; private set; }
        public byte? DeviceIdFromFeature { get; private set; }
        public byte? ReportFESubType { get; private set; } // 2nd data byte
        public byte? FixedValueFromByte28 { get; private set; } // 27th data byte
        //public byte? VersionFromByte30 { get; private set; } // 29th data byte
        //public byte? FixedValueFromByte31 { get; private set; } // 30th data byte
        public int[] ExpectedDongle { get; private set; }


        public bool HasAnalogTrigger { get; private set; }
        public bool HasTopMenu { get; private set; }
        public bool HasBottomMenu { get; private set; }
        public bool HasLogo { get; private set; }
        public bool HasCZTop { get; private set; }
        public bool HasCZBottom { get; private set; }
        public bool HasBumper2 { get; private set; }
        public bool HasMButtons { get; private set; }
        public bool HasMRockers { get; private set; }
        public bool HasWheel { get; private set; }

        public ControllerSubTypeAttribute(
            string[] Token,
            string Name,
            int DeviceIdFromFeature = -1,
            int FixedValueFromByte28 = -1,
            //int VersionFromByte30 = -1,
            //int FixedValueFromByte31 = -1,
            int ReportFESubType = -1,
            int[] ExpectedDongle = null,
            bool HasAnalogTrigger = false,
            bool HasTopMenu = false,
            bool HasBottomMenu = false,
            bool HasLogo = false,
            bool HasCZTop = false,
            bool HasCZBottom = false,
            bool HasBumper2 = false,
            bool HasMButtons = false,
            bool HasMRockers = false,
            bool HasWheel = false)
        {
            this.Tokens = Token;
            this.Name = Name;
            this.DeviceIdFromFeature = DeviceIdFromFeature > -1 ? (byte?)DeviceIdFromFeature : null;
            this.FixedValueFromByte28 = FixedValueFromByte28 > -1 ? (byte?)FixedValueFromByte28 : null;
            //this.VersionFromByte30 = VersionFromByte30 > -1 ? (byte?)VersionFromByte30 : null;
            //this.FixedValueFromByte31 = FixedValueFromByte31 > -1 ? (byte?)FixedValueFromByte31 : null;
            this.ReportFESubType = ReportFESubType > -1 ? (byte?)ReportFESubType : null;
            this.ExpectedDongle = ExpectedDongle;



            this.HasAnalogTrigger = HasAnalogTrigger;
            this.HasTopMenu = HasTopMenu;
            this.HasBottomMenu = HasBottomMenu;
            this.HasLogo = HasLogo;
            this.HasCZTop = HasCZTop;
            this.HasCZBottom = HasCZBottom;
            this.HasBumper2 = HasBumper2;
            this.HasMButtons = HasMButtons;
            this.HasMRockers = HasMRockers;
            this.HasWheel = HasWheel;
        }
    }

    public class FlydigiController : IController
    {
        public const int VENDOR_FLYDIGI = 0x04B4;
        public const int PRODUCT_FLYDIGI_DONGLE_1 = 0x2410;
        public const int PRODUCT_FLYDIGI_DONGLE_2 = 0x2411;
        public const int PRODUCT_FLYDIGI_DONGLE_3 = 0x2411;
        public const int PRODUCT_FLYDIGI_USB = 0x2412;
        public const int REVISION_FLYDIGI_DONGLE_1 = 0x0303;
        public const int REVISION_FLYDIGI_DONGLE_2 = 0x0303;
        public const int REVISION_FLYDIGI_DONGLE_3A = 0x0309;
        public const int REVISION_FLYDIGI_DONGLE_3B = 0x0401;
        public const int REVISION_FLYDIGI_USB = 0x0303;

        #region String Definitions
        private const string ATOM_CONNECTION_WIRE = "CONNECTION_WIRE";
        private const string ATOM_CONNECTION_USB_WIRE = "CONNECTION_WIRE_USB";
        private const string ATOM_CONNECTION_BT = "CONNECTION_BT";
        private const string ATOM_CONNECTION_DONGLE = "CONNECTION_FLYDIGI_DONGLE";
        private const string ATOM_CONNECTION_FLYDIGI_DONGLE = "CONNECTION_FLYDIGI_DONGLE";
        private const string ATOM_CONNECTION_FLYDIGI_DONGLE_1 = "CONNECTION_FLYDIGI_DONGLE_1";
        private const string ATOM_CONNECTION_FLYDIGI_DONGLE_2 = "CONNECTION_FLYDIGI_DONGLE_2";
        private const string ATOM_CONNECTION_FLYDIGI_DONGLE_3 = "CONNECTION_FLYDIGI_DONGLE_3";
        private const string ATOM_CONNECTION_FLYDIGI_DONGLE_3A = "CONNECTION_FLYDIGI_DONGLE_3A";
        private const string ATOM_CONNECTION_FLYDIGI_DONGLE_3B = "CONNECTION_FLYDIGI_DONGLE_3B";
        private const string ATOM_CONNECTION_UNKKNOWN = "CONNECTION_UNKNOWN";

        private readonly string[] _CONNECTION_WIRE = new string[] { ATOM_CONNECTION_USB_WIRE, ATOM_CONNECTION_WIRE };
        private readonly string[] _CONNECTION_BT = new string[] { ATOM_CONNECTION_BT };
        private readonly string[] _CONNECTION_DONGLE = new string[] { ATOM_CONNECTION_FLYDIGI_DONGLE, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_DONGLE_1 = new string[] { ATOM_CONNECTION_FLYDIGI_DONGLE_1, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_DONGLE_2 = new string[] { ATOM_CONNECTION_FLYDIGI_DONGLE_2, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_DONGLE_3A = new string[] { ATOM_CONNECTION_FLYDIGI_DONGLE_3A, ATOM_CONNECTION_FLYDIGI_DONGLE_3, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_DONGLE_3B = new string[] { ATOM_CONNECTION_FLYDIGI_DONGLE_3B, ATOM_CONNECTION_FLYDIGI_DONGLE_3, ATOM_CONNECTION_DONGLE };
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
                Token: new string[] { "DEVICE_FLYDIGI_X9", "DEVICE_GAMEPAD" },
                Name: "Flydigi X9",
                //DeviceIdFromFeature: 0x10,
                ReportFESubType: 0x55,
                HasTopMenu: true,
                HasLogo: true)]
            X9,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_X8", "DEVICE_GAMEPAD" },
                Name: "Flydigi X8",
                //DeviceIdFromFeature: 0x11,
                FixedValueFromByte28: 0x00,
                //VersionFromByte30: 0x32,
                //FixedValueFromByte31: 0x1B,
                ReportFESubType: 0x66,
                HasTopMenu: true,
                HasLogo: true)]
            X8,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_APEX", "DEVICE_GAMEPAD" },
                Name: "Flydigi APEX",
                //DeviceIdFromFeature: 0x12, // removed because we can't get a device ID, if we find we can with a 3dot we will need to put this back but replace nullchecks on it with a new check of a new bool property that says we don't respond on 2dot. We know 1dot doesn't support the query this data is for at all.
                FixedValueFromByte28: 0x01,
                //VersionFromByte30: 0x32,
                ExpectedDongle: new int[] { (PRODUCT_FLYDIGI_DONGLE_1 << 8) | REVISION_FLYDIGI_DONGLE_1 },
                HasAnalogTrigger: true,
                HasBottomMenu: true,
                HasMRockers: true,
                HasCZTop: true)]
            APEX,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_APEX_2", "DEVICE_GAMEPAD" },
                Name: "Flydigi APEX 2",
                DeviceIdFromFeature: 0x13,
                FixedValueFromByte28: 0x01,
                //VersionFromByte30: 0x36,
                ExpectedDongle: new int[] { (PRODUCT_FLYDIGI_DONGLE_2 << 8) | REVISION_FLYDIGI_DONGLE_2 },
                HasBottomMenu: true,
                HasMButtons: true,
                HasCZBottom: true,
                HasBumper2: true)]
            APEX_2,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_F1", "DEVICE_GAMEPAD" },
                Name: "Flydigi F1",
                DeviceIdFromFeature: 0x14,
                ReportFESubType: 0x66,
                HasBottomMenu: true,
                HasMButtons: true,
                HasCZBottom: true)]
            F1,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_F1", "DEVICE_GAMEPAD" },
                Name: "Flydigi F1 WIRED",
                DeviceIdFromFeature: 0x15,
                HasBottomMenu: true,
                HasMButtons: true,
                HasCZBottom: true)]
            F1_WIRED,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WEE1", "DEVICE_GAMEPAD" },
                Name: "Flydigi WEE1",
                DeviceIdFromFeature: 0x20)]
            WEE1,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WEE2", "DEVICE_GAMEPAD" },
                Name: "Flydigi WEE2",
                DeviceIdFromFeature: 0x21)]
            WEE2,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_Q1", "DEVICE_GAMEPAD" },
                Name: "Flydigi Q1",
                DeviceIdFromFeature: 0x30)]
            Q1,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_D1", "DEVICE_GAMEPAD" },
                Name: "Flydigi D1",
                DeviceIdFromFeature: 0x31)]
            D1,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_BT", "DEVICE_GAMEPAD" },
                Name: "Flydigi WASP BT",
                DeviceIdFromFeature: 0x40)]
            WASP_BT,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_N", "DEVICE_GAMEPAD" },
                Name: "Flydigi WASP N",
                DeviceIdFromFeature: 0x41)]
            WASP_N,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_X", "DEVICE_GAMEPAD" },
                Name: "Flydigi WASP X",
                DeviceIdFromFeature: 0x42)]
            WASP_X,

            [ControllerSubType(
                Token: new string[] { "DEVICE_FLYDIGI_WASP_2", "DEVICE_GAMEPAD" },
                Name: "Flydigi WASP 2",
                DeviceIdFromFeature: 0x43)]
            WASP_2,
        }
        //private FlyDigiSubType PreviousControllerSubType = FlyDigiSubType.None;
        private FlyDigiSubType ControllerSubType = FlyDigiSubType.None;
        private ControllerSubTypeAttribute ControllerAttribute = null;
        private SemaphoreSlim MetadataMutationLock = new SemaphoreSlim(1);
        private byte? DetectedDeviceId = null;
        private byte? ReportFESubType = null;
        private byte? FixedValueFromByte28 = null;
        //private byte? VersionFromByte30 = null;
        private byte? FixedValueFromByte31 = null;
        private List<FlyDigiSubType> ManualSelectionList = new List<FlyDigiSubType>();

        private int RequestAndroidInfoTimeGap = 1;

        private bool ControlsCreated = false;
        private bool NoMetaForThisController = false;
        private DateTime LastData;
        private DateTime? FirstDeviceInfoRequestTime = null;

        private HidDevice _device;
        int reportUsageLock = 0;

        private const int _SLOW_POLL_MS = 1000;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        // The controller state can drastically change, where it picks up or loses items based on passive detection of quirky controllers, this makes it safe
        //private ReaderWriterLockSlim StateMutationLock = new ReaderWriterLockSlim();
        private SemaphoreSlim StateMutationLock = new SemaphoreSlim(1);

        bool ResetControllerInfoNeeded = false;
        object ResetControllerInfoNeededLock = new object();


        public bool HasMotion => true;

        public bool IsReady => true;
        public bool IsPresent => true;
        public bool IsVirtual => false;

        //public ControllerState GetState()
        //{
        //    return State;
        //}

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
                    case EConnectionType.Dongle:
                        if (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_1 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_1) return _CONNECTION_DONGLE_1;
                        if (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_2 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_2) return _CONNECTION_DONGLE_2;
                        if (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3A) return _CONNECTION_DONGLE_3A;
                        if (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3B) return _CONNECTION_DONGLE_3B;
                        return _CONNECTION_DONGLE;
                    default: return _CONNECTION_UNKKNOWN;
                }
            }
        }
        public string[] ControllerTypeCode
        {
            get
            {
                return ControllerAttribute?.Tokens ?? new string[] { "DEVICE_UNKNOWN" };
                //return new string[] { "DEVICE_FLYDIGI" };

            }
        }

        public string[] NameDetails
        {
            get
            {
                return null;
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
                UInt16 REV = (UInt16)_device.RevisionNumber;
                if (ControllerSubType == FlyDigiSubType.None)
                {
                    if (VID == VENDOR_FLYDIGI)
                    {
                        if (PID == PRODUCT_FLYDIGI_DONGLE_1 && REV == REVISION_FLYDIGI_DONGLE_1) // 1 dot dongle
                            return "Flydigi USB Dongle• (3.3)";
                        if (PID == PRODUCT_FLYDIGI_DONGLE_2 && REV == REVISION_FLYDIGI_DONGLE_2) // 2 dot dongle
                            return "Flydigi USB Dongle•• (3.3)";
                        if (PID == PRODUCT_FLYDIGI_DONGLE_3 && REV == REVISION_FLYDIGI_DONGLE_3A) // 3 dot dongle
                            return "Flydigi USB Dongle••• (3.9)";
                        if (PID == PRODUCT_FLYDIGI_DONGLE_3 && REV == REVISION_FLYDIGI_DONGLE_3B) // 3 dot dongle
                            return "Flydigi USB Dongle••• (4.1)";
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

        public bool HasSelectableAlternatives
        {
            get
            {
                lock (ManualSelectionList)
                {
                    //Console.WriteLine($"ManualSelectionList.Count = {ManualSelectionList.Count}");
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
                    //Console.WriteLine("ManualSelectionList.ToDictionary");
                    return ManualSelectionList.ToDictionary(dr => dr.ToString(), dx => GetNameForSubType(dx));
                }
            }
        }

        public IDevice DeviceHackRef => _device;

        ControllerState State = new ControllerState();

        bool AbortStatusThread = false;
        //bool EarlyDeviceRecheck = false;
        Thread CheckControllerStatusThread;
        Thread CheckControllerDongleAliveThread;
        public FlydigiController(HidDevice device, EConnectionType ConnectionType = EConnectionType.Unknown)
        {
            //LastData = DateTime.UtcNow;
            LastData = new DateTime();
            this.ConnectionType = ConnectionType;

            _device = device;

            PollingState = EPollingState.Inactive;
            Log($"Polling state set to Inactive", ConsoleColor.Yellow);

            _device.DeviceReport += OnReport;
            State.ControllerStateUpdate += State_ControllerStateUpdate;

            ResetControllerInfo();

            // Dongle capable of controller type detection
            if (_device.VendorId == VENDOR_FLYDIGI)
            {
                if ((_device.ProductId == PRODUCT_FLYDIGI_DONGLE_1 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_1) // 1 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_2 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_2) // 2 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3A) // 3 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3B)) // 3 dot dongle
                {
                    this.ConnectionType = EConnectionType.Dongle;
                    CheckControllerDongleAliveThread = new Thread(() =>
                    {
                        for (; ; )
                        {
                            //if (ControllerSubType != FlyDigiSubType.None && LastData.AddSeconds(PollingState == EPollingState.SlowPoll ? (_SLOW_POLL_MS + 100) / 1000f : 0.2) < DateTime.UtcNow)
                            if (ControllerSubType != FlyDigiSubType.None && LastData.AddSeconds((_SLOW_POLL_MS + 100) / 1000f) < DateTime.UtcNow)
                            {
                                Log($"No report within timeout {(DateTime.UtcNow - LastData).TotalSeconds}", ConsoleColor.Cyan);
                                ChangeControllerSubType(FlyDigiSubType.None);
                                /*
                                ControllerSubType = FlyDigiSubType.None;
                                ControllerAttribute = ControllerSubType.GetAttribute<ControllerSubTypeAttribute>();
                                MetadataMutationLock.Wait(); NoteMetadataMutationLock();
                                try
                                {
                                    DetectedDeviceId = null;
                                    ReportFESubType = null;
                                    FixedValueFromByte28 = null;
                                    //VersionFromByte30 = null;
                                    FixedValueFromByte31 = null;
                                    NoMetaForThisController = false;
                                }
                                finally
                                {
                                    MetadataMutationLock.Release();
                                }
                                */
                            }
                            if (AbortStatusThread)
                                return;
                            Thread.Sleep(1000);
                        }
                    });
                    CheckControllerDongleAliveThread.Start();
                }
                else if ((_device.ProductId == PRODUCT_FLYDIGI_USB && _device.RevisionNumber == REVISION_FLYDIGI_USB))
                {
                    this.ConnectionType = EConnectionType.USB;
                }

                if ((_device.ProductId == PRODUCT_FLYDIGI_DONGLE_2 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_2) // 2 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3A) // 3 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_3 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_3B) // 3 dot dongle
                 || (_device.ProductId == PRODUCT_FLYDIGI_USB && _device.RevisionNumber == REVISION_FLYDIGI_USB)) // controller that supports USB
                {
                    this.PollingState = EPollingState.RunUntilReady; // we need to read until we get device info
                    Log($"Polling state set to RunUntilReady", ConsoleColor.Yellow);
                    _device.StartReading();
                    CheckControllerStatusThread = new Thread(() =>
                    {
                        for (; ; )
                        {
                            Log($"getDeviceInfoInAndroid Timegap {RequestAndroidInfoTimeGap}");
                            getDeviceInfoInAndroid();
                            for (int i = 0; i < RequestAndroidInfoTimeGap; i++)
                            {
                                if (AbortStatusThread)
                                    return;
                                Thread.Sleep(1000);
                            }
                            // expand the time gap until we get to once per min
                            // this allows us to initially more agressively request the android data
                            // the issue is the dongle sends nothing for some devices, but also can have its report missed due to lag caused by slow-poll
                            // we try to work around this by stopping slowpoll during the first request, but this doesn't apply to all cases
                            if (RequestAndroidInfoTimeGap < 60)
                                RequestAndroidInfoTimeGap++;
                        }
                    });
                    CheckControllerStatusThread.Start();
                }
                else if (_device.ProductId == PRODUCT_FLYDIGI_DONGLE_1 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_1) // 1 dot dongle
                {
                    this.PollingState = EPollingState.SlowPoll;
                    Log($"Polling state set to SlowPoll", ConsoleColor.Yellow);
                    _device.StartReading();
                }
                else
                {
                    this.PollingState = EPollingState.RunOnce; // we are either a controller
                    Log($"Polling state set to RunOnce", ConsoleColor.Yellow);
                    _device.StartReading();
                }
            }
        }

        public void Dispose()
        {
            AbortStatusThread = true;
            switch (PollingState)
            {
                case EPollingState.Active:
                case EPollingState.SlowPoll:
                case EPollingState.RunUntilReady:
                    {
                        _device.StopReading();

                        PollingState = EPollingState.Inactive;
                        Log($"Polling state set to Inactive", ConsoleColor.Yellow);
                        _device.CloseDevice();
                    }
                    break;
            }
        }

        public void Initalize()
        {
            Log("Initalize");
            if (PollingState == EPollingState.Active) return;

            PollingState = EPollingState.Active;
            Log($"Polling state set to Active", ConsoleColor.Yellow);
            _device.StartReading();
        }

        public void DeInitalize()
        {
            Log("DeInitalize");
            if (PollingState == EPollingState.Inactive) return;
            if (PollingState == EPollingState.SlowPoll) return;
            //if (PollingState == EPollingState.RunOnce) return;
            if (PollingState == EPollingState.RunUntilReady) return;

            // dongles switch back to slow poll instead of going inactive
            if (ConnectionType == EConnectionType.Dongle)
            {
                PollingState = EPollingState.SlowPoll;
                Log($"Polling state set to SlowPoll", ConsoleColor.Yellow);
            }
            else
            {
                _device.StopReading();

                PollingState = EPollingState.Inactive;
                Log($"Polling state set to Inactive", ConsoleColor.Yellow);
                _device.CloseDevice();
            }
        }

        public async void Identify()
        {
        }


        private void State_ControllerStateUpdate(ControlCollection controls)
        {
            ControllerStateUpdate?.Invoke(this, controls);
        }
        private void OnReport(IReport rawReportData)
        {
            if (PollingState == EPollingState.Inactive) return;

            bool IgnoreSlowPoll = false;

            LastData = DateTime.UtcNow;
            if (ControllerSubType == FlyDigiSubType.None)
            {
                ChangeControllerSubType(FlyDigiSubType.Unknown);

                if (PollingState != EPollingState.Active)
                {
                    PollingState = EPollingState.RunUntilReady;
                    Log($"Polling state set to RunUntilReady", ConsoleColor.Yellow);
                }

                //ControllerSubType = FlyDigiSubType.Unknown;
                //ControllerAttribute = ControllerSubType.GetAttribute<ControllerSubTypeAttribute>();
                FirstDeviceInfoRequestTime = DateTime.UtcNow;
                //getDeviceInfoInAndroid();
                RequestAndroidInfoTimeGap = 1;
                ////EarlyDeviceRecheck = true;
            }

            //if (!(reportData is HidReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.HID) return;
            HidReport reportData = (HidReport)rawReportData;

            if (reportData.ReportId == 0x04)
            {
                switch (reportData.ReportBytes[0])
                {
                    case 0xFE:
                        {
                            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
                            {
                                try
                                {
                                    State.StartStateChange();
                                    try
                                    {
                                        // Clone the current state before altering it since the OldState is likely a shared reference
                                        StateMutationLock.Wait(); NoteStateMutationLock();
                                        try
                                        {
//                                          Log($"Clone State {State.Controls["triggers"]?.GetType()}", ConsoleColor.Cyan);
                                            ControllerSubTypeAttribute ControllerAttributeInFlight = ControllerAttribute;

                                            //byte ControllerID = reportData.ReportBytes[27];
                                            bool AirMouseActive = (reportData.ReportBytes[2] & 0x80) == 0x80;
                                            bool AirMouseClick = (reportData.ReportBytes[2] & 0x01) == 0x01;

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
                                            byte TriggerLeft = reportData.ReportBytes[22];
                                            byte TriggerRight = reportData.ReportBytes[23];
                                            ////////////////////////////////////////////////////

                                            (State.Controls["stick_left"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(LStickX);
                                            (State.Controls["stick_left"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(LStickY);
                                            (State.Controls["stick_left"] as IControlStickWithClick).Click = buttonL3;
                                            (State.Controls["stick_right"] as IControlStickWithClick).X = ControllerMathTools.QuickStickToFloat(RStickX);
                                            (State.Controls["stick_right"] as IControlStickWithClick).Y = ControllerMathTools.QuickStickToFloat(RStickY);
                                            (State.Controls["stick_right"] as IControlStickWithClick).Click = buttonR3;
                                            (State.Controls["bumpers_left"] as IControlButton).DigitalStage1 = buttonL1;
                                            (State.Controls["bumpers_right"] as IControlButton).DigitalStage1 = buttonR1;
                                            (State.Controls["menu_left"] as IControlButton).DigitalStage1 = buttonSelect;
                                            (State.Controls["menu_right"] as IControlButton).DigitalStage1 = buttonStart;

                                            if (ControllerAttributeInFlight.HasAnalogTrigger)
                                            {
                                                if (State.Controls["trigger_left"] is IControlTrigger) (State.Controls["trigger_left"] as IControlTrigger).AnalogStage1 = TriggerLeft > 0 ? TriggerLeft / 255f : buttonL2 ? 255 : 0;
                                                if (State.Controls["trigger_right"] is IControlTrigger) (State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = TriggerRight > 0 ? TriggerRight / 255f : buttonR2 ? 255 : 0;
                                            }
                                            else if (ControllerSubType != FlyDigiSubType.None && ControllerSubType != FlyDigiSubType.Unknown)
                                            {
                                                if (State.Controls["trigger_left"] is IControlButton) (State.Controls["trigger_left"] as IControlButton).DigitalStage1 = buttonL2;
                                                if (State.Controls["trigger_right"] is IControlButton) (State.Controls["trigger_right"] as IControlButton).DigitalStage1 = buttonR2;
                                            }
                                            //if (ControllerAttributeInFlight.HasLogo)
                                            //{
                                            //    (StateInFlight.Controls["logo"] as ControlButton).Button0 = buttonLogo;
                                            //}
                                            /*if (ControllerAttributeInFlight.HasTopMenu)
                                            {
                                                (StateInFlight.Controls["mtop_back"] as ControlButton).Button0 = buttonBack;
                                                (StateInFlight.Controls["mtop_home"] as ControlButton).Button0 = buttonHome;
                                                (StateInFlight.Controls["mtop_pair"] as ControlButton).Button0 = buttonPair;
                                            }
                                            if (ControllerAttributeInFlight.HasBottomMenu)
                                            {
                                                (StateInFlight.Controls["m_back"] as ControlButton).Button0 = buttonBack;
                                                (StateInFlight.Controls["m_home"] as ControlButton).Button0 = buttonHome;
                                                (StateInFlight.Controls["m_pair"] as ControlButton).Button0 = buttonPair;
                                            }*/
                                            if (ControllerAttributeInFlight.HasTopMenu || ControllerAttributeInFlight.HasBottomMenu)
                                            {
                                                (State.Controls["cluster_middle"] as ControlButtonGrid).Button[0, 0] = buttonPair;
                                                (State.Controls["cluster_middle"] as ControlButtonGrid).Button[1, 0] = buttonHome;
                                                (State.Controls["cluster_middle"] as ControlButtonGrid).Button[2, 0] = buttonBack;
                                            }
                                            if (ControllerAttributeInFlight.HasCZBottom)
                                            {
                                                (State.Controls["c"] as IControlButton).DigitalStage1 = buttonC;
                                                (State.Controls["z"] as IControlButton).DigitalStage1 = buttonZ;
                                            }
                                            if (ControllerAttributeInFlight.HasCZTop)
                                            {
                                                (State.Controls["c_top"] as IControlButton).DigitalStage1 = buttonC;
                                                (State.Controls["z_top"] as IControlButton).DigitalStage1 = buttonZ;
                                            }
                                            if (ControllerAttributeInFlight.HasMRockers)
                                            {
                                                (State.Controls["grip_right"] as ControlRocker).Direction = buttonM1 || AirMouseClick ? EDPadDirection.East : buttonM3 ? EDPadDirection.West : EDPadDirection.None;
                                                (State.Controls["grip_left"] as ControlRocker).Direction = buttonM4 ? EDPadDirection.East : buttonM2 ? EDPadDirection.West : EDPadDirection.None;
                                            }
                                            else if (ControllerAttributeInFlight.HasMButtons)
                                            {
                                                (State.Controls["grip_right_left"] as ControlButton).DigitalStage1 = buttonM3;
                                                (State.Controls["grip_right_right"] as ControlButton).DigitalStage1 = buttonM1 || AirMouseClick;
                                                (State.Controls["grip_left_left"] as ControlButton).DigitalStage1 = buttonM2;
                                                (State.Controls["grip_left_right"] as ControlButton).DigitalStage1 = buttonM4;
                                            }
                                            if (ControllerAttributeInFlight.HasBumper2)
                                            {
                                                (State.Controls["shoulder_a_left"] as ControlButton).DigitalStage1 = buttonM6;
                                                (State.Controls["shoulder_a_right"] as ControlButton).DigitalStage1 = buttonM5;
                                            }
                                            if (ControllerAttributeInFlight.HasWheel)
                                            {
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = buttonY; // wheel
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = buttonB;
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = buttonA;
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = buttonX;
                                            }
                                            else if (ControllerSubType != FlyDigiSubType.None && ControllerSubType != FlyDigiSubType.Unknown)
                                            {
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = buttonY;
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = buttonB;
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = buttonA;
                                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = buttonX;
                                            }

                                            int padH = 0;
                                            int padV = 0;
                                            if (buttonUp) padV++;
                                            if (buttonDown) padV--;
                                            if (buttonRight) padH++;
                                            if (buttonLeft) padH--;
                                            if (padH > 0)
                                                if (padV > 0)
                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast;
                                                else if (padV < 0)
                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast;
                                                else
                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East;
                                            else if (padH < 0)
                                                if (padV > 0)
                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest;
                                                else if (padV < 0)
                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest;
                                                else
                                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West;
                                            else
                                                if (padV > 0)
                                                (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North;
                                            else if (padV < 0)
                                                (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South;
                                            else
                                                (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None;

                                            //short yaw = ProcSignedByteNybble((short)(((report.Data[4] & 0x0f) << 8) + report.Data[3]));
                                            //short pitch = ProcSignedByteNybble((short)(((report.Data[4] & 0xf0) >> 4) + (report.Data[5] << 4)));
                                        }
                                        finally
                                        {
                                            StateMutationLock.Release();
                                        }

                                        MetadataMutationLock.Wait(); NoteMetadataMutationLock();
                                        try
                                        {
                                            byte? OldReportFESubType = ReportFESubType;
                                            ReportFESubType = reportData.ReportBytes[1];

                                            byte? OldFixedValueFromByte28 = FixedValueFromByte28;
                                            FixedValueFromByte28 = reportData.ReportBytes[27];

                                            //byte? OldVersionFromByte30 = VersionFromByte30;
                                            //VersionFromByte30 = reportData.ReportBytes[29];

                                            byte? OldFixedValueFromByte31 = FixedValueFromByte31;
                                            FixedValueFromByte31 = reportData.ReportBytes[30];

                                            if (OldReportFESubType != ReportFESubType
                                             || OldFixedValueFromByte28 != FixedValueFromByte28)
                                                ////|| OldVersionFromByte30 != VersionFromByte30
                                                //|| OldFixedValueFromByte31 != FixedValueFromByte31)
                                            {
                                                lock (ResetControllerInfoNeededLock) ResetControllerInfoNeeded = true; // ResetControllerInfo();
                                                Log("ResetControllerInfo Requested", ConsoleColor.Red);
                                            }
                                        }
                                        finally
                                        {
                                            MetadataMutationLock.Release();
                                        }
                                    }
                                    finally
                                    {
                                        State.EndStateChange();
                                    }

                                    if (PollingState == EPollingState.RunUntilReady || PollingState == EPollingState.RunOnce)
                                    {
                                        if (ConnectionType == EConnectionType.Dongle)
                                        {
                                            /*if (_device.VendorId == VENDOR_FLYDIGI && _device.ProductId == PRODUCT_FLYDIGI_DONGLE_1 && _device.RevisionNumber == REVISION_FLYDIGI_DONGLE_1)
                                            {
                                                Console.WriteLine("Dongle is a type 1 that never gives data, so we have what we're going to get");
                                                PollingState = EPollingState.SlowPoll;
                                            }
                                            else*/
                                            if (FirstDeviceInfoRequestTime.HasValue && (FirstDeviceInfoRequestTime.Value.AddSeconds(5) < DateTime.UtcNow)) // check if we are a dongle and asked for this data a while ago, but still don't have it
                                            {
                                                Log("Did not get dongle data within timeout");
                                                FirstDeviceInfoRequestTime = null;
                                                NoMetaForThisController = true;
                                                PollingState = EPollingState.SlowPoll;
                                                Log($"Polling state set to SlowPoll", ConsoleColor.Yellow);
                                                lock (ResetControllerInfoNeededLock) ResetControllerInfoNeeded = true; // ResetControllerInfo();
                                                Log("ResetControllerInfo Requested", ConsoleColor.Red);
                                            }
                                        }
                                        // USB mode we expect data, so don't stop if we get a normal report, we should keep our RunUntilReady until we see what we want
                                        /*else
                                        {
                                            _device.StopReading();
                                            PollingState = EPollingState.Inactive;
                                            _device.CloseDevice();
                                        }*/
                                    }
                                }
                                finally
                                {
                                    Interlocked.Exchange(ref reportUsageLock, 0);
                                }
                            }
                        }
                        break;
                    case 0xFF:
                        {
                            if (reportData.ReportBytes[1] == 0xF0)
                            {
                                if (reportData.ReportBytes[14] == 0xEC)
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
                                    //Log("Controller firmware version: V" + FirmwareMajor + "." + FirmwareMinor + "." + FirmwareBuild + "." + FirmwareRevision);
                                    //Log("Controller Power: " + batteryPercent);

                                    MetadataMutationLock.Wait(); NoteMetadataMutationLock();
                                    try
                                    {
                                        byte? OldDetectedDeviceId = DetectedDeviceId;
                                        DetectedDeviceId = reportData.ReportBytes[2];
                                        Log($"DetectedDeviceId set to {reportData.ReportBytes[2]:X2}", ConsoleColor.DarkYellow);
                                        if (OldDetectedDeviceId != DetectedDeviceId)
                                        {
                                            lock (ResetControllerInfoNeededLock) ResetControllerInfoNeeded = true; // ResetControllerInfo();
                                            Log("ResetControllerInfo Requested", ConsoleColor.Red);
                                        }
                                    }
                                    finally
                                    {
                                        MetadataMutationLock.Release();
                                    }

                                    if (PollingState == EPollingState.RunUntilReady)
                                    {
                                        if (ConnectionType == EConnectionType.Dongle)
                                        {
                                            PollingState = EPollingState.SlowPoll;
                                            Log($"Polling state set to SlowPoll", ConsoleColor.Yellow);
                                        }
                                        else
                                        {
                                            RequestAndroidInfoTimeGap = 60; // we are a wired device that can't change, slow to lowest poll rate as we just want to read the battery now
                                            _device.StopReading();
                                            PollingState = EPollingState.Inactive;
                                            Log($"Polling state set to Inactive", ConsoleColor.Yellow);
                                            _device.CloseDevice();
                                        }
                                    }
                                    IgnoreSlowPoll = true; // don't slow-poll right after this request, but on the one after

                                    Log($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}", ConsoleColor.Blue);
                                }
                                else
                                {
                                    Log($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                                }
                            }
                            else
                            {
                                Log($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                            }
                        }
                        break;
                    default:
                        Log($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
                        break;
                }
            }
            else
            {
                Log($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
            }

            lock (ResetControllerInfoNeededLock)
            {
                if (ResetControllerInfoNeeded)
                {
                    ResetControllerInfo();
                    ResetControllerInfoNeeded = false;
                }
            }

            if (!IgnoreSlowPoll && PollingState == EPollingState.SlowPoll)
            {
                _device.PollingRate = _SLOW_POLL_MS; // if we're a dongle and we're not connected we might only be partially initalized, so slow roll our read
            }
            else
            {
                _device.PollingRate = 0;
            }

            //Console.ForegroundColor = ConsoleColor.DarkGray;
            //Console.WriteLine($"{reportData.ReportId:X2} {BitConverter.ToString(reportData.ReportBytes)}");
            //Console.ResetColor();
        }

        private void getDeviceInfoInAndroid()
        {
            Log("getDeviceInfoInAndroid");

            //if (!NoMetaForThisController && ConnectionType == EConnectionType.Dongle && PollingState == EPollingState.SlowPoll)
            if (ConnectionType == EConnectionType.Dongle && PollingState == EPollingState.SlowPoll)
            {
                PollingState = EPollingState.RunUntilReady;
                Log($"Polling state set to RunUntilReady");
            }

            byte[] array = new byte[12];
            array[0] = 5;
            array[1] = 0xEC;
            bool success = _device.WriteReport(array);
        }

        object UpdateLocalDataLock = new object();
        bool UpdateLocalDataPoison = false;

        private void ResetControllerInfo()
        {
            lock (UpdateLocalDataLock)
            {
                Log("ResetControllerInfo Start", Indent: true);

                FlyDigiSubType NewControllerSubType = GetControllerInitialTypeCode((UInt16)_device.VendorId, (UInt16)_device.ProductId, (UInt16)_device.RevisionNumber);

                ChangeControllerSubType(NewControllerSubType);

                ControllerMetadataUpdate?.Invoke(this);

                Log("ResetControllerInfo End", Indent: false);
            }
        }

        private FlyDigiSubType GetControllerInitialTypeCode(UInt16 VID, UInt16 PID, UInt16 REV)
        {
            const int EXPECTS_NO_ID = 0x100000;
            /*const int ID_MATCH = 0x100000;
            const int HAS_NO_ID = 0x080000;
            const int BATTERY_STATUS = 0x040000;
            const int HAS_MATCHING_AUTH_HASH = 0x020000;
            const int HAS_NO_AUTH_HASH = 0x010000;*/
            const int REPORT_BYTES_MATCH = 0x000100;
            const int TYPE_AUTH = 0x0000FF;
            /*
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
            }*/

            List<Tuple<int, FlyDigiSubType>> Candidates = new List<Tuple<int, FlyDigiSubType>>();
            foreach (FlyDigiSubType subType in Enum.GetValues(typeof(FlyDigiSubType)))
            {
                int Rank = 0;//(int)subType;
                ControllerSubTypeAttribute attr = subType.GetAttribute<ControllerSubTypeAttribute>();
                if ((ConnectionType == EConnectionType.USB && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_USB == PID && REVISION_FLYDIGI_USB == REV)
                 || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_2 == PID && REVISION_FLYDIGI_DONGLE_2 == REV)
                 || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_3 == PID && REVISION_FLYDIGI_DONGLE_3A == REV)
                 || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_3 == PID && REVISION_FLYDIGI_DONGLE_3B == REV))
                {
                    if (DetectedDeviceId.HasValue)
                    {
                        if (attr.DeviceIdFromFeature.HasValue && attr.DeviceIdFromFeature.Value == DetectedDeviceId.Value)
                        {
                            return subType;
                        }
                    }
                    else
                    {
                        if (!attr.DeviceIdFromFeature.HasValue)
                        {
                            Rank += EXPECTS_NO_ID;
                        }
                    }
                }
                /*else if (attr.USB_VID == -1 && attr.USB_PID == -1 && attr.BT_VID == -1 && attr.BT_PID == -1)
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
                }*/
                //if (subType != FlyDigiSubType.None && subType != FlyDigiSubType.Unknown)
                if (subType != FlyDigiSubType.Unknown)
                {
                    bool Matches = true;
                    if (ReportFESubType.HasValue && attr.ReportFESubType.HasValue)
                        if (ReportFESubType.Value == attr.ReportFESubType.Value)
                        {
                            Rank += REPORT_BYTES_MATCH;
                        }
                        else
                        {
                            Matches = false;
                        }
                    if (FixedValueFromByte28.HasValue && attr.FixedValueFromByte28.HasValue)
                        if (FixedValueFromByte28.Value == attr.FixedValueFromByte28.Value)
                        {
                            Rank += REPORT_BYTES_MATCH;
                        }
                        else
                        {
                            Matches = false;
                        }
                    //if (VersionFromByte30.HasValue && attr.VersionFromByte30.HasValue)
                    //    if (VersionFromByte30.Value == attr.VersionFromByte30.Value)
                    //    {
                    //        Rank += REPORT_BYTES_MATCH;
                    //    }
                    //    else
                    //    {
                    //        Matches = false;
                    //    }
                    //if (FixedValueFromByte31.HasValue && attr.FixedValueFromByte31.HasValue)
                    //    if (FixedValueFromByte31.Value == attr.FixedValueFromByte31.Value)
                    //    {
                    //        Rank += REPORT_BYTES_MATCH;
                    //    }
                    //    else
                    //    {
                    //        Matches = false;
                    //    }

                    if (Matches)
                    {
                        Rank += TYPE_AUTH - (int)subType;
                        Candidates.Add(new Tuple<int, FlyDigiSubType>(Rank, subType));
                    }
                }
            }

            return Candidates.OrderByDescending(dr => dr.Item1).FirstOrDefault()?.Item2 ?? FlyDigiSubType.Unknown;
        }
        public void SetActiveAlternateController(string ControllerID)
        {
            ChangeControllerSubType((FlyDigiSubType)Enum.Parse(typeof(FlyDigiSubType), ControllerID));
        }

        private void UpdateAlternateSubTypes()
        {
            Log("UpdateAlternateSubTypes Start", Indent: true);
            lock (ManualSelectionList)
            {
                ManualSelectionList.Clear();
                Log($"ManualSelectionList.Clear for {ControllerSubType}");

                if (ControllerSubType == FlyDigiSubType.None)
                {
                    Log("UpdateAlternateSubTypes End", Indent: false);
                    return;
                }

                UInt16 VID = (UInt16)_device.VendorId;
                UInt16 PID = (UInt16)_device.ProductId;
                UInt16 REV = (UInt16)_device.RevisionNumber;

                /*
                foreach (FlyDigiSubType subType in Enum.GetValues(typeof(FlyDigiSubType)))
                {
                    int Rank = 0;//(int)subType;
                    ControllerSubTypeAttribute attr = subType.GetAttribute<ControllerSubTypeAttribute>();
                    if ((ConnectionType == EConnectionType.USB && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_USB == PID && REVISION_FLYDIGI_USB == REV)
                     || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_2 == PID && REVISION_FLYDIGI_DONGLE_2 == REV)
                     || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_3 == PID && REVISION_FLYDIGI_DONGLE_3 == REV))
                    {
                        if (DetectedDeviceId.HasValue)
                        {
                            if (attr.DeviceIdFromFeature.HasValue && attr.DeviceIdFromFeature.Value == DetectedDeviceId.Value)
                            {
                                ManualSelectionList.Clear();
                                return;
                            }
                        }
                    }
                    if (subType != FlyDigiSubType.None && subType != FlyDigiSubType.Unknown)
                    {
                        bool Matches = true;
                        if (ReportFESubType.HasValue && attr.ReportFESubType.HasValue)
                            if (ReportFESubType.Value != attr.ReportFESubType.Value)
                                Matches = false;
                        if (FixedValueFromByte28.HasValue && attr.FixedValueFromByte28.HasValue)
                            if (FixedValueFromByte28.Value != attr.FixedValueFromByte28.Value)
                                Matches = false;
                        //if (VersionFromByte30.HasValue && attr.VersionFromByte30.HasValue)
                        //    if (VersionFromByte30.Value != attr.VersionFromByte30.Value)
                        //        Matches = false;
                        if (FixedValueFromByte31.HasValue && attr.FixedValueFromByte31.HasValue)
                            if (FixedValueFromByte31.Value != attr.FixedValueFromByte31.Value)
                                Matches = false;

                        if (Matches)
                        {
                            ManualSelectionList.Add(subType);
                            Console.WriteLine($"ManualSelectionList.Add({subType})");
                        }
                    }
                }
                */

                const int EXPECTS_NO_ID = 0x100000;
                /*const int ID_MATCH = 0x100000;
                const int HAS_NO_ID = 0x080000;
                const int BATTERY_STATUS = 0x040000;
                const int HAS_MATCHING_AUTH_HASH = 0x020000;
                const int HAS_NO_AUTH_HASH = 0x010000;*/
                const int REPORT_BYTES_MATCH = 0x000100;
                //const int TYPE_AUTH = 0x0000FF;
                /*
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
                }*/

                List<Tuple<int, FlyDigiSubType>> Candidates = new List<Tuple<int, FlyDigiSubType>>();
                foreach (FlyDigiSubType subType in Enum.GetValues(typeof(FlyDigiSubType)))
                {
                    int Rank = 0;//(int)subType;
                    ControllerSubTypeAttribute attr = subType.GetAttribute<ControllerSubTypeAttribute>();
                    if ((ConnectionType == EConnectionType.USB    && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_USB      == PID && REVISION_FLYDIGI_USB       == REV)
                     || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_2 == PID && REVISION_FLYDIGI_DONGLE_2  == REV)
                     || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_3 == PID && REVISION_FLYDIGI_DONGLE_3A == REV)
                     || (ConnectionType == EConnectionType.Dongle && VENDOR_FLYDIGI == VID && PRODUCT_FLYDIGI_DONGLE_3 == PID && REVISION_FLYDIGI_DONGLE_3B == REV))
                    {
                        if (DetectedDeviceId.HasValue)
                        {
                            if (attr.DeviceIdFromFeature.HasValue && attr.DeviceIdFromFeature.Value == DetectedDeviceId.Value)
                            {
                                ManualSelectionList.Clear();
                                Log("UpdateAlternateSubTypes End", Indent: false);
                                return;
                            }
                        }
                        else
                        {
                            if (!attr.DeviceIdFromFeature.HasValue)
                            {
                                Rank += EXPECTS_NO_ID;
                            }
                        }
                    }
                    if (subType != FlyDigiSubType.None && subType != FlyDigiSubType.Unknown)
                    {
                        bool Matches = true;
                        if (ReportFESubType.HasValue && attr.ReportFESubType.HasValue)
                            if (ReportFESubType.Value == attr.ReportFESubType.Value)
                            {
                                Rank += REPORT_BYTES_MATCH;
                            }
                            else
                            {
                                Matches = false;
                            }
                        if (FixedValueFromByte28.HasValue && attr.FixedValueFromByte28.HasValue)
                            if (FixedValueFromByte28.Value == attr.FixedValueFromByte28.Value)
                            {
                                Rank += REPORT_BYTES_MATCH;
                            }
                            else
                            {
                                Matches = false;
                            }
                        //if (VersionFromByte30.HasValue && attr.VersionFromByte30.HasValue)
                        //    if (VersionFromByte30.Value == attr.VersionFromByte30.Value)
                        //    {
                        //        Rank += REPORT_BYTES_MATCH;
                        //    }
                        //    else
                        //    {
                        //        Matches = false;
                        //    }
                        //if (FixedValueFromByte31.HasValue && attr.FixedValueFromByte31.HasValue)
                        //    if (FixedValueFromByte31.Value == attr.FixedValueFromByte31.Value)
                        //    {
                        //        Rank += REPORT_BYTES_MATCH;
                        //    }
                        //    else
                        //    {
                        //        Matches = false;
                        //    }

                        if (Matches)
                        {
                            //Rank += TYPE_AUTH - (int)subType;
                            Candidates.Add(new Tuple<int, FlyDigiSubType>(Rank, subType));
                        }
                    }
                }

                ManualSelectionList.AddRange(Candidates.GroupBy(dr => dr.Item1).OrderByDescending(dr => dr.Key).First().Select(dr => dr.Item2));
                if (ManualSelectionList.Count == 1 && ManualSelectionList[0] == ControllerSubType)
                    ManualSelectionList.Clear();
            }
            Log("UpdateAlternateSubTypes End", Indent: false);
        }

        private void ChangeControllerSubType(FlyDigiSubType NewControllerSubType)
        {
            Log($"ChangeControllerSubType Start {NewControllerSubType}", ConsoleColor.Green, true);

            if (ControllerSubType == NewControllerSubType && ControlsCreated)
            {
                Log($"ChangeControllerSubType End {NewControllerSubType}", ConsoleColor.Green, false);
                return;
            }

            StateMutationLock.Wait(); NoteStateMutationLock();

            ControllerSubType = NewControllerSubType;
            ControllerAttribute = ControllerSubType.GetAttribute<ControllerSubTypeAttribute>();

            try
            {
                if (NewControllerSubType == FlyDigiSubType.None)
                {
                    MetadataMutationLock.Wait(); NoteMetadataMutationLock();
                    try
                    {
                        Log("DetectedDeviceId set to null", ConsoleColor.DarkYellow);
                        DetectedDeviceId = null;
                        ReportFESubType = null;
                        FixedValueFromByte28 = null;
                        //VersionFromByte30 = null;
                        FixedValueFromByte31 = null;
                        NoMetaForThisController = false;
                    }
                    finally
                    {
                        MetadataMutationLock.Release();
                    }
                }

                {
                    if (!ControlsCreated)
                    {
                        // universal fixed controls that all Flydigi controllers have, these won't change
                        State.Controls["cluster_left"] = new ControlDPad();
                        State.Controls["stick_left"] = new ControlStickWithClick();
                        State.Controls["stick_right"] = new ControlStickWithClick();
                        State.Controls["bumper_left"] = new ControlButton();
                        State.Controls["bumper_right"] = new ControlButton();
                        State.Controls["menu_left"] = new ControlButton();
                        State.Controls["menu_right"] = new ControlButton();

                        ControlsCreated = true;
                    }

                    if (ControllerAttribute.HasAnalogTrigger)
                    {
                        State.Controls["trigger_left"] = new ControlTrigger();
                        State.Controls["trigger_right"] = new ControlTrigger();
                        Log("Trigger set to Analog");
                    }
                    else
                    {
                        State.Controls["trigger_left"] = new ControlButton();
                        State.Controls["trigger_right"] = new ControlButton();
                        Log("Trigger set to Digital");
                    }

                    if (ControllerAttribute.HasLogo)
                    {
                        State.Controls["logo"] = new ControlButton();
                    }
                    else
                    {
                        State.Controls["logo"] = null;
                    }

                    if (ControllerAttribute.HasTopMenu || ControllerAttribute.HasBottomMenu)
                    {
                        State.Controls["cluster_middle"] = new ControlButtonGrid(3, 1);
                    }
                    else
                    {
                        State.Controls["cluster_middle"] = null;
                    }

                    if (ControllerAttribute.HasCZBottom)
                    {
                        State.Controls["c"] = new ControlButton();
                        State.Controls["z"] = new ControlButton();
                    }
                    else
                    {
                        State.Controls["c"] = null;
                        State.Controls["z"] = null;
                    }

                    if (ControllerAttribute.HasCZTop)
                    {
                        State.Controls["c_top"] = new ControlButton();
                        State.Controls["z_top"] = new ControlButton();
                    }
                    else
                    {
                        State.Controls["c_top"] = null;
                        State.Controls["z_top"] = null;
                    }

                    if (ControllerAttribute.HasMRockers)
                    {
                        State.Controls["grip_right"] = new ControlRocker();
                        State.Controls["grip_left"] = new ControlRocker();
                        State.Controls["grip_right_left"] = null;
                        State.Controls["grip_right_right"] = null;
                        State.Controls["grip_left_left"] = null;
                        State.Controls["grip_left_right"] = null;
                    }
                    else if (ControllerAttribute.HasMButtons)
                    {
                        State.Controls["grip_right"] = null;
                        State.Controls["grip_left"] = null;
                        State.Controls["grip_right_left"] = new ControlButton();
                        State.Controls["grip_right_right"] = new ControlButton();
                        State.Controls["grip_left_left"] = new ControlButton();
                        State.Controls["grip_left_right"] = new ControlButton();
                    }
                    else
                    {
                        State.Controls["grip_right"] = null;
                        State.Controls["grip_left"] = null;
                        State.Controls["grip_right_left"] = null;
                        State.Controls["grip_right_right"] = null;
                        State.Controls["grip_left_left"] = null;
                        State.Controls["grip_left_right"] = null;
                    }

                    if (ControllerAttribute.HasBumper2)
                    {
                        State.Controls["shoulder_a_left"] = new ControlButton();
                        State.Controls["shoulder_a_right"] = new ControlButton();
                    }
                    else
                    {
                        State.Controls["shoulder_a_left"] = null;
                        State.Controls["shoulder_a_right"] = null;
                    }

                    if (ControllerAttribute.HasWheel)
                    {
                        State.Controls["cluster_right"] = new ControlButtonQuad(); // wheel
                    }
                    else
                    {
                        State.Controls["cluster_right"] = new ControlButtonQuad();
                    }
                }
            }
            finally
            {
                StateMutationLock.Release();
            }

            UpdateAlternateSubTypes();

            ControllerMetadataUpdate?.Invoke(this);
            Log($"ChangeControllerSubType End {NewControllerSubType}", ConsoleColor.Green, false);
        }

        private object OutLock = new object();
        private Dictionary<int, int> indent = new Dictionary<int, int>();
        private void Log(string line, ConsoleColor color = ConsoleColor.Gray, bool? Indent = null, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            lock (OutLock)
            {
                if (!(Indent ?? true))
                {
                    if (indent.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                    {
                        indent[Thread.CurrentThread.ManagedThreadId]--;
                        if (indent[Thread.CurrentThread.ManagedThreadId] == 0)
                            indent.Remove(Thread.CurrentThread.ManagedThreadId);
                    }
                }
                Console.ForegroundColor = color;
                Console.WriteLine($"{lineNumber.ToString().PadLeft(4)}  {Thread.CurrentThread.ManagedThreadId.ToString().PadLeft(4)}  {new string(' ', (indent.ContainsKey(Thread.CurrentThread.ManagedThreadId) ? indent[Thread.CurrentThread.ManagedThreadId] : 0) * 4)}  {line}");
                Console.ResetColor();
                if (Indent ?? false)
                {
                    if (!indent.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                        indent[Thread.CurrentThread.ManagedThreadId] = 0;
                    indent[Thread.CurrentThread.ManagedThreadId]++;
                }
            }
        }


        int _NoteStateMutationLock = 0;
        private void NoteStateMutationLock([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            _NoteStateMutationLock = lineNumber;
        }
        int _NoteMetadataMutationLock = 0;
        private void NoteMetadataMutationLock([System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            _NoteMetadataMutationLock = lineNumber;
        }

        public bool SetControlState(string control, string state)
        {
            return false;
        }
    }
}
