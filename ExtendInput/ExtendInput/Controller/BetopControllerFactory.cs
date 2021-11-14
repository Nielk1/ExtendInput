using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
{
    public class BetopControllerFactory : IControllerFactory
    {
        private AccesMode AccessMode;
        public BetopControllerFactory(AccesMode AccessMode)
        {
            this.AccessMode = AccessMode;
        }

        Dictionary<Guid, ControllerPair> Controllers = new Dictionary<Guid, ControllerPair>();
        public class ControllerPair
        {
            public bool bVendor;
            public bool bGamepad;
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
                            if (candidate != null && candidate.internalDevice.IsConnected && (byte)candidate.internalDevice.UserIndex == i)
                            {
                                Thread.Sleep(20);
                                candidate.internalDevice.SetVibration(new SharpDX.XInput.Vibration() { LeftMotorSpeed = 0x0100, RightMotorSpeed = 0x0600 });
                                Thread.Sleep(20);
                                candidate.internalDevice.SetVibration(new SharpDX.XInput.Vibration() { LeftMotorSpeed = 0x0500, RightMotorSpeed = 0x0300 });
                                Thread.Sleep(20);
                                candidate.internalDevice.SetVibration(new SharpDX.XInput.Vibration() { LeftMotorSpeed = 0x0200, RightMotorSpeed = 0x0400 });
                                Thread.Sleep(20);
                                candidate.internalDevice.SetVibration(new SharpDX.XInput.Vibration() { LeftMotorSpeed = 0x0000, RightMotorSpeed = 0x0000 });
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

            /*if(_device != null && _device.VendorId == 0x057E && _device.ProductId == 0x2009)
            {
                //HidSharp.OpenConfiguration config = new HidSharp.OpenConfiguration();
                //config.SetOption(HidSharp.OpenOption.Exclusive, true);
                //config.SetOption(HidSharp.OpenOption.Interruptible, false);
                //using (var stream = _device.HackDevice.Open(config))
                //{
                //    Thread.Sleep(1000 * 10);
                //}
                byte[] dat = new byte[64];
                dat[0] = 0x80;
                dat[1] = 0x06;
                bool stat = _device.WriteReport(dat);
            }*/

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
                    if (this.AccessMode != AccesMode.FullControl)
                        return null;

                    if (device.Properties["ProductName"].Contains(" A2 GAMEPAD ") // ASURA3
                     || device.Properties["ProductName"].Contains(" BD4E "))
                    {
                        // hold logo to switch modes, or allow this code below to run (they don't have a choice)
                        lock (CandidateXInputLock)
                            SawCandidateHidDeviceForXInput = DateTime.UtcNow;
                        CheckXInputData();
                    }
                    else if (device.Properties["ProductName"].Contains(" A2 RECEIVER ")) // ASURA3_DONGLE
                    {
                        // hold logo to switch modes
                    }
                    return null;
                }
            }
            else if (_deviceX != null)
            {
                if (this.AccessMode != AccesMode.FullControl)
                    return null;

                lock (CandidateXInputLock)
                {
                    CandidateXInputDevicesX[(byte)_deviceX.internalDevice.UserIndex] = new WeakReference<XInputDevice>(_deviceX);
                    CandidateXInputDevicesLastSeen[(byte)_deviceX.internalDevice.UserIndex] = DateTime.UtcNow;
                }
                CheckXInputData();
                return null;
            }

            if (device.VendorId == BetopController.VENDOR_BETOP && (device.ProductId == BetopController.PRODUCT_BETOP_ASURA3
                                                                 || device.ProductId == BetopController.PRODUCT_BETOP_ASURA3_DONGLE))
            {

                // instead of this look for Capabilities.Usage of 3
                if (device.DevicePath.Contains("&col05") || device.DevicePath.Contains("&col04"))
                {
                    {
                        string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(_device.DevicePath);
                        Guid? ContrainerID = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId);
                        if (ContrainerID.HasValue)
                            lock (Controllers)
                            {
                                if (!Controllers.ContainsKey(ContrainerID.Value))
                                    Controllers[ContrainerID.Value] = new ControllerPair();

                                if (device.DevicePath.Contains("&col05"))
                                {
                                    Controllers[ContrainerID.Value].bVendor = true;
                                    Controllers[ContrainerID.Value].Vendor = new WeakReference<HidDevice>(_device);
                                }
                                if (device.DevicePath.Contains("&col04"))
                                {
                                    Controllers[ContrainerID.Value].bGamepad = true;
                                    Controllers[ContrainerID.Value].Gamepad = new WeakReference<HidDevice>(_device);
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
                                        ctrl = new BetopController(deviceVendor, deviceGamepad);
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

                                return ctrl;
                            }
                    }
                }
            }

            return null;
        }
    }
}
