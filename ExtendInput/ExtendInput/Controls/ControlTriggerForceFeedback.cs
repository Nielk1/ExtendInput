using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    public enum EEffectTriggerForceFeedback
    {
        None,
        Feedback,  // start,      resistance
        Weapon,    // start, end, resistance
        Vibration, // start,      resistance, amplitude, frequency // PS5 doesn't have resistance here
    }
    public interface IControlTriggerForceFeedback : IControl
    {
        EEffectTriggerForceFeedback Effect { get; }
        float Start { get; }
        float End { get; }
        float Strength { get; }
        float Amplitude { get; }
        float Frequency { get; }
    }
}
