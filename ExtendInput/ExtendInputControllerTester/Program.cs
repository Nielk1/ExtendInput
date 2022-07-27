using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.WebSockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtendInput;
using ExtendInput.Controller;
using System.Threading;
using ExtendInput.Controls;
using Newtonsoft.Json.Linq;
using EmbedIO.Files;
using System.Web;
using ExtendInput.DeviceProvider;
using Newtonsoft.Json;
using Swan.Logging;
using System.Text.RegularExpressions;

namespace ExtendInputControllerTester
{
    class Program
    {
        static DeviceManager DeviceManager;
        //static object ControllersLock = new object();
        //static IController activeController = null;
        static WebSocketControllerModule websocket;

        static SemaphoreSlim ControllersLock = new SemaphoreSlim(1);
        static Dictionary<string, IController> Controllers = new Dictionary<string, IController>();
        static Dictionary<IWebSocketContext, HashSet<string>> ActiveControllers = new Dictionary<IWebSocketContext, HashSet<string>>(); // contains activations per context, so we need to subtract these from the below if we find a context is gone
        static Dictionary<string, HashSet<IWebSocketContext>> ActiveContextsPerController = new Dictionary<string, HashSet<IWebSocketContext>>();
        static Dictionary<string, int> ActiveControllerCounts = new Dictionary<string, int>(); // contains counts of activations so 0 will deactivate the controller

        static void Main(string[] args)
        {
            websocket = new WebSocketControllerModule("/socket/");
            string urlX = "http://localhost:9697/";
            //string urlX = "http://192.168.0.201:9697/";
            WebServer server = CreateWebServer(urlX);
            Swan.Logging.Logger.UnregisterLogger<ConsoleLogger>();
            server.RunAsync();

            DeviceManager = new DeviceManager(AccessMode.FullControl);
            DeviceManager.ControllerAdded += DeviceManager_ControllerAdded;
            DeviceManager.ControllerRemoved += DeviceManager_ControllerRemoved;

            websocket.Connected += Websocket_Connected;
            websocket.Disconnected += Websocket_Disconnected;
            websocket.MessageReceived += Websocket_MessageReceived;

            LoadControllers(true);

            Console.ReadKey(true);
        }

        private static void Websocket_MessageReceived(object sender, (IWebSocketContext, JObject) e)
        {
            string function = e.Item2["function"].Value<string>();
            JToken data = e.Item2["data"];
            switch(function)
            {
                case "DeviceManager::ActivateController":
                    ActivateController(e.Item1, data.Value<string>());
                    break;
                case "DeviceManager::AlternateController":
                    AlternateController(e.Item1, data["controller"].Value<string>(), data["alternate"].Value<string>());
                    break;
                case "DeviceManager::ActivateControlMode":
                    ActivateControlMode(e.Item1, data["controller"].Value<string>(), data["control"].Value<string>(), data["state"].Value<string>());
                    break;
            }
        }

        private static void Websocket_Connected(object sender, IWebSocketContext context)
        {
            StartupData(context).Wait();
            ControllersLock.Wait();
            {
                foreach(var ctrl in Controllers)
                {
                    _ = websocket.SendMessage("DeviceManager:ControllerAdded", ctrl.Value);
                }
            }
            ControllersLock.Release();
        }

        private static void Websocket_Disconnected(object sender, IWebSocketContext e)
        {
            ControllersLock.Wait();
            try
            {
                if (!ActiveControllers.ContainsKey(e))
                    return;

                foreach(string ControllerID in ActiveControllers[e].ToList())
                {
                    ActiveContextsPerController[ControllerID].Remove(e);
                    ActiveControllerCounts[ControllerID]--;
                    if (ActiveControllerCounts[ControllerID] == 0)
                    {
                        Controllers[ControllerID].ControllerStateUpdate -= Program_ControllerStateUpdate;
                        ActiveControllers[e].Remove(ControllerID);
                        Controllers[ControllerID].DeInitalize();
                        Console.WriteLine($"Removing Controller {ControllerID}");
                        ActiveControllerCounts.Remove(ControllerID);
                    }
                }

                ActiveControllers.Remove(e);
            }
            finally
            {
                ControllersLock.Release();
            }
        }

        private static void LoadControllers(bool firstload)
        {
            DeviceManager.ScanNow();
        }

