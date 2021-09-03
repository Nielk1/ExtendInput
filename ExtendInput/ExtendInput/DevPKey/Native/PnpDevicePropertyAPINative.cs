using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.DevPKey.Native
{
    public class PnpDevicePropertyAPINative
    {
        public static DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc = new DEVPROPKEY { fmtid = new Guid(0x540b947e, 0x8b40, 0x45bc, 0xa8, 0xa2, 0x6a, 0x0b, 0x89, 0x4c, 0xbd, 0xa2), pid =   4 };
        public static DEVPROPKEY DEVPKEY_Device_HardwareIds           = new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid =   3 };
        public static DEVPROPKEY DEVPKEY_Device_UINumber              = new DEVPROPKEY { fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), pid =  18 };
        public static DEVPROPKEY DEVPKEY_Device_DriverVersion         = new DEVPROPKEY { fmtid = new Guid(0xa8b865dd, 0x2e3d, 0x4094, 0xad, 0x97, 0xe5, 0x93, 0xa7, 0x0c, 0x75, 0xd6), pid =   3 };
        public static DEVPROPKEY DEVPKEY_Device_Parent                = new DEVPROPKEY { fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7), pid =   8 };
        public static DEVPROPKEY DEVPKEY_Device_Siblings              = new DEVPROPKEY { fmtid = new Guid(0x4340a6c5, 0x93fa, 0x4706, 0x97, 0x2c, 0x7b, 0x64, 0x80, 0x08, 0xa5, 0xa7), pid =  10 };
        public static DEVPROPKEY DEVPKEY_Device_InstanceId            = new DEVPROPKEY { fmtid = new Guid(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57), pid = 256 };
        public static DEVPROPKEY DEVPKEY_Device_ContainerId           = new DEVPROPKEY { fmtid = new Guid(0x8c7ed206, 0x3f8a, 0x4827, 0xb3, 0xab, 0xae, 0x9e, 0x1f, 0xae, 0xfc, 0x6c), pid =   2 };
        
        public static DEVPROPKEY DEVPKEY_Audio_InstanceId             = new DEVPROPKEY { fmtid = new Guid(0x9c119480, 0xddc2, 0x4954, 0xa1, 0x50, 0x5b, 0xd2, 0x40, 0xd4, 0x54, 0xad), pid =   2 };


        public const int FILE_FLAG_OVERLAPPED = 0x40000000;
        public const uint FILE_FLAG_NO_BUFFERING = 0x20000000;
        public const uint FILE_FLAG_WRITE_THROUGH = 0x80000000;
        public const uint FILE_ATTRIBUTE_TEMPORARY = 0x100;

        public const short FILE_SHARE_READ = 0x1;
        public const short FILE_SHARE_WRITE = 0x2;
        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const Int32 FileShareRead = 1;
        public const Int32 FileShareWrite = 2;
        public const Int32 OpenExisting = 3;
        public const int ACCESS_NONE = 0;
        public const int INVALID_HANDLE_VALUE = -1;
        public const short OPEN_EXISTING = 3;
        public const int WAIT_TIMEOUT = 0x102;
        public const uint WAIT_OBJECT_0 = 0;
        public const uint WAIT_FAILED = 0xffffffff;

        public const int WAIT_INFINITE = 0xffff;


        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int DBT_DEVTYP_DEVICEINTERFACE = 5;
        public const int DBT_DEVTYP_HANDLE = 6;
        public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        public const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        public const int WM_DEVICECHANGE = 0x219;
        public const short DIGCF_PRESENT = 0x2;
        public const short DIGCF_DEVICEINTERFACE = 0x10;
        public const int DIGCF_ALLCLASSES = 0x4;
        public const int DICS_ENABLE = 1;
        public const int DICS_DISABLE = 2;
        public const int DICS_FLAG_GLOBAL = 1;
        public const int DIF_PROPERTYCHANGE = 0x12;

        public const int MAX_DEV_LEN = 1000;
        public const int SPDRP_ADDRESS = 0x1c;
        public const int SPDRP_BUSNUMBER = 0x15;
        public const int SPDRP_BUSTYPEGUID = 0x13;
        public const int SPDRP_CAPABILITIES = 0xf;
        public const int SPDRP_CHARACTERISTICS = 0x1b;
        public const int SPDRP_CLASS = 7;
        public const int SPDRP_CLASSGUID = 8;
        public const int SPDRP_COMPATIBLEIDS = 2;
        public const int SPDRP_CONFIGFLAGS = 0xa;
        public const int SPDRP_DEVICE_POWER_DATA = 0x1e;
        public const int SPDRP_DEVICEDESC = 0;
        public const int SPDRP_DEVTYPE = 0x19;
        public const int SPDRP_DRIVER = 9;
        public const int SPDRP_ENUMERATOR_NAME = 0x16;
        public const int SPDRP_EXCLUSIVE = 0x1a;
        public const int SPDRP_FRIENDLYNAME = 0xc;
        public const int SPDRP_HARDWAREID = 1;
        public const int SPDRP_LEGACYBUSTYPE = 0x14;
        public const int SPDRP_LOCATION_INFORMATION = 0xd;
        public const int SPDRP_LOWERFILTERS = 0x12;
        public const int SPDRP_MFG = 0xb;
        public const int SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0xe;
        public const int SPDRP_REMOVAL_POLICY = 0x1f;
        public const int SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x20;
        public const int SPDRP_REMOVAL_POLICY_OVERRIDE = 0x21;
        public const int SPDRP_SECURITY = 0x17;
        public const int SPDRP_SECURITY_SDS = 0x18;
        public const int SPDRP_SERVICE = 4;
        public const int SPDRP_UI_NUMBER = 0x10;
        public const int SPDRP_UI_NUMBER_DESC_FORMAT = 0x1d;

        public const int SPDRP_UPPERFILTERS = 0x11;

        public const int ERROR_INSUFFICIENT_BUFFER = 0x7A;


        [DllImport("hid.dll")]
        static public extern void HidD_GetHidGuid(ref Guid hidGuid);


        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr SetupDiGetClassDevs(ref System.Guid classGuid, string enumerator, int hwndParent, int flags);
        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr SetupDiGetClassDevs(IntPtr classGuid, string enumerator, int hwndParent, int flags);


        [DllImport("setupapi.dll")]
        static public extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, int memberIndex, ref SP_DEVINFO_DATA deviceInfoData);


        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public Int32 cbSize;
            public Guid ClassGuid;
            public IntPtr DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public Int32 cbSize;
            public Guid interfaceClassGuid;
            public Int32 flags;
            private UIntPtr reserved;
        }

        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDevicePropertyW", SetLastError = true)]
        public static extern bool SetupDiGetDeviceProperty(IntPtr deviceInfo, ref SP_DEVINFO_DATA deviceInfoData, ref DEVPROPKEY propkey, ref ulong propertyDataType, byte[] propertyBuffer, int propertyBufferSize, ref int requiredSize, uint flags);

        [DllImport("setupapi.dll")]
        static public extern int SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);



        [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDevicePropertyKeys", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiGetDevicePropertyKeys(IntPtr deviceInfo, ref SP_DEVINFO_DATA deviceInfoData, [Out, Optional, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] DEVPROPKEY[] PropertyKeyArray, uint PropertyKeyCount, out uint RequiredPropertyKeyCount, uint Flags = 0);



        [StructLayout(LayoutKind.Sequential)]
        public struct DEVPROPKEY
        {
            public Guid fmtid;
            public UInt32 pid;
        }










        public const int CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
        public const int CR_SUCCESS = 0x00000000;

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int CM_Locate_DevNode(out IntPtr pdnDevInst, string pDeviceID, int ulFlags);
        //CMAPI CONFIGRET CM_Locate_DevNodeA(PDEVINST pdnDevInst, DEVINSTID_A pDeviceID, ULONG ulFlags);

        [DllImport("setupapi.dll")]
        public static extern int CM_Get_Parent(out IntPtr pdnDevInst, IntPtr dnDevInst, int ulFlags = 0);
        //CMAPI CONFIGRET CM_Get_Parent(PDEVINST pdnDevInst, DEVINST dnDevInst, ULONG ulFlags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern int CM_Get_Device_ID_Size(out int pulLen, IntPtr dnDevInst, int flags = 0);
        //CMAPI CONFIGRET CM_Get_Device_ID_Size(PULONG pulLen, DEVINST dnDevInst, ULONG ulFlags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto)]
        public static extern int CM_Get_Device_ID(IntPtr dnDevInst, IntPtr buffer, int bufferLen, int flags = 0);
        //CMAPI CONFIGRET CM_Get_Device_ID(DEVINST dnDevInst, PWSTR Buffer, ULONG BufferLen, ULONG ulFlags);
    }
}
