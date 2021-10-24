using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.DeviceProvider
{
    public interface IDevice : IEquatable<IDevice>, IDisposable
    {
        string DevicePath { get; }
        int ProductId { get; }
        int VendorId { get; }
        string UniqueKey { get; }

        Dictionary<string, dynamic> Properties { get; }
    }
}
