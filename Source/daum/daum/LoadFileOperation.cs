using System;
using System.Collections.Generic;

using DRGOffSetterLib;

namespace daum
{
    class LoadFileOperation : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            doneSomething = true;
            useStandardBackup = false;

            string uassetFileName = args.TakeArg();

            Program.LoadFile(uassetFileName);

            return "";
        }
    }
}
