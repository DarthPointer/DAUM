using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Text;

using daum.OffSetter;

namespace daum
{
    class Program
    {
        private static string configPath;

        private const string exitCommand = "exit";
        private const string printNullConfigCommand = "nullConfig";
        private const string parseCommand = "parse";
        private const string fromScriptModeKey = "-s";
        private const string startRecordingCommand = "StartRec";
        private const string stopRecordingCommand = "StopRec";

        private static Dictionary<string, Operation> operations = new Dictionary<string, Operation>() {
            { "-n", new NameDefOperation() },
            { "-i", new ImportDefOperation() },
            { "-edef", new ExportDefOperation() },

            { "-echange", new ExportChangeOperation() },

            { "ReadNames", new ReadNames() },
            { "-eread", new ExportReadOperation() },

            { "-f", new LoadFileOperation() },

            { "-o", new OffSetterCall() },
            { "PreloadPatterns", new PreloadPatternsOperation() },
            { "ReloadFiles", new ReloadFiles() },
            { "Revert", new Revert() }
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
                        List<string> parsedCommand = ParseCommandString(command);

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

                        List<string> command = ParseCommandString(input);
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

        private static bool ProcessCommand(Config config, RunData runData, List<string> command, out bool doneSomething, out bool parsed, out bool commandWasFound)
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
                                runData.commandsRecordingFile.WriteLine($"-f {runData.uassetFileName}");
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
                return ParseCommandString(File.ReadAllText(runData.toolDir + key));
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

        public static List<string> ParseCommandString(string command)
        {
            List<string> result = new List<string>();

            const char escapeChar = '\\';
            const char spacedArgBracket = '"';
            const char spaceArgSeparator = ' ';

            string buffer = "";
            bool isEscSeq = false;
            bool insideSpacedArgBrackets = false;
            char lastChar = (char)0;

            foreach (char c in command)
            {
                lastChar = c;
                if (!isEscSeq)
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

            if (lastChar != '"' && lastChar != ' ' && lastChar != 0) result.Add(buffer);

            return result;
        }

        public static void LoadFile(string uassetFileName)
        {
            runData.uassetFileName = uassetFileName;
            runData.uexpFileName = uassetFileName.Substring(0, uassetFileName.LastIndexOf('.') + 1) + "uexp";
            runData.fileDir = uassetFileName.Substring(0, uassetFileName.LastIndexOf('\\') + 1);

            runData.uasset = File.ReadAllBytes(runData.uassetFileName);
            runData.uexp = File.ReadAllBytes(runData.uexpFileName);

            LoadNames();
            LoadImports();
        }

        private static void LoadNames()
        {
            Int32 nameCount = BitConverter.ToInt32(runData.uasset, HeaderOffsets.nameCountOffset);
            Int32 currentNameOffset = BitConverter.ToInt32(runData.uasset, HeaderOffsets.nameOffsetOffset);

            runData.nameMap = new string[nameCount];

            for (Int32 i = 0; i < nameCount; i++)
            {
                runData.nameMap[i] = SizePrefixedStringFromOffsetOffsetAdvance(runData.uasset, ref currentNameOffset);

                currentNameOffset += HeaderOffsets.nameHashesSize;
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
            Int32 importCount = BitConverter.ToInt32(runData.uasset, HeaderOffsets.importCountOffset);
            Int32 currentImportOffset = BitConverter.ToInt32(runData.uasset, HeaderOffsets.importOffsetOffset);

            runData.importMap = new ImportData[importCount];

            for (Int32 i = 0; i < importCount; i++)
            {
                runData.importMap[i] = GetImportDataFromOffset(runData.uasset, currentImportOffset);

                currentImportOffset += HeaderOffsets.importDefSize;
            }
        }

        private static ImportData GetImportDataFromOffset(byte[] uasset, Int32 offset)
        {
            return new ImportData()
            {
                packageName = BitConverter.ToInt32(uasset, offset + HeaderOffsets.importPackageOffset),
                className = BitConverter.ToInt32(uasset, offset + HeaderOffsets.importClassOffset),
                outerIndex = BitConverter.ToInt32(uasset, offset + HeaderOffsets.importOuterIndexOffset),
                importName = BitConverter.ToInt32(uasset, offset + HeaderOffsets.importNameOffset)
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
        }

        [JsonObject]
        public class Config
        {
            public string offsetterPath = "";
            public string drgParserPath = "";
            public bool autoParseAfterSuccess = false;

            public bool enablePatternReadingHeuristica = false;
        }

        public record ImportData
        {
            public Int32 packageName;
            public Int32 className;
            public Int32 outerIndex;
            public Int32 importName;
        }

        public static class PatternFolders
        {
            public const string property = "PropertyPatterns";
            public const string body = "BodyPatterns";
            public const string structure = "StructPatterns";
        }
    }
}
