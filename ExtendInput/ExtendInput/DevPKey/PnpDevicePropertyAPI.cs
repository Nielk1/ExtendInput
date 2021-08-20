using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.DevPKey
{
    public class PnpDevicePropertyAPI
    {
        public static string devicePathToInstanceId(string devicePath)
        {
            string deviceInstanceId = devicePath;
            deviceInstanceId = deviceInstanceId.Remove(0, deviceInstanceId.LastIndexOf("?\\") + 2);
            deviceInstanceId = deviceInstanceId.Remove(deviceInstanceId.LastIndexOf('{'));
            deviceInstanceId = deviceInstanceId.Replace('#', '\\');
            if (deviceInstanceId.EndsWith("\\"))
            {
                deviceInstanceId = deviceInstanceId.Remove(deviceInstanceId.Length - 1);
            }

            return deviceInstanceId;
        }


        public static string GetDeviceProperty(string deviceInstanceId, Native.PnpDevicePropertyAPINative.DEVPROPKEY prop)
        {
            string result = string.Empty;
            Native.PnpDevicePropertyAPINative.SP_DEVINFO_DATA deviceInfoData = new Native.PnpDevicePropertyAPINative.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;

            Guid hidGuid = new Guid();
            Native.PnpDevicePropertyAPINative.HidD_GetHidGuid(ref hidGuid);
            IntPtr deviceInfoSet = Native.PnpDevicePropertyAPINative.SetupDiGetClassDevs(ref hidGuid, deviceInstanceId, 0, Native.PnpDevicePropertyAPINative.DIGCF_PRESENT | Native.PnpDevicePropertyAPINative.DIGCF_DEVICEINTERFACE);
            //IntPtr deviceInfoSet = Native.PnpDevicePropertyAPINative.SetupDiGetClassDevs(IntPtr.Zero, deviceInstanceId, 0, Native.PnpDevicePropertyAPINative.DIGCF_PRESENT | Native.PnpDevicePropertyAPINative.DIGCF_DEVICEINTERFACE | Native.PnpDevicePropertyAPINative.DIGCF_ALLCLASSES);
            /*bool tempval = */
            Native.PnpDevicePropertyAPINative.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            if (Native.PnpDevicePropertyAPINative.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref prop, ref propertyType,
                    dataBuffer, dataBuffer.Length, ref requiredSize, 0))
            {
                result = dataBuffer.ToUTF16String();
            }

            if (deviceInfoSet.ToInt64() != Native.PnpDevicePropertyAPINative.INVALID_HANDLE_VALUE)
            {
                Native.PnpDevicePropertyAPINative.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }

            return result;
        }

        public static Dictionary<Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> GetDeviceProperties(string deviceInstanceId)
        {
            //string result = string.Empty;
            Native.PnpDevicePropertyAPINative.SP_DEVINFO_DATA deviceInfoData = new Native.PnpDevicePropertyAPINative.SP_DEVINFO_DATA();
            deviceInfoData.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(deviceInfoData);
            var dataBuffer = new byte[4096];
            ulong propertyType = 0;
            var requiredSize = 0;

            Guid hidGuid = new Guid();
            Native.PnpDevicePropertyAPINative.HidD_GetHidGuid(ref hidGuid);
            IntPtr deviceInfoSet = Native.PnpDevicePropertyAPINative.SetupDiGetClassDevs(ref hidGuid, deviceInstanceId, 0, Native.PnpDevicePropertyAPINative.DIGCF_PRESENT | Native.PnpDevicePropertyAPINative.DIGCF_DEVICEINTERFACE);
            //IntPtr deviceInfoSet = Native.PnpDevicePropertyAPINative.SetupDiGetClassDevs(IntPtr.Zero, deviceInstanceId, 0, Native.PnpDevicePropertyAPINative.DIGCF_PRESENT | Native.PnpDevicePropertyAPINative.DIGCF_DEVICEINTERFACE | Native.PnpDevicePropertyAPINative.DIGCF_ALLCLASSES);
            //int memberIndex = 0;
            Dictionary<Native.PnpDevicePropertyAPINative.DEVPROPKEY, string> results = null;
            Native.PnpDevicePropertyAPINative.SetupDiEnumDeviceInfo(deviceInfoSet, 0, ref deviceInfoData);
            //while (Native.PnpDevicePropertyAPINative.SetupDiEnumDeviceInfo(deviceInfoSet, memberIndex, ref deviceInfoData))
            {
                //if (Native.PnpDevicePropertyAPINative.SetupDiGetDevicePropertyKeys(deviceInfoSet, ref deviceInfoData, null, 0, out var cnt) == Win32Error.ERROR_INSUFFICIENT_BUFFER)
                if (!Native.PnpDevicePropertyAPINative.SetupDiGetDevicePropertyKeys(deviceInfoSet, ref deviceInfoData, null, 0, out var cnt) && Marshal.GetLastWin32Error() == Native.PnpDevicePropertyAPINative.ERROR_INSUFFICIENT_BUFFER)
                {
                    var arr = new Native.PnpDevicePropertyAPINative.DEVPROPKEY[cnt];
                    if (Native.PnpDevicePropertyAPINative.SetupDiGetDevicePropertyKeys(deviceInfoSet, ref deviceInfoData, arr, (uint)arr.Length, out _))
                    {
                        results = new Dictionary<Native.PnpDevicePropertyAPINative.DEVPROPKEY, string>();
                        foreach (var arr_ in arr.Distinct())
                        {
                            var arrItem = arr_;
                            if (Native.PnpDevicePropertyAPINative.SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref arrItem, ref propertyType, dataBuffer, dataBuffer.Length, ref requiredSize, 0))
                            {
                                try
                                {
                                    string result = dataBuffer.Take(requiredSize).ToArray().ToUTF16String();
                                    if (result.Length > 4)
                                    {
                                        results.Add(arrItem, result);
                                    }
                                    else
                                    {
                                        results.Add(arrItem, BitConverter.ToString(dataBuffer.Take(requiredSize).ToArray()).Replace("-", string.Empty));
                                    }
                                }
                                catch
                                {
                                    results.Add(arrItem, BitConverter.ToString(dataBuffer.Take(requiredSize).ToArray()).Replace("-", string.Empty));
                                }
                            }
                            else
                            {
                                //results.Add(arrItem, "null");
                            }
                        }
                    }
                }
                //memberIndex++;
            }
            if (deviceInfoSet.ToInt64() != Native.PnpDevicePropertyAPINative.INVALID_HANDLE_VALUE)
            {
                Native.PnpDevicePropertyAPINative.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            }
            return results;
        }


        public CheckVirtualInfo CheckForVirtualDevice(string deviceInstanceId)
        {
            string temp = GetDeviceProperty(deviceInstanceId,
                Native.PnpDevicePropertyAPINative.DEVPKEY_Device_UINumber);

            CheckVirtualInfo info = new CheckVirtualInfo()
            {
                PropertyValue = temp,
                DeviceInstanceId = deviceInstanceId,
            };
            return info;
        }

        public class CheckVirtualInfo : EventArgs
        {
            private string deviceInstanceId;
            public string DeviceInstanceId
            {
                get => deviceInstanceId;
                set => deviceInstanceId = value;
            }

            private string propertyValue;
            public string PropertyValue { get => propertyValue; set => propertyValue = value; }

            public CheckVirtualInfo() : base()
            {
            }
        }



        public static bool GetParentDeviceInstanceId(string DeviceInstanceId, out string ParentDeviceInstanceId)
        {
            ParentDeviceInstanceId = null;

            IntPtr pdnDevInst;
            int apiResult = Native.PnpDevicePropertyAPINative.CM_Locate_DevNode(out pdnDevInst, DeviceInstanceId, Native.PnpDevicePropertyAPINative.CM_LOCATE_DEVNODE_NORMAL);
            if (apiResult != Native.PnpDevicePropertyAPINative.CR_SUCCESS)
            {
                return false;
            }

            IntPtr parent;
            apiResult = Native.PnpDevicePropertyAPINative.CM_Get_Parent(out parent, pdnDevInst);
            if (apiResult != Native.PnpDevicePropertyAPINative.CR_SUCCESS)
            {
                return false;
            }

            int nBytes;
            apiResult = Native.PnpDevicePropertyAPINative.CM_Get_Device_ID_Size(out nBytes, parent);
            if (apiResult != Native.PnpDevicePropertyAPINative.CR_SUCCESS)
            {
                return false;
            }

            IntPtr ptrInstanceBuf = Marshal.AllocHGlobal(nBytes * 2); // Note: Buffer is *2 size just because it might use Unicode (and normally does)
            try
            {
                apiResult = Native.PnpDevicePropertyAPINative.CM_Get_Device_ID(parent, ptrInstanceBuf, nBytes);
                if (apiResult != Native.PnpDevicePropertyAPINative.CR_SUCCESS)
                {
                    return false;
                }
                ParentDeviceInstanceId = Marshal.PtrToStringAuto(ptrInstanceBuf, nBytes);

                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(ptrInstanceBuf);
            }
        }
    }
}
