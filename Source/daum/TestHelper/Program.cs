using System;
using System.Collections.Generic;

using DRGOffSetterLib;

namespace TestHelper
{
    class Program
    {
        public static Int32 exportCountOffset = 57;

        private static Dictionary<string, Action> commands = new Dictionary<string, Action>()
        {
            { "-ec", Commands.ExportCount }
        };

        public static RunData runData;

        static void Main(string[] args)
        {
            List<string> argList = new List<string>(args);

            runData = new RunData()
            {
                uassetFileName = argList.TakeArg(),
                pendingArgs = argList
            };

            string command = argList.TakeArg();
            commands[command]();
        }

        public record RunData
        {
            public string uassetFileName = "";
            public List<string> pendingArgs = new List<string>();
        }
    }
}
