using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.SixtyBeat
{
    public class SixtyBeatGamepadController : IController
    {
        public EConnectionType ConnectionType => EConnectionType.Unknown;


        public string[] ConnectionTypeCode { get; private set; }
        public string[] ControllerTypeCode { get; private set; }
        public string Name => "SixtyBeat Gamepad";
        public string[] NameDetails
        {
            get
            {
                return new string[] { _device.DevicePath };
            }
        }
        public bool HasSelectableAlternatives => false;
        public Dictionary<string, string> Alternates => null;
        //public IDevice DeviceHackRef => _device;
        private SixtyBeatAudioDevice _device;
        int reportUsageLock = 0;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        ControllerState State = new ControllerState();
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

        public Dictionary<string, dynamic> DeviceProperties => _device.Properties;
        public bool HasMotion => false;
        public bool IsReady => true;
        public bool IsPresent => true;
        public bool IsVirtual => false;

        bool Initalized;
        public SixtyBeatGamepadController(SixtyBeatAudioDevice device)
        {
            ConnectionTypeCode = new string[] { "CONNECTION_WIRE_35MM_PHONE_TRRS", "CONNECTION_WIRE" };
            ControllerTypeCode = new string[] { "DEVICE_SIXTYBEAT_GAMEPAD", "DEVICE_GAMEPAD" };

            State.Controls["cluster_left"] = new ControlButtonQuad();
            State.Controls["cluster_right"] = new ControlButtonQuad();
            State.Controls["bumper_right"] = new ControlButton();
            State.Controls["bumper_left"] = new ControlButton();
            State.Controls["trigger_right"] = new ControlButton();
            State.Controls["trigger_left"] = new ControlButton();
            State.Controls["menu_right"] = new ControlButton();
            State.Controls["menu_left"] = new ControlButton();
            State.Controls["home"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStickWithClick();
            State.Controls["stick_right"] = new ControlStickWithClick();

            _device = device;
            Initalized = false;

            _device.DeviceReport += OnReport;
            State.ControllerStateUpdate += State_ControllerStateUpdate;
        }
        public void Dispose()
        {
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
            //if (!(reportData is GenericBytesReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.GENB) return;
            GenericBytesReport reportData = (GenericBytesReport)rawReportData;
            if (reportData.CodeString != "SXTYBEAT") return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    State.StartStateChange();
                    try
                    {
                        byte SBJoystick_rawRightY = reverseByte(reportData.ReportBytes[2]);
                        byte SBJoystick_rawRightX = reverseByte(reportData.ReportBytes[3]);
                        byte SBJoystick_rawLeftY = reverseByte(reportData.ReportBytes[4]);
                        byte SBJoystick_rawLeftX = reverseByte(reportData.ReportBytes[5]);

                        (State.Controls["stick_left"] as IControlStickWithClick).X = (float)(((double)SBJoystick_rawLeftX + (double)SBJoystick_rawLeftX) / 240.0 + -1.0);
                        (State.Controls["stick_left"] as IControlStickWithClick).Y = (float)(((double)SBJoystick_rawLeftY + (double)SBJoystick_rawLeftY) / 240.0 + -1.0);
                        (State.Controls["stick_right"] as IControlStickWithClick).X = (float)(((double)SBJoystick_rawRightX + (double)SBJoystick_rawRightX) / 240.0 + -1.0);
                        (State.Controls["stick_right"] as IControlStickWithClick).Y = (float)(((double)SBJoystick_rawRightY + (double)SBJoystick_rawRightY) / 240.0 + -1.0);

                        (State.Controls["cluster_left"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[0] & 0x08) == 0x08;
                        (State.Controls["cluster_left"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[6] & 0x20) == 0x20;
                        (State.Controls["cluster_left"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[6] & 0x40) == 0x40;
                        (State.Controls["cluster_left"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[6] & 0x80) == 0x80;

                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = (reportData.ReportBytes[1] & 0x10) == 0x10;
                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = (reportData.ReportBytes[1] & 0x20) == 0x20;
                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = (reportData.ReportBytes[1] & 0x02) == 0x02;
                        (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = (reportData.ReportBytes[1] & 0x01) == 0x01;

                        (State.Controls["stick_right"] as IControlStickWithClick).Click = (reportData.ReportBytes[6] & 0x10) == 0x10;
                        (State.Controls["stick_left"] as IControlStickWithClick).Click = (reportData.ReportBytes[6] & 0x08) == 0x08;
                        (State.Controls["menu_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[1] & 0x80) == 0x80;
                        (State.Controls["menu_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[1] & 0x40) == 0x40;
                        (State.Controls["bumper_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[0] & 0x01) == 0x01;
                        (State.Controls["bumper_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[0] & 0x04) == 0x04;
                        (State.Controls["trigger_right"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[1] & 0x08) == 0x08;
                        (State.Controls["trigger_left"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[0] & 0x02) == 0x02;

                        (State.Controls["home"] as IControlButton).DigitalStage1 = (reportData.ReportBytes[1] & 0x04) == 0x04;
                    }
                    finally
                    {
                        State.EndStateChange();
                    }
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

        private object InitalizeLock = new object();
        public void Initalize()
        {
            lock (InitalizeLock)
            {
                if (Initalized) return;

                if (_device.StartReading())
                    Initalized = true;
            }
        }

        public void DeInitalize()
        {
            lock (InitalizeLock)
            {
                if (!Initalized) return;

                _device.CloseDevice();
                Initalized = false;
            }
        }

        public void SetActiveAlternateController(string ControllerID) { }

        public bool SetControlState(string control, string state)
        {
            return false;
        }

    }
}