using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using daum.OffSetter;

namespace daum
{
    class Program
    {
        private static string configPath;

        private const string exitCommand = "Exit";
        private const string printNullConfigCommand = "NullConfig";
        private const string parseCommand = "Parse";
        private const string fromScriptModeKey = "-s";
        private const string startRecordingCommand = "StartRec";
        private const string stopRecordingCommand = "StopRec";

        private static Dictionary<string, Operation> operations = new Dictionary<string, Operation>() {
            { "-n", new NameDefOperation() },                   // legacy
            { "Name", new NameDefOperation() },
            { "-i", new ImportDefOperation() },                 // legacy
            { "Import", new ImportDefOperation() },
            { "-edef", new ExportDefOperation() },              // legacy
            { "ExportDef", new ExportDefOperation() },

            { "-echange", new ExportChangeOperation() },        // legacy
            { "ExportChange", new ExportChangeOperation() },

            { "ReadNames", new ReadNames() },
            { "ReadImports", new ReadImports() },
            { "ReadExports", new ReadExports() },
            { "-eread", new ExportReadOperation() },            // legacy
            { "ExportRead", new ExportReadOperation() },
            { "DParse", new DParse() },
            { "JParse", new DParse(useJson: true) },
            { "IndividualJParse", new DParse(useJson: true, individualFiles: true) },

            { "-f", new LoadFileOperation() },                  // legacy
            { "File", new LoadFileOperation() },
            { "Syntax", new daum.Syntax() },

            { "OutRedir", new OutRedir() },
            { "OutRestore", new OutRestore() },
            { "ToFile", new ToFile() },

            { "-o", new OffSetterCall() },                      // legacy
            { "OffSetter", new OffSetterCall() },
            { "PreloadPatterns", new PreloadPatternsOperation() },
            { "ReloadFiles", new ReloadFiles() },
            { "Revert", new Revert() }
        };

        static string defaultSyntax = "2.0.1.1";

        public static Dictionary<string, Syntax> syntaxes = new Dictionary<string, Syntax>()
        {
            { "2.0.1.0", new Syntax()
            {
                comments = false
            } },

            { "2.0.1.1", new Syntax() }
        };

        public const string backupPostfix = ".daum";

        public static RunData runData;
        public static Config config;

