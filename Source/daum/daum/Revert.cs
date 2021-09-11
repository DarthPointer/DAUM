using System.Collections.Generic;
using System.IO;

namespace daum
{
    class Revert : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            if (File.Exists(Program.runData.uassetFileName + Program.backupPostfix))
            {
                File.Delete(Program.runData.uassetFileName);
                File.Move(Program.runData.uassetFileName + Program.backupPostfix, Program.runData.uassetFileName);
            }

            if (File.Exists(Program.runData.uexpFileName + Program.backupPostfix))
            {
                File.Delete(Program.runData.uexpFileName);
                File.Move(Program.runData.uexpFileName + Program.backupPostfix, Program.runData.uexpFileName);
            }

            Program.LoadFile(Program.runData.uassetFileName);

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }
    }
}
