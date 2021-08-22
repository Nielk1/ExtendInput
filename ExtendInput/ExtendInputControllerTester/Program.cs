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
            server.RunAsync();

            DeviceManager = new DeviceManager();
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
                string md5 = CreateMD5(controller.UniqueID);
                //Write_ControllerAdded(controller);
                controller.ControllerMetadataUpdate += DeviceManager_ControllerMetadataUpdate;
                Controllers[md5] = controller;
            }
            ControllersLock.Release();
        }

        private static void DeviceManager_ControllerMetadataUpdate(IController sender)
        {

        }

        private static void DeviceManager_ControllerRemoved(object sender, ExtendInput.DeviceProvider.IDevice e)
        {
            ControllersLock.Wait();
            {
                string md5 = CreateMD5(e.UniqueKey);
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
                //.WithModule(new FileModule("/images/controller", provider))
                .WithModule(new ActionModule("/poll_controller", HttpVerbs.Get, PollController))
                .WithModule(new ActionModule("/poll_other", HttpVerbs.Get, PollOther))
                .WithModule(new ActionModule("/activate_controller", HttpVerbs.Post, ActivateController))
                .WithModule(new ActionModule("/alternate_controller", HttpVerbs.Post, AlternateController))
                .WithModule(new ActionModule("/", HttpVerbs.Get, ctx => ctx.SendStringAsync(File.ReadAllText("../../index.html"), "text/html", Encoding.UTF8)));
                //.WithStaticFolder("/images/controller/","../images/controller/",true, new FileModule(,)
            //.WithModule(new ActionModule("/", HttpVerbs.Get, DeviceList))
            //.WithModule(new ActionModule("/", HttpVerbs.Get, DeviceList))
            //.WithModule(new WebSocketControllerModule("/terminal/"));

            // Listen for state changes.
            //server.StateChanged += (s, e) => $"WebServer New State - {e.NewState}".Info();

            return server;
        }

        private static async Task PollController(IHttpContext context)
        {
            await ControllersLock.WaitAsync();
            try
            {
                if (activeController == null) return;
                ControllerState State = activeController.GetState();
                JObject controls = new JObject();
                foreach (string key in State.Controls.Keys)
                {
                    JObject obj = new JObject();
                    obj["Type"] = State.Controls[key]?.GetType()?.ToString();
                    obj["Data"] = State.Controls[key] != null ? JObject.FromObject(State.Controls[key]) : null;
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
            await ControllersLock.WaitAsync();
            try
            {
                Dictionary<string, string> ControllerImages = new Dictionary<string, string>();
                Dictionary<string, string> IconImages = new Dictionary<string, string>();
                foreach (string ControllerID in Controllers.Keys)
                {
                    foreach (string ControllerTypeCode in Controllers[ControllerID].ControllerTypeCode)
                    {
                        /*{
                            string FileName = Path.Combine(@"..\..\images\controller", ControllerTypeCode);
                            if (File.Exists(FileName + ".png")) { ControllerImages[ControllerID] = ControllerTypeCode + ".png"; break; }
                            if (File.Exists(FileName + ".jpg")) { ControllerImages[ControllerID] = ControllerTypeCode + ".jpg"; break; }
                            if (File.Exists(FileName + ".jpeg")) { ControllerImages[ControllerID] = ControllerTypeCode + ".jpeg"; break; }
                        }*/
                        if (!ControllerImages.ContainsKey(ControllerTypeCode))
                        {
                            string FileName = Path.Combine(@"..\..\images\controller", ControllerTypeCode);
                            if (File.Exists(FileName + ".png")) { ControllerImages[ControllerTypeCode] = ControllerTypeCode + ".png"; break; }
                            if (File.Exists(FileName + ".jpg")) { ControllerImages[ControllerTypeCode] = ControllerTypeCode + ".jpg"; break; }
                            if (File.Exists(FileName + ".jpeg")) { ControllerImages[ControllerTypeCode] = ControllerTypeCode + ".jpeg"; break; }
                        }
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

                await context.SendDataAsync(new {
                    Controllers = Controllers,
                    ControllerImages = ControllerImages,
                    IconImages = IconImages,
                });
            }
            finally
            {
                ControllersLock.Release();
            }
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
                activeController?.DeInitalize();
                activeController = null;

                string ControllerID = await context.GetRequestBodyAsStringAsync();

                if (Controllers.ContainsKey(ControllerID))
                {
                    activeController = Controllers[ControllerID];
                    await context.SendDataAsync(true);
                    activeController.Initalize();
                    return;
                }
                await context.SendDataAsync(false);
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