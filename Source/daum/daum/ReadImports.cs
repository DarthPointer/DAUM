using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    class ReadImports : ReadMapOperation
    {
        int currentIndex;

        protected override int EnumerationStart => -1;

        protected override int EnumerationIncrement => -1;

        protected override string MapElementName => "Import";

        protected override void PrepareForEnumeration()
        {
            currentIndex = 0;
        }

        protected override bool HasNext()
        {
            return currentIndex < Program.runData.importMap.Length;
        }

        protected override void ReadNext(bool useJson, int nextIndex)
        {
            Program.ImportData importData = Program.runData.importMap[currentIndex];

            if (useJson)
            {
                FilesStructure.currentFile.importMap.imports[currentIndex] = new ImportStamp()
                {
                    thisIndex = nextIndex,

                    package = importData.PackageString,
                    _class = importData.ClassString,
                    outer = importData.OuterString,
                    name = importData.ObjectNameString
                };
            }
            else
            {
                ReportElementContents($"Package: {importData.PackageString}");
                ReportElementContents($"Class: {importData.ClassString}");
                ReportElementContents($"Outer: {importData.OuterString}");
                ReportElementContents($"Name: {importData.ObjectNameString}");
            }
            
            currentIndex++;
        }

        public static void Read(bool useJson)
        {
            new ReadImports().ReadMap(useJson);
        }
    }
}
