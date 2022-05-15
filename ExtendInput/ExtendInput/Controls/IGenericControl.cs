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
        void SetGenericValue(IReport report);
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
