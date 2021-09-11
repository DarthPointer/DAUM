using System;
using System.IO;
using System.Collections.Generic;


namespace daum
{
    class OutRestore : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            if (Console.Out != Program.runData.ConsoleStdOut) Console.Out.Close();

            Console.SetOut(Program.runData.ConsoleStdOut);

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }
    }
}
