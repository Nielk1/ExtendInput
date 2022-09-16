using ExtendInput.Controller.GenericHid;
using ExtendInput.DeviceProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IGenericControl : IControl
    {
        /// <summary>
        /// Parse incomming report into Control using stored parsing rules
        /// </summary>
        /// <param name="report"></param>
        void ProcessReportForGenericController(IReport report);
    }
    public interface IGenericOutputControl : IControl
    {
        /// <summary>
        /// ReportID keyed report data, 0 if no key exists, will create array if not present, edit if present, resize if too small, might use a default size
        /// </summary>
        /// <param name="rawReport"></param>
        void GenerateReportsForGenericController(Dictionary<byte, byte[]> rawReport);
    }
    public class GenericControlAttribute : Attribute
    {
        public string Name { get; private set; }
        public GenericControlAttribute(string Name)
        {
            this.Name = Name;
        }
    }
}
