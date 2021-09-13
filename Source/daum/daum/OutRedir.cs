using System;
using System.Collections.Generic;
using System.IO;

namespace daum
{
    class OutRedir : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            if (Console.Out != Program.runData.ConsoleStdOut) Console.Out.Close();

            Console.SetOut(new StreamWriter(args.TakeArg()));

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }
    }
}
