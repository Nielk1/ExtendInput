using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControl : ICloneable
    {
        T Value<T>(string key);
        Type Type(string key);

        void ProcessPendingInputs();
    }
}
