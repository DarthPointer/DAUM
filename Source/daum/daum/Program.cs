using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Text;

using DRGOffSetterLib;

namespace daum
{
    class Program
    {
        private static string configPath;

        private static string exitCommand = "exit";
        private static string printNullConfigCommand = "nullConfig";
        private static string parseCommand = "parse";
        private static string fromScriptModeKey = "-s";

        private static Dictionary<string, Operation> operations = new Dictionary<string, Operation>() {
            { "-n", new NameDefOperation() },
            { "-i", new ImportDefOperation() },
            { "-edef", new ExportDefOperation() },

            { "-eread", new ExportReadOperation() },
            { "-echange", new ExportChangeOperation() },

            { "-f", new LoadFileOperation() },

            { "-o", new OffSetterCall() },
            { "PreloadPatterns", new PreloadPatternsOperation() }
        };

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

                        ProcessCommand(config, runData, parsedCommand, out bool _, out bool _);
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
                    ProcessCommand(config, runData, argList, out _, out _);
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
                        List<string> command = ParseCommandString(Console.ReadLine());
                        if (command[0].Length > 0)
                        {
                            runLoop = ProcessCommand(config, runData, command, out bool doneSomething, out bool parsed);
                            if (doneSomething)
                            {
                                if (config.autoParseAfterSuccess && !parsed) ParseFilesWithDRGPareser(config.drgParserPath, runData.uassetFileName);

                                Console.WriteLine("Done!");
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

        private static bool ProcessCommand(Config config, RunData runData, List<string> command, out bool doneSomething, out bool parsed)
        {
            parsed = false;

            if (command.Count > 0)
            {
                if (command[0].Length > 0)      // Count and length comparisons are pretty useless here as of v1.0 but let them stay. 
                {
                    if (command[0] == exitCommand)
                    {
                        doneSomething = false;
                        return false;
                    }

                    if (command[0] == printNullConfigCommand)
                    {
                        WriteConfig(new Config(), "NullConfig.json");
                        doneSomething = false;
                        return true;
                    }

                    if (command[0] == parseCommand)
                    {
                        ParseFilesWithDRGPareser(config.drgParserPath, runData.uassetFileName);
                        parsed = true;
                        doneSomething = false;
                        return true;
                    }

                    string operationKey = command.TakeArg();

                    if (operations.ContainsKey(operationKey))
                    {
                        string offSetterCallArgs = operations[operationKey].ExecuteAndGetOffSetterAgrs(command, out doneSomething, out bool useStandardBackup);

                        if (useStandardBackup)
                        {
                            if (File.Exists(runData.uassetFileName + ".daum")) File.Delete(runData.uassetFileName + ".daum");
                            Directory.Move(runData.uassetFileName, runData.uassetFileName + ".daum");
                            File.WriteAllBytes(runData.uassetFileName, Program.runData.uasset);

                            if (File.Exists(runData.uexpFileName + ".daum")) File.Delete(runData.uexpFileName + ".daum");
                            Directory.Move(runData.uexpFileName, runData.uexpFileName + ".daum");
                            File.WriteAllBytes(runData.uexpFileName, Program.runData.uexp);
                        }

                        if (offSetterCallArgs != "")
                        {
                            CallOffSetterWithArgs(offSetterCallArgs + " -m -r");
                            Program.runData.uasset = File.ReadAllBytes(runData.uassetFileName);
                        }

                        return true;
                    }
                }
            }

            doneSomething = false;

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

        public static void CallOffSetterWithArgs(string offSetterCallArgs, string tgtFile = null)
        {
            Process offSetter = Process.Start(config.offsetterPath, (tgtFile ?? runData.uassetFileName) + offSetterCallArgs);
            offSetter.WaitForExit();
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

            foreach (char c in command)
            {
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

            result.Add(buffer);

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
            Int32 nameCount = BitConverter.ToInt32(runData.uasset, OffsetConstants.nameCountOffset);
            Int32 currentNameOffset = BitConverter.ToInt32(runData.uasset, OffsetConstants.nameOffsetOffset);

            runData.nameMap = new string[nameCount];

            for (Int32 i = 0; i < nameCount; i++)
            {
                runData.nameMap[i] = SizePrefixedStringFromOffsetOffsetAdvance(runData.uasset, ref currentNameOffset);

                currentNameOffset += OffsetConstants.nameHashesSize;
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
            Int32 importCount = BitConverter.ToInt32(runData.uasset, OffsetConstants.importCountOffset);
            Int32 currentImportOffset = BitConverter.ToInt32(runData.uasset, OffsetConstants.importOffsetOffset);

            runData.importMap = new ImportData[importCount];

            for (Int32 i = 0; i < importCount; i++)
            {
                runData.importMap[i] = GetImportDataFromOffset(runData.uasset, currentImportOffset);

                currentImportOffset += OffsetConstants.importDefSize;
            }
        }

        private static ImportData GetImportDataFromOffset(byte[] uasset, Int32 offset)
        {
            return new ImportData()
            {
                packageName = BitConverter.ToInt32(uasset, offset + OffsetConstants.importPackageOffset),
                className = BitConverter.ToInt32(uasset, offset + OffsetConstants.importClassOffset),
                outerIndex = BitConverter.ToInt32(uasset, offset + OffsetConstants.importOuterIndexOffset),
                importName = BitConverter.ToInt32(uasset, offset + OffsetConstants.importNameOffset)
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
