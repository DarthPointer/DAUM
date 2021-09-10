using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    public static class DAUMLib
    {
        public static void AddToInt32ByOffset(byte[] span, Int32 value, Int32 offset)
        {
            WriteInt32IntoOffset(span, BitConverter.ToInt32(span, offset) + value, offset);
        }

        public static void WriteInt32IntoOffset(byte[] span, Int32 value, Int32 offset)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);

            span[offset + 0] = valueBytes[0];
            span[offset + 1] = valueBytes[1];
            span[offset + 2] = valueBytes[2];
            span[offset + 3] = valueBytes[3];
        }
    }

    public static class ListExtension
    {
        public static string TakeArg(this List<string> argList)
        {
            string result = argList[0];
            argList.RemoveAt(0);
            return result;
        }
    }
}
