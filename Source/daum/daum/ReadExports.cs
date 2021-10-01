using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    class ReadExports : ReadMapOperation
    {
        int currentByOrderIndex;
        int currentExportDefOffset;

        protected override int EnumerationStart => 1;

        protected override int EnumerationIncrement => 1;

        protected override string MapElementName => "Export Definition";

        protected override void PrepareForEnumeration()
        {
            currentByOrderIndex = 0;
            currentExportDefOffset = BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.exportOffsetOffset);
        }

        protected override bool HasNext()
        {
            return currentByOrderIndex < BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.exportCountOffset);
        }

        protected override void ReadNext(bool useJson, int nextIndex)
        {
            byte[] uasset = Program.runData.uasset;

            Int32 _class = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportClassOffset);
            Int32 super = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportSuperOffset);
            Int32 template = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportTemplateOffset);
            Int32 outer = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportOuterOffset);
            Int32 name = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportNameOffset);
            Int32 nameAug = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportNameOffset + 4);
            Int32 serialSize = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportSerialSizeOffset);
            Int32 serialOffset = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportSerialOffsetOffset);
            Int32 flags = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportObjectFlagsOffset);

            Int32[] otherData = new Int32[Program.runData.headerOffsets.exportOtherDataInt32Count];

            for (int i = 0; i < Program.runData.headerOffsets.exportOtherDataInt32Count; i++)
            {
                otherData[i] = BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportOtherDataOffset + 4 * i);
            }

            if (useJson)
            {
                FilesStructure.currentFile.exportMap.exports[currentByOrderIndex] = new ExportDefinitionStamp()
                {
                    thisIndex = nextIndex,

                    _class = ObjectNameStringFromIndex(_class),
                    super = super,
                    template = ObjectNameStringFromIndex(template),
                    outer = ObjectNameStringFromIndex(outer),

                    name = new Program.NameEntry(name, nameAug),

                    serialSize = serialSize,
                    serialOffset = serialOffset,

                    flags = flags,
                    otherData = otherData
                };
            }
            else
            {
                ReportElementContents($"Class: {ObjectNameStringFromIndex(_class)}");
                ReportElementContents($"Super: {super}");
                ReportElementContents($"Template: {ObjectNameStringFromIndex(template)}");
                ReportElementContents($"Outer: {ObjectNameStringFromIndex(outer)}");

                ReportElementContents($"Name: {new Program.NameEntry(name, nameAug)}");

                ReportElementContents($"Serial Size: {serialSize}");
                ReportElementContents($"Serial Offset: {serialOffset}");

                ReportElementContents($"Flags: {flags}");

                ReportElementContents($"Other Data: {string.Join(' ', otherData)}");
            }

            currentByOrderIndex++;
            currentExportDefOffset += Program.runData.headerOffsets.exportDefSize;
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
                Int32 exportOffset = BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.exportOffsetOffset) + Program.runData.headerOffsets.exportDefSize * index;

                Int32 name = BitConverter.ToInt32(Program.runData.uasset, exportOffset + Program.runData.headerOffsets.exportNameOffset);
                Int32 nameAug = BitConverter.ToInt32(Program.runData.uasset, exportOffset + Program.runData.headerOffsets.exportNameOffset + 4);

                return new Program.NameEntry(name, nameAug).ToString();
            }
        }

        public static void Read(bool useJson)
        {
            new ReadExports().ReadMap(useJson);
        }
    }
}
