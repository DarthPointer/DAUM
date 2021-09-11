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

        protected override void ReadNext()
        {
            ReportElementContents(Program.runData.nameMap[currentNameIndex++]);
        }
    }
}
