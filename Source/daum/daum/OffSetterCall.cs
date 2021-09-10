using System.Collections.Generic;
using System.IO;

namespace daum
{
    public class OffSetterCall : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            useStandardBackup = false;
            doneSomething = true;

            Program.CallOffSetterWithArgs(' ' + string.Join(' ', args));
            Program.runData.uasset = File.ReadAllBytes(Program.runData.uassetFileName);
            return "";
        }
    }
}
