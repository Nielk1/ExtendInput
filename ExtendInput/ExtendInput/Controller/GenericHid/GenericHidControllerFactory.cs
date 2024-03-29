﻿using ExtendInput.DeviceProvider;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        [JsonProperty("r")]
        public Dictionary<string, dynamic> Properties { get; set; }
    }
    public class ControllerDbEntry
    {
        [JsonProperty("n")]
        public string Name { get; set; }
        [JsonProperty("t")]
        public string[] Tokens { get; set; }
        [JsonProperty("s")]
        public Dictionary<string, dynamic> Select { get; set; }
        [JsonProperty("c")]
        public Dictionary<string, ControlData> Controls { get; set; }
        [JsonProperty("ol")]
        public Dictionary<byte, int> OutputReportLengths { get; set; }
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

            if (File.Exists("extendinputdb.jsonl"))
            {
                using (StreamReader reader = File.OpenText("extendinputdb.jsonl"))
                {
                    while(!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        JObject obj = JObject.Parse(line); // TODO add a system that reads this config centrally and sends these tokens out to factories
                        if (obj["t"].Value<string>() == "GHID")
                        {
                            //ControllerDbEntry entry = JsonConvert.DeserializeObject<ControllerDbEntry>(line);
                            ControllerDbEntry entry = obj["d"].ToObject<ControllerDbEntry>();
                            ControllerDefinitions.Add(entry);
                        }
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
                if (!Controllers.ContainsKey(UniqueKey))
                    return null;
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
