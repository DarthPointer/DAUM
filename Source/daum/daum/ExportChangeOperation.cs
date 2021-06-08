using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DRGOffSetterLib;

namespace daum
{
    class ExportChangeOperation : Operation
    {
        private static Dictionary<string, Func<List<string>, string>> modes = new Dictionary<string, Func<List<string>, string>>()
        {
            { "-r", ReplaceMode }
        };

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            doneSomething = true;
            useStandardBackup = true;

            return modes[args.TakeArg()](args);
        }


        private static string ReplaceMode(List<string> args)
        {
            byte[] uasset = Program.runData.uasset;
            byte[] uexp = Program.runData.uexp;

            Int32 exportIndex = GetExportIndex(uasset, args).Value;
            //Int32 

            Console.WriteLine(exportIndex);

            return "";
        }
    }
}
