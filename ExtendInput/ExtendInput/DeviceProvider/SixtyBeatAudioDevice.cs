﻿using ExtendInput.DeviceProvider;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.DeviceProvider
{
    public class SixtyBeatAudioDevice : IDevice
    {
        private static HashSet<string> ExistingDevices = new HashSet<string>();

        public string DevicePath { get { return $"SixtyBeatAudioDevice"; } }
        public int ProductId { get { return 0; } }
        public int VendorId { get { return 0; } }

        public Dictionary<string, dynamic> Properties { get; private set; }


        private string DeviceID;

        private bool IsOpen = false;


        const int RATE = 44100; // sample rate of the sound card
        const int BUFFER = 40; // sample rate of the sound card
        readonly int BUFFERSIZE = (int)Math.Pow(2, 11); // must be a multiple of 2



        byte _MergedGlobals_currentValue = 0;
        byte _MergedGlobals_lastValue = 0;
        // 1 byte
        Int16 previousAmplitude = 0;
        Int16 previousPreviousAmplitude = 0;
        Int16 _MergedGlobals_previousValue = 0;

        // 2 byte
        int rx_buffer_len = 0;
        int countsBufferIndex = 0;
        int self_checksumSuccessCount = 0;
        int _MergedGlobals_countsSinceLastTransition = 0;
        byte[] _MergedGlobals__rx_buffer = new byte[8];

        Int32[] countsBuffer = new Int32[4000];


        bool self_checksumOK;
        byte self_receivedChecksum;
        byte self_expectedChecksum;
        int self_checksumErrorCount;
        byte[] chksumindex = new byte[] {
            0x00, 0x21, 0x42, 0x63, 0x84, 0xA5, 0xC6, 0xE7, 0x08, 0x29, 0x4A, 0x6B, 0x8C, 0xAD, 0xCE, 0xEF,
            0x31, 0x10, 0x73, 0x52, 0xB5, 0x94, 0xF7, 0xD6, 0x39, 0x18, 0x7B, 0x5A, 0xBD, 0x9C, 0xFF, 0xDE,
            0x62, 0x43, 0x20, 0x01, 0xE6, 0xC7, 0xA4, 0x85, 0x6A, 0x4B, 0x28, 0x09, 0xEE, 0xCF, 0xAC, 0x8D,
            0x53, 0x72, 0x11, 0x30, 0xD7, 0xF6, 0x95, 0xB4, 0x5B, 0x7A, 0x19, 0x38, 0xDF, 0xFE, 0x9D, 0xBC,
            0xC4, 0xE5, 0x86, 0xA7, 0x40, 0x61, 0x02, 0x23, 0xCC, 0xED, 0x8E, 0xAF, 0x48, 0x69, 0x0A, 0x2B,
            0xF5, 0xD4, 0xB7, 0x96, 0x71, 0x50, 0x33, 0x12, 0xFD, 0xDC, 0xBF, 0x9E, 0x79, 0x58, 0x3B, 0x1A,
            0xA6, 0x87, 0xE4, 0xC5, 0x22, 0x03, 0x60, 0x41, 0xAE, 0x8F, 0xEC, 0xCD, 0x2A, 0x0B, 0x68, 0x49,
            0x97, 0xB6, 0xD5, 0xF4, 0x13, 0x32, 0x51, 0x70, 0x9F, 0xBE, 0xDD, 0xFC, 0x1B, 0x3A, 0x59, 0x78,
            0x88, 0xA9, 0xCA, 0xEB, 0x0C, 0x2D, 0x4E, 0x6F, 0x80, 0xA1, 0xC2, 0xE3, 0x04, 0x25, 0x46, 0x67,
            0xB9, 0x98, 0xFB, 0xDA, 0x3D, 0x1C, 0x7F, 0x5E, 0xB1, 0x90, 0xF3, 0xD2, 0x35, 0x14, 0x77, 0x56,
            0xEA, 0xCB, 0xA8, 0x89, 0x6E, 0x4F, 0x2C, 0x0D, 0xE2, 0xC3, 0xA0, 0x81, 0x66, 0x47, 0x24, 0x05,
            0xDB, 0xFA, 0x99, 0xB8, 0x5F, 0x7E, 0x1D, 0x3C, 0xD3, 0xF2, 0x91, 0xB0, 0x57, 0x76, 0x15, 0x34,
            0x4C, 0x6D, 0x0E, 0x2F, 0xC8, 0xE9, 0x8A, 0xAB, 0x44, 0x65, 0x06, 0x27, 0xC0, 0xE1, 0x82, 0xA3,
            0x7D, 0x5C, 0x3F, 0x1E, 0xF9, 0xD8, 0xBB, 0x9A, 0x75, 0x54, 0x37, 0x16, 0xF1, 0xD0, 0xB3, 0x92,
            0x2E, 0x0F, 0x6C, 0x4D, 0xAA, 0x8B, 0xE8, 0xC9, 0x26, 0x07, 0x64, 0x45, 0xA2, 0x83, 0xE0, 0xC1,
            0x1F, 0x0E, 0x5D, 0x0C, 0x0B, 0x0A, 0xD9, 0xF8, 0x17, 0x36, 0x55, 0x74, 0x93, 0xB2, 0xD1, 0xF0,
        };

        object AudioLock = new object();

        Queue<WaveInEventArgs> QueueWaveEvents = new Queue<WaveInEventArgs>();

        RingBuffer<Int16> previousValues;
        RingBuffer<Int16> previousAbsValues;

        byte currentByte = 0;
        int currentBitIndex = 0;


        public static SixtyBeatAudioDevice Create(string deviceId)
        {
            lock (ExistingDevices)
            {
                if (ExistingDevices.Contains(deviceId))
                    return null;

                SixtyBeatAudioDevice device = new SixtyBeatAudioDevice(deviceId);
                if (device != null)
                    ExistingDevices.Add(deviceId);

                return device;
            }
        }
        public static bool DeviceKnown(string deviceId)
        {
            lock (ExistingDevices)
                return ExistingDevices.Contains(deviceId);
        }

        private WaveInEvent wi;
        public SixtyBeatAudioDevice(string deviceId)
        {
            var enumerator = new NAudio.CoreAudioApi.MMDeviceEnumerator();
            //cycle through all audio devices
            int audioDeviceNumber = -1;
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                // these happen to enumate the same order
                NAudio.CoreAudioApi.MMDevice dev = enumerator.EnumerateAudioEndPoints(NAudio.CoreAudioApi.DataFlow.Capture, NAudio.CoreAudioApi.DeviceState.Active)[i];
                //Guid scratch = (Guid)(dev.Properties[new NAudio.CoreAudioApi.PropertyKey(DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Device_ContainerId.fmtid, (int)DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Device_ContainerId.pid)].Value);
                //blder.AppendLine($"{i}\t{scratch}\t{dev}");

                string ItemDeviceID = dev.Properties[new NAudio.CoreAudioApi.PropertyKey(DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Audio_InstanceId.fmtid, (int)DevPKey.Native.PnpDevicePropertyAPINative.DEVPKEY_Audio_InstanceId.pid)].Value.ToString();
                //Dictionary<DevPKey.Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> properties = DevPKey.PnpDevicePropertyAPI.GetDeviceProperties(DeviceID);
                if (ItemDeviceID == deviceId)
                {
                    audioDeviceNumber = i;
                    this.DeviceID = ItemDeviceID;
                    break;
                }
            }

            Properties = new Dictionary<string, dynamic>();

            previousValues = new RingBuffer<Int16>(BUFFER);
            previousAbsValues = new RingBuffer<Int16>(BUFFER);
            
            wi = new WaveInEvent();
            wi.DeviceNumber = audioDeviceNumber;
            wi.WaveFormat = new NAudio.Wave.WaveFormat(RATE, 1);
            wi.BufferMilliseconds = (int)((double)BUFFERSIZE / (double)RATE * 1000.0);
            wi.DataAvailable += new EventHandler<WaveInEventArgs>(AudioDataAvailable);
        }
        public bool StartReading()
        {
            lock (wi)
            {
                try
                {
                    wi.StartRecording();
                    return true;
                }
                catch (InvalidOperationException ex)
                {
                    if (ex.Message == "Already recording")
                    {
                        try
                        {
                            Thread.Sleep(100);
                            wi.StartRecording();
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            }
        }
        public void CloseDevice()
        {
            lock (wi)
                wi.StopRecording();
        }

        private void AudioDataAvailable(object sender, WaveInEventArgs e)
        {
            if (e.BytesRecorded > 0)
            {
                lock (QueueWaveEvents)
                    QueueWaveEvents.Enqueue(e);

                lock (AudioLock)
                {
                    WaveInEventArgs dat;
                    lock (QueueWaveEvents)
                        dat = QueueWaveEvents.Dequeue();
                    for (int i = 0; i < dat.BytesRecorded; i += 2)
                    {
                        handleValue(BitConverter.ToInt16(dat.Buffer, i));
                    }
                }
            }
        }

        private Int16 FirstPassFilter(Int16 amplitude)
        {
            // possbly a 2-wide box filter
            Int16 currentAmplitude = amplitude;
            amplitude = (Int16)((previousPreviousAmplitude + (previousAmplitude * 2) + currentAmplitude) / 4);
            previousPreviousAmplitude = previousAmplitude;
            previousAmplitude = currentAmplitude;

            return amplitude;
        }

        private void handleValue(Int16 amplitude)
        {
            amplitude = FirstPassFilter(amplitude);

            previousValues.Push(amplitude);

            // correct clipping at 255 or -256 using a parabola to synthesize the lost peak/vally
            if (previousValues[BUFFER - 16] == 255 || previousValues[BUFFER - 16] == -256)
            {
                int weightedPrevious = previousValues[BUFFER - 16 - 1];
                int divisor = 0;
                int iterations = 0;
                for (; iterations < 6; iterations++)
                {
                    divisor += (iterations * 2);
                    weightedPrevious += previousValues[BUFFER - 16 - 1] + (previousValues[BUFFER - 16] - previousValues[BUFFER - 16 - 1]);

                    // break if we go out of range early
                    if (previousValues[BUFFER - 16] == 255 && previousValues[BUFFER - 16 + 1 + iterations] >= 255)
                        break;
                    if (previousValues[BUFFER - 16] == -256 && previousValues[BUFFER - 16 + 1 + iterations] <= -256)
                        break;
                }
                if (iterations > 2)
                {
                    float a = (float)(previousValues[BUFFER - 16 + 1 + iterations] - weightedPrevious) / (float)divisor;
                    float b = (float)(previousValues[BUFFER - 16] - previousValues[BUFFER - 16 - 1]) - a;
                    float c = (float)previousValues[BUFFER - 16 - 1];
                    for (int v13 = iterations, v9Index = 16 - 1, x = 2; v13 > 3; v13--, v9Index++, x++)
                    {
                        previousValues[v9Index] = (Int16)(float)Math.Round((a * x * x) + (b * x) + c);
                    }
                }
            }

            // get the max absolute amplitude of the last BUFFER frames
            previousAbsValues.Push((Int16)Math.Abs((previousValues[BUFFER - 16] == Int16.MinValue) ? Int16.MaxValue : previousValues[BUFFER - 16]));
            int maxAbsInWindow = 0;
            for (int i = 0; i < previousAbsValues.Size; i++)
            {
                Int16 newValue = previousAbsValues[i];
                if (newValue > maxAbsInWindow)
                    maxAbsInWindow = newValue;
            }


            // get the min and max for the 9 frames at the read index
            Int16 findMin = 0;
            Int16 findMax = 0;
            for (int i = 0; i < 9; i++)
            {
                Int16 newValue = previousValues[BUFFER - 16 + 2 + i];
                if (newValue > findMax)
                    findMax = newValue;
                if (newValue < findMin)
                    findMin = newValue;
            }

            int rangeTop = Math.Max((int)findMax, 100);
            int rangeBottom = Math.Min((int)findMin, -100);
            int rangeMiddle = (rangeBottom + rangeTop) / 2;

            Int16 rangedValue = (Int16)Math.Min(Math.Max(previousValues[BUFFER - 16] - rangeMiddle, Int16.MinValue), Int16.MaxValue);

            if (maxAbsInWindow < 101)
            {
                if (countsBufferIndex > 0)
                {
                    // process counts data with various possible threshholds
                    rx_buffer_len = 0;
                    currentBitIndex = 0;
                    currentByte = 0;
                    self_checksumOK = false;
                    if (!processCountsWithThreshold(63))
                    {
                        for (int i = 1; i < 10; i++) // shouldn't this start at 1 as the above already tried?
                        {
                            rx_buffer_len = 0;
                            currentBitIndex = 0;
                            currentByte = 0;
                            if (processCountsWithThreshold(63 + i))
                            {
                                if (self_checksumOK)
                                    break;
                            }
                            rx_buffer_len = 0;
                            currentBitIndex = 0;
                            currentByte = 0;
                            if (processCountsWithThreshold(63 - i))
                            {
                                if (self_checksumOK)
                                    break;
                            }
                        }
                    }

                    if (self_checksumOK)
                    {
                        ++self_checksumSuccessCount;
                    }
                    else if ((uint)countsBufferIndex > 20)
                    {
                        ++self_checksumErrorCount;
                    }
                    countsBufferIndex = 0;
                }
            }
            if (rangedValue > 1)
            {
                _MergedGlobals_currentValue = 1;
            }
            else if (rangedValue < -1)
            {
                _MergedGlobals_currentValue = 0;
            }
            {
                _MergedGlobals_countsSinceLastTransition += 10;
                if (_MergedGlobals_currentValue != _MergedGlobals_lastValue)
                {
                    int stepsAgoCrossedZero = 10 * Math.Abs((int)rangedValue) / Math.Abs((int)rangedValue - (int)_MergedGlobals_previousValue);
                    _MergedGlobals_countsSinceLastTransition -= stepsAgoCrossedZero;
                    if (rangedValue > 1 || countsBufferIndex != 0)
                    {
                        if (countsBufferIndex == 0)
                            _MergedGlobals_countsSinceLastTransition = stepsAgoCrossedZero;
                        countsBuffer[countsBufferIndex] = _MergedGlobals_countsSinceLastTransition;

                        // attempt to fix noise by merging overly-short inversion, another method might be detecting slope instead of time
                        if (countsBufferIndex > 4 && _MergedGlobals_countsSinceLastTransition < 35)
                        {
                            int total = countsBuffer[countsBufferIndex - 0] + countsBuffer[countsBufferIndex - 1] + countsBuffer[countsBufferIndex - 2];
                            if (total < 100)
                            {
                                countsBufferIndex -= 2;
                                countsBuffer[countsBufferIndex] = total;
                            }
                        }

                        if (countsBufferIndex < 4000)
                            countsBufferIndex++;
                    }
                    _MergedGlobals_countsSinceLastTransition = stepsAgoCrossedZero;
                }
                _MergedGlobals_lastValue = _MergedGlobals_currentValue;
                _MergedGlobals_previousValue = rangedValue;
            }
        }

        private bool processCountsWithThreshold(int threshold, int countsOffset = 0)
        {
            bool flipFlop = false;
            bool doingFlip = false;

            //handleBit(true);
            bool WholeArray = false;
            for (int i = countsOffset; i < countsBufferIndex; i++)
            {
                if (countsBuffer[i] >= (uint)threshold) // over writes current flipFlop value
                {
                    if (doingFlip) // if we under-over, the data is bad since unders must be paired
                    {
                        return false;
                    }
                    WholeArray = handleBit(flipFlop);
                }
                else if (doingFlip) // 2nd under writes current flipFlop value
                {
                    WholeArray = handleBit(flipFlop);
                    doingFlip = false;
                }
                else // first under skips write and just flipsFlops
                {
                    doingFlip = true;
                }
                flipFlop = !flipFlop;
                if (WholeArray)
                    break;
            }

            //Console.WriteLine();
            return true;
        }

        private bool handleBit(bool bit)
        {
            int bit_ = bit ? 1 : 0;
            /*{
                Console.ForegroundColor = rx_buffer_len % 2 == 0 ? ConsoleColor.Gray : ConsoleColor.DarkGray;
                Console.Write(bit_);
            }*/
            currentByte = (byte)((currentByte << 1) | bit_);

            currentBitIndex++;
            if (currentBitIndex == 8)
            {
                currentBitIndex = 0;
                _MergedGlobals__rx_buffer[rx_buffer_len] = currentByte;
                rx_buffer_len++;
                currentByte = 0;
            }
            if (rx_buffer_len == 7 && currentBitIndex == 1)
            {
                _MergedGlobals__rx_buffer[7] = currentByte;

                verifyChecksum();
                return true;
            }
            return false;
        }

        private bool verifyChecksum()
        {
            self_checksumOK = false;
            if (rx_buffer_len > 6)
            {
                byte flipped67 = currentBitIndex == 1 ? bitreverse((byte)((_MergedGlobals__rx_buffer[6] << 1) | (_MergedGlobals__rx_buffer[7] & 0x01)))
                                                      : bitreverse((byte)((_MergedGlobals__rx_buffer[6] << 5) | (_MergedGlobals__rx_buffer[7] & 0x1F)));

                byte N = 0;
                for (int i = 0; i < 7; i++)
                {
                    byte val = _MergedGlobals__rx_buffer[i];
                    if (i == 6)
                        val = (byte)(_MergedGlobals__rx_buffer[i] & 0xF8);
                    N ^= val;
                }
                byte flippedN = bitreverse(N);
                byte flipped3 = bitreverse((byte)(_MergedGlobals__rx_buffer[3]));
                byte flipped2 = bitreverse((byte)(_MergedGlobals__rx_buffer[2]));
                if (currentBitIndex == 1)
                {
                    self_receivedChecksum = (byte)((flipped67 >> 4) & 0xF);
                    self_expectedChecksum = (byte)(chksumindex[(byte)(flipped2 ^ flipped3 ^ chksumindex[flippedN])] >> 4);
                }
                else
                {
                    self_receivedChecksum = flipped67;
                    self_expectedChecksum = chksumindex[(byte)(flipped2 ^ flipped3 ^ chksumindex[flippedN])];
                }
                self_checksumOK = self_receivedChecksum == self_expectedChecksum;

                {
                    //finishedPacket();
                    DeviceReportEvent threadSafeEvent = DeviceReport;
                    threadSafeEvent?.Invoke(new GenericBytesReport() { CodeString = "SXTYBEAT", ReportBytes = _MergedGlobals__rx_buffer }); // TODO: consider a `60BT` report type instead

                    //Console.WriteLine();
                }
            }

            return self_checksumOK;
        }

        private byte bitreverse(byte a1)
        {
            byte v1 = a1;
            byte v2 = (byte)(a1 & 1);
            for (int i = 0; i < 7; i++)
            {
                v1 >>= 1;
                v2 = (byte)((v1 & 1) | (v2 << 1));
            }

            return v2;
        }

        public bool WriteReport(byte[] data)
        {
            //try
            {
                //GetStream().Write(data);
                //return true;
            }
            //catch
            {
                return false;
            }
        }

        public bool WriteFeatureData(byte[] data)
        {
            //try
            //{
            //int maxLen = internalDevice.GetMaxFeatureReportLength();
            //GetStream().SetFeature(data);
            //    return true;
            //}
            //catch
            {
                return false;
            }
        }

        public bool ReadFeatureData(out byte[] data, byte reportId = 0)
        {
            data = new byte[0];
            /*data = new byte[internalDevice.GetMaxFeatureReportLength()];
            try
            {
                data[0] = reportId;
                byte[] buffer = new byte[data.Length];
                GetStream().GetFeature(data);
                return true;
            }
            catch*/
            {
                return false;
            }
        }

        public void Dispose()
        {
            //if (MonitorDeviceEvents) MonitorDeviceEvents = false;
            //if (IsOpen) CloseDevice();
        }

        public string ReadSerialNumber()
        {
            return string.Empty;// internalDevice.GetSerialNumber();
        }

        public string UniqueKey => $"SixtyBeatAudioDevice {this.DeviceID}";

        bool IEquatable<IDevice>.Equals(IDevice other)
        {
            return this.UniqueKey == other.UniqueKey;
        }

        //public event DeviceReportEvent ControllerNameUpdated;


        public event DeviceReportEvent DeviceReport;
    }
}