        private static void DeviceManager_ControllerAdded(object sender, IController controller)
        {
            ControllersLock.Wait();
            {
                //string md5 = CreateMD5(controller.ConnectionUniqueID);
                ////Write_ControllerAdded(controller);
                controller.ControllerMetadataUpdate += DeviceManager_ControllerMetadataUpdate;
                //Controllers[md5] = controller;
                Controllers[controller.ConnectionUniqueID] = controller;
            }
            _ = websocket.SendMessage("DeviceManager:ControllerAdded", controller);
            ControllersLock.Release();
        }

        private static void DeviceManager_ControllerMetadataUpdate(IController sender)
        {
            _ = websocket.SendMessage("DeviceManager:ControllerMetadataUpdate", sender);
        }

        private static void DeviceManager_ControllerRemoved(object sender, string UniqueKey)
        {
            ControllersLock.Wait();
            {
                if (Controllers.ContainsKey(UniqueKey))
                    Controllers[UniqueKey].Dispose(); // hack solution until this is added to the DeviceManager
                Controllers.Remove(UniqueKey);
                _ = websocket.SendMessage("DeviceManager:ControllerRemoved", UniqueKey);

                if (ActiveContextsPerController.ContainsKey(UniqueKey))
                {
                    HashSet<IWebSocketContext> contextsWithController = ActiveContextsPerController[UniqueKey];
                    ActiveControllerCounts.Remove(UniqueKey);
                    ActiveContextsPerController.Remove(UniqueKey);

                    foreach (IWebSocketContext context in contextsWithController)
                    {
                        ActiveControllers[context].Remove(UniqueKey);
                        _ = websocket.SendMessage("DeviceManager:ActiveControllers", ActiveControllers[context], new IWebSocketContext[] { context });
                    }
                }
            }
            ControllersLock.Release();
        }

        /*public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }*/

