using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.DeviceProvider
{
    public enum REPORT_TYPE : UInt32
    {
        XINP = 0x504e4958, // 'XINP'
        HID  = 0x504e4958, // 'HID~'
        GENB = 0x424e4547, // 'GENB'
    }

    public interface IReport
    {
        REPORT_TYPE ReportTypeCode { get; }
    }

    public enum HidReportType // borrowing these IDs from BT-HID just 'cause I can
    {
        Input = 0xA1, // read
        Output = 0xA2, // write
        FeatureInput = 0xA3, // read
        FeatureOutput = 0x53, // write
    }
    public struct HidReport : IReport
    {
        public REPORT_TYPE ReportTypeCode => REPORT_TYPE.HID;
        public HidReportType ReportType;
        public byte ReportId;
        public byte[] ReportBytes;
    }

    public struct XInputReport : IReport
    {
        public REPORT_TYPE ReportTypeCode => REPORT_TYPE.XINP;
        public UInt16 wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public Int32 sThumbLX;
        public Int32 sThumbLY;
        public Int32 sThumbRX;
        public Int32 sThumbRY;
    }

    public struct GenericBytesReport : IReport
    {
        public REPORT_TYPE ReportTypeCode => REPORT_TYPE.GENB;
        public UInt64 Code;
        public byte[] ReportBytes;

        public string CodeString
        {
            get
            {
                return Encoding.ASCII.GetString(BitConverter.GetBytes(Code));
            }
            set
            {
                Code = BitConverter.ToUInt64(Encoding.ASCII.GetBytes(value), 0);
            }
        }
    }
}
