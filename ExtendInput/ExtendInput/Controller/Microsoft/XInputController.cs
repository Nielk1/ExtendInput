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
                return _device.DevicePath;
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
        public bool IsPresent => true;
        public bool IsVirtual => false;

        int Initalized;
        public XInputController(XInputDevice device)
        {
            ConnectionTypeCode = new string[] { "CONNECTION_UNKNOWN" };
            ControllerTypeCode = new string[] { "DEVICE_XBOX", "DEVICE_GAMEPAD" };

            State.Controls["cluster_left"] = new ControlDPad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair(ButtonProperties.CMB_Bumper);
            State.Controls["triggers"] = new ControlButtonPair(ButtonProperties.CMB_Trigger);
            State.Controls["menu"] = new ControlButtonPair(ButtonProperties.CMB_Button);
            State.Controls["home"] = new ControlButton(ButtonProperties.CMB_Button);
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);

            _device = device;
            Initalized = 0;

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
            if (Initalized < 1) return;
            //if (!(reportData is XInputReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.XINP) return;
            XInputReport reportData = (XInputReport)rawReportData;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    (StateInFlight.Controls["stick_left" ] as ControlStick).X = reportData.sThumbLX *  1.0f / Int16.MaxValue;
                    (StateInFlight.Controls["stick_left" ] as ControlStick).Y = reportData.sThumbLY * -1.0f / Int16.MaxValue;
                    (StateInFlight.Controls["stick_right"] as ControlStick).X = reportData.sThumbRX *  1.0f / Int16.MaxValue;
                    (StateInFlight.Controls["stick_right"] as ControlStick).Y = reportData.sThumbRY * -1.0f / Int16.MaxValue;


                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonN = (reportData.wButtons & 0x8000) == 0x8000;
                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonE = (reportData.wButtons & 0x2000) == 0x2000;
                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonS = (reportData.wButtons & 0x1000) == 0x1000;
                    (StateInFlight.Controls["cluster_right"] as ControlButtonQuad).ButtonW = (reportData.wButtons & 0x4000) == 0x4000;

                    bool DPadUp    = (reportData.wButtons & 0x0001) == 0x0001;
                    bool DPadDown  = (reportData.wButtons & 0x0002) == 0x0002;
                    bool DPadLeft  = (reportData.wButtons & 0x0004) == 0x0004;
                    bool DPadRight = (reportData.wButtons & 0x0008) == 0x0008;

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


                    (StateInFlight.Controls["stick_right"] as ControlStick).Click = (reportData.wButtons & 0x0080) == 0x0080;
                    (StateInFlight.Controls["stick_left" ] as ControlStick).Click = (reportData.wButtons & 0x0040) == 0x0040;
                    (StateInFlight.Controls["menu"       ] as ControlButtonPair).Right.Digital = (reportData.wButtons & 0x0010) == 0x0010;
                    (StateInFlight.Controls["menu"       ] as ControlButtonPair).Left.Digital  = (reportData.wButtons & 0x0020) == 0x0020;
                    (StateInFlight.Controls["bumpers"    ] as ControlButtonPair).Right.Digital = (reportData.wButtons & 0x0200) == 0x0200;
                    (StateInFlight.Controls["bumpers"    ] as ControlButtonPair).Left.Digital  = (reportData.wButtons & 0x0100) == 0x0100;

                    //(State.Controls["home"] as ControlButton).Button0 = (buttons & 0x1) == 0x1;
                    (StateInFlight.Controls["triggers"] as ControlButtonPair).Left.Analog  = (float)reportData.bLeftTrigger  / byte.MaxValue;
                    (StateInFlight.Controls["triggers"] as ControlButtonPair).Right.Analog = (float)reportData.bRightTrigger / byte.MaxValue;

                    // bring OldState in line with new State
                    State = StateInFlight;

                    ControllerStateUpdate?.Invoke(this, State);
                }
                finally
                {
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