        private static WebServer CreateWebServer(string url)
        {
            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                //.WithModule(new ActionModule("/", HttpVerbs.Post, SawVideo));
                //.WithStaticFolder("/", "index.html", true)
            .WithModule(websocket)

                .WithStaticFolder("/images/controller", "../../images/controller", true)
                .WithStaticFolder("/images/icon", "../../images/icon", true)
                .WithStaticFolder("/3d", "../../3d", true)
                ////.WithModule(new FileModule("/images/controller", provider))
                //.WithModule(new ActionModule("/poll_controller", HttpVerbs.Get, PollController))
                //.WithModule(new ActionModule("/poll_other", HttpVerbs.Get, PollOther))
                //.WithModule(new ActionModule("/startup_data", HttpVerbs.Get, StartupData))
                .WithModule(new ActionModule("/manual_device", HttpVerbs.Post, ManualDevice))
                //.WithModule(new ActionModule("/activate_controller", HttpVerbs.Post, ActivateController))
                //.WithModule(new ActionModule("/alternate_controller", HttpVerbs.Post, AlternateController))
                //.WithModule(new ActionModule("/activate_control_mode", HttpVerbs.Post, ActivateControlMode))
                .WithModule(new ActionModule("/", HttpVerbs.Get, ctx => ctx.SendStringAsync(File.ReadAllText("../../index.html"), "text/html", Encoding.UTF8)));
                ////.WithStaticFolder("/images/controller/","../images/controller/",true, new FileModule(,)
            //.WithModule(new ActionModule("/", HttpVerbs.Get, DeviceList))
            //.WithModule(new ActionModule("/", HttpVerbs.Get, DeviceList))

            // Listen for state changes.
            //server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }

        //static ControlCollection State = null;
        /*private static async Task PollController(IHttpContext context)
        {
            await ControllersLock.WaitAsync();
            try
            {
                if (activeController == null) return;
                if (State == null) return;
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

                JObject controls = new JObject();
                foreach (string key in State.Keys)
                {
                    JObject obj = new JObject();
                    string TypeCodeString = State[key]?.GetType()?.ToString();
                    if (!string.IsNullOrWhiteSpace(TypeCodeString))
                        TypeCodeString = Regex.Replace(TypeCodeString, @"`[^\[\]]+\[", "[");
                    obj["Type"] = TypeCodeString;
                    obj["Data"] = State[key] != null ? JObject.FromObject(State[key], serializer) : null;
                    controls[key] = obj;
                }
                JObject output = new JObject()
                {
                    ["controls"] = controls,
                    ["subtype"] = activeController.ControllerTypeCode?.First(),
                };
                //await context.SendDataAsync(output);
                await context.SendStringAsync(output.ToString(Newtonsoft.Json.Formatting.None), "application/json", Encoding.UTF8);
            }
            finally
            {
                ControllersLock.Release();
            }
        }*/


        private static async Task StartupData(IWebSocketContext context)
        {
            if (DeviceManager == null)
                return;

            Dictionary<string, string> ControllerImages = new Dictionary<string, string>();
            foreach (string pattern in new string[] { "*.png", "*.jpg", "*.jpeg" })
                foreach (string ControllerImage in Directory.EnumerateFiles(@"..\..\images\controller", pattern, SearchOption.TopDirectoryOnly))
                    if (!ControllerImages.ContainsKey(Path.GetFileNameWithoutExtension(ControllerImage)))
                        ControllerImages[Path.GetFileNameWithoutExtension(ControllerImage)] = Path.GetFileName(ControllerImage);

            Dictionary<string, string> IconImages = new Dictionary<string, string>();
            foreach (string pattern in new string[] { "*.png", "*.jpg", "*.jpeg" })
                foreach (string IconImage in Directory.EnumerateFiles(@"..\..\images\icon", pattern, SearchOption.TopDirectoryOnly))
                    if (!IconImages.ContainsKey(Path.GetFileNameWithoutExtension(IconImage)))
                        IconImages[Path.GetFileNameWithoutExtension(IconImage)] = Path.GetFileName(IconImage);

            Dictionary<string, string> ManualDevices = new Dictionary<string, string>();
            foreach (IDeviceProvider provider in DeviceManager.GetManualDeviceProviders())
            {
                string Name = (Attribute.GetCustomAttribute(provider.GetType(), typeof(DeviceProviderAttribute)) as DeviceProviderAttribute)?.TypeString;
                string Code = (Attribute.GetCustomAttribute(provider.GetType(), typeof(DeviceProviderAttribute)) as DeviceProviderAttribute)?.TypeCode;
                ManualDevices[Code] = Name;
            }

            await websocket.SendMessage("DeviceManager:StartupData", new
            {
                ControllerImages = ControllerImages,
                IconImages = IconImages,
                ManualDevices = ManualDevices,
            }, new IWebSocketContext[] { context });
        }


        private static async Task PollOther(IHttpContext context)
        {
            if (DeviceManager == null)
                return;

            await ControllersLock.WaitAsync();
            try
            {
                Dictionary<string, string> ControllerImages = new Dictionary<string, string>();
                Dictionary<string, string> IconImages = new Dictionary<string, string>();
                foreach (string ControllerID in Controllers.Keys)
                {
                    foreach (string ControllerTypeCode in Controllers[ControllerID].ControllerTypeCode)
                    {
                        if (!ControllerImages.ContainsKey(ControllerTypeCode))
                        {
                            string FileName = Path.Combine(@"..\..\images\controller", ControllerTypeCode);
                            if (File.Exists(FileName + ".png")) { ControllerImages[ControllerTypeCode] = ControllerTypeCode + ".png"; break; }
                            if (File.Exists(FileName + ".jpg")) { ControllerImages[ControllerTypeCode] = ControllerTypeCode + ".jpg"; break; }
                            if (File.Exists(FileName + ".jpeg")) { ControllerImages[ControllerTypeCode] = ControllerTypeCode + ".jpeg"; break; }
                        }
                    }
                    foreach (string ControllerTypeCode in Controllers[ControllerID].ControllerTypeCode)
                    {
                        if (!IconImages.ContainsKey(ControllerTypeCode))
                        {
                            string FileName = Path.Combine(@"..\..\images\icon", ControllerTypeCode);
                            if (File.Exists(FileName + ".png")) { IconImages[ControllerTypeCode] = ControllerTypeCode + ".png"; break; }
                            if (File.Exists(FileName + ".jpg")) { IconImages[ControllerTypeCode] = ControllerTypeCode + ".jpg"; break; }
                            if (File.Exists(FileName + ".jpeg")) { IconImages[ControllerTypeCode] = ControllerTypeCode + ".jpeg"; break; }
                        }
                    }
                    foreach (string ConnectionTypeCode in Controllers[ControllerID].ConnectionTypeCode)
                    {
                        if (!IconImages.ContainsKey(ConnectionTypeCode))
                        {
                            string FileName = Path.Combine(@"..\..\images\icon", ConnectionTypeCode);
                            if (File.Exists(FileName + ".png")) { IconImages[ConnectionTypeCode] = ConnectionTypeCode + ".png"; break; }
                            if (File.Exists(FileName + ".jpg")) { IconImages[ConnectionTypeCode] = ConnectionTypeCode + ".jpg"; break; }
                            if (File.Exists(FileName + ".jpeg")) { IconImages[ConnectionTypeCode] = ConnectionTypeCode + ".jpeg"; break; }
                        }
                    }
                }

                Dictionary<string, string> ManualDevices = new Dictionary<string, string>();
                foreach (IDeviceProvider provider in DeviceManager.GetManualDeviceProviders())
                {
                    string Name = (Attribute.GetCustomAttribute(provider.GetType(), typeof(DeviceProviderAttribute)) as DeviceProviderAttribute)?.TypeString;
                    string Code = (Attribute.GetCustomAttribute(provider.GetType(), typeof(DeviceProviderAttribute)) as DeviceProviderAttribute)?.TypeCode;
                    //ToolStripMenuItem itm = new ToolStripMenuItem(Name ?? provider.ToString(), null, LoadManualDevice);
                    //itm.DropDown.AutoClose = false;
                    //itm.Tag = new ManualSelectionMetadata() { ParentMenuItem = itm, Provider = provider, DontClose = true, };
                    //tsmiManualControllers.DropDownItems.Add(itm);
                    ManualDevices[Code] = Name;
                }

                await context.SendDataAsync(new {
                    Controllers = Controllers,
                    ControllerImages = ControllerImages,
                    IconImages = IconImages,
                    ManualDevices = ManualDevices,
                });
            }
            finally
            {
                ControllersLock.Release();
            }
        }

        private static async Task ManualDevice(IHttpContext context)
        {
            //await ControllersLock.WaitAsync();
            try
            {
                string raw = await context.GetRequestBodyAsStringAsync();
                ManualData data = JsonConvert.DeserializeObject<ManualData>(raw);

                foreach (IDeviceProvider provider in DeviceManager.GetManualDeviceProviders())
                {
                    string Name = (Attribute.GetCustomAttribute(provider.GetType(), typeof(DeviceProviderAttribute)) as DeviceProviderAttribute)?.TypeString;
                    string Code = (Attribute.GetCustomAttribute(provider.GetType(), typeof(DeviceProviderAttribute)) as DeviceProviderAttribute)?.TypeCode;

                    if (Code == data.Code)
                    {
                        IDeviceManualTriggerContext retVal = provider.ManualTrigger(data.Data);
                        await context.SendDataAsync(new ManualDataOut() { Code = Code, Data = retVal });
                        return;
                    }
                }
            }
            finally
            {
                //ControllersLock.Release();
            }
        }

        class ManualData
        {
            public string Code { get; set; }
            public DeviceManualTriggerContextOption Data { get; set; }
        }

        class ManualDataOut
        {
            public string Code { get; set; }
            public IDeviceManualTriggerContext Data { get; set; }
        }

        private static async Task AlternateController(IWebSocketContext context, string ControllerID, string AlternateID)
        {
            await ControllersLock.WaitAsync();
            try
            {
                if (Controllers.ContainsKey(ControllerID))
                {
                    Controllers[ControllerID].SetActiveAlternateController(AlternateID);
                    _ = websocket.SendMessage("DeviceManager:ControllerMetadataUpdate", Controllers[ControllerID], new IWebSocketContext[] { context });
                    return;
                }
            }
            finally
            {
                ControllersLock.Release();
            }
        }

        private static async Task ActivateController(IWebSocketContext context, string ControllerID)
        {
            await ControllersLock.WaitAsync();
            try
            {
                if (!Controllers.ContainsKey(ControllerID))
                    return;

                if(!ActiveControllers.ContainsKey(context))
                    ActiveControllers[context] = new HashSet<string>();

                if (ActiveControllers[context].Contains(ControllerID))
                {
                    ActiveContextsPerController[ControllerID].Remove(context);
                    if (ActiveContextsPerController[ControllerID].Count == 0)
                        ActiveContextsPerController.Remove(ControllerID);
                    ActiveControllerCounts[ControllerID]--; // it is impossible for this key to not be present unless we're fucked up really badly
                    if (ActiveControllerCounts[ControllerID] == 0)
                    {
                        Controllers[ControllerID].ControllerStateUpdate -= Program_ControllerStateUpdate;
                        ActiveControllers[context].Remove(ControllerID);
                        Controllers[ControllerID].DeInitalize();
                        Console.WriteLine($"Removing Controller {ControllerID}");
                        ActiveControllerCounts.Remove(ControllerID);
                    }
                }
                else
                {
                    if (!ActiveContextsPerController.ContainsKey(ControllerID))
                        ActiveContextsPerController[ControllerID] = new HashSet<IWebSocketContext>();
                    ActiveContextsPerController[ControllerID].Add(context);
                    ActiveControllers[context].Add(ControllerID);
                    if (!ActiveControllerCounts.ContainsKey(ControllerID))
                    {
                        ActiveControllerCounts[ControllerID] = 1;
                        Controllers[ControllerID].ControllerStateUpdate += Program_ControllerStateUpdate;
                        Controllers[ControllerID].Initalize();
                    }
                    else
                    {
                        ActiveControllerCounts[ControllerID]++;
                    }
                }

                _ = websocket.SendMessage("DeviceManager:ActiveControllers", ActiveControllers[context], new IWebSocketContext[] { context });
            }
            finally
            {
                ControllersLock.Release();
            }
        }

        private static void Program_ControllerStateUpdate(IController sender, ControlCollection controls)
        {
            //State = (ControlCollection)controls.Clone();
            //_ = websocket.SendMessage("DeviceManager:ControllerState", controls);

            ControlCollection State = (ControlCollection)controls.Clone();

            ControllersLock.Wait();
            try
            {
                if(!ActiveContextsPerController.ContainsKey(sender.ConnectionUniqueID))
                {
                    // TODO: this should never happen, so probably should disable the controller and clean up any mess
                    {
                        //ActiveContextsPerController[sender.ConnectionUniqueID].Remove(context);
                        ActiveControllerCounts[sender.ConnectionUniqueID]--;
                        if (ActiveControllerCounts[sender.ConnectionUniqueID] == 0)
                        {
                            Controllers[sender.ConnectionUniqueID].ControllerStateUpdate -= Program_ControllerStateUpdate;
                            //ActiveControllers[context].Remove(sender.ConnectionUniqueID);
                            Controllers[sender.ConnectionUniqueID].DeInitalize();
                            Console.WriteLine($"Removing Controller {sender.ConnectionUniqueID}");
                            ActiveControllerCounts.Remove(sender.ConnectionUniqueID);
                        }
                    }
                    return;
                }

                //if (activeController == null) return;
                if (State == null) return;
                JsonSerializer serializer = new JsonSerializer();
                serializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

                JObject controlsJ = new JObject();
                foreach (string key in State.Keys)
                {
                    {
                        JObject obj = new JObject();
                        string TypeCodeString = State[key]?.GetType()?.ToString();
                        if (!string.IsNullOrWhiteSpace(TypeCodeString))
                            TypeCodeString = Regex.Replace(TypeCodeString, @"`[^\[\]]+\[", "[");
                        obj["Type"] = TypeCodeString;
                        obj["Data"] = State[key] != null ? JObject.FromObject(State[key], serializer) : null;
                        controlsJ[key] = obj;
                    }

                    var ConverterList = ControlConverter.Instance.GetConvertList(State[key]?.GetType());
                    if (ConverterList != null)
                    {
                        foreach(var ConvertedType in ConverterList)
                        {
                            if (!ConvertedType.IsAssignableFrom(State[key]?.GetType()))
                            {
                                JObject obj = new JObject();

                                var mi = typeof(ControlConverter).GetMethod("Convert");
                                var fooRef = mi.MakeGenericMethod(State[key].GetType(), ConvertedType);
                                dynamic Converted = fooRef.Invoke(ControlConverter.Instance, new object[] { State[key] });
                                if (Converted != null)
                                {
                                    string TypeCodeString = ConvertedType.ToString();
                                    if (!string.IsNullOrWhiteSpace(TypeCodeString))
                                        TypeCodeString = Regex.Replace(TypeCodeString, @"`[^\[\]]+\[", "[");
                                    obj["Type"] = TypeCodeString;
                                    obj["Data"] = JObject.FromObject(Converted, serializer);
                                    while (TypeCodeString.Contains('.'))
                                        TypeCodeString = TypeCodeString.Replace('.', '-');
                                    controlsJ[key + "-" + TypeCodeString] = obj;
                                }
                            }
                        }
                    }
                }
                JObject output = new JObject()
                {
                    ["State"] = controlsJ,
                    ["ControllerTypeCode"] = sender.ControllerTypeCode?.First(),
                    ["ConnectionUniqueID"] = sender.ConnectionUniqueID,
                };
                //await context.SendDataAsync(output);
                //await context.SendStringAsync(output.ToString(Newtonsoft.Json.Formatting.None), "application/json", Encoding.UTF8);
                _ = websocket.SendMessage("DeviceManager:ControllerState", output, ActiveContextsPerController[sender.ConnectionUniqueID]);
            }
            finally
            {
                ControllersLock.Release();
            }
        }

        private static async Task ActivateControlMode(IWebSocketContext context, string ControllerID, string control, string state)
        {
            await ControllersLock.WaitAsync();
            try
            {
                if (Controllers.ContainsKey(ControllerID))
                {
                    IController controller = Controllers[ControllerID];
                    Thread tmp = new Thread(() =>
                    {
                        controller.LockState();
                        try
                        {
                            bool retVal = controller.SetControlState(control, state);
                        }
                        finally
                        {
                            controller.UnlockState(true);
                        }
                        _ = websocket.SendMessage("DeviceManager:ControllerMetadataUpdate", Controllers[ControllerID], new IWebSocketContext[] { context });
                    });
                    tmp.Start();
                    return;
                }
            }
            finally
            {
                ControllersLock.Release();
            }
        }
    }

