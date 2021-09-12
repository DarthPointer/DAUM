using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace daum
{
    class DParse : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            string parseTarget = args.Count > 0 ? args.TakeArg() : Program.runData.uassetFileName;
            string initialFile = Program.runData.uassetFileName;

            if (File.Exists(parseTarget))
            {
                ParseFile(parseTarget);
            }
            else if (Directory.Exists(parseTarget))
            {
                ParseFolder(parseTarget);
            }

            if (initialFile.Length > 0) Program.LoadFile(initialFile);

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }

        private static void ParseFile(string filePath)
        {
            Program.LoadFile(filePath);

            Console.WriteLine("--------------------");
            Console.WriteLine();
            Console.WriteLine(Program.runData.uassetFileName);
            Console.WriteLine();
            Console.WriteLine("--------------------");

            Console.WriteLine("--------------------");
            Console.WriteLine("Name Map");
            ReadNames.Read();

            Console.WriteLine("--------------------");
            Console.WriteLine("Import Map");
            ReadImports.Read();

            Console.WriteLine("--------------------");
            Console.WriteLine("Export Map");
            ReadExports.Read();

            Console.WriteLine("--------------------");
            Console.WriteLine("Exports");

            Int32 exportCount = BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.exportCountOffset);
            ExportReadOperation exportReadOperation = new ExportReadOperation();

            for (Int32 exportIndex = 1; exportIndex < exportCount + 1; exportIndex++)
            {
                exportReadOperation.ReadExport(exportIndex);
            }
        }

        private static void ParseFolder(string folderPath)
        {
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.uasset", SearchOption.AllDirectories))
            {
                ParseFile(filePath);
            }
        }
    }
}
