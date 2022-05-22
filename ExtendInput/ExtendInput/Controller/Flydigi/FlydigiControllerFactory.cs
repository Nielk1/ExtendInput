using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller.Flydigi
{
    public class FlydigiControllerFactory : IControllerFactory
    {
        private AccessMode AccessMode;

        /// <summary>
        /// Controller instances stored by their parent collection ID
        /// </summary>
        private Dictionary<Guid, FlydigiController> Controllers = new Dictionary<Guid, FlydigiController>();

        /// <summary>
        /// Map of child device paths to parent collection IDs
        /// </summary>
        private Dictionary<string, Guid> ChildToParentMap = new Dictionary<string, Guid>();

        /// <summary>
        /// Map of parent collection IDs to child device paths and those devices
        /// </summary>
        private Dictionary<Guid, Dictionary<string, HidDevice>> ParentToChildMap = new Dictionary<Guid, Dictionary<string, HidDevice>>();

        public FlydigiControllerFactory(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;
        }

        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_DONGLE_1 } },
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_DONGLE_2 } },
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_DONGLE_3 } },
            new Dictionary<string, dynamic>(){ { "VID", FlydigiController.VENDOR_FLYDIGI }, { "PID", FlydigiController.PRODUCT_FLYDIGI_USB } },
            new Dictionary<string, dynamic>(){ { "VID", 0x045E }, { "PID", 0x028E } }, // Microsoft XBox 360 Controller
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
                            if(candidate != null /*&& candidate.IsConnected*/ && (byte)candidate.UserIndex == i)
                            {
                                candidate.SetVibration(0xAA00, 0xBB00);
                                Thread.Sleep(100);
                                candidate.SetVibration(0xBB00, 0xAA00);
                                Thread.Sleep(100);
                                candidate.SetVibration(0xAA00, 0xBB00);
                                Thread.Sleep(100);
                                candidate.SetVibration(0x0000, 0x0000);
                            }
                        }
                    }
                }
            }
        }

        public IController NewDevice(IDevice device)
        {
            HidDevice _device = device as HidDevice;
            XInputDevice _deviceX = device as XInputDevice;

            if (_device == null && _deviceX == null)
                return null;

            // TODO this check system should only run if FullControl mode is enabled
            //      this is all irrelevent if we have a system where we can get the true ProductName, VID, and PID of the XInput controller
            //      if we ever gain access to that info, we will have to make sure the below logic is still present for if the XInput provider
            //      doesn't have that info (check presence of Properties perhapse?)
            //      It might be simpler to keep the XInputDevice handling as is if the VID/PID/ProductName are null but if they are known immediately rumble
            //      In this design, if we see a HID device for the controller and we want to correct it, we'd start a count-down and if we didn't see any XInput
            //      devices we could manually trigger within that time (inlcuding one that came through just ahead) we'd force run over all the memorized ones
            //      Note that for some reason this doesn't work properly for an APEX2 on USB, it switches right back to XInput again.
            if (_device != null)
            {
                if (device.VendorId == 0x045E && device.ProductId == 0x028E)
                {
                    if (this.AccessMode != AccessMode.FullControl)
                        return null;

                    if (device.Properties["ProductName"] == "Controller (Flydigi 2.4G x360)"
                     || device.Properties["ProductName"] == "Controller (Flydigi 2.4G Android)") // APEX2 on USB does this for some reason
                    {
                        lock (CandidateXInputLock)
                            SawCandidateHidDeviceForXInput = DateTime.UtcNow;
                        CheckXInputData();
                    }
                    return null;
                }
            }
            else if (_deviceX != null)
            {
                if (this.AccessMode != AccessMode.FullControl)
                    return null;

                lock (CandidateXInputLock)
                {
                    CandidateXInputDevicesX[(byte)_deviceX.UserIndex] = new WeakReference<XInputDevice>(_deviceX);
                    CandidateXInputDevicesLastSeen[(byte)_deviceX.UserIndex] = DateTime.UtcNow;
                }
                CheckXInputData();
                return null;
            }

            // if we are in full control mode, check for xbox here and if it is a flydigi controller send the command that changes the mode here and return null

            if (_device.VendorId == FlydigiController.VENDOR_FLYDIGI)
            {
                if (!new int[] {
                    FlydigiController.PRODUCT_FLYDIGI_DONGLE_1,
                    FlydigiController.PRODUCT_FLYDIGI_DONGLE_2,
                    FlydigiController.PRODUCT_FLYDIGI_DONGLE_3,
                    FlydigiController.PRODUCT_FLYDIGI_USB,
                }.Contains(_device.ProductId))
                    return null;
            }
            else
            {
                return null;
            }

            //string bt_hid_id = @"00001124-0000-1000-8000-00805f9b34fb";

            string devicePath = _device.DevicePath.ToString();

            uint[] Usages = device.Properties.ContainsKey("Usages") ? device.Properties["Usages"] as uint[] : null;
            EConnectionType ConType = EConnectionType.Unknown;
            bool subDevice = false;
            switch (_device.VendorId)
            {
                case FlydigiController.VENDOR_FLYDIGI:
                    if (_device.Properties.ContainsKey("ProductName") && (((_device.Properties["ProductName"] as string)?.ToLowerInvariant()?.Contains("flydigi") ?? false) || ((_device.Properties["ProductName"] as string)?.ToLowerInvariant()?.Contains("feizhi") ?? false)))
                    {
                        //&& devicePath.Contains("mi_02")
                        if (Usages == null || Usages.Contains(0xffa00001u))
                        {
                            // we don't have a usage (not windows?) or we do and it's the custom device
                        }
                        else
                        {
                            subDevice = true;
                            //return null;
                        }
                        //if (devicePath.Contains(bt_hid_id))
                        //{
                        //    ConType = EConnectionType.Bluetooth;
                        //}
                        //else
                        //{
                        //    ConType = EConnectionType.USB;
                        //}
                    }
                    //else if (_device.ProductId == FlydigiController.PRODUCT_FLYDIGI_USB && devicePath.Contains("mi_00"))
                    //{
                    //
                    //}
                    else
                    {
                        return null;
                    }
                    break;
            }

            string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(_device.DevicePath);
            Guid? ContrainerID = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId);

            if (!ContrainerID.HasValue)
                return null; // TODO handle non-windows, which means moving the entire device parent system

            lock (Controllers)
            {
                if (!ParentToChildMap.ContainsKey(ContrainerID.Value))
                    ParentToChildMap[ContrainerID.Value] = new Dictionary<string, HidDevice>();
                ParentToChildMap[ContrainerID.Value][_device.UniqueKey] = _device;
                ChildToParentMap[_device.UniqueKey] = ContrainerID.Value;

                if (subDevice)
                {
                    if (Controllers.ContainsKey(ContrainerID.Value))
                    {
                        FlydigiController ctrl = Controllers[ContrainerID.Value];
                        ctrl.AddSubDevice(_device);
                        return ctrl;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (Controllers.ContainsKey(ContrainerID.Value))
                    {
                        return Controllers[ContrainerID.Value];// wtf?
                    }
                    else
                    {
                        FlydigiController ctrl = new FlydigiController(_device, ConType, AccessMode);
                        if (ParentToChildMap.ContainsKey(ContrainerID.Value))
                            foreach (var kv in ParentToChildMap[ContrainerID.Value])
                                ctrl.AddSubDevice(kv.Value);
                        Controllers[ContrainerID.Value] = ctrl;
                        return ctrl;
                    }
                }
            }
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                //Console.WriteLine($"Removing {UniqueKey}");
                if (ChildToParentMap.ContainsKey(UniqueKey))
                {
                    Guid ParentKey = ChildToParentMap[UniqueKey];

                    if (ParentToChildMap[ParentKey].ContainsKey(UniqueKey))
                    {
                        ParentToChildMap[ParentKey].Remove(UniqueKey);
                        if (ParentToChildMap[ParentKey].Count == 0)
                            ParentToChildMap.Remove(ParentKey);
                    }
                    ChildToParentMap.Remove(UniqueKey);

                    if (Controllers.ContainsKey(ParentKey))
                    {
                        FlydigiController ctrl = Controllers[ParentKey];
                        string UniqueControllerId = ctrl.ConnectionUniqueID;

                        ctrl.DeInitalize();
                        ctrl.Dispose();
                        Controllers.Remove(ParentKey);

                        return UniqueControllerId;
                    }
                }
            }
            return null;
        }
    }
}
