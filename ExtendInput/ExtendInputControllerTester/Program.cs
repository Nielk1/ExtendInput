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
                .WithModule(new ActionModule("/poll_controller", HttpVerbs.Get, PollController))
                .WithModule(new ActionModule("/poll_other", HttpVerbs.Get, PollOther))
                .WithModule(new ActionModule("/activate_controller", HttpVerbs.Post, ActivateController))
                .WithModule(new ActionModule("/", HttpVerbs.Get, ctx => ctx.SendStringAsync(File.ReadAllText("../../index.html"), "text/html", Encoding.UTF8)));
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
                JObject output = new JObject();
                foreach (string key in State.Controls.Keys)
                {
                    JObject obj = new JObject();
                    obj["Type"] = State.Controls[key]?.GetType()?.ToString();
                    obj["Data"] = State.Controls[key] != null ? JObject.FromObject(State.Controls[key]) : null;
                    output[key] = obj;
                }
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
                await context.SendDataAsync(new { Controllers = Controllers });
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