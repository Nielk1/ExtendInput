using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
{
    public class SixtyBeatGamepadController : IController
    {
        public EConnectionType ConnectionType => EConnectionType.Unknown;


        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }


        public IDevice DeviceHackRef => _device;
        private SixtyBeatAudioDevice _device;
        int stateUsageLock = 0, reportUsageLock = 0;

        public event ControllerNameUpdateEvent ControllerNameUpdated;

        ControllerState State = new ControllerState();
        ControllerState OldState = new ControllerState();

        public bool HasMotion => false;

        int Initalized;
        public SixtyBeatGamepadController(SixtyBeatAudioDevice device)
        {
            ConnectionTypeCode = new string[] { "UNKNOWN" };
            ControllerTypeCode = new string[] { "GAMEPAD" };

            State.Controls["quad_left"] = new ControlButtonQuad();
            State.Controls["quad_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair();
            State.Controls["bumpers2"] = new ControlButtonPair();
            State.Controls["menu"] = new ControlButtonPair();
            State.Controls["home"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);

            _device = device;
            Initalized = 0;

            _device.DeviceReport += OnReport;
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
            //if (Initalized < 1) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                {
                    byte SBJoystick_rawRightY = reverseByte(reportData[2]);
                    byte SBJoystick_rawRightX = reverseByte(reportData[3]);
                    byte SBJoystick_rawLeftY = reverseByte(reportData[4]);
                    byte SBJoystick_rawLeftX = reverseByte(reportData[5]);

                    (State.Controls["stick_left"] as ControlStick).PendingX = (float)(((double)SBJoystick_rawLeftX + (double)SBJoystick_rawLeftX) / 240.0 + -1.0);
                    (State.Controls["stick_left"] as ControlStick).PendingY = (float)(((double)SBJoystick_rawLeftY + (double)SBJoystick_rawLeftY) / 240.0 + -1.0);
                    (State.Controls["stick_right"] as ControlStick).PendingX = (float)(((double)SBJoystick_rawRightX + (double)SBJoystick_rawRightX) / 240.0 + -1.0);
                    (State.Controls["stick_right"] as ControlStick).PendingY = (float)(((double)SBJoystick_rawRightY + (double)SBJoystick_rawRightY) / 240.0 + -1.0);

                    (State.Controls["quad_left"] as ControlButtonQuad).PendingButtonN = (reportData[0] & 0x08) == 0x08;
                    (State.Controls["quad_left"] as ControlButtonQuad).PendingButtonE = (reportData[6] & 0x20) == 0x20;
                    (State.Controls["quad_left"] as ControlButtonQuad).PendingButtonS = (reportData[6] & 0x40) == 0x40;
                    (State.Controls["quad_left"] as ControlButtonQuad).PendingButtonW = (reportData[6] & 0x80) == 0x80;

                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonN = (reportData[1] & 0x10) == 0x10;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonE = (reportData[1] & 0x20) == 0x20;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonS = (reportData[1] & 0x02) == 0x02;
                    (State.Controls["quad_right"] as ControlButtonQuad).PendingButtonW = (reportData[1] & 0x01) == 0x01;

                    (State.Controls["stick_right"] as ControlStick).PendingClick = (reportData[6] & 0x10) == 0x10;
                    (State.Controls["stick_left"] as ControlStick).PendingClick = (reportData[6] & 0x08) == 0x08;
                    (State.Controls["menu"] as ControlButtonPair).Right.PendingButton0 = (reportData[1] & 0x80) == 0x80;
                    (State.Controls["menu"] as ControlButtonPair).Left.PendingButton0 = (reportData[1] & 0x40) == 0x40;
                    (State.Controls["bumpers"] as ControlButtonPair).Right.PendingButton0 = (reportData[0] & 0x01) == 0x01;
                    (State.Controls["bumpers"] as ControlButtonPair).Left.PendingButton0 = (reportData[0] & 0x04) == 0x04;
                    (State.Controls["bumpers2"] as ControlButtonPair).Right.PendingButton0 = (reportData[1] & 0x08) == 0x08;
                    (State.Controls["bumpers2"] as ControlButtonPair).Left.PendingButton0 = (reportData[0] & 0x02) == 0x02;

                    (State.Controls["home"] as ControlButton).PendingButton0 = (reportData[1] & 0x04) == 0x04;

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

        static byte reverseByte(byte a1)
        {
            byte v1 = a1;
            byte v2 = 0;
            for (int i = 0; i < 8; i++)
            {
                v2 = (byte)((v1 & 1) | (v2 << 1));
                v1 >>= 1;
            }
            return v2;
        }

        public void Identify()
        {
            
        }

        public void Initalize()
        {
            if (Initalized > 1) return;

            HalfInitalize();

            Initalized = 2;
            //_device.StartReading();
        }

        public void HalfInitalize()
        {
            if (Initalized > 0) return;

            Initalized = 1;

            if (ConnectionType == EConnectionType.Dongle)
            {
                //_device.StartReading();
            }
        }

        public void DeInitalize()
        {
            if (Initalized == 0) return;

            //_device.StopReading();

            Initalized = 0;
            //_device.CloseDevice();
        }
    }
}