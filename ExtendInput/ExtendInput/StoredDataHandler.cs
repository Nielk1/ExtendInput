using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput
{
    class StoredDataHandler
    {
        public static string GetMacData(string Mac)
        {
            string Filename = Path.Combine("extend_input", Mac.Replace(":", string.Empty) + ".mac");
            if (File.Exists(Filename))
                return File.ReadAllText(Filename);

            return null;
        }

        public static void SetMacData(string Mac, string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                if (!Directory.Exists("extend_input"))
                    Directory.CreateDirectory("extend_input");
                string Filename = Path.Combine("extend_input", Mac.Replace(":", string.Empty) + ".mac");
                File.WriteAllText(Filename, data);
            }
        }
    }
}
