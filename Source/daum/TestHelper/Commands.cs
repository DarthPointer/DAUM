using System;
using System.IO;

namespace TestHelper
{
    static class Commands
    {
        public static void ExportCount()
        {
            BinaryReader uasset = new BinaryReader(File.OpenRead(Program.runData.uassetFileName));
            uasset.ReadBytes(Program.exportCountOffset);

            Console.Write(uasset.ReadInt32());
            uasset.Close();
        }
    }
}
