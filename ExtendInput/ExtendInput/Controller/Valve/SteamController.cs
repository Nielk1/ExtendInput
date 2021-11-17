using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace ExtendInput.Controller.Valve
{
    public class SteamController : IController
    {
        public bool EnableMotion { get; set; } // temporary hack for VSCView

        public const int VendorId = 0x28DE; // 10462
        public const int ProductIdDongle = 0x1142; // 4418
        public const int ProductIdWired = 0x1102; // 4354
        public const int ProductIdChell = 0x1101; // 4353
        public const int ProductIdBT = 0x1106; // 4358

        const byte k_nKeyboardReportNumber = 0x02;
        const byte k_nMouseReportNumber = 0x01;

        const byte k_nSegmentHasDataFlag = 0x80;
        const byte k_nLastSegmentFlag = 0x40;
        const byte k_nSegmentNumberMask = 0x07;

        const float PadAngle = 0.261799f; // 15 deg in radians

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
                return SerialNumber;
            }
        }
        public string[] ConnectionTypeCode
        {
            get
            {
                switch (this.ConnectionType)
                {
                    case EConnectionType.USB:
                        return new string[] { "CONNECTION_WIRE_USB", "CONNECTION_WIRE" };
                    case EConnectionType.Bluetooth:
                        return new string[] { "CONNECTION_BT" };
                    case EConnectionType.Dongle:
                        return new string[] { "CONNECTION_DONGLE_SC", "CONNECTION_DONGLE" };
                    default:
                        return new string[] { "CONNECTION_UNKNOWN" };
                }
            }
        }
        public string[] ControllerTypeCode
        {
            get
            {
                if (ConnectionType == EConnectionType.Dongle)
                {
                    switch (ConState)
                    {
                        case InternalConState.Disconnected: return new string[] { "DEVICE_NONE" };
                        case InternalConState.Unknown: return new string[] { "DEVICE_UNKNOWN" };
                    }
                }

                switch(ControllerType)
                {
                    case EControllerType.Chell: return new string[] { "DEVICE_SC_CHELL", "DEVICE_GAMEPAD" };
                    case EControllerType.Gordon: return new string[] { "DEVICE_SC", "DEVICE_GAMEPAD" }; ;
                }

                return new string[] { "DEVICE_UNKNOWN" };
            }
        }
        public string Name
        {
            get
            {
                if (ConnectionType == EConnectionType.Dongle)
                    switch (ConState)
                    {
                        case InternalConState.Unknown:
                        case InternalConState.Disconnected:
                            return "Valve Steam Controller Dongle";
                    }

                string retVal = "Valve Steam Controller";
                switch (ControllerType)
                {
                    case EControllerType.Gordon: retVal += " Gordon"; break;
                    //case EControllerType.ReleaseV2: retVal += " V2"; break;
                    case EControllerType.Chell: retVal += " Chell"; break;
                }
                return retVal;
            }
        }

        public string[] NameDetails
        {
            get
            {
                if (ConnectionType == EConnectionType.Dongle)
                    switch (ConState)
                    {
                        case InternalConState.Unknown:
                            return new string[] { $"Unknown State" };
                        case InternalConState.Disconnected:
                            return new string[] { $"No Controller" };
                    }

                return new string[] { $"[{SerialNumber ?? "No ID"}]" };
            }
        }

        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;

        public bool HasMotion => true;

        public bool IsReady
        {
            get
            {
                switch (ConnectionType)
                {
                    case EConnectionType.USB: return true;
                    case EConnectionType.Bluetooth: return true;
                    case EConnectionType.Dongle: return ConState != InternalConState.Unknown;
                    case EConnectionType.Virtual: return true;
                    default: return false;
                }
            }
        }

        public bool IsPresent
        {
            get
            {
                switch (ConnectionType)
                {
                    case EConnectionType.USB: return true;
                    case EConnectionType.Bluetooth: return true;
                    case EConnectionType.Dongle: return ConState == InternalConState.Connected;
                    case EConnectionType.Virtual: return true;
                    default: return false;
                }
            }
        }
        public bool IsVirtual => false;

        public bool SensorsEnabled;
        private HidDevice _device;
        int reportUsageLock = 0;

        #region DATA STRUCTS
        public enum VSCEventType
        {
            CONTROL_UPDATE = 0x01,
            CONNECTION_DETAIL = 0x03,
            BATTERY_UPDATE = 0x04,
        }

        public enum ConnectionState
        {
            DISCONNECT = 0x01,
            CONNECT = 0x02,
            PAIRING = 0x03,
        }

        public enum Melody : UInt32
        {
            Warm_and_Happy = 0x00,
            Invader = 0x01,
            Controller_Confirmed = 0x02,
            Victory = 0x03,
            Rise_and_Shine = 0x04,
            Shorty = 0x05,
            Warm_Boot = 0x06,
            Next_Level = 0x07,
            Shake_it_off = 0x08,
            Access_Denied = 0x09,
            Deactivate = 0x0a,
            Discovery = 0x0b,
            Triumph = 0x0c,
            The_Mann = 0x0d,
        }

        public enum EControllerType
        {
            Unknown,
            Chell,
            Gordon,
        }

        private enum EBLEOptionDataChunksBitmask : UInt16
        {
            // First byte uppper nibble
            BLEButtonChunk1 = 0x10,
            BLEButtonChunk2 = 0x20,
            BLEButtonChunk3 = 0x40,
            BLELeftJoystickChunk = 0x80,

            // Second full byte
            BLELeftTrackpadChunk = 0x100,
            BLERightTrackpadChunk = 0x200,
            BLEIMUAccelChunk = 0x400,
            BLEIMUGyroChunk = 0x800,
            BLEIMUQuatChunk = 0x1000,
        };
        private enum EBLEPacketReportNums : byte
        {
            BLEReportState = 4,
            BLEReportStatus = 5,
        };
        public ControllerState GetState()
        {
            return State;
        }
        #endregion

#if Serial
        public string Serial { get; private set; }
