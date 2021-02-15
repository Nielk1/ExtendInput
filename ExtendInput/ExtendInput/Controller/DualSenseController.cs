using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
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

        public bool SensorsEnabled;
        private HidDevice _device;
        int stateUsageLock = 0, reportUsageLock = 0;
        private byte last_touch_timestamp;
        private bool touch_last_frame;
        //private DateTime tmp = DateTime.Now;

        public event ControllerNameUpdateEvent ControllerNameUpdated;

        public bool HasMotion => true;

        #region DATA STRUCTS

        public ControllerState GetState()
        {
            if (0 == Interlocked.Exchange(ref stateUsageLock, 1))
            {
                ControllerState newState = (ControllerState)State.Clone();
                State = newState;
                Interlocked.Exchange(ref stateUsageLock, 0);
            }
            return State;
        }
        #endregion

        public EConnectionType ConnectionType { get; private set; }

        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }


        public IDevice DeviceHackRef => _device;

        ControllerState State = new ControllerState();
        ControllerState OldState = new ControllerState();

        int Initalized;

        public delegate void StateUpdatedEventHandler(object sender, ControllerState e);
        public event StateUpdatedEventHandler StateUpdated;
        protected virtual void OnStateUpdated(ControllerState e)
        {
            StateUpdated?.Invoke(this, e);
        }

        public DualSenseController(HidDevice device, EConnectionType ConnectionType = EConnectionType.Unknown)
        {
            this.ConnectionType = ConnectionType;

            State.Controls["quad_left"] = new ControlDPad();
            State.Controls["quad_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair();
            State.Controls["bumpers2"] = new ControlButtonPair();
            State.Controls["triggers"] = new ControlTriggerPair(HasStage2: false);
            State.Controls["menu"] = new ControlButtonPair();
            State.Controls["home"] = new ControlButton();
            State.Controls["mute"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);
            State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
            State.Controls["motion"] = new ControlMotion();

            // According to this the normalized domain of the DS4 gyro is 1024 units per rad/s: https://gamedev.stackexchange.com/a/87178

            _device = device;

            Initalized = 0;

            _device.DeviceReport += OnReport;
        }

        public void Initalize()
        {
            if (Initalized > 1) return;

            HalfInitalize();

            Initalized = 2;
            _device.StartReading();
        }

        public void HalfInitalize()
        {
            if (Initalized > 0) return;

            // open the device overlapped read so we don't get stuck waiting for a report when we write to it
            //_device.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
            _device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

            //_device.Inserted += DeviceAttachedHandler;
            //_device.Removed += DeviceRemovedHandler;

            //_device.MonitorDeviceEvents = true;

            Initalized = 1;
            touch_last_frame = false;

            //_attached = _device.IsConnected;

            if (ConnectionType == EConnectionType.Dongle)
            {
                _device.StartReading();
            }
        }

        public void DeInitalize()
        {
            if (Initalized == 0) return;

            //_device.Inserted -= DeviceAttachedHandler;
            //_device.Removed -= DeviceRemovedHandler;

            //_device.MonitorDeviceEvents = false;

            _device.StopReading();

            Initalized = 0;
            _device.CloseDevice();
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

        public string GetName()
        {
            string retVal = "Sony DUALSENSE Controller";

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

            return retVal += $" [{Serial ?? "No ID"}]"; ;
        }

        bool DisconnectedBit = false;
        private void OnReport(byte[] reportData)
        {
            if (Initalized < 1) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                OldState = State; // shouldn't this be a clone?
                //if (_attached == false) { return; }

                int baseOffset = 0;
                bool HasStateData = true;
                if (ConnectionType == EConnectionType.Bluetooth && reportData[0] == 0x01)
                {
                    (State.Controls["stick_left"] as ControlStick).PendingX = (reportData[1 + baseOffset + 0] - 128) / 128f;
                    (State.Controls["stick_left"] as ControlStick).PendingY = (reportData[1 + baseOffset + 1] - 128) / 128f;
                    (State.Controls["stick_right"] as ControlStick).PendingX = (reportData[1 + baseOffset + 2] - 128) / 128f;
                    (State.Controls["stick_right"] as ControlStick).PendingY = (reportData[1 + baseOffset + 3] - 128) / 128f;

                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonN = (reportData[1 + baseOffset + 4] & 128) == 128;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonE = (reportData[1 + baseOffset + 4] & 64) == 64;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonS = (reportData[1 + baseOffset + 4] & 32) == 32;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonW = (reportData[1 + baseOffset + 4] & 16) == 16;

                    switch ((reportData[1 + baseOffset + 4] & 0x0f))
                    {
                        case 0: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.North; break;
                        case 1: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.NorthEast; break;
                        case 2: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.East; break;
                        case 3: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.SouthEast; break;
                        case 4: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.South; break;
                        case 5: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.SouthWest; break;
                        case 6: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.West; break;
                        case 7: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.NorthWest; break;
                        default: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.None; break;
                    }

                    (State.Controls["stick_right"] as ControlStick).PendingClick = (reportData[1 + baseOffset + 5] & 128) == 128;
                    (State.Controls["stick_left"] as ControlStick).PendingClick = (reportData[1 + baseOffset + 5] & 64) == 64;
                    (State.Controls["menu"] as ControlButtonPair).Right.PendingButton0 = (reportData[1 + baseOffset + 5] & 32) == 32;
                    (State.Controls["menu"] as ControlButtonPair).Left.PendingButton0 = (reportData[1 + baseOffset + 5] & 16) == 16;
                    (State.Controls["bumpers2"] as ControlButtonPair).Right.PendingButton0 = (reportData[1 + baseOffset + 5] & 8) == 8;
                    (State.Controls["bumpers2"] as ControlButtonPair).Left.PendingButton0 = (reportData[1 + baseOffset + 5] & 4) == 4;
                    (State.Controls["bumpers"] as ControlButtonPair).Right.PendingButton0 = (reportData[1 + baseOffset + 5] & 2) == 2;
                    (State.Controls["bumpers"] as ControlButtonPair).Left.PendingButton0 = (reportData[1 + baseOffset + 5] & 1) == 1;

                    // counter
                    // bld.Append((reportData[1 + baseOffset + 6] & 0xfc).ToString().PadLeft(3, '0'));

                    (State.Controls["home"] as ControlButton).PendingButton0 = (reportData[1 + baseOffset + 6] & 0x1) == 0x1;
                    (State.Controls["touch_center"] as ControlTouch).PendingClick = (reportData[1 + baseOffset + 6] & 0x2) == 0x2;
                    (State.Controls["triggers"] as ControlTriggerPair).Left.PendingAnalog = (float)reportData[1 + baseOffset + 7] / byte.MaxValue;
                    (State.Controls["triggers"] as ControlTriggerPair).Right.PendingAnalog = (float)reportData[1 + baseOffset + 8] / byte.MaxValue;
                }
                else
                {
                    if (ConnectionType == EConnectionType.Bluetooth)
                    {
                        baseOffset = 1;
                        HasStateData = (reportData[1 + 1] & 0x01) == 0x01;
                    }

                    if (HasStateData)
                    {
                        (State.Controls["stick_left"] as ControlStick).PendingX = (reportData[1 + baseOffset + 0] - 128) / 128f;
                        (State.Controls["stick_left"] as ControlStick).PendingY = (reportData[1 + baseOffset + 1] - 128) / 128f;
                        (State.Controls["stick_right"] as ControlStick).PendingX = (reportData[1 + baseOffset + 2] - 128) / 128f;
                        (State.Controls["stick_right"] as ControlStick).PendingY = (reportData[1 + baseOffset + 3] - 128) / 128f;

                        (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonN = (reportData[1 + baseOffset + 7] & 128) == 128;
                        (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonE = (reportData[1 + baseOffset + 7] & 64) == 64;
                        (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonS = (reportData[1 + baseOffset + 7] & 32) == 32;
                        (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonW = (reportData[1 + baseOffset + 7] & 16) == 16;

                        switch ((reportData[1 + baseOffset + 7] & 0x0f))
                        {
                            case 0: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.North; break;
                            case 1: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.NorthEast; break;
                            case 2: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.East; break;
                            case 3: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.SouthEast; break;
                            case 4: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.South; break;
                            case 5: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.SouthWest; break;
                            case 6: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.West; break;
                            case 7: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.NorthWest; break;
                            default: (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.None; break;
                        }

                        (State.Controls["stick_right"] as ControlStick).PendingClick = (reportData[1 + baseOffset + 8] & 128) == 128;
                        (State.Controls["stick_left"] as ControlStick).PendingClick = (reportData[1 + baseOffset + 8] & 64) == 64;
                        (State.Controls["menu"] as ControlButtonPair).Right.PendingButton0 = (reportData[1 + baseOffset + 8] & 32) == 32;
                        (State.Controls["menu"] as ControlButtonPair).Left.PendingButton0 = (reportData[1 + baseOffset + 8] & 16) == 16;
                        (State.Controls["bumpers2"] as ControlButtonPair).Right.PendingButton0 = (reportData[1 + baseOffset + 8] & 8) == 8;
                        (State.Controls["bumpers2"] as ControlButtonPair).Left.PendingButton0 = (reportData[1 + baseOffset + 8] & 4) == 4;
                        (State.Controls["bumpers"] as ControlButtonPair).Right.PendingButton0 = (reportData[1 + baseOffset + 8] & 2) == 2;
                        (State.Controls["bumpers"] as ControlButtonPair).Left.PendingButton0 = (reportData[1 + baseOffset + 8] & 1) == 1;

                        (State.Controls["home"] as ControlButton).PendingButton0 = (reportData[1 + baseOffset + 9] & 0x1) == 0x1;
                        (State.Controls["touch_center"] as ControlTouch).PendingClick = (reportData[1 + baseOffset + 9] & 0x2) == 0x2;
                        (State.Controls["mute"] as ControlButton).PendingButton0 = (reportData[1 + baseOffset + 9] & 0x4) == 0x4;
                        (State.Controls["triggers"] as ControlTriggerPair).Left.PendingAnalog = (float)reportData[1 + baseOffset + 4] / byte.MaxValue;
                        (State.Controls["triggers"] as ControlTriggerPair).Right.PendingAnalog = (float)reportData[1 + baseOffset + 5] / byte.MaxValue;

                        (State.Controls["motion"] as ControlMotion).AngularVelocityX = BitConverter.ToInt16(reportData, 1 + baseOffset + 15);
                        (State.Controls["motion"] as ControlMotion).AngularVelocityZ = BitConverter.ToInt16(reportData, 1 + baseOffset + 17);
                        (State.Controls["motion"] as ControlMotion).AngularVelocityY = BitConverter.ToInt16(reportData, 1 + baseOffset + 19);
                        (State.Controls["motion"] as ControlMotion).AccelerometerX = BitConverter.ToInt16(reportData, 1 + baseOffset + 21);
                        (State.Controls["motion"] as ControlMotion).AccelerometerY = BitConverter.ToInt16(reportData, 1 + baseOffset + 23);
                        (State.Controls["motion"] as ControlMotion).AccelerometerZ = BitConverter.ToInt16(reportData, 1 + baseOffset + 25);

                        //bool DisconnectedFlag = (reportData[1 + baseOffset + 30] & 0x04) == 0x04;
                        //if (DisconnectedFlag != DisconnectedBit)
                        //{
                        //    DisconnectedBit = DisconnectedFlag;
                        //    ControllerNameUpdated?.Invoke();
                        //}

                        //int TouchDataCount = reportData[1 + baseOffset + 32];
                        int TouchDataCount = 1;

                        for (int FingerCounter = 0; FingerCounter < TouchDataCount; FingerCounter++)
                        {
                            bool Finger1 = (reportData[1 + baseOffset + 32 + (FingerCounter * 9)] & 0x80) != 0x80;
                            byte Finger1Index = (byte)(reportData[1 + baseOffset + 32 + (FingerCounter * 9)] & 0x7f);
                            int F1X = reportData[1 + baseOffset + 33 + (FingerCounter * 9)]
                                                   | ((reportData[1 + baseOffset + 34 + (FingerCounter * 9)] & 0xF) << 8);
                            int F1Y = ((reportData[1 + baseOffset + 34 + (FingerCounter * 9)] & 0xF0) >> 4)
                                                    | (reportData[1 + baseOffset + 35 + (FingerCounter * 9)] << 4);

                            bool Finger2 = (reportData[1 + baseOffset + 36 + (FingerCounter * 9)] & 0x80) != 0x80;
                            byte Finger2Index = (byte)(reportData[1 + baseOffset + 36 + (FingerCounter * 9)] & 0x7f);
                            int F2X = reportData[1 + baseOffset + 37 + (FingerCounter * 9)]
                                                   | ((reportData[1 + baseOffset + 38 + (FingerCounter * 9)] & 0xF) << 8);
                            int F2Y = ((reportData[1 + baseOffset + 38 + (FingerCounter * 9)] & 0xF0) >> 4)
                                                    | (reportData[1 + baseOffset + 39 + (FingerCounter * 9)] << 4);

                            byte touch_timestamp = reportData[1 + baseOffset + 41 + (FingerCounter * 9)]; // Touch Pad Counter
                                                                                                          //DateTime tmp_now = DateTime.Now;

                            byte TimeDelta = touch_last_frame ? GetOverflowedDelta(last_touch_timestamp, touch_timestamp) : (byte)0;

                            (State.Controls["touch_center"] as ControlTouch).AddTouch(0, Finger1, (F1X / 1919f) * 2f - 1f, (F1Y / 1079f) * 2f - 1f, TimeDelta); // everything beyond 1073 is not reliably producible
                            (State.Controls["touch_center"] as ControlTouch).AddTouch(1, Finger2, (F2X / 1919f) * 2f - 1f, (F2Y / 1079f) * 2f - 1f, TimeDelta); // everything beyond 1073 is not reliably producible

                            last_touch_timestamp = touch_timestamp;
                        }

                        touch_last_frame = TouchDataCount > 0;

                        foreach (string controlKey in State.Controls.Keys)
                        {
                            State.Controls[controlKey].ProcessPendingInputs();
                        }

                        ControllerState NewState = GetState();
                        OnStateUpdated(NewState);
                    }
                }
                Interlocked.Exchange(ref reportUsageLock, 0);

                if (ConnectionType == EConnectionType.Dongle && DisconnectedBit)
                    Thread.Sleep(1000); // if we're a dongle and we're not connected we might only be partially initalized, so slow roll our read
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
    }
}
