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


        public delegate void StateUpdatedEventHandler(object sender, ControllerState e);
        public event StateUpdatedEventHandler StateUpdated;

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

        public ControllerState GetState()
        {
            return State;
        }

        private void OnReport(byte[] reportData)
        {
            //if (Initalized < 1) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    byte SBJoystick_rawRightY = reverseByte(reportData[2]);
                    byte SBJoystick_rawRightX = reverseByte(reportData[3]);
                    byte SBJoystick_rawLeftY = reverseByte(reportData[4]);
                    byte SBJoystick_rawLeftX = reverseByte(reportData[5]);

                    (StateInFlight.Controls["stick_left"] as ControlStick).X = (float)(((double)SBJoystick_rawLeftX + (double)SBJoystick_rawLeftX) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_left"] as ControlStick).Y = (float)(((double)SBJoystick_rawLeftY + (double)SBJoystick_rawLeftY) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_right"] as ControlStick).X = (float)(((double)SBJoystick_rawRightX + (double)SBJoystick_rawRightX) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_right"] as ControlStick).Y = (float)(((double)SBJoystick_rawRightY + (double)SBJoystick_rawRightY) / 240.0 + -1.0);

                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonN = (reportData[0] & 0x08) == 0x08;
                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonE = (reportData[6] & 0x20) == 0x20;
                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonS = (reportData[6] & 0x40) == 0x40;
                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonW = (reportData[6] & 0x80) == 0x80;

                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonN = (reportData[1] & 0x10) == 0x10;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonE = (reportData[1] & 0x20) == 0x20;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonS = (reportData[1] & 0x02) == 0x02;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonW = (reportData[1] & 0x01) == 0x01;

                    (StateInFlight.Controls["stick_right"] as ControlStick).Click = (reportData[6] & 0x10) == 0x10;
                    (StateInFlight.Controls["stick_left"] as ControlStick).Click = (reportData[6] & 0x08) == 0x08;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Right.Button0 = (reportData[1] & 0x80) == 0x80;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Left.Button0 = (reportData[1] & 0x40) == 0x40;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Button0 = (reportData[0] & 0x01) == 0x01;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Button0 = (reportData[0] & 0x04) == 0x04;
                    (StateInFlight.Controls["bumpers2"] as ControlButtonPair).Right.Button0 = (reportData[1] & 0x08) == 0x08;
                    (StateInFlight.Controls["bumpers2"] as ControlButtonPair).Left.Button0 = (reportData[0] & 0x02) == 0x02;

                    (StateInFlight.Controls["home"] as ControlButton).Button0 = (reportData[1] & 0x04) == 0x04;

                    // bring OldState in line with new State
                    State = StateInFlight;

                    StateUpdated?.Invoke(this, State);
                }
                finally
                {
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }
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