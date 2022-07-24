using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExtendInput
{
    static class Tools
    {
        public static unsafe byte[] ConvertToBytes<T>(T value) where T : unmanaged
        {
            byte* pointer = (byte*)&value;

            byte[] bytes = new byte[sizeof(T)];
            for (int i = 0; i < sizeof(T); i++)
            {
                bytes[i] = pointer[i];
            }

            return bytes;
        }
        public static unsafe void ConvertToBytes<T>(T value, ref byte[] buffer, int offset) where T : unmanaged
        {
            byte* pointer = (byte*)&value;

            // TODO find a way to resolve the issue caused by structure alignment of the end
            //if (buffer.Length < sizeof(T) + offset)
            //    throw new IndexOutOfRangeException();

            int max = sizeof(T);
            if (max + offset > buffer.Length)
                max = buffer.Length - offset;

            for (int i = 0; i < max; i++)
            {
                buffer[offset + i] = pointer[i];
            }
        }
    }
}
