using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public class ControllerState : ICloneable
    {
        public ControlCollection Controls { get; private set; }
        private SemaphoreSlim StateLock;

        public delegate void ControllerStateUpdateEvent(ControlCollection controls);
        public event ControllerStateUpdateEvent ControllerStateUpdate;

        public ControllerState()
        {
            Controls = new ControlCollection();
            StateLock = new SemaphoreSlim(1);
        }
        public void StartStateChange()
        {
            StateLock.Wait();
        }
        public void EndStateChange(bool Notify)
        {
            if (Notify)
                ControllerStateUpdate?.Invoke(Controls);
            StateLock.Release();
        }

        public object Clone()
        {
            ControllerState newState = new ControllerState();

            newState.Controls = (ControlCollection)this.Controls.Clone();

            return newState;
        }
    }
}