#endif

        public IDevice DeviceHackRef => _device;


        ControllerState State = new ControllerState();
        ControllerState OldState = new ControllerState();

        struct RawSteamControllerState
        {
            public byte[] ulButtons;
            public byte sTriggerL;
            public byte sTriggerR;
            public byte[] ulButtons5; // what's this?

            public Int16 sLeftStickX;
            public Int16 sLeftStickY;

            public Int16 sLeftPadX;
            public Int16 sLeftPadY;

            public Int16 sRightPadX;
            public Int16 sRightPadY;

            public Int16 sAccelX { get; set; }
            public Int16 sAccelY { get; set; }
            public Int16 sAccelZ { get; set; }
            public Int16 sGyroX { get; set; }
            public Int16 sGyroY { get; set; }
            public Int16 sGyroZ { get; set; }
            public Int16 sGyroQuatW { get; set; }
            public Int16 sGyroQuatX { get; set; }
            public Int16 sGyroQuatY { get; set; }
            public Int16 sGyroQuatZ { get; set; }

            // We only need this bceause we add touch events rather than using the raw touch x/y
            public bool LeftTouchChange { get; set; }
        }

        RawSteamControllerState RawState = new RawSteamControllerState()
        {
            ulButtons = new byte[3],
            ulButtons5 = new byte[3]
        };

        //int Initalized;

        public EConnectionType ConnectionType { get; private set; }
        public EControllerType ControllerType { get; private set; }
        public EPollingState PollingState { get; private set; }

        private enum InternalConState
        {
            Unknown,
            Disconnected,
            Connected,
        }
        private InternalConState ConState;
        private DateTime ConnectedTime;

        private string SerialNumber;
        SemaphoreSlim UpdateLocalDataLock = new SemaphoreSlim(1);
        SemaphoreSlim SharedDongleLock;

        Wrapper<bool> UpdateLocalDataPoison;
        Thread GetSerialNumberThread;

        // TODO for now it is safe to assume the startup connection type is correct, however, in the future we will need to have connection events trigger a recheck of the type or something once the V2 controller is out (if ever)
        public SteamController(HidDevice device, SemaphoreSlim SharedDongleLock, EConnectionType ConnectionType = EConnectionType.Unknown, EControllerType type = EControllerType.Unknown)
        {
            this.SharedDongleLock = SharedDongleLock;

            this.ConnectionType = ConnectionType;
            this.ControllerType = type;

            _device = device;

            PollingState = EPollingState.Inactive;

            _device.DeviceReport += OnReport;

            if (type != EControllerType.Chell)
                State.Controls["cluster_left"] = new ControlDPad(/*4*/);
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair(ButtonProperties.CMB_Bumper);
            State.Controls["triggers"] = new ControlButtonPair(ButtonProperties.CMB_2StageTrigger);
            State.Controls["menu"] = new ControlButtonPair(ButtonProperties.CMB_Button);
            State.Controls["grip"] = new ControlButtonPair(ButtonProperties.CMB_Button);
            State.Controls["home"] = new ControlButton(ButtonProperties.CMB_Button);
            if (type != EControllerType.Chell)
                State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["touch_left"] = new ControlTouch(TouchCount: 1, HasClick: true);
            State.Controls["touch_right"] = new ControlTouch(TouchCount: 1, HasClick: true);
            if (type == EControllerType.Chell)
                State.Controls["grid_center"] = new ControlButtonGrid(2, 2);
            State.Controls["motion"] = new ControlMotion();

            //Initalized = 0;

            ConnectedTime = DateTime.MinValue;

            if (this.ConnectionType == EConnectionType.Dongle)
            {
                ConState = InternalConState.Unknown;
                //Initalized = 1;
                PollingState = EPollingState.SlowPoll;
                _device.StartReading();
            }
            if (string.IsNullOrWhiteSpace(SerialNumber))
            {
                StartSerialNumberCheck();
            }
        }

        private void StartSerialNumberCheck(bool ControllerAdd = true)
        {
            // if poison exists, switch it to true
            if (UpdateLocalDataPoison != null) UpdateLocalDataPoison.Value = true;

            if (ControllerAdd)
            {
                switch (ControllerType)
                {
                    case EControllerType.Gordon:
                        {
                            UpdateLocalDataPoison = new Wrapper<bool>(false);
                            GetSerialNumberThread = new Thread(GetSerialThread);
                            GetSerialNumberThread.Start(UpdateLocalDataPoison);
                        }
                        break;
                }
            }
            else
            {
                SerialNumber = null;
            }
        }

        private void GetSerialThread(object LocalPoisonO)
        {
            Wrapper<bool> LocalPoison = (Wrapper<bool>)LocalPoisonO;
            if (LocalPoison.Value) return;
            Thread.Sleep(1000);
            if (LocalPoison.Value) return;
            SharedDongleLock?.Wait();
            try
            {
                if (LocalPoison.Value) return;
                UpdateLocalDataLock.Wait();
                try
                {
                    if (LocalPoison.Value) return;
                    if (!LocalPoison.Value && ConState != InternalConState.Disconnected && string.IsNullOrWhiteSpace(SerialNumber))
                    {
                        if (LocalPoison.Value) return;
                        string _SerialNumber;
                        if (GetSerialNumber(StringAttribute.Device, out _SerialNumber))
                        {
                            if (LocalPoison.Value) return;
                            SerialNumber = _SerialNumber;
                            ConState = string.IsNullOrWhiteSpace(SerialNumber) ? InternalConState.Disconnected : InternalConState.Connected;
                            ControllerMetadataUpdate?.Invoke(this);
                        }
                    }
                }
                finally
                {
                    UpdateLocalDataLock.Release();
                }
            }
            finally
            {
                SharedDongleLock?.Release();
            }
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

            PollingState = EPollingState.Active;
            _device.StartReading();

            if (EnableMotion)
                new Thread(() =>
                {
                    Thread.Sleep(5000);
                    EnableGyroSensors();
                }).Start();
        }

        /*public void HalfInitalize()
        {
            if (Initalized > 0) return;

            // open the device overlapped read so we don't get stuck waiting for a report when we write to it
            //_device.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
            //_device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

            //_device.Inserted += DeviceAttachedHandler;
            //_device.Removed += DeviceRemovedHandler;

            //_device.MonitorDeviceEvents = true;

            Initalized = 1;

#if Serial
            new Thread(() =>
            {
                try {
                    while (Initalized > 0)
                    {
                        UpdateMetadata();
                        Thread.Sleep(1000);
                    }
                } catch { }
            }).Start();
#endif
        }*/

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        public void DeInitalize()
        {
            if (PollingState == EPollingState.Inactive) return;
            if (PollingState == EPollingState.SlowPoll) return;

            //if (EnableMotion)
                ResetGyroSensors();

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

        private object FeatureReportLocalLock = new object();
        public byte[] GetFeatureReport(byte[] reportData)
        {
            if (reportData.Length < 2) return null;

            lock (FeatureReportLocalLock)
            {
                var result = _device.WriteFeatureData(reportData);

                if (!result) return null;

                byte[] reply;
                bool success;
                do
                {
                    success = _device.ReadFeatureData(out reply, 0);
                } while (success && reply[1] != reportData[1]);
                return reply;
            }
        }

        public bool EnableGyroSensors()
        {
            if (!SensorsEnabled)
            {
                byte[] reportData = new byte[65];
                reportData[1] = 0x87; // 0x87 = register write command
                reportData[2] = 0x03; // 0x03 = length of data to be written (data + 1 empty bit)
                reportData[3] = 0x30; // 0x30 = register of Gyro data
                //reportData[4] = 0x10 | 0x08 | 0x04; // enable raw Gyro, raw Accel, and Quaternion data
                reportData[4] = 0x10 | 0x04;
                Debug.WriteLine("Attempting to reenable MPU accelerometer sensor");
                var result = _device.WriteFeatureData(reportData);
                //var result = GetFeatureReport(reportData) != null;
                SensorsEnabled = true;
                return result;
            }
            return false;
        }

        public bool ResetGyroSensors()
        {
            if (SensorsEnabled)
            {
                byte[] reportData = new byte[65];
                reportData[1] = 0x87; // 0x87 = register write command
                reportData[2] = 0x03; // 0x03 = length of data to be written (data + 1 empty bit)
                reportData[3] = 0x30; // 0x30 = register of Gyro data
                reportData[4] = 0x10 | 0x04; // enable raw Gyro, raw Accel, and Quaternion data
                Debug.WriteLine("Attempting to restore default sensor state");
                var result = _device.WriteFeatureData(reportData);
                //var result = GetFeatureReport(reportData) != null;
                SensorsEnabled = false;
                return result;
            }
            return false;
        }

        public void Identify()
        {
            PlayMelody(Melody.Rise_and_Shine);
        }

        public bool PlayMelody(Melody melody)
        {
            byte[] reportData = new byte[65];
            reportData[1] = 0xB6; // 0xB6 = play melody
            reportData[2] = 0x04; // 0x04 = length of data to be written
            byte[] data = BitConverter.GetBytes((UInt32)melody);
            reportData[3] = data[0];
            reportData[4] = data[1];
            reportData[5] = data[2];
            reportData[6] = data[3];
            Debug.WriteLine("Triggering Melody");

            //return GetFeatureReport(reportData) != null;
            var result = _device.WriteFeatureData(reportData);
            return result;
        }


        const int DONGLE_FEATURE_WRITE_TRIES = 60;//50;
        const int DONGLE_FEATURE_WRITE_WAIT_MS = 100;//1;
        const int BLE_FEATURE_READ_WAIT_MS = 100;//1;
        const int BLE_FEATURE_READ_TRIES = 8;
        const byte BLE_REPORT_NUMBER = 0x03;
        enum FeatureReportIDs : byte
        {
            GetStringAttribute = 0xAE,
        }
        enum StringAttribute : byte
        {
            Board = 0x00,
            Device = 0x01,
        }
        private bool GetSerialNumber(StringAttribute Type, out string SerialNumber)
        {
            SerialNumber = null;

            byte[] reportData = new byte[65];
            reportData[1] = (byte)FeatureReportIDs.GetStringAttribute;
            reportData[2] = 0x15;
            reportData[3] = (byte)Type;

            bool result = false;
            /*for (int i = 1; i <= WIRELESS_FEATURE_READ_TRIES; i++)
            {
                if (i == WIRELESS_FEATURE_READ_TRIES) break;
                result = _device.WriteFeatureData(reportData);
                if (result) break;
                //if (ConnectionType == EConnectionType.USB) break;
                if (i == 9) break;
                Thread.Sleep(WIRELESS_FEATURE_WRITE_WAIT_MS);
            }*/
            result = WriteFeatureReport(reportData, 4);
            if (!result) return false;

            reportData = new byte[65];
            result = ReadFeatureReport(out reportData);
            /*for (int i = 1; i <= WIRELESS_FEATURE_WRITE_TRIES; i++)
            {
                if (i == WIRELESS_FEATURE_WRITE_TRIES) break;
                result = _device.ReadFeatureData(out reportData);
                if (result) break;
                //if (ConnectionType == EConnectionType.USB) break;
                Thread.Sleep(WIRELESS_FEATURE_READ_WAIT_MS);
            }*/
            if (!result) return false;

            if (reportData[1] == (byte)FeatureReportIDs.GetStringAttribute)
            {
                if (reportData[3] == (byte)Type)
                {
                    int i = 0;
                    for (; i < reportData[2]; i++)
                        if (reportData[i + 4] == 0x00)
                            break;
                    SerialNumber = Encoding.ASCII.GetString(reportData, 4, i);
                    //SerialNumber = Encoding.ASCII.GetString(reportData, 4, reportData[2] - 4);
                }
                return true;
            }
            return false;
        }

        const int MAX_REPORT_SEGMENT_PAYLOAD_SIZE = 18;
        const int MAX_REPORT_SEGMENT_SIZE = (MAX_REPORT_SEGMENT_PAYLOAD_SIZE + 2);
        const byte REPORT_SEGMENT_DATA_FLAG = 0x80;
        const byte REPORT_SEGMENT_LAST_FLAG = 0x40;
        private byte GetSegmentHeader(byte nSegmentNumber, bool bLastPacket)
        {
            byte header = REPORT_SEGMENT_DATA_FLAG;
            header |= nSegmentNumber;
            if (bLastPacket)
                header |= REPORT_SEGMENT_LAST_FLAG;

            return header;
        }
        private bool WriteFeatureReport(byte[] Buffer, int Length)
        {
            switch (ConnectionType)
            {
                case EConnectionType.Bluetooth:
                    {
                        byte nSegmentNumber = 0;
                        byte[] uPacketBuffer = new byte[MAX_REPORT_SEGMENT_SIZE];

                        // Skip report number in data
                        int ReadOffset = 1;
                        Length--;

                        bool retVal = false;

                        while (Length > 0)
                        {
                            int nBytesInPacket = Length > MAX_REPORT_SEGMENT_PAYLOAD_SIZE ? MAX_REPORT_SEGMENT_PAYLOAD_SIZE : Length;

                            Length -= nBytesInPacket;

                            // Construct packet
                            uPacketBuffer[0] = BLE_REPORT_NUMBER;
                            uPacketBuffer[1] = GetSegmentHeader(nSegmentNumber, Length == 0);
                            Array.Copy(Buffer, ReadOffset, uPacketBuffer, 2, nBytesInPacket);

                            ReadOffset += nBytesInPacket;
                            nSegmentNumber++;

                            retVal = _device.WriteFeatureData(uPacketBuffer);
                        }
                        return retVal;
                    }
                case EConnectionType.Dongle:
                    {
                        bool retVal = false;
                        for (int i = 1; i <= DONGLE_FEATURE_WRITE_TRIES; i++)
                        {
                            if (i == DONGLE_FEATURE_WRITE_TRIES) break;
                            retVal = _device.WriteFeatureData(Buffer);
                            if (retVal) break;
                            Thread.Sleep(DONGLE_FEATURE_WRITE_WAIT_MS);
                        }
                        return retVal;
                    }
            }
            return _device.WriteFeatureData(Buffer);
        }

        private bool ReadFeatureReport(out byte[] Buffer)
        {
            if (ConnectionType == EConnectionType.Bluetooth)
            {
                bool Success = false;

                byte[] AssembledBuffer = new byte[MAX_REPORT_SEGMENT_PAYLOAD_SIZE * 8 + 1];
                int m_unCurrentMsgIndex = 0;
                byte NextSegmentId = 0;

                int RetryCounter = 0;
                //byte[] uSegmentBuffer = new byte[MAX_REPORT_SEGMENT_SIZE];
                byte[] ReportBuffer = new byte[65]; // need 65 or windows fails to read it because it tries the wrong size
                while (RetryCounter < BLE_FEATURE_READ_TRIES)
                {
                    //uSegmentBuffer[0] = BLE_REPORT_NUMBER;
                    Success = _device.ReadFeatureDataCustom(ref ReportBuffer, BLE_REPORT_NUMBER);

                    // all offsets +1 due to strange double-ID thing going on
                    ReportBuffer = ReportBuffer.Skip(1).ToArray();

                    if (!Success)
                    {
                        RetryCounter++;
                        Thread.Sleep(BLE_FEATURE_READ_WAIT_MS);
                        continue;
                    }

                    byte ucHeader = ReportBuffer[1 + 0];
                    if ((ucHeader & k_nSegmentHasDataFlag) != k_nSegmentHasDataFlag)
                    {
                        Buffer = null;
                        return false; // steam itself actually asserts in this case
                    }

                    if (!IsSegmentNumberValid(ucHeader, NextSegmentId))
                    {
                        Buffer = null;
                        return false; // steam itself actually asserts in this case
                    }

                    if (ReportBuffer[0] != BLE_REPORT_NUMBER)
                        continue; // this report isn't for us

                    if ((ReportBuffer[1] & REPORT_SEGMENT_DATA_FLAG) == 0)
                    {
                        RetryCounter++;
                        continue;
                    }

                    RetryCounter = 0; // reset retry counter as we got a segment

                    // TODO nLength may be wrong due to the double reportID inclusion
                    // TODO unlike normal report, final segment appears to have no data, confirm this and if true don't process data of last packet
                    {
                        int nLength = k_nMaxSegmentSize - 1;
                        Array.Copy(ReportBuffer, 1 + 1, AssembledBuffer, m_unCurrentMsgIndex, nLength);
                        m_unCurrentMsgIndex += nLength;
                        ++NextSegmentId;
                    }

                    // TODO check if 65 is the max buffer size or if BLE's segemented packets are taken advantage of to send more than 64 bytes of data
                    if ((ucHeader & k_nLastSegmentFlag) == k_nLastSegmentFlag)
                    {
                        // Last packet
                        Buffer = new byte[65];
                        Buffer[0] = BLE_REPORT_NUMBER;
                        Array.Copy(AssembledBuffer, 0, Buffer, 1, Math.Min(64, AssembledBuffer.Length));
                        return true;
                    }
                }
                Buffer = null;
                return false;
            }

            Buffer = new byte[65];
            return _device.ReadFeatureDataCustom(ref Buffer);
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

        private void OnReport(IReport rawReportData)
        {
            if (PollingState == EPollingState.Inactive) return;

            //if (!(reportData is HidReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.HID) return;
            HidReport reportData = (HidReport)rawReportData;

            // If we happen to receive any keyboard or mouse reports just skip them and keep reading
            if (reportData.ReportId == k_nKeyboardReportNumber || reportData.ReportId == k_nMouseReportNumber)
                return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    //ControllerState StateInFlight = (ControllerState)State.Clone();

                    //OldState = State; // shouldn't this be a clone?
                    //if (_attached == false) { return; }

                    switch (ConnectionType)
                    {
                        // report ID here is 3, do check what it is for the other connection types
                        case EConnectionType.Bluetooth:
                            {
                                if (reportData.ReportId != BLE_REPORT_NUMBER)
                                    return; // this report isn't for us

                                //Console.WriteLine($"Unknown Packet {reportData.Length}\t{BitConverter.ToString(reportData)}");

                                byte ucHeader = reportData.ReportBytes[0];
                                if ((ucHeader & k_nSegmentHasDataFlag) != k_nSegmentHasDataFlag)
                                    return; // steam itself actually asserts in this case

                                if (!IsSegmentNumberValid(ucHeader))
                                {
                                    // This likely means that we missed a packet.
                                    //m_InputReportState.Reset();
                                    ResetSegment();

                                    // If the sequence number is zero then we can use it, otherwise we need to flush
                                    // the remainder of the partial message.
                                    if ((ucHeader & k_nSegmentNumberMask) > 0)
                                        return;
                                }

                                {
                                    int nLength = k_nMaxSegmentSize - 1;
                                    Array.Copy(reportData.ReportBytes, 1, m_rgubBuffer, m_unCurrentMsgIndex, nLength);
                                    m_unCurrentMsgIndex += nLength;
                                    ++m_unNextSegmentNumber;
                                }

                                if ((ucHeader & k_nLastSegmentFlag) == k_nLastSegmentFlag)
                                {
                                    // Last packet

                                    EBLEPacketReportNums reportType = (EBLEPacketReportNums)(m_rgubBuffer[0] & 0x0f);
                                    switch (reportType)
                                    {
                                        case EBLEPacketReportNums.BLEReportState:
                                            {
                                                EBLEOptionDataChunksBitmask ucOptionDataMask = (EBLEOptionDataChunksBitmask)(BitConverter.ToUInt16(m_rgubBuffer, 0) & 0xfff0);
                                                int pBuf = 2;

                                                try
                                                {
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLEButtonChunk1))
                                                    {
                                                        bool LeftTouchOld = (RawState.ulButtons[2] & 8) == 8;
                                                        bool RightTouchOld = (RawState.ulButtons[2] & 16) == 16;

                                                        Array.Copy(m_rgubBuffer, pBuf, RawState.ulButtons, 0, 3);

                                                        if (LeftTouchOld != ((RawState.ulButtons[2] & 8) == 8))
                                                            RawState.LeftTouchChange = true;

                                                        //if (RightTouchOld != ((RawState.ulButtons[2] & 16) == 16))
                                                        //    RawState.RightTouchChange = true;

                                                        //Console.WriteLine($"{Convert.ToString(RawState.ulButtons[0], 2).PadLeft(8, '0')} {Convert.ToString(RawState.ulButtons[1], 2).PadLeft(8, '0')} {Convert.ToString(RawState.ulButtons[2], 2).PadLeft(8, '0')}");

                                                        pBuf += 3;
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLEButtonChunk2))
                                                    {
                                                        RawState.sTriggerL = m_rgubBuffer[pBuf + 0];
                                                        RawState.sTriggerR = m_rgubBuffer[pBuf + 1];

                                                        pBuf += 2;
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLEButtonChunk3))
                                                    {
                                                        Array.Copy(m_rgubBuffer, pBuf, RawState.ulButtons5, 0, 3);

                                                        Console.WriteLine($"{Convert.ToString(RawState.ulButtons5[0], 2).PadLeft(8, '0')} {Convert.ToString(RawState.ulButtons5[1], 2).PadLeft(8, '0')} {Convert.ToString(RawState.ulButtons5[2], 2).PadLeft(8, '0')}");

                                                        pBuf++;
                                                        pBuf++;
                                                        pBuf++;
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLELeftJoystickChunk))
                                                    {
                                                        RawState.sLeftStickX = BitConverter.ToInt16(m_rgubBuffer, pBuf);
                                                        RawState.sLeftStickY = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16));

                                                        pBuf += sizeof(Int16) + sizeof(Int16);
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLELeftTrackpadChunk))
                                                    {
                                                        //if ((RawState.ulButtons[2] & 8) == 8) // let's only set the X/Y with a touch to avoid jumps to 0
                                                        {
                                                            int X = BitConverter.ToInt16(m_rgubBuffer, pBuf);
                                                            int Y = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16));

                                                            RotateXY(-PadAngle, ref X, ref Y);

                                                            RawState.sLeftPadX = (short)Math.Min(Math.Max(X, Int16.MinValue), Int16.MaxValue);
                                                            RawState.sLeftPadY = (short)Math.Min(Math.Max(Y, Int16.MinValue), Int16.MaxValue);

                                                            RawState.LeftTouchChange = true;
                                                        }

                                                        pBuf += sizeof(Int16) + sizeof(Int16);
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLERightTrackpadChunk))
                                                    {
                                                        //if ((RawState.ulButtons[2] & 16) == 16) // let's only set the X/Y with a touch to avoid jumps to 0
                                                        {
                                                            int X = BitConverter.ToInt16(m_rgubBuffer, pBuf);
                                                            int Y = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16));

                                                            RotateXY(PadAngle, ref X, ref Y);

                                                            RawState.sRightPadX = (short)Math.Min(Math.Max(X, Int16.MinValue), Int16.MaxValue);
                                                            RawState.sRightPadY = (short)Math.Min(Math.Max(Y, Int16.MinValue), Int16.MaxValue);

                                                            //RawState.RightTouchChange = true;
                                                        }

                                                        pBuf += sizeof(Int16) + sizeof(Int16);
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLEIMUAccelChunk))
                                                    {
                                                        RawState.sAccelX = BitConverter.ToInt16(m_rgubBuffer, pBuf);
                                                        RawState.sAccelY = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16));
                                                        RawState.sAccelZ = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16) + sizeof(Int16));

                                                        pBuf += sizeof(Int16) + sizeof(Int16) + sizeof(Int16);
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLEIMUGyroChunk))
                                                    {
                                                        RawState.sGyroX = BitConverter.ToInt16(m_rgubBuffer, pBuf);
                                                        RawState.sGyroY = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16));
                                                        RawState.sGyroZ = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16) + sizeof(Int16));

                                                        pBuf += sizeof(Int16) + sizeof(Int16) + sizeof(Int16);
                                                    }
                                                    if (ucOptionDataMask.HasFlag(EBLEOptionDataChunksBitmask.BLEIMUQuatChunk))
                                                    {
                                                        RawState.sGyroQuatW = BitConverter.ToInt16(m_rgubBuffer, pBuf);
                                                        RawState.sGyroQuatX = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16));
                                                        RawState.sGyroQuatY = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16) + sizeof(Int16));
                                                        RawState.sGyroQuatZ = BitConverter.ToInt16(m_rgubBuffer, pBuf + sizeof(Int16) + sizeof(Int16) + sizeof(Int16));

                                                        pBuf += sizeof(Int16) + sizeof(Int16) + sizeof(Int16) + sizeof(Int16);
                                                    }

                                                    //Console.WriteLine($"Good Packet {reportID}\t{BitConverter.ToString(m_rgubBuffer)}");
                                                }
                                                catch
                                                {
                                                    //Console.WriteLine($"Error Packet {reportID}\t{BitConverter.ToString(m_rgubBuffer)}");
                                                }

                                                //ProcessStateBytes();
                                                ControllerState StateInFlight = ProcessStateBytes();

                                                if (ConState != InternalConState.Connected)
                                                {
                                                    ConState = InternalConState.Connected;
                                                    StartSerialNumberCheck();
                                                    ControllerMetadataUpdate?.Invoke(this);
                                                }
                                                ConnectedTime = DateTime.UtcNow;

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
                                            break;
                                        case EBLEPacketReportNums.BLEReportStatus:
                                            break;
                                        default:
                                            Console.WriteLine($"Unknown Packet {reportData.ReportId}\t{BitConverter.ToString(m_rgubBuffer)}");
                                            break;
                                    }

                                    ResetSegment();
                                }
                            }
                            break;
                        default:
                            {
                                byte Unknown1 = reportData.ReportBytes[0]; // always 0x01?
                                byte Unknown2 = reportData.ReportBytes[1]; // always 0x00?
                                VSCEventType EventType = (VSCEventType)reportData.ReportBytes[2];
                                //reportData.ReportBytes[3] // length

                                switch (EventType)
                                {
                                    case 0: // not sure what this is but wired controllers do it
                                        break;
                                    case VSCEventType.CONTROL_UPDATE:
                                        {

                                            UInt32 PacketIndex = BitConverter.ToUInt32(reportData.ReportBytes, 4);

                                            Array.Copy(reportData.ReportBytes, 8 + 0, RawState.ulButtons, 0, 3);

                                            bool LeftAnalogMultiplexMode = (RawState.ulButtons[2] & 128) == 128;
                                            bool LeftStickClick = (RawState.ulButtons[2] & 64) == 64;
                                            //(StateInFlight.Controls["stick_left"] as ControlStick).Click = LeftStickClick;
                                            //bool Unknown = (RawState.ulButtons[2] & 32) == 32; // what is this?
                                            //bool RightPadTouch = (RawState.ulButtons[2] & 16) == 16;
                                            bool LeftPadTouch = (RawState.ulButtons[2] & 8) == 8;
                                            //(StateInFlight.Controls["touch_right"] as ControlTouch).Click = (RawState.ulButtons[2] & 4) == 4;
                                            bool ThumbOrLeftPadPress = (RawState.ulButtons[2] & 2) == 2; // what is this even for?
                                            //(StateInFlight.Controls["grip"] as ControlButtonPair).Right.Button0 = (RawState.ulButtons[2] & 1) == 1;

                                            RawState.sTriggerL = reportData.ReportBytes[8 + 3];
                                            RawState.sTriggerR = reportData.ReportBytes[8 + 4];

                                            if (LeftAnalogMultiplexMode)
                                            {
                                                if (LeftPadTouch)
                                                {
                                                    int X = BitConverter.ToInt16(reportData.ReportBytes, 8 + 8);
                                                    int Y = BitConverter.ToInt16(reportData.ReportBytes, 8 + 10);

                                                    RotateXY(-PadAngle, ref X, ref Y);

                                                    RawState.sLeftPadX = (short)Math.Min(Math.Max(X, Int16.MinValue), Int16.MaxValue);
                                                    RawState.sLeftPadY = (short)Math.Min(Math.Max(Y, Int16.MinValue), Int16.MaxValue);

                                                    RawState.LeftTouchChange = true;
                                                }
                                                else
                                                {
                                                    RawState.sLeftStickX = BitConverter.ToInt16(reportData.ReportBytes, 8 + 8);
                                                    RawState.sLeftStickY = BitConverter.ToInt16(reportData.ReportBytes, 8 + 10);
                                                }
                                            }
                                            else
                                            {
                                                if (LeftPadTouch)
                                                {
                                                    int X = BitConverter.ToInt16(reportData.ReportBytes, 8 + 8);
                                                    int Y = BitConverter.ToInt16(reportData.ReportBytes, 8 + 10);

                                                    RotateXY(-PadAngle, ref X, ref Y);

                                                    RawState.sLeftPadX = (short)Math.Min(Math.Max(X, Int16.MinValue), Int16.MaxValue);
                                                    RawState.sLeftPadY = (short)Math.Min(Math.Max(Y, Int16.MinValue), Int16.MaxValue);
                                                }
                                                else
                                                {
                                                    // // we're trying to fix the pad jumping to center by not sending new coords if the pad is not touched
                                                    RawState.sLeftPadX = 0;
                                                    RawState.sLeftPadY = 0;

                                                    RawState.sLeftStickX = BitConverter.ToInt16(reportData.ReportBytes, 8 + 8);
                                                    RawState.sLeftStickY = BitConverter.ToInt16(reportData.ReportBytes, 8 + 10);
                                                }

                                                RawState.LeftTouchChange = true;

                                                //(State.Controls["touch_left"] as ControlTouch).Click = ThumbOrLeftPadPress && !LeftStickClick;
                                            }

                                            //if (RightPadTouch) // we're trying to fix the pad jumping to center by not sending new coords if the pad is not touched
                                            {
                                                int X = BitConverter.ToInt16(reportData.ReportBytes, 8 + 12);
                                                int Y = BitConverter.ToInt16(reportData.ReportBytes, 8 + 14);

                                                RotateXY(PadAngle, ref X, ref Y);

                                                RawState.sRightPadX = (short)Math.Min(Math.Max(X, Int16.MinValue), Int16.MaxValue);
                                                RawState.sRightPadY = (short)Math.Min(Math.Max(Y, Int16.MinValue), Int16.MaxValue);
                                            }

                                            //RawState.RightTouchChange = true;

                                            RawState.sAccelX = BitConverter.ToInt16(reportData.ReportBytes, 8 + 20);
                                            RawState.sAccelY = BitConverter.ToInt16(reportData.ReportBytes, 8 + 22);
                                            RawState.sAccelZ = BitConverter.ToInt16(reportData.ReportBytes, 8 + 24);
                                            RawState.sGyroX = BitConverter.ToInt16(reportData.ReportBytes, 8 + 26);
                                            RawState.sGyroY = BitConverter.ToInt16(reportData.ReportBytes, 8 + 28);
                                            RawState.sGyroZ = BitConverter.ToInt16(reportData.ReportBytes, 8 + 30);
                                            RawState.sGyroQuatW = BitConverter.ToInt16(reportData.ReportBytes, 8 + 32);
                                            RawState.sGyroQuatX = BitConverter.ToInt16(reportData.ReportBytes, 8 + 34);
                                            RawState.sGyroQuatY = BitConverter.ToInt16(reportData.ReportBytes, 8 + 36);
                                            RawState.sGyroQuatZ = BitConverter.ToInt16(reportData.ReportBytes, 8 + 38);

                                            ControllerState StateInFlight = ProcessStateBytes();

                                            if (ConState != InternalConState.Connected)
                                            {
                                                ConState = InternalConState.Connected;
                                                StartSerialNumberCheck();
                                                ControllerMetadataUpdate?.Invoke(this);
                                            }
                                            ConnectedTime = DateTime.UtcNow;

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
                                        break;

                                    case VSCEventType.CONNECTION_DETAIL:
                                        {
                                            //reportData.ReportBytes[3] // 0x01?

                                            // Connection detail. 0x01 for disconnect, 0x02 for connect, 0x03 for pairing request.
                                            ConnectionState ConnectionStateV = (ConnectionState)reportData.ReportBytes[4];

                                            switch (ConnectionStateV)
                                            {
                                                case ConnectionState.CONNECT:
                                                    ConState = InternalConState.Connected;
                                                    ConnectedTime = DateTime.UtcNow;
                                                    StartSerialNumberCheck();
                                                    ControllerMetadataUpdate?.Invoke(this);
                                                    break;
                                                case ConnectionState.DISCONNECT:
                                                    ConState = InternalConState.Disconnected;
                                                    StartSerialNumberCheck(false);
                                                    ControllerMetadataUpdate?.Invoke(this);
                                                    break;
                                            }
                                        }
                                        break;

                                    case VSCEventType.BATTERY_UPDATE:
                                        {
                                            //reportData.ReportBytes[3] // 0x0B?

                                            UInt32 PacketIndex = BitConverter.ToUInt32(reportData.ReportBytes, 4);

                                            // only works if controller is configured to send this data

                                            // millivolts
                                            UInt16 BatteryVoltage = BitConverter.ToUInt16(reportData.ReportBytes, 8);
                                            //BitConverter.ToUInt16(reportData, 1 + 10); // UNKNOWN, stuck at 100
                                        }
                                        break;

                                    default:
                                        {
                                            Console.WriteLine($"Unknown Packet Type {(int)EventType:D3} of length {reportData.ReportBytes.Length}\t{reportData.ReportId:X2}:{BitConverter.ToString(reportData.ReportBytes)}");
                                        }
                                        break;
                                }
                            }
                            break;
                    }

                    //// bring OldState in line with new State
                    //OldState = State;
                    //State = StateInFlight;
                    //
                    //StateUpdated?.Invoke(this, State);
                }
                finally
                {
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }
            }
        }

        private void RotateXY(float padAngle, ref int x, ref int y, int cx = 0, int cy = 0)
        {
            int xrot = (int)(Math.Cos(padAngle) * (x - cx) - Math.Sin(padAngle) * (y - cy) + cx);
            int yrot = (int)(Math.Sin(padAngle) * (x - cx) + Math.Cos(padAngle) * (y - cy) + cy);

            x = xrot;
            y = yrot;
        }

        const int k_nMaxSegmentSize = 19;
        const int k_nMaxSegmentPayloadSize = k_nMaxSegmentSize - 1;
        const int k_nMaxNumberOfSegments = 8;
        const int k_nMaxMessageSize = k_nMaxSegmentPayloadSize * k_nMaxNumberOfSegments;
        byte[] m_rgubBuffer = new byte[k_nMaxMessageSize];

        byte m_unNextSegmentNumber = 0x00;
        int m_unCurrentMsgIndex = 0x00;

        private void ResetSegment()
        {
            m_unNextSegmentNumber = 0;
            m_unCurrentMsgIndex = 0;
            Array.Clear(m_rgubBuffer, 0, m_rgubBuffer.Length);
        }

        private bool IsSegmentNumberValid(byte ucHeader)
        {
            return ((ucHeader & k_nSegmentNumberMask) == m_unNextSegmentNumber);
        }
        private bool IsSegmentNumberValid(byte ucHeader, byte m_unNextSegmentNumber)
        {
            return ((ucHeader & k_nSegmentNumberMask) == m_unNextSegmentNumber);
        }

        private ControllerState ProcessStateBytes()
        {
            //OldState = State; // shouldn't this be a clone?
            ControllerState StateInFlight = (ControllerState)State.Clone(); // shouldn't this be a clone?

            (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonS = (RawState.ulButtons[0] & 128) == 128; // A - S SE
            (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonW = (RawState.ulButtons[0] & 64) == 64;   // X - W SW
            (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonE = (RawState.ulButtons[0] & 32) == 32;   // B - E NE
            (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonN = (RawState.ulButtons[0] & 16) == 16;   // Y - N NW
            (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Digital = (RawState.ulButtons[0] & 8) == 8;
            (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Digital = (RawState.ulButtons[0] & 4) == 4;
            (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.DigitalStage2 = (RawState.ulButtons[0] & 2) == 2;
            (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.DigitalStage2 = (RawState.ulButtons[0] & 1) == 1;

            (StateInFlight.Controls["grip"] as ControlButtonPair).Left.Digital = (RawState.ulButtons[1] & 128) == 128;
            (StateInFlight.Controls["menu"] as ControlButtonPair).Right.Digital = (RawState.ulButtons[1] & 64) == 64;
            (StateInFlight.Controls["home"] as ControlButton).Digital = (RawState.ulButtons[1] & 32) == 32;
            (StateInFlight.Controls["menu"] as ControlButtonPair).Left.Digital = (RawState.ulButtons[1] & 16) == 16;

            if (ControllerType == EControllerType.Chell)
            {
                // for the Chell controller, these are the 4 face buttons
                (StateInFlight.Controls["grid_center"] as ControlButtonGrid).Button[0, 0] = (RawState.ulButtons[1] & 0x01) == 0x01; // State.ButtonsOld.Touch0 = (RawState.ulButtons[1] & 0x01) == 0x01; // NW
                (StateInFlight.Controls["grid_center"] as ControlButtonGrid).Button[1, 0] = (RawState.ulButtons[1] & 0x02) == 0x02; // State.ButtonsOld.Touch1 = (RawState.ulButtons[1] & 0x02) == 0x02; // NE
                (StateInFlight.Controls["grid_center"] as ControlButtonGrid).Button[0, 1] = (RawState.ulButtons[1] & 0x04) == 0x04; // State.ButtonsOld.Touch2 = (RawState.ulButtons[1] & 0x04) == 0x04; // SW
                (StateInFlight.Controls["grid_center"] as ControlButtonGrid).Button[1, 1] = (RawState.ulButtons[1] & 0x08) == 0x08; // State.ButtonsOld.Touch3 = (RawState.ulButtons[1] & 0x08) == 0x08; // SE
            }
            else
            {
                // these are mutually exclusive in the raw data, so let's act like they are in the code too, even though they use 4 bits
                if ((RawState.ulButtons[1] & 1) == 1)
                {
                    (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.North;
                }
                else if ((RawState.ulButtons[1] & 2) == 2)
                {
                    (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.East;
                }
                else if ((RawState.ulButtons[1] & 8) == 8)
                {
                    (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.South;
                }
                else if ((RawState.ulButtons[1] & 4) == 4)
                {
                    (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.West;
                }
                else
                {
                    (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.None;
                }
            }
            //bool LeftAnalogMultiplexMode = (RawState.ulButtons[2] & 128) == 128;
            bool LeftStickClick = (RawState.ulButtons[2] & 64) == 64;
            if (ControllerType != EControllerType.Chell)
                (StateInFlight.Controls["stick_left"] as ControlStick).Click = LeftStickClick;
            //bool Unknown = (RawState.ulButtons[2] & 32) == 32; // what is this?
            bool RightPadTouch = (RawState.ulButtons[2] & 16) == 16;
            bool LeftPadTouch = (RawState.ulButtons[2] & 8) == 8;
            (StateInFlight.Controls["touch_right"] as ControlTouch).Click = (RawState.ulButtons[2] & 4) == 4;
            bool ThumbOrLeftPadPress = (RawState.ulButtons[2] & 2) == 2; // what is this even for?
            (StateInFlight.Controls["grip"] as ControlButtonPair).Right.Digital = (RawState.ulButtons[2] & 1) == 1;

            (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.Analog = (float)RawState.sTriggerL / byte.MaxValue;
            (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.Analog = (float)RawState.sTriggerR / byte.MaxValue;
            if (ControllerType != EControllerType.Chell)
            {
                (StateInFlight.Controls["stick_left"] as ControlStick).X = (float)RawState.sLeftStickX / Int16.MaxValue;
                (StateInFlight.Controls["stick_left"] as ControlStick).Y = (float)-RawState.sLeftStickY / Int16.MaxValue;
            }
            if (RawState.LeftTouchChange)
            {
                float LeftPadX = LeftPadTouch ? (float)RawState.sLeftPadX / Int16.MaxValue : 0f;
                float LeftPadY = LeftPadTouch ? (float)-RawState.sLeftPadY / Int16.MaxValue : 0f;
                (StateInFlight.Controls["touch_left"] as ControlTouch).AddTouch(0, LeftPadTouch, LeftPadX, LeftPadY, 0);
            }

            float RightPadX = (float)RawState.sRightPadX / Int16.MaxValue;
            float RightPadY = (float)-RawState.sRightPadY / Int16.MaxValue;

            (StateInFlight.Controls["touch_right"] as ControlTouch).AddTouch(0, RightPadTouch, RightPadX, RightPadY, 0);
            (StateInFlight.Controls["touch_left"] as ControlTouch).Click = ThumbOrLeftPadPress && !LeftStickClick;

            /*
            State.DataStuck = CheckSensorDataStuck();
            if (!SensorsEnabled || DataStuck) { EnableGyroSensors(); }
            */

            (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerX = RawState.sAccelX;
            (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerY = RawState.sAccelY;
            (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerZ = RawState.sAccelZ;
            (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityX = RawState.sGyroX;
            (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityY = RawState.sGyroY;
            (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityZ = RawState.sGyroZ;
            (StateInFlight.Controls["motion"] as ControlMotion).OrientationW = RawState.sGyroQuatW;
            (StateInFlight.Controls["motion"] as ControlMotion).OrientationX = RawState.sGyroQuatX;
            (StateInFlight.Controls["motion"] as ControlMotion).OrientationY = RawState.sGyroQuatY;
            (StateInFlight.Controls["motion"] as ControlMotion).OrientationZ = RawState.sGyroQuatZ;

            RawState.LeftTouchChange = false;

            return StateInFlight;
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
    }
}
