using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    class ReadExports : ReadMapOperation
    {
        int currentIndex;
        int currentExportDefOffset;

        protected override int EnumerationStart => 1;

        protected override int EnumerationIncrement => 1;

        protected override string MapElementName => "Export Definition";

        protected override void PrepareForEnumeration()
        {
            currentIndex = 0;
            currentExportDefOffset = BitConverter.ToInt32(Program.runData.uasset, HeaderOffsets.exportOffsetOffset);
        }

        protected override bool HasNext()
        {
            return currentIndex < BitConverter.ToInt32(Program.runData.uasset, HeaderOffsets.exportCountOffset);
        }

        protected override void ReadNext()
        {
            byte[] uasset = Program.runData.uasset;

            Int32 _class = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportClassOffset);
            Int32 super = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportSuperOffset);
            Int32 template = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportTemplateOffset);
            Int32 outer = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportOuterOffset);
            Int32 name = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportNameOffset);
            Int32 nameAug = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportNameOffset + 4);
            Int32 serialSize = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportSerialSizeOffset);
            Int32 serialOffset = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportSuperOffset);
            Int32 flags = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportObjectFlagsOffset);

            Int32[] otherData = new Int32[HeaderOffsets.exportOtherDataInt32Count];

            for (int i = 0; i < HeaderOffsets.exportOtherDataInt32Count; i++)
            {
                otherData[i] = BitConverter.ToInt32(uasset, currentExportDefOffset + HeaderOffsets.exportOtherDataOffset + 4 * i);
            }

            ReportElementContents($"Class: {ObjectNameStringFromIndex(_class)}");
            ReportElementContents($"Super: {super}");
            ReportElementContents($"Template: {ObjectNameStringFromIndex(template)}");
            ReportElementContents($"Outer: {ObjectNameStringFromIndex(outer)}");

            ReportElementContents($"Name: {new Program.NameEntry(name, nameAug)}");

            ReportElementContents($"Serial Size: {serialSize}");
            ReportElementContents($"Serial Offset: {serialOffset}");

            ReportElementContents($"Flags: {flags}");

            ReportElementContents($"Other Data: {string.Join(' ', otherData)}");

            currentIndex++;
            currentExportDefOffset += HeaderOffsets.exportDefSize;
        }

        [Obsolete(message: "To be replaced when OOP stored export map becomes a thing")]
        private static string ObjectNameStringFromIndex(Int32 index)
        {
            if (index == 0)
            {
                return "null";
            }
            else if (index < 0)
            {
                return $"Import:{Program.runData.importMap[-index - 1].ObjectNameString}";
            }
            else
            {
                index--;
                Int32 exportOffset = BitConverter.ToInt32(Program.runData.uasset, HeaderOffsets.exportOffsetOffset) + HeaderOffsets.exportDefSize * index;

                Int32 name = BitConverter.ToInt32(Program.runData.uasset, exportOffset + HeaderOffsets.exportNameOffset);
                Int32 nameAug = BitConverter.ToInt32(Program.runData.uasset, exportOffset + HeaderOffsets.exportNameOffset + 4);

                return new Program.NameEntry(name, nameAug).ToString();
            }
        }
    }
}
