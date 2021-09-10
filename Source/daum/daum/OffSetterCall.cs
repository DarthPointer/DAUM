using System.Collections.Generic;
using System.IO;

namespace daum
{
    public class OffSetterCall : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            useStandardBackup = true;
            doneSomething = true;

            Program.CallOffSetterWithArgs(string.Join(' ', args));
            return "";
        }
    }
}
