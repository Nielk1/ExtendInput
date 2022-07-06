using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.IO;
using Newtonsoft.Json.Linq;

namespace DatabaseBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            string outputPath = ConfigurationManager.AppSettings.Get("output_path");
            using (FileStream fs = File.Open(outputPath, FileMode.Create))
            using (TextWriter writer = new StreamWriter(fs))
            {
                string generic_hid_controller_maps_Path = ConfigurationManager.AppSettings.Get("generic_hid_controller_maps");
                foreach (string sourceFile in Directory.EnumerateFiles(generic_hid_controller_maps_Path, "*.json", SearchOption.AllDirectories))
                {
                    JObject obj = JObject.Parse(File.ReadAllText(sourceFile));
                    JObject newObj = new JObject();
                    newObj["n"] = obj["Name"];
                    newObj["t"] = obj["Tokens"];
                    newObj["s"] = obj["Select"];
                    newObj["c"] = new JObject();
                    foreach (JProperty control in obj["Controls"])
                    {
                        newObj["c"][control.Name] = new JObject();
                        newObj["c"][control.Name]["c"] = control.Value["ControlName"];
                        if (control.Value["FactoryName"] != null)
                            newObj["c"][control.Name]["f"] = control.Value["FactoryName"];
                        newObj["c"][control.Name]["p"] = control.Value["Paramaters"];
                    }
                    JObject wrapperObject = new JObject();
                    wrapperObject["t"] = "GHID";
                    wrapperObject["d"] = newObj;
                    writer.WriteLine(wrapperObject.ToString(Newtonsoft.Json.Formatting.None));
                }
            }
        }
    }
}
