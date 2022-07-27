using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public interface IControlButtonWithStateLight : IControl
    {
        bool DigitalStage1 { get; set; }
        string[] States { get; }
        string State { get; set; }
    }
}
