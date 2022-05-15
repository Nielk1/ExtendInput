﻿using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.Controller.GenericHid
{
    public class GenericHidController : IController
    {
        public EConnectionType ConnectionType => EConnectionType.Unknown;

        public string[] ConnectionTypeCode => new string[] { "DEVICE_UNKNOWN" };

        public string[] ControllerTypeCode => new string[] { "CONNECTION_UNKNOWN" };

        public bool HasSelectableAlternatives => false;

        public Dictionary<string, string> Alternates => null;

        public string Name => genericControllerData.Name;

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

        int reportUsageLock = 0;


        private HidDevice _device;
        public AccessMode AccessMode { get; private set; }
        private ControllerDbEntry genericControllerData;
        public EPollingState PollingState { get; private set; }

        ControllerState State = new ControllerState();
        Dictionary<string, AddressableValue[]> AddressableValues = new Dictionary<string, AddressableValue[]>();

        public GenericHidController(HidDevice device, AccessMode AccessMode, ControllerDbEntry candidateController)
        {
            this._device = device;
            this.AccessMode = AccessMode;
            this.genericControllerData = candidateController;

            foreach (var control in candidateController.Controls)
            {
                string controlName = control.Key;
                // TODO use reflection here, probably via a control manager
                switch (control.Value[0])
                {
                    case "Button":
                        {
                            AddressableValue val = new AddressableValue(control.Value[1]);
                            State.Controls[controlName] = new ControlButton();
                            AddressableValues[controlName] = new AddressableValue[] { val };
                        }
                        break;
                    default:
                        Console.WriteLine($"Unknown control {control.Value[0]}");
                        break;
                }
            }

            PollingState = EPollingState.Inactive;

            _device.DeviceReport += OnReport;
            State.ControllerStateUpdate += State_ControllerStateUpdate;
        }



        private void State_ControllerStateUpdate(ControlCollection controls)
        {
            ControllerStateUpdate?.Invoke(this, controls);
        }
        private void OnReport(IReport rawReportData)
        {
            //if (PollingState == EPollingState.Inactive) return;
            if (rawReportData.ReportTypeCode != REPORT_TYPE.HID) return;
            HidReport reportData = (HidReport)rawReportData;


            if (0 == Interlocked.Exchange(ref reportUsageLock, 1))
            {
                try
                {
                    State.StartStateChange();
                    try
                    {
                        foreach (string controlName in State.Controls.Keys)
                        {
                            // TODO add a common function here that takes in an array of AddressableValues for generic use
                            bool? val = AddressableValues[controlName][0].GetBoolean(reportData.ReportId, reportData.ReportBytes);
                            if (val.HasValue)
                                (State.Controls[controlName] as ControlButton).DigitalStage1 = val.Value;
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
                        _device.CloseDevice();
                    }
                    break;
            }
        }

        public void Initalize()
        {
            if (PollingState == EPollingState.Active) return;

            //touch_last_frame = false;

            PollingState = EPollingState.Active;
            _device.StartReading();
        }

        public void DeInitalize()
        {
            if (PollingState == EPollingState.Inactive) return;
            if (PollingState == EPollingState.RunOnce) return;
            if (PollingState == EPollingState.SlowPoll) return;

            // dongles switch back to slow poll instead of going inactive
            if (ConnectionType == EConnectionType.Dongle)
            {
                PollingState = EPollingState.SlowPoll;
            }
            else
            {
                _device.StopReading();

                PollingState = EPollingState.Inactive;
                _device.CloseDevice();
            }
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

    enum EOperation : byte
    {
        BitAnd,
        ShiftLeft,
        ShiftRight,
        MakeSigned,
    }
    struct Oppr
    {
        public EOperation Operation;
        public int Operand; // TODO consider making this another AddressableValue if in parenthesis
        public Oppr(EOperation Operation, int Opperand)
        {
            this.Operation = Operation;
            this.Operand = Opperand;
        }
    }

    class AddressableValue
    {
        const string numerics = "0123456789ABCDEF";

        int ReportID = 0;
        int ByteOffset = 0;
        int BitOffset = -1;
        int Length = 1;
        int Minimum = 0;
        int Maximum = 255;
        bool AnalogCenter = false;
        bool BigEndian = false;
        int? RawValue = null; // TODO consider if it's even worth having constants, because that implies dynamics in other places and that might be unwise as we could just write a class then
        List<Oppr> Operations = new List<Oppr>();

        public AddressableValue(string formula)
        {
            for (int i = 0; i < formula.Length; i++)
            {
                string parsingToken = string.Empty;

                if (numerics.Contains(formula[i]))
                {
                    parsingToken += formula[i];
                    while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                    {
                        i++;
                        parsingToken += formula[i];
                    }
                    RawValue = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                }
                else
                {
                    switch (formula[i])
                    {
                        case 'r': // report ID
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                ReportID = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            break;
                        case 'b': // byte offset
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                ByteOffset = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            break;
                        case 'a': // atom (bit)
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                BitOffset = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            break;
                        case '-': // minimum
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                Minimum = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            break;
                        case '+': // maximum
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                Maximum = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            break;
                        case 'l': // length in bytes
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                Length = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            break;
                        case '>': // shift right
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                Operations.Add(new Oppr(EOperation.ShiftRight, int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber)));
                            break;
                        case '<': // shift left
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                Operations.Add(new Oppr(EOperation.ShiftLeft, int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber)));
                            break;
                        case '&': // bitwise and
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                Operations.Add(new Oppr(EOperation.BitAnd, int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber)));
                            break;
                        case 's': // signed
                            while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                            {
                                i++;
                                parsingToken += formula[i];
                            }
                            if (parsingToken.Length > 0)
                                Operations.Add(new Oppr(EOperation.MakeSigned, int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber)));
                            break;
                        case '/': // analog is centered
                            AnalogCenter = true;
                            break;
                        case 'e': // big endian
                            BigEndian = true;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private int? GetWorkingValue(int reportID, byte[] report)
        {
            int Working = 0;
            if (RawValue.HasValue)
            {
                Working = RawValue.Value;
            }
            else if (reportID != ReportID) return null;
            else if (ByteOffset >= report.Length) return null;
            else if (ByteOffset + Length > report.Length) return null;
            else
            {
                Working = report[ByteOffset]; // TODO rewrite this to handle sizes other than 1 and also deal with Endian order
            }
            foreach (var opr in Operations)
            {
                switch (opr.Operation)
                {
                    case EOperation.BitAnd: Working &= opr.Operand; break;
                    case EOperation.ShiftLeft: Working = Working << opr.Operand; break;
                    case EOperation.ShiftRight: Working = Working >> opr.Operand; break;
                    case EOperation.MakeSigned:
                        {
                            UInt32 SignedBit = (UInt32)(1 << opr.Operand);
                            if ((SignedBit & Working) == SignedBit)
                            {
                                Working = (Int32)((UInt32)Working | (0xFFFFFFFFu << opr.Operand));
                            }
                        }
                        break;
                }
            }

            return Working;
        }
        public bool? GetBoolean(int reportID, byte[] report)
        {
            int? Working = GetWorkingValue(reportID, report);
            if (!Working.HasValue) return null;
            if (BitOffset >= 0) return (Working.Value & (1 << BitOffset)) != 0;
            return Working.Value != 0;
        }
        public byte? GetByte(int reportID, byte[] report)
        {
            int? Working = GetWorkingValue(reportID, report);
            if (!Working.HasValue) return null;
            return (byte)Working.Value;
        }
        public sbyte? GetSByte(int reportID, byte[] report)
        {
            int? Working = GetWorkingValue(reportID, report);
            if (!Working.HasValue) return null;
            return (sbyte)Working.Value;
        }
        public float? GetFloat(int reportID, byte[] report)
        {
            int? Working = GetWorkingValue(reportID, report);
            if (!Working.HasValue) return null;

            int localMax = Maximum;
            int localMin = Minimum;
            int invert = 1;
            if(Minimum > Maximum)
            {
                localMin = Maximum;
                localMax = Minimum;
                invert = -1;
            }

            Working = Math.Max(Math.Min(Working.Value, localMax), localMin);

            float fVal = 1f * (Working.Value - localMin) / (localMax - localMin);

            if (AnalogCenter)
                return fVal * 2f - 1f;
            return fVal;

            // TODO deal with signed input, which currently makes no sense anyway
        }
    }
}
