using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    class Syntax : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            Program.runData.currentSyntax = Program.syntaxes[args.TakeArg()];
            
            doneSomething = false;
            useStandardBackup = false;
            return "";
        }
    }
}
