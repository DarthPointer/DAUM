using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using DRGOffSetterLib;
using System.Reflection;

namespace daum
{
    class Program
    {
        private static string configPath;

        private static string exitCommand = "exit";
        private static string printNullConfigCommand = "nullConfig";
        private static string parseCommand = "parse";
        private static string fromScriptModeKey = "-s";
        private static string setTargetFileScriptMode = "-f";

        private static Dictionary<string, Operation> operations = new Dictionary<string, Operation>() {
            { "-n", new NameDefOperation() },
            { "-i", new ImportDefOperation() },
            { "-edef", new ExportDefOperation() },
            { "-o", new OffSetterCall() },
            { "-eread", new ExportReadOperation() }
        };

        public static RunData runData;
        public static Config config;

        static void Main(string[] args)
        {
            string toolDir = Assembly.GetExecutingAssembly().Location;
            toolDir = toolDir.Substring(0, toolDir.LastIndexOf('\\') + 1);

            configPath = toolDir + "Config.json";

            config = GetConfig();

            List<string> argList = new List<string>(args);

            if (argList[0] == fromScriptModeKey)
            {
                argList.TakeArg();

                runData = new RunData()
                {
                    toolDir = toolDir,
                };

                string scriptFile = argList.TakeArg();
                string[] commands = File.ReadAllLines(scriptFile);
                Span<byte> uasset = null;

                foreach (string command in commands)
                {
                    try
                    {
                        List<string> parsedCommand = ParseCommandString(command);

                        if (parsedCommand[0] == setTargetFileScriptMode)
                        {
                            parsedCommand.TakeArg();

                            string uassetFileName = parsedCommand.TakeArg();

                            runData.uassetFileName = uassetFileName;
                            runData.uexpFileName = uassetFileName.Substring(0, uassetFileName.LastIndexOf('.') + 1) + "uexp";
                            runData.fileDir = uassetFileName.Substring(0, uassetFileName.LastIndexOf('\\') + 1);

                            uasset = File.ReadAllBytes(uassetFileName);

                            Console.WriteLine("--------------------");
                            Console.WriteLine();
                            Console.WriteLine(runData.uassetFileName);
                            Console.WriteLine();
                            Console.WriteLine("--------------------");
                        }
                        else
                        {
                            ProcessCommand(ref uasset, config, runData, parsedCommand, out bool _, out bool _);
                        }
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

            runData = new RunData()
            {
                toolDir = toolDir,
                uassetFileName = fileName,
                uexpFileName = fileName.Substring(0, fileName.LastIndexOf('.') + 1) + "uexp",
                fileDir = fileName.Substring(0, fileName.LastIndexOf('\\') + 1)
            };


            if (runData.uassetFileName.Length > 0)
            {
                Span<byte> span = File.ReadAllBytes(runData.uassetFileName);

                if (argList.Count > 0)
                {
                    ProcessCommand(ref span, config, runData, argList, out _, out _);
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
                            runLoop = ProcessCommand(ref span, config, runData, command, out bool doneSomething, out bool parsed);
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

        private static bool ProcessCommand(ref Span<byte> span, Config config, RunData runData, List<string> command, out bool doneSomething, out bool parsed)
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
                        string offSetterCallArgs = operations[operationKey].ExecuteAndGetOffSetterAgrs(ref span, command, out doneSomething, out bool useStandardBackup);

                        if (useStandardBackup)
                        {
                            if (File.Exists(runData.uassetFileName + ".daum")) File.Delete(runData.uassetFileName + ".daum");
                            Directory.Move(runData.uassetFileName, runData.uassetFileName + ".daum");
                            File.WriteAllBytes(runData.uassetFileName, span.ToArray());
                        }

                        if (offSetterCallArgs != "")
                        {
                            CallOffSetterWithArgs(offSetterCallArgs + " -m -r");
                        }

                        span = File.ReadAllBytes(runData.uassetFileName);

                        return true;
                    }
                }
            }

            doneSomething = false;

            return true;
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


        public record RunData
        {
            public string uassetFileName = "";
            public string uexpFileName = "";
            public string fileDir = "";
            public string toolDir = "";
        }

        [JsonObject]
        public class Config
        {
            public string offsetterPath = "";
            public string drgParserPath = "";
            public bool autoParseAfterSuccess = false;
        }
    }
}
