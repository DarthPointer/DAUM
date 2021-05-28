using System;
using System.Collections.Generic;
using DRGOffSetterLib;

namespace daum
{
    public class ExportReadOperation : Operation
    {
        private static string endOfStructConfigName = "None";

        public override string ExecuteAndGetOffSetterAgrs(ref Span<byte> span, List<string> args, out bool useStandardBackup)
        {
            useStandardBackup = false;

            Int32 exportIndex = GetExportIndex(span, args).Value;

            Int32 fisrtExportOffset = DOLib.Int32FromSpanOffset(span, exportOffsetOffset);
            Int32 uexpStructureOffset = DOLib.Int32FromSpanOffset(span, fisrtExportOffset + (exportIndex - 1) * exportDefSize + exportSerialOffsetOffset)
                - DOLib.Int32FromSpanOffset(span, headerSizeOffset);

            Console.WriteLine(uexpStructureOffset);

            return "";
        }
    }
}
