﻿using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
{
    public class XInputController : IController
    {
        public EConnectionType ConnectionType => EConnectionType.Unknown;


        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }


        public IDevice DeviceHackRef => _device;
        private XInputDevice _device;
        int stateUsageLock = 0, reportUsageLock = 0;

        public event ControllerNameUpdateEvent ControllerNameUpdated;

        ControllerState State = new ControllerState();
        ControllerState OldState = new ControllerState();

        public bool HasMotion => false;

        int Initalized;
        public XInputController(XInputDevice device)
        {
            ConnectionTypeCode = new string[] { "UNKNOWN" };
            ControllerTypeCode = new string[] { "XBOX", "GAMEPAD" };

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

            _device.ControllerNameUpdated += OnReport;
        }

        public string GetName()
        {
            return _device.DevicePath;
        }

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

        private void OnReport(byte[] reportData)
        {
            if (Initalized < 1) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                {
                    (State.Controls["stick_left"] as ControlStick).PendingX = BitConverter.ToInt16(reportData, 4) * 1.0f / Int16.MaxValue;
                    (State.Controls["stick_left"] as ControlStick).PendingY = BitConverter.ToInt16(reportData, 6) * -1.0f / Int16.MaxValue;
                    (State.Controls["stick_right"] as ControlStick).PendingX = BitConverter.ToInt16(reportData, 8) * 1.0f / Int16.MaxValue;
                    (State.Controls["stick_right"] as ControlStick).PendingY = BitConverter.ToInt16(reportData, 10) * -1.0f / Int16.MaxValue;

                    UInt16 buttons = BitConverter.ToUInt16(reportData, 0);

                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonN = (buttons & 0x8000) == 0x8000;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonE = (buttons & 0x2000) == 0x2000;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonS = (buttons & 0x1000) == 0x1000;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonW = (buttons & 0x4000) == 0x4000;

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
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.NorthEast;
                        }
                        else if (DPadLeft)
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.NorthWest;
                        }
                        else
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.North;
                        }
                    }
                    else if (DPadDown)
                    {
                        if (DPadRight)
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.SouthEast;
                        }
                        else if (DPadLeft)
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.SouthWest;
                        }
                        else
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.South;
                        }
                    }
                    else
                    {
                        if (DPadRight)
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.East;
                        }
                        else if (DPadLeft)
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.West;
                        }
                        else
                        {
                            (State.Controls["quad_left"] as ControlDPad).PendingDirection = EDPadDirection.None;
                        }
                    }


                    (State.Controls["stick_right"] as ControlStick).PendingClick = (buttons & 0x0080) == 0x0080;
                    (State.Controls["stick_left"] as ControlStick).PendingClick = (buttons & 0x0040) == 0x0040;
                    (State.Controls["menu"] as ControlButtonPair).Right.PendingButton0 = (buttons & 0x0010) == 0x0010;
                    (State.Controls["menu"] as ControlButtonPair).Left.PendingButton0 = (buttons & 0x0020) == 0x0020;
                    (State.Controls["bumpers"] as ControlButtonPair).Right.PendingButton0 = (buttons & 0x0200) == 0x0200;
                    (State.Controls["bumpers"] as ControlButtonPair).Left.PendingButton0 = (buttons & 0x0100) == 0x0100;

                    //(State.Controls["home"] as ControlButton).Button0 = (buttons & 0x1) == 0x1;
                    (State.Controls["triggers"] as ControlTriggerPair).Left.PendingAnalog = (float)reportData[2] / byte.MaxValue;
                    (State.Controls["triggers"] as ControlTriggerPair).Right.PendingAnalog = (float)reportData[3] / byte.MaxValue;

                    foreach (string controlKey in State.Controls.Keys)
                    {
                        State.Controls[controlKey].ProcessPendingInputs();
                    }

                    ControllerState NewState = GetState();
                    //OnStateUpdated(NewState);
                }
                Interlocked.Exchange(ref reportUsageLock, 0);
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
    }
}