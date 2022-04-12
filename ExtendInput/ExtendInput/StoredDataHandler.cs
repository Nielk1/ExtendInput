using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput
{
    // TODO rewrite this to be a generic data storage tool
    class StoredDataHandler
    {
        static object KnownMacsLock = new object();
        public static string[] GetStoredMacConectionIdList()
        {
            lock (KnownMacsLock)
            {
                string filename = Path.Combine("extend_input", "known_macs.data");
                if (!File.Exists(filename))
                    return null;
                return File.ReadAllLines(filename).Select(dr => dr.Split('\t')[0]).ToArray();
            }
        }
        public static string GetStoredMacForConectionId(string ConnectionId)
        {
            lock (KnownMacsLock)
            {
                string filename = Path.Combine("extend_input", "known_macs.data");
                if (!File.Exists(filename))
                    return null;
                //Dictionary<string, string> Macs = new Dictionary<string, string>();
                return File.ReadAllLines(filename).Where(dr => dr.Split('\t')[0] == ConnectionId).Select(dr => dr.Split('\t')[1]).FirstOrDefault();
            }
        }
        public static bool SaveStoredMacForConnectionId(string ConnectionId, string Mac)
        {
            lock (KnownMacsLock)
            {
                string filename = Path.Combine("extend_input", "known_macs.data");
                if (!File.Exists(filename))
                    File.Create(filename).Close();
                Dictionary<string, string> Macs = new Dictionary<string, string>();
                foreach (string line in File.ReadAllLines(filename))
                {
                    string[] parts = line.Split('\t');
                    Macs[parts[0]] = parts[1];
                }
                Macs[ConnectionId] = Mac;
                File.WriteAllLines(filename, Macs.Select(dr => $"{dr.Key}\t{dr.Value}").ToArray());
                return true;
            }
        }
        public static bool RemoveStoredMacForConectionId(string ConnectionId)
        {
            lock (KnownMacsLock)
            {
                string filename = Path.Combine("extend_input", "known_macs.data");
                if (!File.Exists(filename))
                    return false;
                Dictionary<string, string> Macs = new Dictionary<string, string>();
                foreach (string line in File.ReadAllLines(filename))
                {
                    string[] parts = line.Split('\t');
                    Macs[parts[0]] = parts[1];
                }
                if (!Macs.ContainsKey(ConnectionId))
                    return false;
                Macs.Remove(ConnectionId);
                File.WriteAllLines(filename, Macs.Select(dr => $"{dr.Key}\t{dr.Value}").ToArray());
                return true;
            }
        }

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
