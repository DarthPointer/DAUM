using System;
using System.Collections.Generic;

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

            Console.WriteLine("--------------------");
            Console.WriteLine();
            Console.WriteLine(Program.runData.uassetFileName);
            Console.WriteLine();
            Console.WriteLine("--------------------");

            return "";
        }
    }
}
