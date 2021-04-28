using System;
using System.Collections.Generic;
using DRGOffSetterLib;

namespace daum
{
    public abstract class Operation
    {
        public abstract string ExecuteAndGetOffSetterAgrs(ref Span<byte> span, List<string> args);

        protected static Span<byte> Insert(Span<byte> span, Span<byte> insert, Int32 offset)
        {
            Span<byte> spanStart = span.Slice(0, offset);
            Span<byte> spanEnd = span.Slice(offset);

            byte[] newArray = new byte[span.Length + insert.Length];
            spanStart.ToArray().CopyTo(newArray, 0);
            insert.ToArray().CopyTo(newArray, offset);
            spanEnd.ToArray().CopyTo(newArray, offset + insert.Length);

            return newArray;
        }

        protected static Span<byte> StringToBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                bytes[i] = (byte)str[i];
            }

            return bytes;
        }
    }

    public abstract class MapOperation : Operation
    {
        private static string addOpKey = "-a";
        private static string replaceKey = "-r";
        private static string byIndexKey = "-i";

        protected abstract Int32 nextBlockOffsetOffset { get; }

        public override string ExecuteAndGetOffSetterAgrs(ref Span<byte> span, List<string> args)
        {
            string opKey = args.TakeArg();
            if (opKey == addOpKey)
            {
                return " -n" + AddOperation(ref span, args, DOLib.Int32FromSpanOffset(span, nextBlockOffsetOffset));
            }

            return "";
        }

        protected abstract string AddOperation(ref Span<byte> span, List<string> args, Int32 offset);
    }

    public class NameDefOperation : MapOperation
    {
        protected override Int32 nextBlockOffsetOffset => 69;

        protected override string AddOperation(ref Span<byte> span, List<string> args, int offset)
        {
            Span<byte> insert = new Span<byte>(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            insert = Insert(insert, StringToBytes(args[0]), 4);

            DOLib.WriteInt32IntoOffset(insert, args[0].Length + 1, 0);
            span = Insert(span, insert, offset);

            return $" {insert.Length} {1}";
        }
    }
}
