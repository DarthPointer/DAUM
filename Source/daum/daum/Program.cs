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

        private static Dictionary<string, Operation> operations = new Dictionary<string, Operation>() {
            { "-n", new NameDefOperation() }
        };

        static void Main(string[] args)
        {
            configPath = Assembly.GetExecutingAssembly().Location;
            configPath = configPath.Substring(0, configPath.LastIndexOf('\\') + 1) + "Config.json";

            //Console.WriteLine(configPath);
            Config config = GetConfig();
            RunData runData = ProcessArgs(args);

            //Console.WriteLine(config.offsetterPath);

            if (runData.fileName.Length > 0)
            {
                bool runLoop = true;

                Span<byte> span = File.ReadAllBytes(runData.fileName);

                Console.WriteLine($"Drg Automation Utility for Modding welcomes you!");
                Console.WriteLine($"Entered interactive mode for file {runData.fileName}");
                Console.WriteLine();

                while (runLoop)
                {
                    try
                    {
                        runLoop = ProcessCommand(ref span, config, runData, out string offSetterCallArgs);

                        
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

        private static void WriteConfig(Config config)
        {
            File.WriteAllText(configPath, JsonConvert.SerializeObject(config, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented
            }));
        }

        private static RunData ProcessArgs(string[] args)
        {
            RunData runData = new RunData();

            if (args.Length > 0)
            {
                runData.fileName = args[0];
            }

            return runData;
        }

        private static bool ProcessCommand(ref Span<byte> span, Config config, RunData runData, out string offSetterCallArgs)
        {
            List<string> command = new List<string>(Console.ReadLine().Split(' '));

            if (command.Count > 0)
            {
                if (command[0].Length > 0)
                {
                    if (command[0] == exitCommand)
                    {
                        offSetterCallArgs = "";
                        return false;
                    }

                    string operationKey = command.TakeArg();

                    if (operations.ContainsKey(operationKey))
                    {
                        offSetterCallArgs = operations[operationKey].ExecuteAndGetOffSetterAgrs(ref span, command);

                        if (File.Exists(runData.fileName + ".daum")) File.Delete(runData.fileName + ".daum");
                        Directory.Move(runData.fileName, runData.fileName + ".daum");
                        File.WriteAllBytes(runData.fileName, span.ToArray());

                        Process offSetter = Process.Start(config.offsetterPath, runData.fileName + offSetterCallArgs + " -m -r");

                        offSetter.WaitForExit();
                        span = File.ReadAllBytes(runData.fileName);

                        Console.WriteLine("Done!");
                        return true;
                    }
                }
            }

            offSetterCallArgs = "";
            return true;
        }

        private record RunData
        {
            public string fileName = "";
        }

        [JsonObject]
        private class Config
        {
            public string offsetterPath = "";
        }
    }
}
