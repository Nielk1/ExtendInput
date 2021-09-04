﻿using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
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
        public IDevice DeviceHackRef => _device;
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

        public bool HasMotion => false;
        public bool IsReady => true;
        public bool IsPresent => true;
        public bool IsVirtual => false;

        bool Initalized;
        public SixtyBeatGamepadController(SixtyBeatAudioDevice device)
        {
            ConnectionTypeCode = new string[] { "CONNECTION_WIRE_35MM_PHONE_TRRS", "CONNECTION_WIRE" };
            ControllerTypeCode = new string[] { "DEVICE_SIXTYBEAT_GAMEPAD", "DEVICE_GAMEPAD" };

            State.Controls["quad_left"] = new ControlButtonQuad();
            State.Controls["quad_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair();
            State.Controls["bumpers2"] = new ControlButtonPair();
            State.Controls["menu"] = new ControlButtonPair();
            State.Controls["home"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);

            _device = device;
            Initalized = false;

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
            //if (!(reportData is GenericBytesReport)) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.GENB) return;
            GenericBytesReport reportData = (GenericBytesReport)rawReportData;
            if (reportData.CodeString != "SXTYBEAT") return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    byte SBJoystick_rawRightY = reverseByte(reportData.ReportBytes[2]);
                    byte SBJoystick_rawRightX = reverseByte(reportData.ReportBytes[3]);
                    byte SBJoystick_rawLeftY = reverseByte(reportData.ReportBytes[4]);
                    byte SBJoystick_rawLeftX = reverseByte(reportData.ReportBytes[5]);

                    (StateInFlight.Controls["stick_left"] as ControlStick).X = (float)(((double)SBJoystick_rawLeftX + (double)SBJoystick_rawLeftX) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_left"] as ControlStick).Y = (float)(((double)SBJoystick_rawLeftY + (double)SBJoystick_rawLeftY) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_right"] as ControlStick).X = (float)(((double)SBJoystick_rawRightX + (double)SBJoystick_rawRightX) / 240.0 + -1.0);
                    (StateInFlight.Controls["stick_right"] as ControlStick).Y = (float)(((double)SBJoystick_rawRightY + (double)SBJoystick_rawRightY) / 240.0 + -1.0);

                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonN = (reportData.ReportBytes[0] & 0x08) == 0x08;
                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonE = (reportData.ReportBytes[6] & 0x20) == 0x20;
                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonS = (reportData.ReportBytes[6] & 0x40) == 0x40;
                    (StateInFlight.Controls["quad_left"] as ControlButtonQuad).ButtonW = (reportData.ReportBytes[6] & 0x80) == 0x80;

                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonN = (reportData.ReportBytes[1] & 0x10) == 0x10;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonE = (reportData.ReportBytes[1] & 0x20) == 0x20;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonS = (reportData.ReportBytes[1] & 0x02) == 0x02;
                    (StateInFlight.Controls["quad_right"] as ControlButtonQuad).ButtonW = (reportData.ReportBytes[1] & 0x01) == 0x01;

                    (StateInFlight.Controls["stick_right"] as ControlStick).Click = (reportData.ReportBytes[6] & 0x10) == 0x10;
                    (StateInFlight.Controls["stick_left"] as ControlStick).Click = (reportData.ReportBytes[6] & 0x08) == 0x08;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Right.Button0 = (reportData.ReportBytes[1] & 0x80) == 0x80;
                    (StateInFlight.Controls["menu"] as ControlButtonPair).Left.Button0 = (reportData.ReportBytes[1] & 0x40) == 0x40;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Right.Button0 = (reportData.ReportBytes[0] & 0x01) == 0x01;
                    (StateInFlight.Controls["bumpers"] as ControlButtonPair).Left.Button0 = (reportData.ReportBytes[0] & 0x04) == 0x04;
                    (StateInFlight.Controls["bumpers2"] as ControlButtonPair).Right.Button0 = (reportData.ReportBytes[1] & 0x08) == 0x08;
                    (StateInFlight.Controls["bumpers2"] as ControlButtonPair).Left.Button0 = (reportData.ReportBytes[0] & 0x02) == 0x02;

                    (StateInFlight.Controls["home"] as ControlButton).Button0 = (reportData.ReportBytes[1] & 0x04) == 0x04;

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

    }
}