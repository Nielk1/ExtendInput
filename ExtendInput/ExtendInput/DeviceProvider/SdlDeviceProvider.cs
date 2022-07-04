using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SDL2;

namespace ExtendInput.DeviceProvider
{
    [DeviceProvider(TypeString = "SDL", TypeCode = "SDL", SupportsAutomaticDetection = true, SupportsManualyQuery = true, RequiresManualConfiguration = false)]
    public class SdlDeviceProvider : IDeviceProvider
    {
        public event DeviceAddedEventHandler DeviceAdded;
        public event DeviceRemovedEventHandler DeviceRemoved;

        object lock_device_list = new object();

        bool AbortStatusThread = false;
        Thread CheckControllerStatusThread;

        Dictionary<int, IDevice> GameControllers = new Dictionary<int, IDevice>();

        bool Active = false;










        public SdlDeviceProvider()
        {
            try
            {
                //SDL.SDL_SetHint(SDL.SDL_HINT_ACCELEROMETER_AS_JOYSTICK, "0");
                //SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_JOY_CONS, "1");
                //SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS4_RUMBLE, "1");
                //SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_HIDAPI_PS5_RUMBLE, "1");
                //SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ROG_CHAKRAM, "1");
                //SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
                //SDL.SDL_SetHint(SDL.SDL_HINT_LINUX_JOYSTICK_DEADZONES, "1");

                SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_THREAD, "1");
                SDL.SDL_SetHint(SDL.SDL_HINT_MAC_BACKGROUND_APP, "1");
                //SDL.SDL_SetHint(SDL.SDL_HINT_GAMECONTROLLERCONFIG, "1");

                //if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_GAMECONTROLLER) < 0)
                if (SDL.SDL_Init(SDL.SDL_INIT_JOYSTICK | SDL.SDL_INIT_GAMECONTROLLER) < 0)
                {
                    //SDL.SDL_LogError(SDL.SDL_LOG_CATEGORY_APPLICATION, "Couldn't initialize SDL: %s\n", SDL.SDL_GetError());
                    //return 1;
                    return;
                }

                SDL.SDL_GameControllerAddMappingsFromFile("gamecontrollerdb.txt");

                Active = true;
            }
            catch (System.DllNotFoundException ex)
            {

            }

            CheckControllerStatusThread = new Thread(() =>
                {
                    for (; ; )
                    {
                        SDL.SDL_Event evt;
                        if (SDL.SDL_PollEvent(out evt) != 0)
                        {
                            switch(evt.type)
                            {
                                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                                    if (SDL.SDL_IsGameController(evt.cdevice.which) == SDL.SDL_bool.SDL_TRUE)
                                    {
                                        IntPtr device_handle = SDL.SDL_JoystickOpen(evt.cdevice.which);
                                        if (device_handle != IntPtr.Zero)
                                        {
                                            int instance_id = SDL.SDL_JoystickInstanceID(device_handle);
                                            SDL.SDL_JoystickClose(device_handle);
                                            ControllerAdded(instance_id);
                                            //Console.WriteLine($"Device Added {evt.cdevice.which}");
                                            //ScanNow();
                                        }
                                    }
                                    break;
                                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                                    ControllerRemoved(evt.cdevice.which);
                                    //Console.WriteLine($"Device Removed {evt.cdevice.which}");
                                    //ScanNow();
                                    break;
                            }
                        }
                        //Thread.Sleep(1000);
                        Thread.Sleep(1);
                        //ScanNow();
                        if (AbortStatusThread)
                            return;
                    }
                });
            CheckControllerStatusThread.Start();
        }

        public void Dispose()
        {
            AbortStatusThread = true;
        }

        public IDeviceManualTriggerContext ManualTrigger(DeviceManualTriggerContextOption Option)
        {
            throw new NotImplementedException();
        }

        public void RegisterWhitelist(Dictionary<string, dynamic>[] deviceWhitelist)
        { }

        private void ControllerAdded(int instance_id)
        {
            lock (lock_device_list)
            {
                if (!GameControllers.ContainsKey(instance_id))
                {
                    DeviceAddedEventHandler threadSafeEventHandler = DeviceAdded;
                    GameControllers[instance_id] = new SdlDevice(instance_id);
                    threadSafeEventHandler?.Invoke(this, GameControllers[instance_id]);
                }
            }
        }

