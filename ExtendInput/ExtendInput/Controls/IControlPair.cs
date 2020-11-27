using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.Controls
{
    interface IControlPair<T> where T : IControl
    {
        T Left { get; }
        T Right { get; }
    }
}
