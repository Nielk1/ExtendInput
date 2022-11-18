using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.Test
{
    public class TestControllerFactory : IControllerFactory
    {
        private AccessMode AccessMode;
        //private object DeviceControllerMapLock = new object(); // use the locking of Controllers with these for now
        private Dictionary<string, Guid> DeviceToControllerKeyMap = new Dictionary<string, Guid>();
        private Dictionary<Guid, HashSet<string>> ControllerToDeviceKeyMap = new Dictionary<Guid, HashSet<string>>();
        public TestControllerFactory(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
        }

        Dictionary<Guid, TestController> Controllers = new Dictionary<Guid, TestController>();
        public class ControllerPair
        {
            public bool bVendor;
            public bool bGamepad;
            // TOOD: if these are both weak references, are we literally just praying we get both before a race condition eliminates one?
            public WeakReference<HidDevice> Vendor;
            public WeakReference<HidDevice> Gamepad;
        }

        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", TestController.VENDOR_TEST }, { "PID", TestController.PRODUCT_TEST_TEST } }, // asume USB, if ever anything else code changes needed
        };

        const int XINPUT_PLAYER_COUNT = 4;
        object CandidateXInputLock = new object();
        WeakReference<XInputDevice>[] CandidateXInputDevicesX = new WeakReference<XInputDevice>[XINPUT_PLAYER_COUNT];
        // TODO: Store a XInputDevice.UniqueKey to UserIndex map so we can handle XInputDevice removals, make the above WeakReference into a strong reference
        DateTime[] CandidateXInputDevicesLastSeen = new DateTime[XINPUT_PLAYER_COUNT];
        DateTime SawCandidateHidDeviceForXInput = DateTime.MinValue;

        private void CheckXInputData()
        {
            lock (CandidateXInputLock)
            {
                DateTime checkTime = DateTime.UtcNow;
                if (SawCandidateHidDeviceForXInput.AddSeconds(10) > checkTime)
                {
                    for (int i = 0; i < XINPUT_PLAYER_COUNT; i++)
                    {
                        if (CandidateXInputDevicesLastSeen[i].AddSeconds(10) > checkTime)
                        {
                            XInputDevice candidate = null;
                            CandidateXInputDevicesX[i]?.TryGetTarget(out candidate);
                            // If the candidate still exists, is connected, and has the expected player number
                            if (candidate != null /*&& candidate.IsConnected*/ && (byte)candidate.UserIndex == i)
                            {
                                candidate.SetVibration(0x0015, 0x0015);
                                Thread.Sleep(50);
                                candidate.SetVibration(0x0000, 0x0000);
                                candidate.SetVibration(0x000C, 0x000C);
                                Thread.Sleep(20);
                                candidate.SetVibration(0x0000, 0x0000);
                                candidate.SetVibration(0x0015, 0x0015);
                                Thread.Sleep(20);
                                candidate.SetVibration(0x0000, 0x0000);
                                candidate.SetVibration(0x0036, 0x0036);
                                Thread.Sleep(20);
                                candidate.SetVibration(0x0000, 0x0000);
                            }
                        }
                    }
                }
            }
        }

        public IController NewDevice(IDevice device)
        {
            HidDevice deviceHid = device as HidDevice;
            XInputDevice deviceXInput = device as XInputDevice;

            // We dont understand this device type
            if (deviceHid == null && deviceXInput == null)
                return null;

            if (deviceXInput != null && this.AccessMode != AccessMode.FullControl)
                return null;

            /*if (deviceHid != null)
            {
                if (deviceHid.VendorId == TestController.VENDOR_TEST && deviceHid.ProductId == TestController.PRODUCT_TEST_TEST)
                {
                    if (this.AccessMode != AccessMode.FullControl)
                        return null;

                    // force switch into Test mode
                    lock (CandidateXInputLock)
                        SawCandidateHidDeviceForXInput = DateTime.UtcNow;
                    CheckXInputData();

                    return null;
                }
            }
            else*/
            if (deviceXInput != null)
            {
                if (this.AccessMode != AccessMode.FullControl)
                    return null;

                lock (CandidateXInputLock)
                {
                    // Keep a weak reference on all 4 XInput controllers. Note that this weak reference will fail to work properly if there is no XInput controller.
                    CandidateXInputDevicesX[(byte)deviceXInput.UserIndex] = new WeakReference<XInputDevice>(deviceXInput);
                    CandidateXInputDevicesLastSeen[(byte)deviceXInput.UserIndex] = DateTime.UtcNow;
                }
                CheckXInputData();
                return null;
            }

            if (device.VendorId == TestController.VENDOR_TEST && (device.ProductId == TestController.PRODUCT_TEST_TEST))
            {
                ////uint[] Usages = device.Properties.ContainsKey("Usages") ? device.Properties["Usages"] as uint[] : null;
                ////if (Usages != null && (Usages.Contains(0xff000003u) || Usages.Contains(0x00010005u)))
                //if (device.DevicePath.Contains("&col05") || device.DevicePath.Contains("&col04"))
                {
                    {
                        string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(deviceHid.DevicePath);
                        Guid? ContrainerID = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId);
                        if (ContrainerID.HasValue)
                            lock (Controllers)
                            {
                                TestController ctrl = null;
                                if (Controllers.ContainsKey(ContrainerID.Value))
                                {
                                    ctrl = Controllers[ContrainerID.Value];
                                    ctrl.AddDevice(deviceHid);
                                }
                                else
                                {
                                    Controllers[ContrainerID.Value] = new TestController(ContrainerID.Value.ToString(), AccessMode, deviceHid);
                                    ctrl = Controllers[ContrainerID.Value];
                                }

                                DeviceToControllerKeyMap[device.UniqueKey] = ContrainerID.Value;
                                if (!ControllerToDeviceKeyMap.ContainsKey(ContrainerID.Value))
                                    ControllerToDeviceKeyMap[ContrainerID.Value] = new HashSet<string>();
                                ControllerToDeviceKeyMap[ContrainerID.Value].Add(device.UniqueKey);

                                return ctrl;
                            }
                    }
                }
            }

            return null;
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                if (!DeviceToControllerKeyMap.ContainsKey(UniqueKey))
                    return null;

                Guid DeviceParent = DeviceToControllerKeyMap[UniqueKey];

                foreach(string DeviceKey in ControllerToDeviceKeyMap[DeviceParent])
                    if (DeviceToControllerKeyMap.ContainsKey(DeviceKey))
                        DeviceToControllerKeyMap.Remove(DeviceKey);

                TestController ctrl = Controllers[DeviceParent];
                string UniqueControllerId = ctrl.ConnectionUniqueID;

                ctrl.DeInitalize();
                ctrl.Dispose();
                Controllers.Remove(DeviceParent);
                ControllerToDeviceKeyMap.Remove(DeviceParent);

                return UniqueControllerId;
            }
        }
    }
}