        private void ControllerRemoved(int instance_id)
        {
            lock (lock_device_list)
            {
                if (GameControllers.ContainsKey(instance_id))
                {
                    DeviceRemovedEventHandler threadSafeEventHandler = DeviceRemoved;
                    threadSafeEventHandler?.Invoke(this, $"SDL2:{instance_id}");
                    GameControllers.Remove(instance_id);
                }
            }
        }

        public void ScanNow()
        {
            if (!Active)
                return;

            //lock (lock_device_list)
            {
                //Console.WriteLine("---------------------");
                try
                {
                    HashSet<string> SeenActive = new HashSet<string>();
                    HashSet<string> SeenIDs = new HashSet<string>();
                    for (byte device_index = 0; device_index < SDL.SDL_NumJoysticks(); device_index++)
                    {
                        Guid controllerGUID = SDL.SDL_JoystickGetDeviceGUID(device_index);

                        if (SDL.SDL_IsGameController(device_index) == SDL.SDL_bool.SDL_TRUE)
                        {
                            //string name = SDL.SDL_GameControllerNameForIndex(i);
                            //string path = SDL_GameControllerPathForIndex(i)?.ToLowerInvariant();
                            //SDL.SDL_GameControllerType controllerType = SDL.SDL_GameControllerTypeForIndex(i);
                            //SDL.SDL_JoystickType joystickType = SDL.SDL_JoystickGetDeviceType(i);
                            IntPtr device_handle = SDL.SDL_JoystickOpen(device_index);
                            if (device_handle != IntPtr.Zero)
                            {
                                int instance_id = SDL.SDL_JoystickInstanceID(device_handle);
                                SDL.SDL_JoystickClose(device_handle);
                                ControllerAdded(instance_id);

                                //string path = SDL_GameControllerPath(handle).ToLowerInvariant();

                                //int ControllerID = SDL.SDL_JoystickGetDeviceInstanceID(device_index);
                                //int ControllerID2 = SDL.SDL_JoystickInstanceID(handle);
                                //Console.WriteLine($"SDL joystick {path}:{controllerGUID}:{ControllerID}:{ControllerID2}");

                                //// skip repeated devices with the same path, such as DI versions of HID controllers, where SDL seems to put HID first
                                ////if (SeenIDs.Contains(path))
                                ////    continue;
                                ////SeenIDs.Add(path);

                                //string UniqueKey = $"{path}:{controllerGUID}";
                                //bool attached = SDL.SDL_GameControllerGetAttached(handle) == SDL.SDL_bool.SDL_TRUE;
                                //if (attached)
                                //{
                                //    SeenActive.Add(UniqueKey);
                                //    if (GameControllers.ContainsKey(UniqueKey))
                                //    {
                                //        //Console.WriteLine($"SDL controller already exists {UniqueKey}");
                                //        // TODO make sure to deal with swapping out a new handle (device index?) if our GUID is differnt, as the DI one might have gotten in first if we're HID based
                                //    }
                                //    else
                                //    {
                                //        GameControllers[UniqueKey] = new SdlDevice(UniqueKey, handle);

                                //        DeviceAddedEventHandler threadSafeEventHandler = DeviceAdded;
                                //        threadSafeEventHandler?.Invoke(this, GameControllers[UniqueKey]);

                                //        //Console.WriteLine($"SDL controller Added {UniqueKey}");
                                //    }
                                //}
                                //else
                                //{
                                //    SDL.SDL_GameControllerClose(handle);
                                //}
                                //string serial = SDL.SDL_GameControllerGetSerial(handle);
                                //Console.WriteLine($"SDL controller at index {i}, {path}, {serial}, {attached}, {controllerGUID}, {joystickType}, {controllerType}, {name}");
                            }
                        }
                        else
                        {
                            //string name = SDL.SDL_JoystickNameForIndex(i);
                            //string path = SDL.SDL_JoystickPathForIndex(i);
                            //Console.WriteLine($"SDL joystick at index {i}, {name}");
                        }
                    }

                    /*foreach (string UniqueId in GameControllers.Keys.ToList())
                    {
                        if (!SeenActive.Contains(UniqueId))
                        {
                            DeviceRemovedEventHandler threadSafeEventHandler = DeviceRemoved;
                            //threadSafeEventHandler?.Invoke(this, GameControllers[UniqueId].UniqueKey);
                            threadSafeEventHandler?.Invoke(this, UniqueId);
                            //GameControllers[UniqueId] = null;
                            GameControllers.Remove(UniqueId);

                            //Console.WriteLine($"SDL controller Removed {UniqueId}");
                        }
                    }*/
                }
                catch { }
            }
        }
    }
}
