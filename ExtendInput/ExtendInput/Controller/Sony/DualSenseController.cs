using Brod.Common.Utilities;
using ExtendInput.Controls;
using ExtendInput.DataTools.DualSense;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ExtendInput.Controller.Sony
{
    public class DualSenseController : IController
    {
        public const int VendorId = 0x054C;
        public static int ProductId = 0x0CE6;

        private const byte _REPORT_STATE_USB = 0x02;
        private const byte _REPORT_STATE_1 = 0x31;
        private const byte _REPORT_STATE_2 = 0x32;
        private const byte _REPORT_STATE_3 = 0x33;
        private const byte _REPORT_STATE_4 = 0x34;
        private const byte _REPORT_STATE_5 = 0x35;
        private const byte _REPORT_STATE_6 = 0x36;
        private const byte _REPORT_STATE_7 = 0x37;
        private const byte _REPORT_STATE_8 = 0x38;
        private const byte _REPORT_STATE_9 = 0x39;

        enum MuteLight : byte
        {
            Off = 0,
            On,
            Breathing,
        };
        enum LightBrightness : byte
        {
            Bright = 0,
            Mid,
            Dim,
        };
        enum LightFadeAnimation : byte
        {
            Nothing = 0,
            FadeIn, // from black to blue
            FadeOut // from blue to black
        };

        bool OutputThreadActive = false;
        Thread OutputThread;
        unsafe private void StartOutputThread()
        {
            outBuffer = new SetStateData();
            OutputThreadActive = true;
            OutputThread = new Thread(() =>
            {
                for (; ; )
                {
                    if (!OutputThreadActive) break;
                    Thread.Sleep(1);
                    if (!OutputThreadActive) break;
                    //if(WriteStateDirtyPossible)
                    {
                        bool DataToWrite = false;
                        {
                            IControlButtonWithStateLight ctrl = (State.Controls["mute"] as IControlButtonWithStateLight);
                            if (ctrl != null && ctrl.IsWriteDirty)
                            {
                                for (int i = 0; i < ctrl.States.Length; i++)
                                {
                                    if (ctrl.States[i] == ctrl.State)
                                    {
                                        if (!DataToWrite) outBuffer = new SetStateData();
                                        outBuffer.AllowMuteLight = true;
                                        outBuffer.MuteLightMode = (MuteLight)i;
                                        DataToWrite = true;
                                        ctrl.CleanWriteDirty();
                                        break;
                                    }
                                }
                            }
                        }
                        {
                            IControlTriggerPS5 ctrl = (State.Controls["trigger_right"] as IControlTriggerPS5);
                            if (ctrl != null && ctrl.IsWriteDirty)
                            {
                                if (!DataToWrite) outBuffer = new SetStateData();
                                outBuffer.AllowRightTriggerFFB = true;
                                fixed (byte* FFBData = outBuffer.RightTriggerFFB)
                                {
                                    switch (ctrl.Effect)
                                    {
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_NONE: TriggerEffectGenerator.Off(FFBData, 0); break;
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_FEEDBACK: TriggerEffectGenerator.Feedback(FFBData, 0, ctrl.Start, ctrl.Resistance); break;
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_WEAPON: TriggerEffectGenerator.Weapon(FFBData, 0, ctrl.Start, ctrl.End, ctrl.Resistance); break;
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_VIBRATION: TriggerEffectGenerator.Vibration(FFBData, 0, ctrl.Start, ctrl.Amplitude, ctrl.Frequency); break;
                                    }
                                }
                                DataToWrite = true;
                            }
                        }
                        {
                            IControlTriggerPS5 ctrl = (State.Controls["trigger_left"] as IControlTriggerPS5);
                            if (ctrl != null && ctrl.IsWriteDirty)
                            {
                                if (!DataToWrite) outBuffer = new SetStateData();
                                outBuffer.AllowLeftTriggerFFB = true;
                                fixed (byte* FFBData = outBuffer.LeftTriggerFFB)
                                {
                                    switch (ctrl.Effect)
                                    {
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_NONE: TriggerEffectGenerator.Off(FFBData, 0); break;
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_FEEDBACK: TriggerEffectGenerator.Feedback(FFBData, 0, ctrl.Start, ctrl.Resistance); break;
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_WEAPON: TriggerEffectGenerator.Weapon(FFBData, 0, ctrl.Start, ctrl.End, ctrl.Resistance); break;
                                        case EEffectTriggerForceFeedbackPS5.STATE_PS5_TRIGGER_VIBRATION: TriggerEffectGenerator.Vibration(FFBData, 0, ctrl.Start, ctrl.Amplitude, ctrl.Frequency); break;
                                    }
                                }
                                DataToWrite = true;
                            }
                        }
                        if (DataToWrite)
                            SendReport();

                        //WriteStateDirtyPossible = false;
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
        SetStateData outBuffer;// = new SetStateData();
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        unsafe struct SetStateData
        {
            public byte Flags1;
            public byte Flags2;
            public bool EnableRumbleEmulation { get { return (Flags1 & 0x01) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x01) : (Flags1 & ~0x01)); } }
            public bool UseRumbleNotHaptics   { get { return (Flags1 & 0x02) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x02) : (Flags1 & ~0x02)); } }

            public bool AllowRightTriggerFFB { get { return (Flags1 & 0x04) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x04) : (Flags1 & ~0x04)); } }
            public bool AllowLeftTriggerFFB  { get { return (Flags1 & 0x08) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x08) : (Flags1 & ~0x08)); } }

            public bool AllowHeadphoneVolume { get { return (Flags1 & 0x10) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x10) : (Flags1 & ~0x10)); } }
            public bool AllowSpeakerVolume   { get { return (Flags1 & 0x20) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x20) : (Flags1 & ~0x20)); } }
            public bool AllowMicVolume       { get { return (Flags1 & 0x40) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x40) : (Flags1 & ~0x40)); } }

            public bool AllowAudioControl { get { return (Flags1 & 0x80) != 0; } set { Flags1 = (byte)(value ? (Flags1 | 0x80) : (Flags1 & ~0x80)); } }
            public bool AllowMuteLight    { get { return (Flags2 & 0x01) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x01) : (Flags2 & ~0x01)); } }
            public bool AllowAudioMute    { get { return (Flags2 & 0x02) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x02) : (Flags2 & ~0x02)); } }

            public bool AllowLedColor { get { return (Flags2 & 0x04) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x04) : (Flags2 & ~0x04)); } }

            public bool ResetLights { get { return (Flags2 & 0x08) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x08) : (Flags2 & ~0x08)); } }

            public bool AllowPlayerIndicators    { get { return (Flags2 & 0x10) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x10) : (Flags2 & ~0x10)); } }
            public bool AllowHapticLowPassFilter { get { return (Flags2 & 0x20) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x20) : (Flags2 & ~0x20)); } }
            public bool AllowMotorPowerLevel     { get { return (Flags2 & 0x40) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x40) : (Flags2 & ~0x40)); } }
            public bool AllowAudioControl2       { get { return (Flags2 & 0x80) != 0; } set { Flags2 = (byte)(value ? (Flags2 | 0x80) : (Flags2 & ~0x80)); } }

            public byte RumbleEmulationRight;
            public byte RumbleEmulationLeft;

            public byte VolumeHeadphones;
            public byte VolumeSpeaker;
            public byte VolumeMic;

            public byte AudioControl;
            public byte MicSelect { get { return (byte)(AudioControl & 0x03); } set { AudioControl = (byte)((AudioControl & 0xFC) | (value & 0x04)); } }

            public bool EchoCancelEnable  { get { return (AudioControl & 0x04) != 0; } set { AudioControl = (byte)(value ? (AudioControl | 0x04) : (AudioControl & ~0x04)); } }
            public bool NoiseCancelEnable { get { return (AudioControl & 0x08) != 0; } set { AudioControl = (byte)(value ? (AudioControl | 0x08) : (AudioControl & ~0x08)); } }
            public byte OutputPathSelect { get { return (byte)(AudioControl & 0x30); } set { AudioControl = (byte)((AudioControl & 0xCF) | (value & 0x30 >> 4)); } }
            public byte InputPathSelect  { get { return (byte)(AudioControl & 0xC0); } set { AudioControl = (byte)((AudioControl & 0x3F) | (value & 0xC0 >> 6)); } }

            public MuteLight MuteLightMode;

            public byte MuteControl;
            public bool TouchPowerSave  { get { return (MuteControl & 0x01) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x01) : (MuteControl & ~0x01)); } }
            public bool MotionPowerSave { get { return (MuteControl & 0x02) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x02) : (MuteControl & ~0x02)); } }
            public bool HapticPowerSave { get { return (MuteControl & 0x04) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x04) : (MuteControl & ~0x04)); } }
            public bool AudioPowerSave  { get { return (MuteControl & 0x08) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x08) : (MuteControl & ~0x08)); } }
            public bool MicMute         { get { return (MuteControl & 0x10) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x10) : (MuteControl & ~0x10)); } }
            public bool SpeakerMute     { get { return (MuteControl & 0x20) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x20) : (MuteControl & ~0x20)); } }
            public bool HeadphoneMute   { get { return (MuteControl & 0x40) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x40) : (MuteControl & ~0x40)); } }
            public bool HapticMute      { get { return (MuteControl & 0x80) != 0; } set { MuteControl = (byte)(value ? (MuteControl | 0x80) : (MuteControl & ~0x80)); } }

            public fixed byte RightTriggerFFB[11];
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            //public byte[] RightTriggerFFB;
            public fixed byte LeftTriggerFFB[11];
            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
            //public byte[] LeftTriggerFFB;
            public UInt32 HostTimestamp;

            public byte MotorPowerLevel;
            //public byte TriggerMotorPowerReduction : 4;
            //public byte RumbleMotorPowerReduction : 4;

            public byte AudioControl2;
            //public byte SpeakerCompPreGain: 3;
            //public byte BeamformingEnable: 1;
            //public byte UnkAudioControl2: 4;

            public byte LightAnimationFlags;
            //public byte AllowLightBrightnessChange: 1;
            //public byte AllowColorLightFadeAnimation: 1;
            //public byte UNKBITC: 6;

            public byte HapticFlags;
            //public byte HapticLowPassFilter: 1;
            //public byte UNKBIT: 7;

            public byte UNKBYTE;

            public LightFadeAnimation LightFadeAnimation;
            public LightBrightness LightBrightness;

            public byte PlayerLight;
            //byte PlayerLight1 : 1;
            //byte PlayerLight2 : 1;
            //byte PlayerLight3 : 1;
            //byte PlayerLight4 : 1;
            //byte PlayerLight5 : 1;
            //byte PlayerLightFade: 1;
            //byte PlayerLightUNK : 2;

            public byte LedRed;
            public byte LedGreen;
            public byte LedBlue;
        };

        #region String Definitions
        private const string ATOM_CONNECTION_WIRE = "CONNECTION_WIRE";
        private const string ATOM_CONNECTION_USB_WIRE = "CONNECTION_WIRE_USB";
        private const string ATOM_CONNECTION_BT = "CONNECTION_BT";
        //private const string ATOM_CONNECTION_DONGLE = "CONNECTION_DONGLE";
        //private const string ATOM_CONNECTION_DS4_DONGLE = "CONNECTION_DONGLE_DS5";
        private const string ATOM_CONNECTION_UNKKNOWN = "CONNECTION_UNKNOWN";

        private readonly string[] _CONNECTION_WIRE = new string[] { ATOM_CONNECTION_USB_WIRE, ATOM_CONNECTION_WIRE };
        private readonly string[] _CONNECTION_BT = new string[] { ATOM_CONNECTION_BT };
        //private readonly string[] _CONNECTION_DONGLE = new string[] { ATOM_CONNECTION_DS4_DONGLE, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_UNKKNOWN = new string[] { ATOM_CONNECTION_UNKKNOWN };
        #endregion String Definitions

        public bool SensorsEnabled;
        private HidDevice _device;
        int reportUsageLock = 0;
        private byte last_touch_timestamp;
        private bool touch_last_frame;
        //private DateTime tmp = DateTime.Now;

        //bool WriteStateDirtyPossible = false; // TODO consider a thread control mechanism instead

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        public bool HasMotion => true;

        public bool IsReady => true;
        public bool IsPresent => true;
        public bool IsVirtual => false;


        public AccessMode AccessMode { get; private set; }
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
                    //case EConnectionType.Dongle: return _CONNECTION_DONGLE;
                    default: return _CONNECTION_UNKKNOWN;
                }
            }
        }
        public string[] ControllerTypeCode
        {
            get
            {
                return new string[] { "DEVICE_DS5" };

            }
        }
        public string Name => "Sony DUALSENSE Controller";
        public string[] NameDetails
        {
            get
            {
                string Serial = null;

                if (_device.VendorId == VendorId
                && (_device.ProductId == ProductId))
                {
                    try
                    {
                        byte[] data;
                        _device.ReadFeatureData(out data, 0x09);
                        Serial = string.Join(":", data.Skip(1).Take(6).Reverse().Select(dr => $"{dr:X2}").ToArray());
                    }
                    catch { }
                }
                if (string.IsNullOrWhiteSpace(Serial))
                {
                    string SerialNumber = _device.ReadSerialNumber();
                    if (!string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        Serial = SerialNumber;
                    }
                }
                if (string.IsNullOrWhiteSpace(Serial))
                    Serial = null;

                return new string[] { $"[{Serial ?? "No ID"}]" };
            }
        }
        public Dictionary<string, dynamic> DeviceProperties => _device.Properties;
        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;

        //public IDevice DeviceHackRef => _device;

        ControllerState State = new ControllerState();


        private void SendReport()
        {
            {
                if (ConnectionType == EConnectionType.Bluetooth)
                {
                    byte[] outDataFinal = new byte[79];
                    outDataFinal[0] = 0xa2;
                    outDataFinal[1] = _REPORT_STATE_1;
                    outDataFinal[2] = 0x02;
                    Tools.ConvertToBytes<SetStateData>(outBuffer, ref outDataFinal, 3);

                    Crc32 crcE = new Crc32();
                    byte[] crc = crcE.ComputeHash(outDataFinal, 0, outDataFinal.Length - 4);

                    outDataFinal[outDataFinal.Length - 1] = crc[0];
                    outDataFinal[outDataFinal.Length - 2] = crc[1];
                    outDataFinal[outDataFinal.Length - 3] = crc[2];
                    outDataFinal[outDataFinal.Length - 4] = crc[3];

                    bool success = _device.WriteReport(outDataFinal.Skip(1).ToArray());
                }
                else
                {
                    byte[] outDataFinal = new byte[48];
                    outDataFinal[0] = _REPORT_STATE_USB;
                    Tools.ConvertToBytes<SetStateData>(outBuffer, ref outDataFinal, 1);

                    bool success = _device.WriteReport(outDataFinal);
                }
            }
        }

        public DualSenseController(HidDevice device, AccessMode AccessMode, EConnectionType ConnectionType = EConnectionType.Unknown)
        {
            this.AccessMode = AccessMode;
            this.ConnectionType = ConnectionType;

            State.Controls["cluster_left"] = new ControlDPad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumper_left"] = new ControlButton();
            State.Controls["bumper_right"] = new ControlButton();
            //State.Controls["bumpers2"] = new ControlButtonPair();
            //State.Controls["trigger_left"] = new ControlTrigger();
            //State.Controls["trigger_right"] = new ControlTrigger();
            State.Controls["trigger_left"] = new ControlTriggerPS5(AccessMode);
            State.Controls["trigger_right"] = new ControlTriggerPS5(AccessMode);
            State.Controls["menu_left"] = new ControlButton();
            State.Controls["menu_right"] = new ControlButton();
            State.Controls["home"] = new ControlButton();
            State.Controls["mute"] = new ControlButtonPS5Mute(AccessMode);
            State.Controls["stick_left"] = new ControlStickWithClick();
            State.Controls["stick_right"] = new ControlStickWithClick();
            State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true) { PhysicalWidth = 50, PhysicalHeight = 28, };
            State.Controls["motion"] = new ControlMotion();

            // According to this the normalized domain of the DS4 gyro is 1024 units per rad/s: https://gamedev.stackexchange.com/a/87178

            _device = device;
            /*
            //new Thread(() =>
            {
                //Thread.Sleep(1);
                {
                    byte oldFlags = data[2];
                    data[2] |= 0x08;
                    SendReport();
                    data[2] = oldFlags;
                }
                //Thread.Sleep(1);
                {
                    byte oldFlags = data[2];
                    data[2] |= 0x04;
                    data[45] = 0xff;// (byte)random.Next(0, 255);
                    data[46] = 0x00;// (byte)random.Next(0, 255);
                    data[47] = 0x00;// (byte)random.Next(0, 255);
                    SendReport();
                    data[2] = oldFlags;
                }
            }//).Start();
            */

            PollingState = EPollingState.Inactive;

            _device.DeviceReport += OnReport;
            State.ControllerStateUpdate += State_ControllerStateUpdate;
        }
        public void Dispose()
        {
            switch (PollingState)
            {
                case EPollingState.Active:
                case EPollingState.SlowPoll:
                case EPollingState.RunOnce:
                    {
                        _device.StopReading();

                        PollingState = EPollingState.Inactive;
                        _device.CloseDevice();
                    }
                    break;
            }
        }

        public void Initalize()
        {
            if (PollingState == EPollingState.Active) return;

            touch_last_frame = false;

            PollingState = EPollingState.Active;
            _device.StartReading();
            StartOutputThread();
        }

        public void DeInitalize()
        {
            if (PollingState == EPollingState.Inactive) return;
            if (PollingState == EPollingState.RunOnce) return;
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
                StopOutputThread();
                _device.CloseDevice();
            }
        }

        public async void Identify()
        {
            /*byte[] report;
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
            }*/
        }



        private void State_ControllerStateUpdate(ControlCollection controls)
        {
            ControllerStateUpdate?.Invoke(this, controls);
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
                    int baseOffset = 0;
                    bool HasStateData = true;
                    if (ConnectionType == EConnectionType.Bluetooth && reportData.ReportId == 0x01)
                    {
                        State.StartStateChange();
                        try
                        {
                            (State.Controls["stick_left"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 0] - 128) / 128f;
                            (State.Controls["stick_left"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 1] - 128) / 128f;
                            (State.Controls["stick_right"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 2] - 128) / 128f;
                            (State.Controls["stick_right"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 3] - 128) / 128f;

                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[baseOffset + 4] & 128) == 128;
                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[baseOffset + 4] & 64) == 64;
                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[baseOffset + 4] & 32) == 32;
                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[baseOffset + 4] & 16) == 16;

                            switch ((reportData.ReportBytes[baseOffset + 4] & 0x0f))
                            {
                                case 0: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North; break;
                                case 1: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast; break;
                                case 2: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East; break;
                                case 3: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast; break;
                                case 4: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South; break;
                                case 5: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest; break;
                                case 6: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West; break;
                                case 7: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest; break;
                                default: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None; break;
                            }

                            (State.Controls["stick_right"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 5] & 128) == 128;
                            (State.Controls["stick_left"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 5] & 64) == 64;
                            (State.Controls["menu_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 32) == 32;
                            (State.Controls["menu_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 16) == 16;
                            //(StateInFlight.Controls["bumpers2"] as IControlButton).Right.Button0 = (reportData.ReportBytes[baseOffset + 5] & 8) == 8;
                            //(StateInFlight.Controls["bumpers2"] as IControlButton).Left.Button0 = (reportData.ReportBytes[baseOffset + 5] & 4) == 4;
                            (State.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 2) == 2;
                            (State.Controls["bumper_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 1) == 1;

                            // counter
                            // bld.Append((reportData.ReportBytes[baseOffset + 6] & 0xfc).ToString().PadLeft(3, '0'));

                            (State.Controls["home"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 6] & 0x1) == 0x1;
                            (State.Controls["touch_center"] as ControlTouch).Click = (reportData.ReportBytes[baseOffset + 6] & 0x2) == 0x2;
                            (State.Controls["trigger_left"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 7] / byte.MaxValue;
                            (State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 8] / byte.MaxValue;
                        }
                        finally
                        {
                            State.EndStateChange(true);
                        }
                    }
                    else
                    {
                        if (ConnectionType == EConnectionType.Bluetooth)
                        {
                            baseOffset = 1;
                            HasStateData = (reportData.ReportBytes[0] & 0x01) == 0x01;
                        }

                        if (HasStateData)
                        {
                            State.StartStateChange();
                            try
                            {
                                (State.Controls["stick_left"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 0] - 128) / 128f;
                                (State.Controls["stick_left"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 1] - 128) / 128f;
                                (State.Controls["stick_right"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 2] - 128) / 128f;
                                (State.Controls["stick_right"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 3] - 128) / 128f;

                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[baseOffset + 7] & 128) == 128;
                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[baseOffset + 7] & 64) == 64;
                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[baseOffset + 7] & 32) == 32;
                                (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[baseOffset + 7] & 16) == 16;

                                switch ((reportData.ReportBytes[baseOffset + 7] & 0x0f))
                                {
                                    case 0: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North; break;
                                    case 1: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast; break;
                                    case 2: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East; break;
                                    case 3: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast; break;
                                    case 4: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South; break;
                                    case 5: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest; break;
                                    case 6: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West; break;
                                    case 7: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest; break;
                                    default: (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None; break;
                                }

                                (State.Controls["stick_right"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 8] & 128) == 128;
                                (State.Controls["stick_left"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 8] & 64) == 64;
                                (State.Controls["menu_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 32) == 32;
                                (State.Controls["menu_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 16) == 16;
                                //(StateInFlight.Controls["bumpers2"] as ControlButtonPair).Right.Button0 = (reportData.ReportBytes[baseOffset + 8] & 8) == 8;
                                //(StateInFlight.Controls["bumpers2"] as ControlButtonPair).Left.Button0 = (reportData.ReportBytes[baseOffset + 8] & 4) == 4;
                                (State.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 2) == 2;
                                (State.Controls["bumper_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 1) == 1;

                                (State.Controls["home"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 9] & 0x1) == 0x1;
                                (State.Controls["touch_center"] as ControlTouch).Click = (reportData.ReportBytes[baseOffset + 9] & 0x2) == 0x2;
                                (State.Controls["mute"] as IControlButtonWithStateLight).DigitalStage1 = (reportData.ReportBytes[baseOffset + 9] & 0x4) == 0x4;
                                (State.Controls["trigger_left"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 4] / byte.MaxValue;
                                (State.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 5] / byte.MaxValue;

                                (State.Controls["trigger_left" ] as IControlTriggerPS5).TriggerStop   = (byte)(reportData.ReportBytes[baseOffset + 42] & 0x0f);
                                (State.Controls["trigger_left" ] as IControlTriggerPS5).TriggerStatus = (byte)((reportData.ReportBytes[baseOffset + 42] & 0xf0) >> 4);
                                (State.Controls["trigger_right"] as IControlTriggerPS5).TriggerStop   = (byte)(reportData.ReportBytes[baseOffset + 41] & 0x0f);
                                (State.Controls["trigger_right"] as IControlTriggerPS5).TriggerStatus = (byte)((reportData.ReportBytes[baseOffset + 41] & 0xf0) >> 4);

                                (State.Controls["motion"] as ControlMotion).AngularVelocityX = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 15);
                                (State.Controls["motion"] as ControlMotion).AngularVelocityZ = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 17);
                                (State.Controls["motion"] as ControlMotion).AngularVelocityY = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 19);
                                (State.Controls["motion"] as ControlMotion).AccelerometerX = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 21);
                                (State.Controls["motion"] as ControlMotion).AccelerometerY = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 23);
                                (State.Controls["motion"] as ControlMotion).AccelerometerZ = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 25);

                                //bool DisconnectedFlag = (reportData.ReportBytes[baseOffset + 30] & 0x04) == 0x04;
                                //if (DisconnectedFlag != DisconnectedBit)
                                //{
                                //    DisconnectedBit = DisconnectedFlag;
                                //    ControllerNameUpdated?.Invoke();
                                //}

                                //int TouchDataCount = reportData.ReportBytes[baseOffset + 32];
                                int TouchDataCount = 1;

                                for (int FingerCounter = 0; FingerCounter < TouchDataCount; FingerCounter++)
                                {
                                    bool Finger1 = (reportData.ReportBytes[baseOffset + 32 + (FingerCounter * 9)] & 0x80) != 0x80;
                                    byte Finger1Index = (byte)(reportData.ReportBytes[baseOffset + 32 + (FingerCounter * 9)] & 0x7f);
                                    int F1X = reportData.ReportBytes[baseOffset + 33 + (FingerCounter * 9)]
                                          | ((reportData.ReportBytes[baseOffset + 34 + (FingerCounter * 9)] & 0xF) << 8);
                                    int F1Y = ((reportData.ReportBytes[baseOffset + 34 + (FingerCounter * 9)] & 0xF0) >> 4)
                                             | (reportData.ReportBytes[baseOffset + 35 + (FingerCounter * 9)] << 4);

                                    bool Finger2 = (reportData.ReportBytes[baseOffset + 36 + (FingerCounter * 9)] & 0x80) != 0x80;
                                    byte Finger2Index = (byte)(reportData.ReportBytes[baseOffset + 36 + (FingerCounter * 9)] & 0x7f);
                                    int F2X = reportData.ReportBytes[baseOffset + 37 + (FingerCounter * 9)]
                                          | ((reportData.ReportBytes[baseOffset + 38 + (FingerCounter * 9)] & 0xF) << 8);
                                    int F2Y = ((reportData.ReportBytes[baseOffset + 38 + (FingerCounter * 9)] & 0xF0) >> 4)
                                             | (reportData.ReportBytes[baseOffset + 39 + (FingerCounter * 9)] << 4);

                                    byte touch_timestamp = reportData.ReportBytes[baseOffset + 41 + (FingerCounter * 9)]; // Touch Pad Counter
                                                                                                                          //DateTime tmp_now = DateTime.Now;

                                    byte TimeDelta = touch_last_frame ? GetOverflowedDelta(last_touch_timestamp, touch_timestamp) : (byte)0;

                                    (State.Controls["touch_center"] as ControlTouch).AddTouch(0, Finger1, (F1X / 1919f) * 2f - 1f, (F1Y / 1079f) * 2f - 1f, TimeDelta); // everything beyond 1073 is not reliably producible
                                    (State.Controls["touch_center"] as ControlTouch).AddTouch(1, Finger2, (F2X / 1919f) * 2f - 1f, (F2Y / 1079f) * 2f - 1f, TimeDelta); // everything beyond 1073 is not reliably producible

                                    last_touch_timestamp = touch_timestamp;
                                }

                                touch_last_frame = TouchDataCount > 0;
                            }
                            finally
                            {
                                State.EndStateChange(true);
                            }
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }
            }
        }

        // Pure function
        private byte GetOverflowedDelta(byte prev, byte cur, uint overflow = byte.MaxValue + 1)
        {
            uint _cur = cur;
            while (_cur < prev)
                _cur += overflow;
            return (byte)(_cur - prev);
        }

        /*private void DeviceAttachedHandler()
        {
            lock (controllerStateLock)
            {
                _attached = true;
                Console.WriteLine("VSC Address Attached");
                _device.ReadReport(OnReport);
            }
        }

        private void DeviceRemovedHandler()
        {
            lock (controllerStateLock)
            {
                _attached = false;
                Console.WriteLine("VSC Address Removed");
            }
        }*/

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
    }
}
