using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput.DevPKey
{
    public static class Extensions
    {
        public static string ToUTF8String(this byte[] buffer)
        {
            var value = Encoding.UTF8.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }

        public static string ToUTF16String(this byte[] buffer)
        {
            var value = Encoding.Unicode.GetString(buffer);
            return value.Remove(value.IndexOf((char)0));
        }
    }
}
