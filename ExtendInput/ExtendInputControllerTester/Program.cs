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
        static SemaphoreSlim ControllersLock = new SemaphoreSlim(1);
        static Dictionary<string, IController> Controllers = new Dictionary<string, IController>();
        static IController activeController = null;
        static void Main(string[] args)
        {
            string urlX = "http://localhost:9697/";
            //string urlX = "http://192.168.0.201:9697/";
            WebServer server = CreateWebServer(urlX);
            Swan.Logging.Logger.UnregisterLogger<ConsoleLogger>();
            server.RunAsync();

            DeviceManager = new DeviceManager(AccessMode.FullControl);
            DeviceManager.ControllerAdded += DeviceManager_ControllerAdded;
            DeviceManager.ControllerRemoved += DeviceManager_ControllerRemoved;

            LoadControllers(true);

            Console.ReadKey(true);
        }

        private static void LoadControllers(bool firstload)
        {
            DeviceManager.ScanNow();
        }

        private static void DeviceManager_ControllerAdded(object sender, IController controller)
        {
            ControllersLock.Wait();
            {
                string md5 = CreateMD5(controller.ConnectionUniqueID);
                //Write_ControllerAdded(controller);
                controller.ControllerMetadataUpdate += DeviceManager_ControllerMetadataUpdate;
                Controllers[md5] = controller;
            }
            ControllersLock.Release();
        }

        private static void DeviceManager_ControllerMetadataUpdate(IController sender)
        {

        }

        private static void DeviceManager_ControllerRemoved(object sender, string UniqueKey)
        {
            ControllersLock.Wait();
            {
                string md5 = CreateMD5(UniqueKey);
                if (Controllers.ContainsKey(md5))
                    Controllers[md5].Dispose(); // hack solution until this is added to the DeviceManager
                Controllers.Remove(md5);
            }
            ControllersLock.Release();
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", string.Empty);
            }
        }

        private static WebServer CreateWebServer(string url)
        {
            var server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                //.WithModule(new ActionModule("/", HttpVerbs.Post, SawVideo));
                //.WithStaticFolder("/", "index.html", true)
                .WithStaticFolder("/images/controller", "../../images/controller", true)
                .WithStaticFolder("/images/icon", "../../images/icon", true)
                .WithStaticFolder("/3d", "../../3d", true)
                //.WithModule(new FileModule("/images/controller", provider))
                .WithModule(new ActionModule("/poll_controller", HttpVerbs.Get, PollController))
                .WithModule(new ActionModule("/poll_other", HttpVerbs.Get, PollOther))
                .WithModule(new ActionModule("/manual_device", HttpVerbs.Post, ManualDevice))
                .WithModule(new ActionModule("/activate_controller", HttpVerbs.Post, ActivateController))
                .WithModule(new ActionModule("/alternate_controller", HttpVerbs.Post, AlternateController))
                .WithModule(new ActionModule("/activate_control_mode", HttpVerbs.Post, ActivateControlMode))
                .WithModule(new ActionModule("/", HttpVerbs.Get, ctx => ctx.SendStringAsync(File.ReadAllText("../../index.html"), "text/html", Encoding.UTF8)));
                //.WithStaticFolder("/images/controller/","../images/controller/",true, new FileModule(,)
            //.WithModule(new ActionModule("/", HttpVerbs.Get, DeviceList))
            //.WithModule(new ActionModule("/", HttpVerbs.Get, DeviceList))
            //.WithModule(new WebSocketControllerModule("/terminal/"));

            // Listen for state changes.
            //server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }

        static ControlCollection State = null;
        private static async Task PollController(IHttpContext context)
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

        private static async Task AlternateController(IHttpContext context)
        {
            await ControllersLock.WaitAsync();
            try
            {
                string raw = await context.GetRequestBodyAsStringAsync();
                var data = HttpUtility.ParseQueryString(raw);

                if (Controllers.ContainsKey(data["controller"]))
                {
                    Controllers[data["controller"]].SetActiveAlternateController(data["alternate"]);
                    await context.SendDataAsync(true);
                    return;
                }
                await context.SendDataAsync(false);
            }
            finally
            {
                ControllersLock.Release();
            }
        }

        private static async Task ActivateController(IHttpContext context)
        {
            await ControllersLock.WaitAsync();
            try
            {
                if (activeController != null)
                    activeController.GetState().ControllerStateUpdate -= Program_ControllerStateUpdate;
                activeController?.DeInitalize();
                activeController = null;
                State = null;

                string ControllerID = await context.GetRequestBodyAsStringAsync();

                if (!string.IsNullOrWhiteSpace(ControllerID) && Controllers.ContainsKey(ControllerID))
                {
                    activeController = Controllers[ControllerID];
                    await context.SendDataAsync(true);
                    activeController.Initalize();
                    activeController.GetState().ControllerStateUpdate += Program_ControllerStateUpdate;
                    return;
                }
                await context.SendDataAsync(false);
            }
            finally
            {
                ControllersLock.Release();
            }
        }

        private static void Program_ControllerStateUpdate(ControlCollection controls)
        {
            State = (ControlCollection)controls.Clone();
        }

        private static async Task ActivateControlMode(IHttpContext context)
        {
            await ControllersLock.WaitAsync();
            try
            {
                if (activeController != null)
                {
                    string raw = await context.GetRequestBodyAsStringAsync();
                    var data = HttpUtility.ParseQueryString(raw);

                    //bool retVal = await Task.Run(() => activeController.SetControlState(data["id"], data["state"]));
                    //bool retVal = activeController.SetControlState(data["id"], data["state"]);
                    bool retVal = false;
                    Thread tmp = new Thread(() =>
                    {
                        retVal = activeController.SetControlState(data["id"], data["state"]);
                    });
                    tmp.Start();
                    while (tmp.IsAlive)
                        await Task.Delay(100);

                    await context.SendDataAsync(retVal);
                }
                else
                {
                    await context.SendDataAsync(false);
                }
            }
            finally
            {
                ControllersLock.Release();
            }
        }
    }

    /*class WebSocketControllerModule : WebSocketModule
    {
        public WebSocketControllerModule(string urlPath) : base(urlPath, true)
        {
            // placeholder
        }

        protected override Task OnMessageReceivedAsync(
            IWebSocketContext context,
            byte[] rxBuffer,
            IWebSocketReceiveResult rxResult)
            => SendToOthersAsync(context, Encoding.GetString(rxBuffer));

        protected override Task OnClientConnectedAsync(IWebSocketContext context)
            => Task.WhenAll(
                SendAsync(context, "Welcome to the chat room!"),
                SendToOthersAsync(context, "Someone joined the chat room."));

        protected override Task OnClientDisconnectedAsync(IWebSocketContext context)
            => SendToOthersAsync(context, "Someone left the chat room.");

        private Task SendToOthersAsync(IWebSocketContext context, string payload)
            => BroadcastAsync(payload, c => c != context);
    }*/
}