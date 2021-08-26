using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
{
    public class XInputController : IController
    {
        public EConnectionType ConnectionType => EConnectionType.Unknown;

        public string UniqueID
        {
            get
            {
                //if (!string.IsNullOrWhiteSpace(SerialNumber))
                //    return SerialNumber;
                return _device.UniqueKey;
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
        public bool IsPresent => true;

        int Initalized;
        public XInputController(XInputDevice device)
        {
            ConnectionTypeCode = new string[] { "CONNECTION_UNKNOWN" };
            ControllerTypeCode = new string[] { "DEVICEXBOX", "DEVICEGAMEPAD" };

            State.Controls["quad_left"] = new ControlDPad();
            State.Controls["quad_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair();
            State.Controls["triggers"] = new ControlTriggerPair(HasStage2: false);
            State.Controls["menu"] = new ControlButtonPair();
            State.Controls["home"] = new ControlButton();
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

        private void OnReport(byte[] reportData)
        {
            if (Initalized < 1) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    (StateInFlight.Controls["stick_left"] as ControlStick).X = BitConverter.ToInt16(reportData, 4) * 1.0f / Int16.MaxValue;
                    (StateInFlight.Controls["stick_left"] as ControlStick).Y = BitConverter.ToInt16(reportData, 6) * -1.0f / Int16.MaxValue;
                    (StateInFlight.Controls["stick_right"] as ControlStick).X = BitConverter.ToInt16(reportData, 8) * 1.0f / Int16.MaxValue;
                    (StateInFlight.Controls["stick_right"] as ControlStick).Y = BitConverter.ToInt16(reportData, 10) * -1.0f / Int16.MaxValue;

                    UInt16 buttons = BitConverter.ToUInt16(reportData, 0);

                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonN = (buttons & 0x8000) == 0x8000;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonE = (buttons & 0x2000) == 0x2000;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonS = (buttons & 0x1000) == 0x1000;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonW = (buttons & 0x4000) == 0x4000;

                    bool DPadUp = (buttons & 0x0001) == 0x0001;
                    bool DPadDown = (buttons & 0x0002) == 0x0002;
                    bool DPadLeft = (buttons & 0x0004) == 0x0004;
                    bool DPadRight = (buttons & 0x0008) == 0x0008;

                    if (DPadUp && DPadDown)
                        DPadUp = DPadDown = false;

                    if (DPadLeft && DPadRight)
                        DPadLeft = DPadRight = false;

                    if (DPadUp)
                    {
                        if (DPadRight)
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.NorthEast;
                        }
                        else if (DPadLeft)
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.NorthWest;
                        }
                        else
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.North;
                        }
                    }
                    else if (DPadDown)
                    {
                        if (DPadRight)
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.SouthEast;
                        }
                        else if (DPadLeft)
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.SouthWest;
                        }
                        else
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.South;
                        }
                    }
                    else
                    {
                        if (DPadRight)
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.East;
                        }
                        else if (DPadLeft)
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.West;
                        }
                        else
                        {
                            (StateInFlight.Controls["quad_left"] as ControlDPad).Direction = EDPadDirection.None;
                        }
                    }


                    (StateInFlight.Controls["stick_right"] as ControlStick).Click = (buttons & 0x0080) == 0x0080;
                    (StateInFlight.Controls["stick_left"] as ControlStick).Click = (buttons & 0x0040) == 0x0040;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Right.Button0 = (buttons & 0x0010) == 0x0010;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Left.Button0 = (buttons & 0x0020) == 0x0020;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Button0 = (buttons & 0x0200) == 0x0200;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Button0 = (buttons & 0x0100) == 0x0100;

                    //(State.Controls["home"] as ControlButton).Button0 = (buttons & 0x1) == 0x1;
                    (StateInFlight.Controls["triggers"] as ControlTriggerPair).Left.Analog = (float)reportData[2] / byte.MaxValue;
                    (StateInFlight.Controls["triggers"] as ControlTriggerPair).Right.Analog = (float)reportData[3] / byte.MaxValue;

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