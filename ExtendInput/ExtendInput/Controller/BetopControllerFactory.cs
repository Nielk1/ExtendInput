using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ExtendInput.Controller
{
    public class BetopControllerFactory : IControllerFactory
    {
        Dictionary<Guid, WeakReference<BetopController>> Controllers = new Dictionary<Guid, WeakReference<BetopController>>();


        public Dictionary<string, dynamic>[] DeviceWhitelist => new Dictionary<string, dynamic>[]
        {
            new Dictionary<string, dynamic>(){ { "VID", BetopController.VENDOR_BETOP }, { "PID", BetopController.PRODUCT_BETOP_ASURA3 } },
            new Dictionary<string, dynamic>(){ { "VID", 0x045E }, { "PID", 0x028E } },
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
                    if (device.Properties["ProductName"].Contains(" A2 GAMEPAD ")
                     || device.Properties["ProductName"].Contains(" BD4E "))
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
                lock (CandidateXInputLock)
                {
                    CandidateXInputDevicesX[(byte)_deviceX.internalDevice.UserIndex] = new WeakReference<XInputDevice>(_deviceX);
                    CandidateXInputDevicesLastSeen[(byte)_deviceX.internalDevice.UserIndex] = DateTime.UtcNow;
                }
                CheckXInputData();
                return null;
            }

            if (device.VendorId == BetopController.VENDOR_BETOP && device.ProductId == BetopController.PRODUCT_BETOP_ASURA3)
            {

                // instead of this look for Capabilities.Usage of 3
                if (device.DevicePath.Contains("&col05") || device.DevicePath.Contains("&col04"))
                {
                    BetopController ctrl = null;
                    {
                        string deviceInstanceId = DevPKey.PnpDevicePropertyAPI.devicePathToInstanceId(_device.DevicePath);
                        Guid? ContrainerID = DevPKey.PnpDevicePropertyAPI.GetDeviceContainerId(deviceInstanceId);
                        if (ContrainerID.HasValue)
                            lock (Controllers)
                            {
                                bool newController = false;
                                if (!Controllers.ContainsKey(ContrainerID.Value) || !Controllers[ContrainerID.Value].TryGetTarget(out ctrl))
                                {
                                    ctrl = new BetopController(_device);
                                    Controllers.Add(ContrainerID.Value, new WeakReference<BetopController>(ctrl));
                                    newController = true;
                                }

                                Console.WriteLine(ContrainerID.Value.ToString());

                                // clear any dead refs
                                foreach (Guid key in Controllers.Keys.ToList()) // ToList to clone the key list so we can modify it
                                    if (!Controllers[key].TryGetTarget(out _))
                                        Controllers.Remove(key);

                                if (newController)
                                {
                                    return ctrl;
                                }
                                else
                                {
                                    ctrl.AddDevice(_device);
                                }
                            }
                    }
                }
            }

            return null;
        }
    }
}
