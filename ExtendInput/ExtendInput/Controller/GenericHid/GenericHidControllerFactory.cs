using ExtendInput.DeviceProvider;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controller.GenericHid
{
    public class ControlData
    {
        [JsonProperty("c")]
        public string ControlName { get; set; }
        [JsonProperty("f")]
        public string FactoryName { get; set; }
        [JsonProperty("p")]
        public dynamic[] Paramaters { get; set; }
    }
    public class ControllerDbEntry
    {
        public string Name { get; set; }
        public string[] Tokens { get; set; }
        public Dictionary<string, dynamic> Select { get; set; }
        public decimal Version { get; set; }
        public Dictionary<string, ControlData> Controls { get; set; }
    }
    public class GenericHidControllerFactory : IControllerFactory
    {
        private AccessMode AccessMode;
        private List<ControllerDbEntry> ControllerDefinitions = new List<ControllerDbEntry>();
        private Dictionary<string, GenericHidController> Controllers = new Dictionary<string, GenericHidController>();

        public Dictionary<string, dynamic>[] DeviceWhitelist => ControllerDefinitions.Select(dr => dr.Select).ToArray();

        public GenericHidControllerFactory(AccessMode AccessMode)
        {
            this.AccessMode = AccessMode;

            if (File.Exists("controllerdb.jsonl"))
            {
                using (StreamReader reader = File.OpenText("controllerdb.jsonl"))
                {
                    while(!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        ControllerDbEntry entry = JsonConvert.DeserializeObject<ControllerDbEntry>(line);
                        ControllerDefinitions.Add(entry);
                    }
                }
            }
        }

        public IController NewDevice(IDevice device)
        {
            HidDevice _device = device as HidDevice;

            if (_device == null)
                return null;

            // TODO make this a class with a "is match" function, or perhapse a weighting function
            var CandidateController = ControllerDefinitions.Where(dr => dr.Select["VID"] == _device.VendorId && dr.Select["PID"] == _device.ProductId).FirstOrDefault();

            //if (!CandidateControllers.Any())
            //    return null;

            if (CandidateController == null)
                return null;

            lock (Controllers)
            {
                GenericHidController ctrl = null;
                if (Controllers.ContainsKey(device.UniqueKey))
                {
                    // TODO handle subdevices, such as the audio device
                    //ctrl = Controllers[ContrainerID.Value];
                    //ctrl.AddDevice(_device);
                }
                else
                {
                    Controllers[device.UniqueKey] = new GenericHidController(_device, AccessMode, CandidateController);
                    ctrl = Controllers[device.UniqueKey];
                }

                return ctrl;
            }

            return null;
        }

        public string RemoveDevice(string UniqueKey)
        {
            lock (Controllers)
            {
                GenericHidController ctrl = Controllers[UniqueKey];
                string UniqueControllerId = ctrl.ConnectionUniqueID;

                ctrl.DeInitalize();
                ctrl.Dispose();
                Controllers.Remove(UniqueKey);

                return UniqueControllerId;
            }
        }
    }
}
