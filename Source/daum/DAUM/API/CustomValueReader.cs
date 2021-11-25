using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAUM.API
{
    static class CustomValueReader
    {
        public static string SizePrefixedStringFromOffsetOffsetAdvance(byte[] bytes, ref Int32 offset)
        {
            Int32 count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            string value = "";

            if (count > 0)
            {
                value = Encoding.UTF8.GetString(bytes, offset, count - 1);
                offset += count;
            }
            else if (count < 0)
            {
                value = Encoding.Unicode.GetString(bytes, offset, -1 * count - 1);
                offset += -2 * count;
            }

            return value;
        }
    }
}
