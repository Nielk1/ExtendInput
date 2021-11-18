using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.Betop
{
    public class BetopControllerFactory : IControllerFactory
    {
        private AccesMode AccessMode;
        //private object DeviceControllerMapLock = new object(); // use the locking of Controllers with these for now
        private Dictionary<string, Guid> DeviceToControllerKeyMap = new Dictionary<string, Guid>();
        private Dictionary<Guid, HashSet<string>> ControllerToDeviceKeyMap = new Dictionary<Guid, HashSet<string>>();
        public BetopControllerFactory(AccesMode AccessMode)
        {
            this.AccessMode = AccessMode;
        }

        Dictionary<Guid, BetopController> Controllers = new Dictionary<Guid, BetopController>();
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
            new Dictionary<string, dynamic>(){ { "VID", BetopController.VENDOR_BETOP }, { "PID", BetopController.PRODUCT_BETOP_ASURA3 } }, // asume USB, if ever anything else code changes needed
            new Dictionary<string, dynamic>(){ { "VID", BetopController.VENDOR_BETOP }, { "PID", BetopController.PRODUCT_BETOP_ASURA3_DONGLE } },
            new Dictionary<string, dynamic>(){ { "VID", 0x045E }, { "PID", 0x028E } },

            // temporary
            //new Dictionary<string, dynamic>(){ { "VID", 0x057E }, { "PID", 0x2009 } },
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
                                Thread.Sleep(20);
                                candidate.SetVibration(0x0100, 0x0600);
                                Thread.Sleep(20);
                                candidate.SetVibration(0x0500, 0x0300);
                                Thread.Sleep(20);
                                candidate.SetVibration(0x0200, 0x0400);
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

            if (deviceXInput != null && this.AccessMode != AccesMode.FullControl)
                return null;

            if (deviceHid != null)
            {
                if (deviceHid.VendorId == 0x045E && deviceHid.ProductId == 0x028E)
                {
                    if (this.AccessMode != AccesMode.FullControl)
                        return null;

                    if (deviceHid.Properties["ProductName"].Contains(" A2 GAMEPAD ") // ASURA3
                     || deviceHid.Properties["ProductName"].Contains(" BD4E "))
                    {
                        // hold logo to switch modes, or allow this code below to run (they don't have a choice)
                        lock (CandidateXInputLock)
                            SawCandidateHidDeviceForXInput = DateTime.UtcNow;
                        CheckXInputData();
                    }
                    else if (deviceHid.Properties["ProductName"].Contains(" A2 RECEIVER ")) // ASURA3_DONGLE
                    {
                        // hold logo to switch modes
                    }
                    return null;
                }
            }
            else if (deviceXInput != null)
            {
                if (this.AccessMode != AccesMode.FullControl)
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

            if (device.VendorId == BetopController.VENDOR_BETOP && (device.ProductId == BetopController.PRODUCT_BETOP_ASURA3
                                                                 || device.ProductId == BetopController.PRODUCT_BETOP_ASURA3_DONGLE))
            {
                uint[] Usages = device.Properties.ContainsKey("Usages") ? device.Properties["Usages"] as uint[] : null;
                if (Usages != null && (Usages.Contains(0xff000003u) || Usages.Contains(0x00010005u)))
                //if (device.DevicePath.Contains("&col05") || device.DevicePath.Contains("&col04"))
                {
                    {
                        string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(deviceHid.DevicePath);
                        Guid? ContrainerID = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId);
                        if (ContrainerID.HasValue)
                            lock (Controllers)
                            {
                                BetopController ctrl = null;
                                if (Controllers.ContainsKey(ContrainerID.Value))
                                {
                                    ctrl = Controllers[ContrainerID.Value];
                                    ctrl.AddDevice(deviceHid);
                                }
                                else
                                {
                                    Controllers[ContrainerID.Value] = new BetopController(ContrainerID.Value.ToString(), AccessMode, deviceHid);
                                    ctrl = Controllers[ContrainerID.Value];
                                }

                                DeviceToControllerKeyMap[device.UniqueKey] = ContrainerID.Value;
                                if (!ControllerToDeviceKeyMap.ContainsKey(ContrainerID.Value))
                                    ControllerToDeviceKeyMap[ContrainerID.Value] = new HashSet<string>();
                                ControllerToDeviceKeyMap[ContrainerID.Value].Add(device.UniqueKey);

                                return ctrl;


                                /*if (Usages.Contains(0xff000003u))
                                //if (device.DevicePath.Contains("&col05"))
                                {
                                    Controllers[ContrainerID.Value].bVendor = true;
                                    Controllers[ContrainerID.Value].Vendor = new WeakReference<HidDevice>(deviceHid);
                                }
                                if (Usages.Contains(0x00010005u))
                                //if (device.DevicePath.Contains("&col04"))
                                {
                                    Controllers[ContrainerID.Value].bGamepad = true;
                                    Controllers[ContrainerID.Value].Gamepad = new WeakReference<HidDevice>(deviceHid);
                                }

                                Console.WriteLine(ContrainerID.Value.ToString());

                                BetopController ctrl = null;
                                if (Controllers[ContrainerID.Value].bGamepad && Controllers[ContrainerID.Value].bVendor)
                                {
                                    HidDevice deviceVendor = null;
                                    HidDevice deviceGamepad = null;
                                    Controllers[ContrainerID.Value]?.Vendor.TryGetTarget(out deviceVendor);
                                    Controllers[ContrainerID.Value]?.Gamepad.TryGetTarget(out deviceGamepad);

                                    if (deviceVendor != null && deviceGamepad != null)
                                    {
                                        ctrl = new BetopController(AccessMode, deviceVendor, deviceGamepad);
                                    }
                                    else
                                    {
                                        Controllers.Remove(ContrainerID.Value); // wtf this should never happen
                                    }
                                }

                                // clear any dead refs
                                foreach (Guid key in Controllers.Keys.ToList()) // ToList to clone the key list so we can modify it
                                {
                                    ControllerPair candidate = Controllers[key];
                                    if ((!candidate.bVendor && !candidate.bGamepad) || ((!candidate.Vendor?.TryGetTarget(out _) ?? false) && (!candidate.Gamepad?.TryGetTarget(out _) ?? false)))
                                        Controllers.Remove(key);
                                }

                                return ctrl;*/
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

                BetopController ctrl = Controllers[DeviceParent];
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
