using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public class ControllerState : ICloneable
    {
        public ControlCollection Controls { get; private set; }

        public ControllerState()
        {
            Controls = new ControlCollection();
        }
        
        public object Clone()
        {
            ControllerState newState = new ControllerState();

            newState.Controls = (ControlCollection)this.Controls.Clone();

            return newState;
        }
    }
}