        static void Main(string[] args)
        {
            string toolDir = Assembly.GetExecutingAssembly().Location;
            toolDir = toolDir.Substring(0, toolDir.LastIndexOf('\\') + 1);

            configPath = toolDir + "Config.json";

            config = GetConfig();

            runData = new RunData()
            {
                toolDir = toolDir,
                currentSyntax = syntaxes[defaultSyntax]
            };

            List<string> argList = new List<string>(args);

            if (argList[0] == fromScriptModeKey)
            {
                argList.TakeArg();


                string scriptFile = argList.TakeArg();
                string[] commands = File.ReadAllLines(scriptFile);

                foreach (string command in commands)
                {
                    try
                    {
                        List<string> parsedCommand = ParseCommandString(command, ref runData.multiLineCommented);

                        ProcessCommand(config, runData, parsedCommand, out _, out _, out _);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception!");
                        Console.WriteLine(e.ToString());
                    }
                }

                return;
            }

            string fileName = argList.TakeArg();

            LoadFile(fileName);


            if (runData.uassetFileName.Length > 0)
            {
                byte[] uassetBytes = File.ReadAllBytes(runData.uassetFileName);

                if (argList.Count > 0)
                {
                    ProcessCommand(config, runData, argList, out _, out _, out _);
                    return;
                }

                Console.WriteLine($"Drg Automation Utility for Modding welcomes you!");
                Console.WriteLine($"Entered interactive mode for file {runData.uassetFileName}");
                Console.WriteLine();

                bool runLoop = true;
                while (runLoop)
                {
                    try
                    {
                        string input = Console.ReadLine();

                        if (runData.recordCommands && input != stopRecordingCommand) runData.commandsRecordingFile.WriteLine(input);

                        List<string> command = ParseCommandString(input, ref runData.multiLineCommented);
                        if (command.Count > 0)
                        {
                            if (command[0].Length > 0)
                            {
                                runLoop = ProcessCommand(config, runData, command, out bool doneSomething, out bool parsed, out bool commandWasFound);
                                if (doneSomething)
                                {
                                    if (config.autoParseAfterSuccess && !parsed) ParseFilesWithDRGPareser(config.drgParserPath, runData.uassetFileName);

                                    Console.WriteLine("Done!");
                                }
                                if (!commandWasFound)
                                {
                                    Console.WriteLine("No valid command found");
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception!");
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            
            //WriteConfig(config);
        }

        private static Config GetConfig()
        {
            Config config;
            if (File.Exists(configPath))
            {
                config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath), new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented
                });
            }
            else
            {
                config = new Config();
            }

            return config;
        }

        private static void WriteConfig(Config config, string fileName)
        {
            File.WriteAllText(runData.toolDir + "NullConfig.json", JsonConvert.SerializeObject(config, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            }));
        }

        public static bool ProcessCommand(Config config, RunData runData, List<string> command, out bool doneSomething, out bool parsed, out bool commandWasFound)
        {
            parsed = false;

            if (command.Count > 0)
            {
                if (command[0].Length > 0)      // Count and length comparisons are pretty useless here as of v1.0 but let them stay. 
                {
                    if (command[0] == exitCommand)
                    {
                        doneSomething = false;
                        commandWasFound = true;
                        return false;
                    }

                    if (command[0] == printNullConfigCommand)
                    {
                        WriteConfig(new Config(), "NullConfig.json");
                        doneSomething = false;
                        commandWasFound = true;
                        return true;
                    }

                    if (command[0] == parseCommand)
                    {
                        ParseFilesWithDRGPareser(config.drgParserPath, runData.uassetFileName);
                        parsed = true;
                        doneSomething = false;
                        commandWasFound = true;
                        return true;
                    }


                    if (command[0] == startRecordingCommand)
                    {
                        if (!runData.recordCommands)
                        {
                            command.TakeArg();
                            runData.commandsRecordingFileName = command.TakeArg();

                            runData.recordCommands = true;

                            if (File.Exists(runData.commandsRecordingFileName)) File.Delete(runData.commandsRecordingFileName);

                            File.WriteAllText(runData.commandsRecordingFileName, "");
                            runData.commandsRecordingFile = File.AppendText(runData.commandsRecordingFileName);

                            if (runData.uassetFileName != "")
                            {
                                runData.commandsRecordingFile.WriteLine($"Syntax {runData.currentSyntax.Code}");
                                runData.commandsRecordingFile.WriteLine($"File {runData.uassetFileName.Replace('\\', '/')}");
                            }
                        }

                        doneSomething = false;
                        commandWasFound = true;
                        return true;
                    }

                    if (command[0] == stopRecordingCommand)
                    {
                        runData.recordCommands = false;
                        runData.commandsRecordingFile.Close();
                        runData.commandsRecordingFile = null;
                        runData.commandsRecordingFileName = "";

                        doneSomething = false;
                        commandWasFound = true;
                        return true;
                    }

                    string operationKey = command.TakeArg();

                    if (operations.ContainsKey(operationKey))
                    {
                        string offSetterCallArgs = operations[operationKey].ExecuteAndGetOffSetterAgrs(command, out doneSomething, out bool useStandardBackup);

                        if (offSetterCallArgs != "")
                        {
                            CallOffSetterWithArgs(offSetterCallArgs);
                        }

                        if (useStandardBackup || offSetterCallArgs != "")
                        {
                            if (File.Exists(runData.uassetFileName + backupPostfix)) File.Delete(runData.uassetFileName + backupPostfix);
                            Directory.Move(runData.uassetFileName, runData.uassetFileName + backupPostfix);
                            File.WriteAllBytes(runData.uassetFileName, Program.runData.uasset);

                            if (File.Exists(runData.uexpFileName + backupPostfix)) File.Delete(runData.uexpFileName + backupPostfix);
                            Directory.Move(runData.uexpFileName, runData.uexpFileName + backupPostfix);
                            File.WriteAllBytes(runData.uexpFileName, Program.runData.uexp);
                        }

                        commandWasFound = true;
                        return true;
                    }
                }
            }

            doneSomething = false;
            commandWasFound = false;
            return true;
        }

        public static List<string> GetPattern(string key)
        {
            if (runData.patternsArePreloaded)
            {
                return new List<string>(runData.preloadedPatterns[key]);
            }

            else
            {
                bool _ = false;
                return ParseCommandString(File.ReadAllText(runData.toolDir + key), ref _);
            }
        }

        public static bool PatternExists(string key)
        {
            if (runData.patternsArePreloaded)
            {
                return runData.preloadedPatterns.ContainsKey(key);
            }

            else
            {
                return File.Exists(runData.toolDir + key);
            }
        }

        public static void CallOffSetterWithArgs(string offSetterCallArgs)
        {
            OffSetterExecution.Execute(offSetterCallArgs);
        }

        private static void ParseFilesWithDRGPareser(string parserPath, string uassetFileName)
        {
            Process parser = Process.Start(parserPath, uassetFileName);
            parser.WaitForExit();
        }

        public static List<string> ParseCommandString(string command, ref bool isMultiLineCommented)
        {
            List<string> result = new List<string>();

            const char escapeChar = '\\';
            const char spacedArgBracket = '"';
            const char spaceArgSeparator = ' ';

            const string singleLineComment = "//";
            const string multiLineCommentStart = "/*";
            const string multiLineCommentEnd = "*/";

            string buffer = "";
            bool isEscSeq = false;
            bool insideSpacedArgBrackets = false;
            char lastChar = (char)0;
            char prevChar = (char)0;
            bool singleLineCommented = false;

            foreach (char c in command)
            {
                prevChar = lastChar;
                lastChar = c;

                if (!singleLineCommented && !isMultiLineCommented)
                {
                    if (!insideSpacedArgBrackets && prevChar == singleLineComment[0] && c == singleLineComment[1] && runData.currentSyntax.comments)
                    {
                        singleLineCommented = true;

                        buffer = buffer.Remove(buffer.Length - 1);
                        if (buffer != "")
                        {
                            result.Add(buffer);
                            buffer = "";
                        }

                        lastChar = (char)0;
                    }
                    else if (!insideSpacedArgBrackets && prevChar == multiLineCommentStart[0] && c == multiLineCommentStart[1] && runData.currentSyntax.comments)
                    {
                        isMultiLineCommented = true;

                        buffer = buffer.Remove(buffer.Length - 1);
                        if (buffer != "")
                        {
                            result.Add(buffer);
                            buffer = "";
                        }
                    
                        lastChar = (char)0;
                    }
                    else if (!isEscSeq)
                    {
                        switch (c)
                        {
                            case escapeChar:
                                isEscSeq = true;
                                break;


                            case spacedArgBracket:
                                insideSpacedArgBrackets = !insideSpacedArgBrackets;
                                if (!insideSpacedArgBrackets)
                                {
                                    result.Add(buffer);
                                    buffer = "";
                                }
                                break;


                            case spaceArgSeparator:
                                if (!insideSpacedArgBrackets)
                                {
                                    if (buffer != "")
                                    {
                                        result.Add(buffer);
                                        buffer = "";
                                    }
                                }
                                else
                                {
                                    buffer += c;
                                }
                                break;


                            default:
                                buffer += c;
                                break;
                        }
                    }
                    else
                    {
                        switch (c)
                        {
                            case escapeChar:
                                buffer += c;
                                isEscSeq = false;
                                break;

                            case spacedArgBracket:
                                buffer += c;
                                isEscSeq = false;
                                break;

                            default:
                                throw new FormatException($"Escape seq '{escapeChar}{c}' is not supported");
                        }
                    }
                }
                else
                {
                    if (isMultiLineCommented && prevChar == multiLineCommentEnd[0] && c == multiLineCommentEnd[1])
                    {
                        isMultiLineCommented = false;
                    }
                }
            }

            if (lastChar != '"' && lastChar != ' ' && lastChar != 0) result.Add(buffer);

            return result;
        }

        public static void LoadFile(string uassetFileName)
        {
            runData.headerOffsets = new HeaderOffsets();

            runData.uassetFileName = uassetFileName;
            runData.uexpFileName = uassetFileName.Substring(0, uassetFileName.LastIndexOf('.') + 1) + "uexp";
            runData.fileDir = uassetFileName.Substring(0, uassetFileName.LastIndexOf('\\') + 1);

            runData.uasset = File.ReadAllBytes(runData.uassetFileName);
            runData.uexp = File.ReadAllBytes(runData.uexpFileName);

            LoadCustomVersion();

            LoadNames();
            LoadImports();
        }

        private static void LoadCustomVersion()
        {
            int customVersionCount = BitConverter.ToInt32(runData.uasset, runData.headerOffsets.customVersionCountOffset);

            // May be read them if they are needed lol?

            runData.headerOffsets.ApplyCustomVersionSize(customVersionCount * runData.headerOffsets.customVersionElementSize);
        }

        private static void LoadNames()
        {
            Int32 nameCount = BitConverter.ToInt32(runData.uasset, runData.headerOffsets.nameCountOffset);
            Int32 currentNameOffset = BitConverter.ToInt32(runData.uasset, runData.headerOffsets.nameOffsetOffset);

            runData.nameMap = new string[nameCount];

            for (Int32 i = 0; i < nameCount; i++)
            {
                runData.nameMap[i] = SizePrefixedStringFromOffsetOffsetAdvance(runData.uasset, ref currentNameOffset);

                currentNameOffset += runData.headerOffsets.nameHashesSize;
            }
        }

        public static string SizePrefixedStringFromOffsetOffsetAdvance(byte[] bytes, ref Int32 offset)
        {
            Int32 count = BitConverter.ToInt32(bytes, offset);
            offset += 4;

            string value = "";

            if (count > 0)
            {
                value = Encoding.UTF8.GetString(bytes, offset, count - 1);
                offset += count;
            }
            else if (count < 0)
            {
                value = Encoding.Unicode.GetString(bytes, offset, -1*count - 1);
                offset += -2 * count;
            }

            return value;
        }

        private static void LoadImports()
        {
            Int32 importCount = BitConverter.ToInt32(runData.uasset, runData.headerOffsets.importCountOffset);
            Int32 currentImportOffset = BitConverter.ToInt32(runData.uasset, runData.headerOffsets.importOffsetOffset);

            runData.importMap = new ImportData[importCount];

            for (Int32 i = 0; i < importCount; i++)
            {
                runData.importMap[i] = GetImportDataFromOffset(runData.uasset, currentImportOffset);

                currentImportOffset += runData.headerOffsets.importDefSize;
            }
        }

        private static ImportData GetImportDataFromOffset(byte[] uasset, Int32 offset)
        {
            return new ImportData()
            {
                packageName = new NameEntry(BitConverter.ToInt32(uasset, offset + runData.headerOffsets.importPackageOffset), BitConverter.ToInt32(uasset, offset + runData.headerOffsets.importPackageOffset + 4)),
                className = new NameEntry(BitConverter.ToInt32(uasset, offset + runData.headerOffsets.importClassOffset), BitConverter.ToInt32(uasset, offset + runData.headerOffsets.importClassOffset + 4)),
                outerIndex = BitConverter.ToInt32(uasset, offset + runData.headerOffsets.importOuterIndexOffset),
                importName = new NameEntry(BitConverter.ToInt32(uasset, offset + runData.headerOffsets.importNameOffset), BitConverter.ToInt32(uasset, offset + runData.headerOffsets.importNameOffset + 4))
            };
        }

        public record RunData
        {
            public string uassetFileName = "";
            public string uexpFileName = "";
            public byte[] uasset = null;
            public byte[] uexp = null;

            public string[] nameMap = null;
            public ImportData[] importMap = null;

            public string fileDir = "";
            public string toolDir = "";

            public bool patternsArePreloaded = false;
            public Dictionary<string, List<string>> preloadedPatterns = new Dictionary<string, List<string>>();

            public bool recordCommands = false;
            public string commandsRecordingFileName = "";
            public StreamWriter commandsRecordingFile = null;

            public TextWriter ConsoleStdOut = Console.Out;

            public HeaderOffsets headerOffsets;

            public Syntax currentSyntax;

            public bool multiLineCommented;
        }

        public class Syntax
        {
            public string Code => syntaxes.Where(x => x.Value == this).First().Key;

            public bool comments = true;
        }

        [JsonObject]
        public class Config
        {
            public string drgParserPath = "";
            public bool autoParseAfterSuccess = false;

            public bool enablePatternReadingHeuristica = false;
        }

        public class NameEntry
        {
            public NameEntry(Int32 nameIndex, Int32 nameAug)
            {
                this.nameIndex = nameIndex;
                this.nameAug = nameAug;
            }

            public Int32 nameIndex;
            public Int32 nameAug;

            public override string ToString()
            {
                return $"{Program.runData.nameMap[nameIndex]} : {nameAug}";
            }

            public static implicit operator string(NameEntry param)
            {
                return param.ToString();
            }

        }

        public class ImportData
        {
            public NameEntry packageName;
            public NameEntry className;
            public Int32 outerIndex;
            public NameEntry importName;

            public string PackageString => packageName.ToString();
            public string ClassString => className.ToString();
            public string OuterString => GetImportObjectNameString(outerIndex); 
            public string ObjectNameString => importName.ToString();

            private string GetImportObjectNameString(Int32 importIndex)
            {
                if (importIndex == 0)
                {
                    return "null";
                }
                else
                {
                    return Program.runData.importMap[-importIndex - 1].ObjectNameString;
                }
            }
        }

        public static class PatternFolders
        {
            public const string property = "PropertyPatterns";
            public const string body = "BodyPatterns";
            public const string structure = "StructPatterns";
        }
    }
}
