using System;
using System.Collections.Generic;

namespace daum.OffSetter
{
    class OffSetterExecution
    {
        private static List<OffSetterBlock> blocks = new List<OffSetterBlock> { new NamesMap(), new ImportsMap(), new ExportsMap(), new Exports() };
        private static Dictionary<string, int> modeKeysToBlockIndex = new Dictionary<string, int> {
            { "-n" , 0 },
            { "-i" , 1 },
            { "-edef" , 2 },
            { "-e" , 3 }
        };

        //static void Main(string[] args)
        //{
        //    RunData runData = ProcessArgs(args);

        //    if (runData.fileName.Length == 0)
        //    {
        //        Console.WriteLine("Need filename to operate!");
        //    }
        //    else
        //    {
        //        byte[] span = Program.runData.uasset;

        //        if (runData.modeCode != -1)
        //        {
        //            ExecuteMode(span, runData.modeCode, 0, runData.modeArgs, runData.mutedMode);
        //        }
        //        else
        //        {
        //            //ExecuteAllModes(span, runData.modeArgs, runData.mutedMode); // OffSetter Legacy supposed but aborted feature. F.
        //        }

        //        StoreResult(span, runData.fileName, runData.replaceMode);

        //        if (!runData.mutedMode) Console.WriteLine("Done!");
        //    }

        //    if (!runData.mutedMode) Console.ReadKey();
        //}

        public static void Execute(string args)
        {
            RunData runData = ProcessArgs(args);


            byte[] span = Program.runData.uasset;

            if (runData.modeCode != -1)
            {
                ExecuteMode(span, runData.modeCode, 0, runData.modeArgs, true);
            }
            else
            {
                //ExecuteAllModes(span, runData.modeArgs, runData.mutedMode); // OffSetter Legacy supposed but aborted feature. F.
            }

            StoreResult(span);
        }

        private static RunData ProcessArgs(string argsString)
        {
            List<string> args = Program.ParseCommandString(argsString);
            RunData result = new RunData();

            foreach (string arg in args)
            {
                if (modeKeysToBlockIndex.ContainsKey(arg))
                {
                    result.modeCode = modeKeysToBlockIndex[arg];
                }
                else
                {
                    result.modeArgs.Add(arg);
                }
            }

            return result;
        }

        private static int ExecuteMode(byte[] span, int modeIndex, int cumulativeOffset, List<string> modeArgs, bool mutedMode)
        {
            OffSetterBlock block = blocks[modeIndex];

            Dictionary<RequiredOffSettingData, Int32> args = new Dictionary<RequiredOffSettingData, int>();

            BlockAttribute blockAttribute = (BlockAttribute)Attribute.GetCustomAttribute(block.GetType(), typeof(BlockAttribute));

            string humanReadableBlockName = blockAttribute.humanReadableBlockName;
            RequiredOffSettingData blockArgs = blockAttribute.offSettingArguments;

            int sizeChange = 0;
            if ((blockArgs & RequiredOffSettingData.SizeChange) != RequiredOffSettingData.None)
            {
                sizeChange = int.Parse(UseOrRequestArg($"Input size change for {humanReadableBlockName}", modeArgs, mutedMode));

                args[RequiredOffSettingData.SizeChange] = sizeChange;
            }

            if ((blockArgs & RequiredOffSettingData.SizeChangeOffset) != RequiredOffSettingData.None)
            {
                int sizeChangeOffset = int.Parse(UseOrRequestArg($"Input size change offset for {humanReadableBlockName}", modeArgs, mutedMode));

                args[RequiredOffSettingData.SizeChangeOffset] = sizeChangeOffset + cumulativeOffset;
            }

            if ((blockArgs & RequiredOffSettingData.CountChange) != RequiredOffSettingData.None)
            {
                int countChange = int.Parse(UseOrRequestArg($"Input count change for {humanReadableBlockName}", modeArgs, mutedMode));

                args[RequiredOffSettingData.CountChange] = countChange;
            }

            block.OffSet(span, args);

            modeIndex++;

            while (modeIndex < blocks.Count)
            {
                blocks[modeIndex].PreviousBlocksOffSet(span, sizeChange);

                modeIndex++;
            }

            return cumulativeOffset + sizeChange;
        }

        private static string UseOrRequestArg(string requestMessage, List<string> modeArgs, bool mutedMode)
        {
            if (modeArgs.Count > 0)
            {
                return modeArgs.TakeArg();
            }
            else if (!mutedMode)
            {
                Console.WriteLine(requestMessage);
                return Console.ReadLine();
            }
            else
            {
                throw new Exception("Insufficient number of arguments passed on launch in muted mode");
            }
        }

        private static void StoreResult(byte[] span)
        {
            Program.runData.uasset = span;
        }

        private record RunData
        {
            //public bool mutedMode = false;
            //public bool replaceMode = false;
            //public string fileName = "";
            public int modeCode = -1;

            public List<string> modeArgs = new List<string>();
        }
    }
}
