using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.TourBox
{
    public class TourBoxController : IController
    {
        public const int VENDOR_SILICON_LABS = 0x10C4;
        public const int PRODUCT_CP210X_UART_BRIDGE = 0xEA60;


        #region String Definitions
        private const string ATOM_CONNECTION_UNKNOWN = "CONNECTION_UNKNOWN";
        private const string ATOM_DEVICE_UNKNOWN = "DEVICE_UNKNOWN";

        private readonly string[] _CONNECTION_UNKNOWN = new string[] { ATOM_CONNECTION_UNKNOWN };
        private readonly string[] _DEVICE_UNKNOWN = new string[] { ATOM_DEVICE_UNKNOWN };

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

        public Dictionary<string, dynamic> DeviceProperties => _device.Properties;
        //public IDevice DeviceHackRef => _device;
        private SerialDevice _device;
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
        bool Initalized;
        public TourBoxController(SerialDevice device)
        {
            device.HackDevice.BaudRate = 115200;

            ConnectionTypeCode = _CONNECTION_UNKNOWN;
            ControllerTypeCode = _DEVICE_UNKNOWN;

            State.Controls["button_tall"] = new ControlButton();
            State.Controls["button_side"] = new ControlButton();
            State.Controls["button_top"] = new ControlButton();
            State.Controls["button_short"] = new ControlButton();
            State.Controls["button_c1"] = new ControlButton();
            State.Controls["button_c2"] = new ControlButton();
            State.Controls["button_tour"] = new ControlButton();
            State.Controls["dpad"] = new ControlButtonQuad();
            State.Controls["spinner_knob"] = new ControlSpinnerRelative() { Divisions = 30f };
            State.Controls["spinner_dial"] = new ControlSpinnerRelative() { Divisions = 30f };
            State.Controls["scrollwheel"] = new ControlScrollWheelRelative() { HasClick = true, Divisions = 24f };

            IsPresent = true;
            //Connected = true;

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
            if (rawReportData.ReportTypeCode != REPORT_TYPE.SER) return;
            SerialReport reportData = (SerialReport)rawReportData;

            if (!Initalized) return;

            if (reportData.ReportBytes.Length == 0)
                return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                ConnectedState.Wait();
                try
                {
                    foreach(byte b in reportData.ReportBytes)
                    {
                        State.StartStateChange();
                        try
                        {
                            bool down = (b & 0x80) == 0x00;
                            switch (b & ~0x80)
                            {
                                case 0x00: // Tall
                                    (State.Controls["button_tall"] as IControlButton).DigitalStage1 = down;
                                    break;
                                case 0x01: // Side
                                    (State.Controls["button_side"] as IControlButton).DigitalStage1 = down;
                                    break;
                                case 0x02: // Top
                                    (State.Controls["button_top"] as IControlButton).DigitalStage1 = down;
                                    break;
                                case 0x03: // Short
                                    (State.Controls["button_short"] as IControlButton).DigitalStage1 = down;
                                    break;
                                case 0x22: // C1
                                    (State.Controls["button_c1"] as IControlButton).DigitalStage1 = down;
                                    break;
                                case 0x23: // C2
                                    (State.Controls["button_c2"] as IControlButton).DigitalStage1 = down;
                                    break;
                                case 0x2A: // Tour
                                    (State.Controls["button_tour"] as IControlButton).DigitalStage1 = down;
                                    break;
                                case 0x10: // Up
                                    (State.Controls["dpad"] as IControlButtonQuad).ButtonN = down;
                                    break;
                                case 0x11: // Down
                                    (State.Controls["dpad"] as IControlButtonQuad).ButtonS = down;
                                    break;
                                case 0x12: // Left
                                    (State.Controls["dpad"] as IControlButtonQuad).ButtonW = down;
                                    break;
                                case 0x13: // Right
                                    (State.Controls["dpad"] as IControlButtonQuad).ButtonE = down;
                                    break;
                                case 0x04: // Knob CCW
                                    if (down)
                                        (State.Controls["spinner_knob"] as IControlSpinnerRelative).Delta -= 1;
                                    break;
                                case 0x44: // Knob CW
                                    if (down)
                                        (State.Controls["spinner_knob"] as IControlSpinnerRelative).Delta += 1;
                                    break;
                                case 0x09: // Scroll Dwn
                                    if (down)
                                        (State.Controls["scrollwheel"] as IControlScrollWheelRelative).Delta += 1;
                                    break;
                                case 0x0A: // Scroll Clk
                                    (State.Controls["scrollwheel"] as IControlScrollWheelRelative).Click = down;
                                    break;
                                case 0x49: // Scroll Up
                                    if (down)
                                        (State.Controls["scrollwheel"] as IControlScrollWheelRelative).Delta -= 1;
                                    break;
                                case 0x0F: // Dial CCW
                                    if (down)
                                        (State.Controls["spinner_dial"] as IControlSpinnerRelative).Delta -= 1;
                                    break;
                                case 0x4F: // Dial CW
                                    if (down)
                                        (State.Controls["spinner_dial"] as IControlSpinnerRelative).Delta += 1;
                                    break;
                            }
                        }
                        finally
                        {
                            State.EndStateChange(true);
                        }
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
            if (Initalized) return;

            Initalized = true;
            _device.StartReading();
        }

        public void DeInitalize()
        {
            if (!Initalized) return;

            _device.StopReading();

            Initalized = false;
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