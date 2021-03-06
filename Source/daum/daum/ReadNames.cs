using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    class ReadNames : ReadMapOperation
    {
        int currentNameIndex;

        protected override string MapElementName => "Name";

        protected override int EnumerationStart => 0;

        protected override int EnumerationIncrement => 1;

        protected override void PrepareForEnumeration()
        {
            currentNameIndex = 0;
        }

        protected override bool HasNext()
        {
            return Program.runData.nameMap.Length > currentNameIndex;
        }

        protected override void ReadNext(bool useJson, int nextIndex)
        {
            if (useJson)
            {
                FilesStructure.currentFile.nameMap.names[currentNameIndex] = new NameStamp()
                {
                    thisIndex = nextIndex,
                    name = Program.runData.nameMap[currentNameIndex++]
                };
            }
            else
            {
                ReportElementContents(Program.runData.nameMap[currentNameIndex++]);
            }
        }

        public static void Read(bool useJson)
        {
            new ReadNames().ReadMap(useJson);
        }
    }
}
