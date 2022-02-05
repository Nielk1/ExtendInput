using Brod.Common.Utilities;
using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.Sony
{
    public class DualSenseController : IController
    {
        public const int VendorId = 0x054C;
        public static int ProductId = 0x0CE6;

        private const byte _REPORT_STATE_1 = 0x31;
        private const byte _REPORT_STATE_2 = 0x32;
        private const byte _REPORT_STATE_3 = 0x33;
        private const byte _REPORT_STATE_4 = 0x34;
        private const byte _REPORT_STATE_5 = 0x35;
        private const byte _REPORT_STATE_6 = 0x36;
        private const byte _REPORT_STATE_7 = 0x37;
        private const byte _REPORT_STATE_8 = 0x38;
        private const byte _REPORT_STATE_9 = 0x39;

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

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

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
        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;

        public IDevice DeviceHackRef => _device;

        ControllerState State = new ControllerState();
        ControllerState OldState = null;



        static byte[] data = new byte[] {
                0x02, // 0x31 // report id
       /*  1 */ 0x00, // 0x03 rumble emulation 0xF0
                      // 0x04 right trigger motor
                      // 0x08 left trigger motor
                      // 0x10
                      // 0x20
                      // 0x40
                      // 0x80
       /*  2 */ 0x00, // 0x01 Mute light, off/on/blink
                      // 0x02
                      // 0x04 LED color
                      // 0x08 all lights turn off?
                      // 0x10 5 indiactor lights
                      // 0x20
                      // 0x40
                      // 0x80
       /*  3 */ 0x00, // Rumble right (weak)
       /*  4 */ 0x00, // Rumble left (strong)
       /*  5 */ 0x00,
       /*  6 */ 0x00,
       /*  7 */ 0x00,
       /*  8 */ 0x00, // 0x02 activated mic
       /*  9 */ 0x00, // 0x00 Mute light off
                      // 0x01 Mute light on
                      // 0x02 Mute light fade on/mid
                      // no other values change anything?
       /* 10 */ 0x00, // 0x10 sets unknown bit in byte 53
       /* 11 */ 0x00, // Right Trigger Motor // 06 26 0F 35 B3 00 EF A2 08 D6 39 acts like a machine gun
       /* 12 */ 0x00, //                     // 10 06 09 21 38 58 53 7C 48 10 57 acts like a machine gun
       /* 13 */ 0x00, //                     //
       /* 14 */ 0x00, //                     //
       /* 15 */ 0x00, //                     //
       /* 16 */ 0x00, //                     //
       /* 17 */ 0x00, //                     //
       /* 18 */ 0x00, //                     //
       /* 19 */ 0x00, //                     //
       /* 20 */ 0x00, //                     //
       /* 21 */ 0x00, //                     //
       /* 22 */ 0x00, // Left Trigger Motor  // 96 25 B9 93 D4 8E CC 8D AE A6 23 acts like 2stage
       /* 23 */ 0x00, //
       /* 24 */ 0x00, //
       /* 25 */ 0x00, //
       /* 26 */ 0x00, //
       /* 27 */ 0x00, //
       /* 28 */ 0x00, //
       /* 29 */ 0x00, //
       /* 30 */ 0x00, //
       /* 31 */ 0x00, //
       /* 32 */ 0x00, //
       /* 33 */ 0x00, 0x00, 0x00, 0x00, // copied into range 43-46
       /* 37 */ 0x00,
       /* 38 */ 0x00,
       /* 39 */ 0x00, // 0x01 - enable brightness change
                      // 0x02 - enable color light fade anim
                      // 0x04
                      // 0x08
                      // 0x10
                      // 0x20
                      // 0x40
                      // 0x80
       /* 40 */ 0x00,
       /* 41 */ 0x00, // 0x01 - Unknown bit in byte 54
       /* 42 */ 0x00, // 0x00 - nothing?
                      // 0x01 - fade in
                      // 0x02 - fade out
       /* 43 */ 0x00, // 0x00 - bright
                      // 0x01 - mid
                      // 0x02 - dim
       /* 44 */ 0x00, // 0x1f are light bits for the 5 lights
       /* 45 */ 0x00, // LED Red
       /* 46 */ 0x00, // LED Green
       /* 47 */ 0x00, // LED Blue
       /* 48 */ 0x00,
       /* 49 */ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

        /*private void SendReport()
        {
            {
                byte[] outDataFinal = null;
                if (_device.DevicePath.Contains(@"00001124-0000-1000-8000-00805f9b34fb"))
                {
                    outDataFinal = new byte[] {
                        0xa2,
                        0x31,
                        0x02
                    }.Concat(data.Skip(1)).Concat(new byte[44]).Take(79).ToArray();

                    Crc32 crcE = new Crc32();
                    byte[] crc = crcE.ComputeHash(outDataFinal, 0, outDataFinal.Length - 4);

                    outDataFinal[outDataFinal.Length - 1] = crc[0];
                    outDataFinal[outDataFinal.Length - 2] = crc[1];
                    outDataFinal[outDataFinal.Length - 3] = crc[2];
                    outDataFinal[outDataFinal.Length - 4] = crc[3];
                }
                else
                {
                    outDataFinal = new byte[] { 0x00 }.Concat(data).Concat(new byte[99]).Take(79).ToArray();
                }
                bool success = _device.WriteReport(outDataFinal.Skip(1).ToArray());
            }
        }*/

        public DualSenseController(HidDevice device, EConnectionType ConnectionType = EConnectionType.Unknown)
        {
            this.ConnectionType = ConnectionType;

            State.Controls["cluster_left"] = new ControlDPad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumper_left"] = new ControlButton();
            State.Controls["bumper_right"] = new ControlButton();
            //State.Controls["bumpers2"] = new ControlButtonPair();
            State.Controls["trigger_left"] = new ControlTrigger();
            State.Controls["trigger_right"] = new ControlTrigger();
            State.Controls["menu_left"] = new ControlButton();
            State.Controls["menu_right"] = new ControlButton();
            State.Controls["home"] = new ControlButton();
            State.Controls["mute"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStickWithClick();
            State.Controls["stick_right"] = new ControlStickWithClick();
            State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
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
                    if (ConnectionType == EConnectionType.Bluetooth && reportData.ReportId == 0x01)
                    {
                        (StateInFlight.Controls["stick_left"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 0] - 128) / 128f;
                        (StateInFlight.Controls["stick_left"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 1] - 128) / 128f;
                        (StateInFlight.Controls["stick_right"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 2] - 128) / 128f;
                        (StateInFlight.Controls["stick_right"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 3] - 128) / 128f;

                        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[baseOffset + 4] & 128) == 128;
                        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[baseOffset + 4] & 64) == 64;
                        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[baseOffset + 4] & 32) == 32;
                        (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[baseOffset + 4] & 16) == 16;

                        switch ((reportData.ReportBytes[baseOffset + 4] & 0x0f))
                        {
                            case 0: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North; break;
                            case 1: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast; break;
                            case 2: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East; break;
                            case 3: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast; break;
                            case 4: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South; break;
                            case 5: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest; break;
                            case 6: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West; break;
                            case 7: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest; break;
                            default: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None; break;
                        }

                        (StateInFlight.Controls["stick_right"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 5] & 128) == 128;
                        (StateInFlight.Controls["stick_left"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 5] & 64) == 64;
                        (StateInFlight.Controls["menu_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 32) == 32;
                        (StateInFlight.Controls["menu_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 16) == 16;
                        //(StateInFlight.Controls["bumpers2"] as IControlButton).Right.Button0 = (reportData.ReportBytes[baseOffset + 5] & 8) == 8;
                        //(StateInFlight.Controls["bumpers2"] as IControlButton).Left.Button0 = (reportData.ReportBytes[baseOffset + 5] & 4) == 4;
                        (StateInFlight.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 2) == 2;
                        (StateInFlight.Controls["bumper_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 5] & 1) == 1;

                        // counter
                        // bld.Append((reportData.ReportBytes[baseOffset + 6] & 0xfc).ToString().PadLeft(3, '0'));

                        (StateInFlight.Controls["home"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 6] & 0x1) == 0x1;
                        (StateInFlight.Controls["touch_center"] as ControlTouch).Click = (reportData.ReportBytes[baseOffset + 6] & 0x2) == 0x2;
                        (StateInFlight.Controls["trigger_left"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 7] / byte.MaxValue;
                        (StateInFlight.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 8] / byte.MaxValue;
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
                            (StateInFlight.Controls["stick_left"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 0] - 128) / 128f;
                            (StateInFlight.Controls["stick_left"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 1] - 128) / 128f;
                            (StateInFlight.Controls["stick_right"] as IControlStickWithClick).X = (reportData.ReportBytes[baseOffset + 2] - 128) / 128f;
                            (StateInFlight.Controls["stick_right"] as IControlStickWithClick).Y = (reportData.ReportBytes[baseOffset + 3] - 128) / 128f;

                            (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[baseOffset + 7] & 128) == 128;
                            (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[baseOffset + 7] & 64) == 64;
                            (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[baseOffset + 7] & 32) == 32;
                            (StateInFlight.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[baseOffset + 7] & 16) == 16;

                            switch ((reportData.ReportBytes[baseOffset + 7] & 0x0f))
                            {
                                case 0: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North; break;
                                case 1: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast; break;
                                case 2: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East; break;
                                case 3: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast; break;
                                case 4: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South; break;
                                case 5: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest; break;
                                case 6: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West; break;
                                case 7: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest; break;
                                default: (StateInFlight.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None; break;
                            }

                            (StateInFlight.Controls["stick_right"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 8] & 128) == 128;
                            (StateInFlight.Controls["stick_left"] as IControlStickWithClick).Click = (reportData.ReportBytes[baseOffset + 8] & 64) == 64;
                            (StateInFlight.Controls["menu_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 32) == 32;
                            (StateInFlight.Controls["menu_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 16) == 16;
                            //(StateInFlight.Controls["bumpers2"] as ControlButtonPair).Right.Button0 = (reportData.ReportBytes[baseOffset + 8] & 8) == 8;
                            //(StateInFlight.Controls["bumpers2"] as ControlButtonPair).Left.Button0 = (reportData.ReportBytes[baseOffset + 8] & 4) == 4;
                            (StateInFlight.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 2) == 2;
                            (StateInFlight.Controls["bumper_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 8] & 1) == 1;

                            (StateInFlight.Controls["home"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 9] & 0x1) == 0x1;
                            (StateInFlight.Controls["touch_center"] as ControlTouch).Click = (reportData.ReportBytes[baseOffset + 9] & 0x2) == 0x2;
                            (StateInFlight.Controls["mute"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[baseOffset + 9] & 0x4) == 0x4;
                            (StateInFlight.Controls["trigger_left"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 4] / byte.MaxValue;
                            (StateInFlight.Controls["trigger_right"] as IControlTrigger).AnalogStage1 = (float)reportData.ReportBytes[baseOffset + 5] / byte.MaxValue;

                            (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityX = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 15);
                            (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityZ = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 17);
                            (StateInFlight.Controls["motion"] as ControlMotion).AngularVelocityY = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 19);
                            (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerX = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 21);
                            (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerY = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 23);
                            (StateInFlight.Controls["motion"] as ControlMotion).AccelerometerZ = BitConverter.ToInt16(reportData.ReportBytes, baseOffset + 25);

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

                                (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(0, Finger1, (F1X / 1919f) * 2f - 1f, (F1Y / 1079f) * 2f - 1f, TimeDelta); // everything beyond 1073 is not reliably producible
                                (StateInFlight.Controls["touch_center"] as ControlTouch).AddTouch(1, Finger2, (F2X / 1919f) * 2f - 1f, (F2Y / 1079f) * 2f - 1f, TimeDelta); // everything beyond 1073 is not reliably producible

                                last_touch_timestamp = touch_timestamp;
                            }

                            touch_last_frame = TouchDataCount > 0;

                            // bring OldState in line with new State
                            OldState = State;
                            State = StateInFlight;

                            ControllerStateUpdate?.Invoke(this, State);
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
    }
}
