using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
{
    public class FlydigiController : IController
    {
        #region String Definitions
        private const string ATOM_CONNECTION_WIRE = "CONNECTION_WIRE";
        private const string ATOM_CONNECTION_USB_WIRE = "CONNECTION_WIRE_USB";
        private const string ATOM_CONNECTION_BT = "CONNECTION_BT";
        private const string ATOM_CONNECTION_DONGLE = "CONNECTION_DONGLE";
        private const string ATOM_CONNECTION_DONGLE_1 = "CONNECTION_DONGLE_1";
        private const string ATOM_CONNECTION_DONGLE_2 = "CONNECTION_DONGLE_2";
        private const string ATOM_CONNECTION_DONGLE_3 = "CONNECTION_DONGLE_3";
        private const string ATOM_CONNECTION_UNKKNOWN = "CONNECTION_UNKNOWN";

        private readonly string[] _CONNECTION_WIRE = new string[] { ATOM_CONNECTION_USB_WIRE, ATOM_CONNECTION_WIRE };
        private readonly string[] _CONNECTION_BT = new string[] { ATOM_CONNECTION_BT };
        private readonly string[] _CONNECTION_DONGLE = new string[] { ATOM_CONNECTION_DONGLE_1, ATOM_CONNECTION_DONGLE };
        private readonly string[] _CONNECTION_UNKKNOWN = new string[] { ATOM_CONNECTION_UNKKNOWN };
        #endregion String Definitions

        private HidDevice _device;
        int reportUsageLock = 0;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        public bool HasMotion => true;

        public bool IsReady => true;
        public bool IsPresent => true;
        public bool IsVirtual => false;

        public ControllerState GetState()
        {
            return State;
        }

        public EConnectionType ConnectionType { get; private set; }

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

        public string[] ConnectionTypeCode
        {
            get
            {
                switch (ConnectionType)
                {
                    case EConnectionType.USB: return _CONNECTION_WIRE;
                    case EConnectionType.Bluetooth: return _CONNECTION_BT;
                    case EConnectionType.Dongle: return _CONNECTION_DONGLE;
                    default: return _CONNECTION_UNKKNOWN;
                }
            }
        }
        public string[] ControllerTypeCode
        {
            get
            {
                return new string[] { "DEVICE_DS5" };

            }
        }
        public string Name => "Flydigi Controller";
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

        ControllerState State = new ControllerState();
        ControllerState OldState = null;

        int Initalized;

        public FlydigiController(HidDevice device, EConnectionType ConnectionType = EConnectionType.Unknown)
        {
            this.ConnectionType = ConnectionType;

            State.Controls["quad_left"] = new ControlDPad();
            State.Controls["quad_right"] = new ControlButtonQuad();
            State.Controls["bumpers"] = new ControlButtonPair();
            State.Controls["bumpers2"] = new ControlButtonPair();
            State.Controls["triggers"] = new ControlTriggerPair(HasStage2: false);
            State.Controls["menu"] = new ControlButtonPair();
            State.Controls["home"] = new ControlButton();
            State.Controls["mute"] = new ControlButton();
            State.Controls["stick_left"] = new ControlStick(HasClick: true);
            State.Controls["stick_right"] = new ControlStick(HasClick: true);
            State.Controls["touch_center"] = new ControlTouch(TouchCount: 2, HasClick: true);
            State.Controls["motion"] = new ControlMotion();

            _device = device;

            Initalized = 0;

            _device.DeviceReport += OnReport;
        }
        public void Dispose()
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

            // open the device overlapped read so we don't get stuck waiting for a report when we write to it
            //_device.OpenDevice(DeviceMode.Overlapped, DeviceMode.NonOverlapped, ShareMode.ShareRead | ShareMode.ShareWrite);
            _device.OpenDevice(DeviceMode.Overlapped, DeviceMode.Overlapped, ShareMode.ShareRead | ShareMode.ShareWrite);

            //_device.Inserted += DeviceAttachedHandler;
            //_device.Removed += DeviceRemovedHandler;

            //_device.MonitorDeviceEvents = true;

            Initalized = 1;

            //_attached = _device.IsConnected;

            if (ConnectionType == EConnectionType.Dongle)
            {
                _device.StartReading();
            }
        }

        public void DeInitalize()
        {
            if (Initalized == 0) return;

            //_device.Inserted -= DeviceAttachedHandler;
            //_device.Removed -= DeviceRemovedHandler;

            //_device.MonitorDeviceEvents = false;

            _device.StopReading();

            Initalized = 0;
            _device.CloseDevice();
        }

        public async void Identify()
        {
        }

        private void OnReport(IReport reportData)
        {
            if (Initalized < 1) return;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    // Clone the current state before altering it since the OldState is likely a shared reference
                    ControllerState StateInFlight = (ControllerState)State.Clone();

                    ControllerStateUpdate?.Invoke(this, State);
                }
                finally
                {
                    Interlocked.Exchange(ref reportUsageLock, 0);
                }
            }
        }
        public void SetActiveAlternateController(string ControllerID) { }
    }
}
