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

        public IDevice DeviceHackRef => _device;
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
            State.Controls["bumpers"] = new ControlButtonPair(ButtonProperties.CMB_Bumper);
            State.Controls["triggers"] = new ControlButtonPair(ButtonProperties.CMB_Trigger);
            State.Controls["menu"] = new ControlButtonPair(ButtonProperties.CMB_Button);
            State.Controls["home"] = new ControlButton(ButtonProperties.CMB_Button);
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);

            IsPresent = true;
            //Connected = true;

            _device = device;
            Initalized = 0;

            ControllerTypeCode = _DEVICE_XBOX360;

            _device.DeviceReport += OnReport;
        }
        public void Dispose()
        {
        }

        public ControllerState GetState()
        {
            return State;
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
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    if (reportData.sThumbLX.HasValue) (StateInFlight.Controls["stick_left" ] as ControlStick).X = reportData.sThumbLX.Value *  1.0f / Int16.MaxValue;
                    if (reportData.sThumbLY.HasValue) (StateInFlight.Controls["stick_left" ] as ControlStick).Y = reportData.sThumbLY.Value * -1.0f / Int16.MaxValue;
                    if (reportData.sThumbRX.HasValue) (StateInFlight.Controls["stick_right"] as ControlStick).X = reportData.sThumbRX.Value *  1.0f / Int16.MaxValue;
                    if (reportData.sThumbRY.HasValue) (StateInFlight.Controls["stick_right"] as ControlStick).Y = reportData.sThumbRY.Value * -1.0f / Int16.MaxValue;

                    if (reportData.wButtons.HasValue)
                    {
                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonN = (reportData.wButtons.Value & 0x8000) == 0x8000;
                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonE = (reportData.wButtons.Value & 0x2000) == 0x2000;
                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonS = (reportData.wButtons.Value & 0x1000) == 0x1000;
                        (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonW = (reportData.wButtons.Value & 0x4000) == 0x4000;

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
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.NorthEast;
                            }
                            else if (DPadLeft)
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.NorthWest;
                            }
                            else
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.North;
                            }
                        }
                        else if (DPadDown)
                        {
                            if (DPadRight)
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.SouthEast;
                            }
                            else if (DPadLeft)
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.SouthWest;
                            }
                            else
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.South;
                            }
                        }
                        else
                        {
                            if (DPadRight)
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.East;
                            }
                            else if (DPadLeft)
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.West;
                            }
                            else
                            {
                                (StateInFlight.Controls["cluster_left"] as ControlDPad).Direction = EDPadDirection.None;
                            }
                        }

                        (StateInFlight.Controls["stick_right"] as ControlStick).Click = (reportData.wButtons.Value & 0x0080) == 0x0080;
                        (StateInFlight.Controls["stick_left" ] as ControlStick).Click = (reportData.wButtons.Value & 0x0040) == 0x0040;
                        (StateInFlight.Controls["menu"       ] as ControlButtonPair).Right.DigitalStage1 = (reportData.wButtons.Value & 0x0010) == 0x0010;
                        (StateInFlight.Controls["menu"       ] as ControlButtonPair).Left.DigitalStage1  = (reportData.wButtons.Value & 0x0020) == 0x0020;
                        (StateInFlight.Controls["bumpers"    ] as ControlButtonPair).Right.DigitalStage1 = (reportData.wButtons.Value & 0x0200) == 0x0200;
                        (StateInFlight.Controls["bumpers"    ] as ControlButtonPair).Left.DigitalStage1  = (reportData.wButtons.Value & 0x0100) == 0x0100;
                        (StateInFlight.Controls["home"       ] as ControlButton).DigitalStage1 = (reportData.wButtons.Value & 0x0400) == 0x0400;
                    }

                    //(State.Controls["home"] as ControlButton).Button0 = (buttons & 0x1) == 0x1;
                    if (reportData.bLeftTrigger.HasValue)  (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.AnalogStage1  = (float)reportData.bLeftTrigger.Value  / byte.MaxValue;
                    if (reportData.bRightTrigger.HasValue) (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.AnalogStage1 = (float)reportData.bRightTrigger.Value / byte.MaxValue;

                    // bring OldState in line with new State
                    State = StateInFlight;

                    ControllerStateUpdate?.Invoke(this, State);
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

            Initalized = 0;
            //_device.CloseDevice();
        }

        public void SetActiveAlternateController(string ControllerID) { }

    }
}