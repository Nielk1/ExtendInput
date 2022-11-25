using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.Microsoft
{
    public class XInputController : IController
    {
        #region String Definitions
        private const string ATOM_CONNECTION_XINPUT = "CONNECTION_XINPUT";
        private const string ATOM_CONNECTION_XINPUT_1 = "CONNECTION_XINPUT_1";
        private const string ATOM_CONNECTION_XINPUT_2 = "CONNECTION_XINPUT_2";
        private const string ATOM_CONNECTION_XINPUT_3 = "CONNECTION_XINPUT_3";
        private const string ATOM_CONNECTION_XINPUT_4 = "CONNECTION_XINPUT_4";
        private const string ATOM_DEVICE_GAMEPAD = "DEVICE_GAMEPAD";
        private const string ATOM_DEVICE_UNKNOWN = "DEVICE_UNKNOWN";
        private const string ATOM_DEVICE_NONE = "DEVICE_NONE";
        private const string ATOM_DEVICE_XBOX = "DEVICE_XBOX";
        private const string ATOM_DEVICE_XBOX360 = "DEVICE_XBOX360";

        private readonly string[] _CONNECTION_XINPUT = new string[] { ATOM_CONNECTION_XINPUT };
        private readonly string[][] _CONNECTION_XINPUT_PLAYER = new string[][] {
            new string[] { ATOM_CONNECTION_XINPUT_1, ATOM_CONNECTION_XINPUT },
            new string[] { ATOM_CONNECTION_XINPUT_2, ATOM_CONNECTION_XINPUT },
            new string[] { ATOM_CONNECTION_XINPUT_3, ATOM_CONNECTION_XINPUT },
            new string[] { ATOM_CONNECTION_XINPUT_4, ATOM_CONNECTION_XINPUT },
        };
        private readonly string[] _DEVICE_UNKNOWN = new string[] { ATOM_DEVICE_UNKNOWN };
        private readonly string[] _DEVICE_NONE = new string[] { ATOM_DEVICE_NONE };
        private readonly string[] _DEVICE_XBOX360 = new string[] { ATOM_DEVICE_XBOX360, ATOM_DEVICE_GAMEPAD };

        #endregion String Definitions

        public EConnectionType ConnectionType => EConnectionType.Unknown;

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

        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }
        public string Name
        {
            get
            {
                //return _device.DevicePath;
                return $"USB\\VID_{_device.VendorId:X4}&PID_{_device.ProductId:X4}\\{(_device.XID & 0x0FFFFFFF):X7}";
            }
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

        public Dictionary<string, dynamic> DeviceProperties => _device.Properties;
        //public IDevice DeviceHackRef => _device;
        private XInputDevice _device;
        int reportUsageLock = 0;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        ControllerState State = new ControllerState();


        public bool HasMotion => false;
        public bool IsReady => true;
        public bool IsPresent { get; set; }
        public bool IsVirtual => false;

        //bool Connected;
        SemaphoreSlim ConnectedState = new SemaphoreSlim(1, 1);
        int Initalized;
        public XInputController(XInputDevice device)
        {
            if (device.UserIndex < 4)
            {
                ConnectionTypeCode = _CONNECTION_XINPUT_PLAYER[device.UserIndex];
            }
            else
            {
                ConnectionTypeCode = _CONNECTION_XINPUT;
            }
            ControllerTypeCode = _DEVICE_XBOX360;

            State.Controls["cluster_left"] = new ControlDPad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumper_right"] = new ControlButton();
            State.Controls["bumper_left"] = new ControlButton();
            State.Controls["trigger_right"] = new ControlTrigger();
            State.Controls["trigger_left"] = new ControlTrigger();
            State.Controls["menu_right"] = new ControlButton();
            State.Controls["menu_left"] = new ControlButton();
            State.Controls["home"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStickWithClick();
            State.Controls["stick_right"] = new ControlStickWithClick();

            State.Controls["rumble_left"] = new ControlEccentricRotatingMass(AccessMode.FullControl, IsHeavy: true);
            State.Controls["rumble_right"] = new ControlEccentricRotatingMass(AccessMode.FullControl, IsHeavy: false);

            IsPresent = true;
            //Connected = true;

            _device = device;
            Initalized = 0;

            ControllerTypeCode = _DEVICE_XBOX360;

            _device.DeviceReport += OnReport;
            State.ControllerStateUpdate += State_ControllerStateUpdate;
        }
        public void Dispose()
        {
        }









        bool OutputThreadActive = false;
        Thread OutputThread;
        private void StartOutputThread()
        {
            OutputThreadActive = true;
            // TODO: fix the speed of this thread, it runs too fast, might need per reportID/type throttles or something
            OutputThread = new Thread(() =>
            {
                for (; ; )
                {
                    if (!OutputThreadActive) break;
                    Thread.Sleep(1000 / 60);
                    if (!OutputThreadActive) break;
                    //if(WriteStateDirtyPossible)
                    {
                        IControlEccentricRotatingMass left1 = State.Controls["rumble_left"] as IControlEccentricRotatingMass;
                        IControlEccentricRotatingMass right1 = State.Controls["rumble_right"] as IControlEccentricRotatingMass;

                        if (left1.IsWriteDirty
                        || right1.IsWriteDirty)
                        {
                            _device.SetVibration((ushort)(left1.Power.Value * ushort.MaxValue), (ushort)(right1.Power.Value * ushort.MaxValue));
                            left1.CleanWriteDirty();
                            right1.CleanWriteDirty();
                        }
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












        public ControllerState GetState()
        {
            return State;
        }



        private void State_ControllerStateUpdate(ControlCollection controls)
        {
            ControllerStateUpdate?.Invoke(this, controls);
        }
        private void OnReport(IReport rawReportData)
        {
            //if (!(reportData is XInputReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.XINP) return;
            XInputReport reportData = (XInputReport)rawReportData;

            /*if (reportData.Connected != Connected)
            {
                ConnectedState.Wait();
                try
                {
                    if (reportData.Connected)
                    {
                        ControllerTypeCode = _DEVICE_XBOX360;
                        IsPresent = true;
                        Connected = true;
                    }
                    else
                    {
                        ControllerTypeCode = _DEVICE_NONE;
                        IsPresent = false;
                        Connected = false;
                    }

                    ControllerMetadataUpdate?.Invoke(this);
                }
                finally
                {
                    ConnectedState.Release();
                }
            }*/

            if (Initalized < 1) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                ConnectedState.Wait();
                try
                {
                    State.StartStateChange();
                    try
                    {
                        if (reportData.sThumbLX.HasValue) (State.Controls["stick_left" ] as IControlStickWithClick).X = reportData.sThumbLX.Value *  1.0f / Int16.MaxValue;
                        if (reportData.sThumbLY.HasValue) (State.Controls["stick_left" ] as IControlStickWithClick).Y = reportData.sThumbLY.Value * -1.0f / Int16.MaxValue;
                        if (reportData.sThumbRX.HasValue) (State.Controls["stick_right"] as IControlStickWithClick).X = reportData.sThumbRX.Value *  1.0f / Int16.MaxValue;
                        if (reportData.sThumbRY.HasValue) (State.Controls["stick_right"] as IControlStickWithClick).Y = reportData.sThumbRY.Value * -1.0f / Int16.MaxValue;

                        if (reportData.wButtons.HasValue)
                        {
                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.wButtons.Value & 0x8000) == 0x8000;
                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.wButtons.Value & 0x2000) == 0x2000;
                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.wButtons.Value & 0x1000) == 0x1000;
                            (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.wButtons.Value & 0x4000) == 0x4000;

                            bool DPadUp    = (reportData.wButtons.Value & 0x0001) == 0x0001;
                            bool DPadDown  = (reportData.wButtons.Value & 0x0002) == 0x0002;
                            bool DPadLeft  = (reportData.wButtons.Value & 0x0004) == 0x0004;
                            bool DPadRight = (reportData.wButtons.Value & 0x0008) == 0x0008;

                            if (DPadUp && DPadDown)
                                DPadUp = DPadDown = false;

                            if (DPadLeft && DPadRight)
                                DPadLeft = DPadRight = false;

                            if (DPadUp)
                            {
                                if (DPadRight)
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthEast;
                                }
                                else if (DPadLeft)
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.NorthWest;
                                }
                                else
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.North;
                                }
                            }
                            else if (DPadDown)
                            {
                                if (DPadRight)
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthEast;
                                }
                                else if (DPadLeft)
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.SouthWest;
                                }
                                else
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.South;
                                }
                            }
                            else
                            {
                                if (DPadRight)
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.East;
                                }
                                else if (DPadLeft)
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.West;
                                }
                                else
                                {
                                    (State.Controls["cluster_left"] as IControlDPad).Direction = EDPadDirection.None;
                                }
                            }

                            (State.Controls["stick_right" ] as IControlStickWithClick).Click = (reportData.wButtons.Value & 0x0080) == 0x0080;
                            (State.Controls["stick_left"  ] as IControlStickWithClick).Click = (reportData.wButtons.Value & 0x0040) == 0x0040;
                            (State.Controls["menu_right"  ] as ControlButton).DigitalStage1 = (reportData.wButtons.Value & 0x0010) == 0x0010;
                            (State.Controls["menu_left"   ] as ControlButton).DigitalStage1  = (reportData.wButtons.Value & 0x0020) == 0x0020;
                            (State.Controls["bumper_right"] as ControlButton).DigitalStage1 = (reportData.wButtons.Value & 0x0200) == 0x0200;
                            (State.Controls["bumper_left" ] as ControlButton).DigitalStage1  = (reportData.wButtons.Value & 0x0100) == 0x0100;
                            (State.Controls["home"        ] as IControlButton).DigitalStage1 = (reportData.wButtons.Value & 0x0400) == 0x0400;
                        }

                        //(State.Controls["home"] as ControlButton).Button0 = (buttons & 0x1) == 0x1;
                        if (reportData.bLeftTrigger.HasValue)  (State.Controls["trigger_left"] as ControlTrigger).AnalogStage1  = (float)reportData.bLeftTrigger.Value  / byte.MaxValue;
                        if (reportData.bRightTrigger.HasValue) (State.Controls["trigger_right"] as ControlTrigger).AnalogStage1 = (float)reportData.bRightTrigger.Value / byte.MaxValue;
                    }
                    finally
                    {
                        State.EndStateChange(true);
                    }
                }
                finally
                {
                    ConnectedState.Release();
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }
            }
        }

        public void Identify()
        {
            
        }

        public void Initalize()
        {
            if (Initalized > 1) return;

            HalfInitalize();

            Initalized = 2;
            _device.StartReading();
            StartOutputThread();
        }

        public void HalfInitalize()
        {
            if (Initalized > 0) return;

            Initalized = 1;

            if (ConnectionType == EConnectionType.Dongle)
            {
                _device.StartReading();
            }
        }

        public void DeInitalize()
        {
            if (Initalized == 0) return;

            _device.StopReading();
            StopOutputThread();

            Initalized = 0;
            //_device.CloseDevice();
        }

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
        public bool SetControlState(string control, string state, params object[] args)
        {
            return false;
        }

    }
}