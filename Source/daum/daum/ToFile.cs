using System;
using System.Collections.Generic;
using System.IO;

namespace daum
{
    class ToFile : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            if (Console.Out != Program.runData.ConsoleStdOut) Console.Out.Close();

            Console.SetOut(new StreamWriter(args.TakeArg()));

            Program.ProcessCommand(Program.config, Program.runData, args, out doneSomething, out bool _, out bool _);

            if (Console.Out != Program.runData.ConsoleStdOut) Console.Out.Close();

            Console.SetOut(Program.runData.ConsoleStdOut);

            useStandardBackup = false;
            return "";
        }
    }
}
