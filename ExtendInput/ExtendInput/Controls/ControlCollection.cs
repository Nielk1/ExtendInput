using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public class ControlCollection /*: ICloneable*/// where T : IControl
    {
        private Dictionary<string, IControl> Data = new Dictionary<string, IControl>();

        public string[] Keys
        {
            get
            {
                return Data.Keys.ToArray();
            }
        }

        public IControl this[string key]
        {
            get
            {
                if (Data.ContainsKey(key))
                    return Data[key];
                return default;
            }
            set
            {
                Data[key] = value;
            }
        }

        public object Clone()
        {
            ControlCollection newData = new ControlCollection();

            foreach (var key in Data.Keys)
            {
                newData[key] = (IControl)Data[key].Clone();
            }

            return newData;
        }
    }


}
