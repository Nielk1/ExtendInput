using ExtendInput.Controls;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.Controller.GenericHid
{
    public class GenericHidController : IController
    {
        // TODO: don't run this thread at all if there's no output controls, but we still need to write optimiations for that
        bool OutputThreadActive = false;
        Thread OutputThread;
        private void StartOutputThread()
        {
            OutputThreadActive = true;
            // TODO: fix the speed of this thread, it runs too fast, might need per reportID/type throttles or something
            OutputThread = new Thread(() =>
            {
                for (; ; )
                {
                    if (!OutputThreadActive) break;
                    Thread.Sleep(1000 / 60);
                    if (!OutputThreadActive) break;
                    //if(WriteStateDirtyPossible)
                    {
                        //bool DataToWrite = false;
                        Dictionary<byte, byte[]> OutputBuffers = null;

                        bool IsWriteDirty = false;
                        foreach (string ControlKey in State.Controls.Keys)
                        {
                            IGenericOutputControl ctrl = State.Controls[ControlKey] as IGenericOutputControl;
                            IsWriteDirty = ctrl != null && ctrl.IsWriteDirty;
                            if (IsWriteDirty)
                                break;
                        }
                        foreach (string ControlKey in State.Controls.Keys)
                        {
                            IGenericOutputControl ctrl = State.Controls[ControlKey] as IGenericOutputControl;
                            if (ctrl != null && IsWriteDirty) // if anything in the set of outputs is dirty, write out, TODO: find a way to condense this in a multi-report situation to dirty flag individual report IDs
                            {  
                                if (OutputBuffers == null) OutputBuffers = new Dictionary<byte, byte[]>();
                                ctrl.GenerateReportsForGenericController(OutputBuffers);
                                ctrl.CleanWriteDirty();
                            }
                        }

                        // if it's been a whole second since the last write, write again
                        // TODO: move this into the ControlEccentricRotatingMass as an auto re-dirty system or something?
                        //if (LastWrite.AddSeconds(1) < DateTime.UtcNow)
                        //    DataToWrite = true;

                        if (OutputBuffers != null)
                            SendReport(OutputBuffers);
                    }
                    if (!OutputThreadActive) break;
                }
            });
            OutputThread.Start();
        }
        private void StopOutputThread()
        {
            OutputThreadActive = false;
        }
        private void SendReport(Dictionary<byte, byte[]> OutputBuffers)
        {
            foreach (var kv in OutputBuffers)
            {
                if (kv.Value != null)
                {
                    int Length = kv.Value.Length;// _device.; // TODO: maybe use the windows set report length here? not sure as that has caused issues before too, could also do that in the HidDevice layer, as right now the way we implemented it the report sizes must be perfect or it doesn't work, but we don't know if perfect is true correct, or windows correct
                    if (OutputReportLengths.ContainsKey(kv.Key))
                        Length = OutputReportLengths[kv.Key];
                    byte[] buff = new byte[Length + 1];
                    Array.Copy(kv.Value, 0, buff, 1, Length);
                    bool success = _device.WriteReport(buff);
                }
            }
        }




        public EConnectionType ConnectionType => EConnectionType.Unknown;

        public string[] ConnectionTypeCode => new string[] { "CONNECTION_UNKNOWN" };
        public string[] ControllerTypeCode => genericControllerData.Tokens ?? new string[] { "DEVICE_UNKNOWN" };


        public bool HasSelectableAlternatives => false;

        public Dictionary<string, string> Alternates => null;

        public string Name => genericControllerData.Name;

        public string[] NameDetails => null;

        public string ConnectionUniqueID => _device.UniqueKey;

        public string DeviceUniqueID => null;

        //public IDevice DeviceHackRef => null;

        public bool HasMotion => false;

        public bool IsReady => true;

        public bool IsPresent => true;

        public bool IsVirtual => false;

        public event ControllerNameUpdateEvent ControllerMetadataUpdate;
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        int reportUsageLock = 0;


        public Dictionary<string, dynamic> DeviceProperties => _device.Properties;
        private HidDevice _device;
        public AccessMode AccessMode { get; private set; }
        private ControllerDbEntry genericControllerData;
        public EPollingState PollingState { get; private set; }

        ControllerState State = new ControllerState();
        Dictionary<string, AddressableValue[]> AddressableValues = new Dictionary<string, AddressableValue[]>();
        Dictionary<byte, int> OutputReportLengths;

        public GenericHidController(HidDevice device, AccessMode AccessMode, ControllerDbEntry candidateController)
        {
            this._device = device;
            this.AccessMode = AccessMode;
            this.genericControllerData = candidateController;

            if (candidateController.Controls.Count > 0)
            {
                Dictionary<string, Type> ControlTypes = new Dictionary<string, Type>();

                foreach (Type item in typeof(IGenericControl).GetTypeInfo().Assembly.GetTypes())
                {
                    if (item.GetInterfaces().Contains(typeof(IGenericControl)))
                    {
                        GenericControlAttribute attr = item.GetCustomAttributes(false).OfType<GenericControlAttribute>().SingleOrDefault();
                        if (attr != null)
                        {
                            ConstructorInfo con = item.GetConstructor(new Type[] { typeof(AccessMode), typeof(string), typeof(AddressableValue[]), typeof(Dictionary<string, dynamic>) });
                            if (con != null)
                                ControlTypes[attr.Name] = item;
                        }
                    }
                }

                foreach (var control in candidateController.Controls)
                {
                    string controlName = control.Key;
                    // TODO implement a control manager to factory contols instead

                    if (!ControlTypes.ContainsKey(control.Value.ControlName))
                    {
                        Console.WriteLine($"Unknown control {control.Value.ControlName}");
                        continue;
                    }

                    var addressables = control.Value.Paramaters.Select(dr => new AddressableValue(dr)).ToArray();
                    IControl controlInstance = (IControl)Activator.CreateInstance(ControlTypes[control.Value.ControlName], AccessMode, control.Value.FactoryName, (object)addressables, control.Value.Properties);
                    State.Controls[controlName] = controlInstance;
                    AddressableValues[controlName] = addressables;
                }

                OutputReportLengths = candidateController.OutputReportLengths;
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
                            (State.Controls[controlName] as IGenericControl).ProcessReportForGenericController(reportData);
                        }
                    }
                    finally
                    {
                        State.EndStateChange(true);
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
            new Thread(() =>
            {
                State.StartStateChange();
                State.EndStateChange(true);
            }).Start(); // fire this off in a thread so we don't get stuck as what called us to Initalize is probably locking in a way that will block their event handler
            _device.StartReading();
            StartOutputThread();
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
                StopOutputThread();
                _device.CloseDevice();
            }
        }


        public void Identify()
        {
        }

        public void SetActiveAlternateController(string ControllerID)
        {
            
        }

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

    public class AddressableValue
    {
        const string numerics = "0123456789ABCDEF";

        public int FormulaLength = 0;
        public int ReportID = 0;
        int ByteOffset = 0;
        int BitOffset = -1;
        int Length = 1;
        int RawMinimum = 0;
        AddressableValue Minimum = null;
        int RawMaximum = 255;
        AddressableValue Maximum = null;
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
                            if (formula[i + 1] == '(')
                            {
                                i++;
                                Minimum = new AddressableValue(formula.Substring(i + 1));
                                i += Minimum.FormulaLength + 1;
                            }
                            else
                            {
                                while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                                {
                                    i++;
                                    parsingToken += formula[i];
                                }
                                if (parsingToken.Length > 0)
                                    RawMinimum = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            }
                            break;
                        case '+': // maximum
                            if (formula[i + 1] == '(')
                            {
                                i++;
                                Maximum = new AddressableValue(formula.Substring(i));
                                i += Maximum.FormulaLength + 1;
                            }
                            else
                            {
                                while (i + 1 < formula.Length && numerics.Contains(formula[i + 1]))
                                {
                                    i++;
                                    parsingToken += formula[i];
                                }
                                if (parsingToken.Length > 0)
                                    RawMaximum = int.Parse(parsingToken, System.Globalization.NumberStyles.HexNumber);
                            }
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
                        case ')': // abort any more formula reading
                            return;
                        default:
                            break;
                    }
                }
                FormulaLength = i + 1;
            }
        }

        private int? GetWorkingValue(IReport report)
        {
            // TODO removed reliance on the report being a HID report

            if (report.ReportTypeCode != REPORT_TYPE.HID)
                return null;

            HidReport reportData = (HidReport)report;

            int Working = 0;
            if (RawValue.HasValue)
            {
                Working = RawValue.Value;
            }
            else if (reportData.ReportId != ReportID) return null;
            else if (ByteOffset >= reportData.ReportBytes.Length) return null;
            else if (ByteOffset + Length > reportData.ReportBytes.Length) return null;
            else
            {
                Working = 0;
                for (int i = 0; i < Length; i++)
                    Working |= reportData.ReportBytes[ByteOffset + i] << (8 * i); // TODO rewrite this to deal with Endian order, might have to change endian to care about when in the sequence it is, or make it an operation
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
        public bool? GetBoolean(IReport report)
        {
            int? Working = GetWorkingValue(report);
            if (!Working.HasValue) return null;
            if (BitOffset >= 0) return (Working.Value & (1 << BitOffset)) != 0;
            return Working.Value != 0;
        }
        public byte? GetByte(IReport report)
        {
            int? Working = GetWorkingValue(report);
            if (!Working.HasValue) return null;
            return (byte)Working.Value;
        }
        public sbyte? GetSByte(IReport report)
        {
            int? Working = GetWorkingValue(report);
            if (!Working.HasValue) return null;
            return (sbyte)Working.Value;
        }
        public float? GetFloat(IReport report)
        {
            int? Working = GetWorkingValue(report);
            if (!Working.HasValue) return null;

            int localMax = Maximum?.GetWorkingValue(report) ?? RawMaximum;
            int localMin = Minimum?.GetWorkingValue(report) ?? RawMinimum;
            bool invert = false;
            if(localMin > localMax)
            {
                int localTmp = localMin;
                localMin = localMax;
                localMax = localTmp;
                invert = true;
            }

            Working = Math.Max(Math.Min(Working.Value, localMax), localMin);

            float fVal = 1.0f * (Working.Value - localMin) / (localMax - localMin);

            if (invert)
                fVal = 1 - fVal;

            if (AnalogCenter)
                return fVal * 2f - 1f;
            return fVal;
        }



        public static int DEFAULT_REPORT_LENGTH = 20;
        public byte[] SetValue(byte[] buffer, byte value)
        {
            int size = Math.Max(DEFAULT_REPORT_LENGTH, ByteOffset + 1);
            if (buffer == null)
            {
                buffer = new byte[size];
            }
            else if (buffer.Length < size)
            {
                byte[] oldBuffer = buffer;
                buffer = new byte[size];
                Array.Copy(oldBuffer, buffer, oldBuffer.Length);
            }

            // TODO implement more properties such as specific bit setting, etc
            buffer[ByteOffset] = value;

            return buffer;
        }
    }
}
