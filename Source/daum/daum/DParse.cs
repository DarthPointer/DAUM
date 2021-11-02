using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace daum
{
    class DParse : Operation
    {
        public bool useJson = false;
        public bool individualFiles = false;

        public DParse(bool useJson = false, bool individualFiles = false)
        {
            this.useJson = useJson;
            this.individualFiles = individualFiles;
        }

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            string parseTarget = args.Count > 0 ? args.TakeArg() : Program.runData.uassetFileName;
            string initialFile = Program.runData.uassetFileName;

            if (useJson && !individualFiles)
            {
                FilesStructure.instance = new FilesStructure();
            }

            if (File.Exists(parseTarget))
            {
                ParseFile(parseTarget, useJson, individualFiles);
            }
            else if (Directory.Exists(parseTarget))
            {
                ParseFolder(parseTarget, useJson, individualFiles);
            }

            if (initialFile.Length > 0) Program.LoadFile(initialFile);

            if (useJson && !individualFiles)
            {
                Console.Write(JsonConvert.SerializeObject(FilesStructure.instance, new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented
                }));
                Console.WriteLine();
            }

            doneSomething = false;
            useStandardBackup = false;
            return "";
        }

        private static void ParseFile(string filePath, bool useJson, bool individualFiles)
        {
            Program.LoadFile(filePath);

            Int32 exportCount = BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.exportCountOffset);

            if (useJson)
            {
                if (individualFiles)
                {
                    FilesStructure.instance = new FilesStructure();
                }

                FilesStructure.currentFile = new FileStructure();
                FilesStructure.instance.files[filePath.Substring(0, filePath.LastIndexOf('.'))] = FilesStructure.currentFile;

                Int32 nameCount = BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.nameCountOffset);
                Int32 importCount = BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.importCountOffset);

                FilesStructure.currentFile.nameMap = new NameMapStamp();
                FilesStructure.currentFile.nameMap.names = new NameStamp[nameCount];

                FilesStructure.currentFile.importMap = new ImportMapStamp();
                FilesStructure.currentFile.importMap.imports = new ImportStamp[importCount];

                FilesStructure.currentFile.exportMap = new ExportMapStamp();
                FilesStructure.currentFile.exportMap.exports = new ExportDefinitionStamp[exportCount];
                FilesStructure.currentFile.exportsExpansion = new ExportExpansion[exportCount];
            }
            else
            {
                Console.WriteLine("--------------------");
                Console.WriteLine();
                Console.WriteLine(Program.runData.uassetFileName);
                Console.WriteLine();
                Console.WriteLine("--------------------");

                Console.WriteLine("--------------------");
                Console.WriteLine("Name Map");
            }
            ReadNames.Read(useJson);

            if (!useJson)
            {
                Console.WriteLine("--------------------");
                Console.WriteLine("Import Map");
            }
            ReadImports.Read(useJson);

            if (!useJson)
            {
                Console.WriteLine("--------------------");
                Console.WriteLine("Export Map");
            }
            ReadExports.Read(useJson);

            if (!useJson)
            {
                Console.WriteLine("--------------------");
                Console.WriteLine("Exports");
            }

            //Int32 exportCount = BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.exportCountOffset);
            ExportReadOperation exportReadOperation = new ExportReadOperation();

            for (Int32 exportIndex = 1; exportIndex < exportCount + 1; exportIndex++)
            {
                exportReadOperation.ReadExport(exportIndex, useJson);
            }

            if (useJson && individualFiles)
            {
                File.WriteAllText(Program.runData.uassetFileName.Substring(0, Program.runData.uassetFileName.LastIndexOf('.') + 1) + "json",
                    JsonConvert.SerializeObject(FilesStructure.instance, new JsonSerializerSettings()
                    {
                        Formatting = Formatting.Indented
                    }));
            }
        }

        private static void ParseFolder(string folderPath, bool useJson, bool individualFiles)
        {
            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.uasset", SearchOption.AllDirectories))
            {
                ParseFile(filePath, useJson, individualFiles);
            }
        }
    }
}
