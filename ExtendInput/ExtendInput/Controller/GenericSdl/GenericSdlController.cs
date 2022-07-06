using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDL2;
using System.Threading;

namespace ExtendInput.Controller.GenericSdl
{
    public class GenericSdlController : IController
    {
        public EConnectionType ConnectionType => EConnectionType.Unknown;

        public string[] ConnectionTypeCode => new string[] { "CONNECTION_UNKNOWN" };
        public string[] ControllerTypeCode => new string[] { "DEVICE_UNKNOWN" };


        public bool HasSelectableAlternatives => false;

        public Dictionary<string, string> Alternates => null;

        public string Name => "SDL Controller";

        public string[] NameDetails => null;

        public string ConnectionUniqueID => _device.UniqueKey;

        public string DeviceUniqueID => null;

        public IDevice DeviceHackRef => null;

        public bool HasMotion => false;

        public bool IsReady => true;

        public bool IsPresent => true;

        public bool IsVirtual => false;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;



        public Dictionary<string, dynamic> DeviceProperties => _device.Properties;
        private SdlDevice _device;
        int reportUsageLock = 0;
        public AccessMode AccessMode { get; private set; }
        public EPollingState PollingState { get; private set; }

        ControllerState State = new ControllerState();

        public GenericSdlController(SdlDevice device, AccessMode AccessMode)
        {
            this._device = device;
            this.AccessMode = AccessMode;

            PollingState = EPollingState.Inactive;

            _device.DeviceReport += OnReport;
            State.ControllerStateUpdate += State_ControllerStateUpdate;

            ResetControllerInfo();
        }

        private void State_ControllerStateUpdate(ControlCollection controls)
        {
            ControllerStateUpdate?.Invoke(this, controls);
        }

        private void OnReport(IReport rawReportData)
        {
            if (PollingState == EPollingState.Inactive) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.SDL) return;
            SdlReport reportData = (SdlReport)rawReportData;

            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    State.StartStateChange();
                    try
                    {
                        switch (reportData.Event.type)
                        {
                            case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                            case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                                switch((SDL.SDL_GameControllerButton)reportData.Event.cbutton.button)
                                {
                                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y: (State.Controls["cluster_right"] as IControlButtonQuad).ButtonN = reportData.Event.cbutton.state == 1; break;
                                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B: (State.Controls["cluster_right"] as IControlButtonQuad).ButtonE = reportData.Event.cbutton.state == 1; break;
                                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A: (State.Controls["cluster_right"] as IControlButtonQuad).ButtonS = reportData.Event.cbutton.state == 1; break;
                                    case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X: (State.Controls["cluster_right"] as IControlButtonQuad).ButtonW = reportData.Event.cbutton.state == 1; break;
                                }
                                break;
                            case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                                break;
                            case SDL.SDL_EventType.SDL_CONTROLLERTOUCHPADDOWN:
                            case SDL.SDL_EventType.SDL_CONTROLLERTOUCHPADUP:
                            case SDL.SDL_EventType.SDL_CONTROLLERTOUCHPADMOTION:
                                break;
                            case SDL.SDL_EventType.SDL_CONTROLLERSENSORUPDATE:
                                break;
                        }
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

        private void ResetControllerInfo()
        {
            SDL.SDL_GameControllerType controller_type = _device.ControllerType;
            switch(controller_type)
            {
                case SDL.SDL_GameControllerType.SDL_CONTROLLER_TYPE_XBOX360:
                case SDL.SDL_GameControllerType.SDL_CONTROLLER_TYPE_XBOXONE:
                    State.Controls["cluster_right"] = new ControlButtonQuad();
                    break;
                case SDL.SDL_GameControllerType.SDL_CONTROLLER_TYPE_PS3:
                case SDL.SDL_GameControllerType.SDL_CONTROLLER_TYPE_PS4:
                case SDL.SDL_GameControllerType.SDL_CONTROLLER_TYPE_PS5:
                    State.Controls["cluster_right"] = new ControlButtonQuad();
                    break;
                case SDL.SDL_GameControllerType.SDL_CONTROLLER_TYPE_NINTENDO_SWITCH_PRO:
                    State.Controls["cluster_right"] = new ControlButtonQuad();
                    break;
                //case SDL.SDL_GameControllerType.SDL_CONTROLLER_TYPE_VIRTUAL:
            }
        }

        public void Dispose()
        {
            switch (PollingState)
            {
                case EPollingState.Active:
                case EPollingState.SlowPoll:
                case EPollingState.RunOnce:
                    {
                        _device.StopReading();

                        PollingState = EPollingState.Inactive;
                        //_device.CloseDevice();
                    }
                    break;
            }
        }

        public void Initalize()
        {
            if (PollingState == EPollingState.Active) return;

            PollingState = EPollingState.Active;
            _device.StartReading();
        }

        public void DeInitalize()
        {
            if (PollingState == EPollingState.Inactive) return;
            if (PollingState == EPollingState.RunOnce) return;
            if (PollingState == EPollingState.SlowPoll) return;

            _device.StopReading();

            PollingState = EPollingState.Inactive;
            //_device.CloseDevice();
        }

        public void Identify()
        {
        }

        public void SetActiveAlternateController(string ControllerID)
        {
        }

        public bool SetControlState(string control, string state)
        {
            return false;
        }
    }
}
