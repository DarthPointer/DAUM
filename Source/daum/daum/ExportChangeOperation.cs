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
        private static ECOCustomRunDara customRunDara;

        readonly static Dictionary<string, Action<List<string>>> replaceModeAdditionalKeys = new Dictionary<string, Action<List<string>>>()
        {
            { "-r", (args) => customRunDara.reportSearchSteps = true }
        };

        private static Dictionary<string, Func<List<string>, string>> modes = new Dictionary<string, Func<List<string>, string>>()
        {
            { "-r", ReplaceMode }
        };

        private static Dictionary<string, ExportParsingMachine.PatternElementProcesser> contextSearchProcessers =
            new Dictionary<string, ExportParsingMachine.PatternElementProcesser>()
            {
            };

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            doneSomething = true;
            useStandardBackup = true;

            customRunDara = new ECOCustomRunDara();

            return modes[args.TakeArg()](args);
        }


        private static string ReplaceMode(List<string> args)
        {
            byte[] uasset = Program.runData.uasset;
            byte[] uexp = Program.runData.uexp;

            Int32 exportIndex = GetExportIndex(uasset, args).Value;
            Int32 exportDefOffset = BitConverter.ToInt32(uasset, exportOffsetOffset) + (exportIndex - 1) * exportDefSize;
            Int32 exportOffset = BitConverter.ToInt32(uasset, exportDefOffset + exportSerialOffsetOffset) -
                BitConverter.ToInt32(uasset, headerSizeOffset);

            string targetContext = args.TakeArg();
            customRunDara.newValue = args.TakeArg();

            while (args.Count > 0)
            {
                replaceModeAdditionalKeys[args.TakeArg()](args);
            }

            ExportParsingMachine.ResetSLIString();

            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents(
                $"Searching for {targetContext} in export at {exportOffset} to set new value {customRunDara.newValue}");

            

            return "";
        }

        private class ECOCustomRunDara
        {
            public bool reportSearchSteps = false;
            public string newValue = "";
        }
    }
}
