using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    class ReloadFiles : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            Program.LoadFile(Program.runData.uassetFileName);

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }
    }
}