    class WebSocketControllerModule : WebSocketModule
    {
        public event EventHandler<IWebSocketContext> Connected;
        public event EventHandler<IWebSocketContext> Disconnected;
        public event EventHandler<(IWebSocketContext, JObject)> MessageReceived;

        public WebSocketControllerModule(string urlPath) : base(urlPath, true)
        {
            // placeholder
        }

        protected async override Task OnMessageReceivedAsync(
            IWebSocketContext context,
            byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
        {
            //return SendToOthersAsync(context, Encoding.GetString(rxBuffer));
            //return Task.Run(() => Thread.Sleep(10));
            MessageReceived?.Invoke(this, (context, JObject.Parse(Encoding.GetString(rxBuffer))));
        }

        protected async override Task OnClientConnectedAsync(IWebSocketContext context)
        {
            //return Task.WhenAll(
            //    SendAsync(context, "Welcome to the chat room!"),
            //    SendToOthersAsync(context, "Someone joined the chat room."));
            Connected?.Invoke(this, context);
        }

        protected async override Task OnClientDisconnectedAsync(IWebSocketContext context)
        {
            //return SendToOthersAsync(context, "Someone left the chat room.");
            Disconnected?.Invoke(this, context);
        }

        /*private Task SendToOthersAsync(IWebSocketContext context, string payload)
        {
            return BroadcastAsync(payload, c => c != context);
        }*/

        SemaphoreSlim MessageSendLock = new SemaphoreSlim(1);

        public async Task SendMessage(string function, object payload, IEnumerable<IWebSocketContext> contexts = null)
        {
            await MessageSendLock.WaitAsync();
            try
            {
                await BroadcastAsync(new JObject()
                {
                    { "function", function },
                    { "data", payload is string              ? (JToken)new JValue(payload) :
                              payload is JObject             ? (JToken)payload :
                              payload is IEnumerable<object> ? (JToken)JArray.FromObject(payload) : (JToken)JObject.FromObject(payload) },
                }.ToString(), c => contexts?.Contains(c) ?? true);
                //Console.WriteLine($"Sending {function}");
            }
            finally
            {
                MessageSendLock.Release();
            }
        }
    }
}