using System;
using System.Collections.Generic;
using DRGOffSetterLib;

namespace daum
{
    static class ExportParsingMachine
    {
        public delegate void PatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext);

        public static Stack<ReadingContext> machineState;



        public const string endOfStructConfigName = "None";



        private static int currentStructLevel = 0;
        private static string structLevelIdent = "  ";
        private static string currentStructLevelIdent = "";



        public static void StepsTilEndOfStruct(byte[] uasset, byte[] uexp)
        {
            while (Step(uasset, uexp)) ;
        }

        private static bool Step(byte[] uasset, byte[] uexp)
        {
            ReadingContext readingContext = machineState.Peek();

            if (readingContext.nextStep == ReadingContext.NextStep.substructNameAndType)
            {
                string substructName = FullNameString(uexp, readingContext.currentUexpOffset);
                readingContext.currentUexpOffset += 8;

                if (substructName == endOfStructConfigName)
                {
                    return false;
                }

                string typeName = FullNameString(uexp, readingContext.currentUexpOffset);
                readingContext.currentUexpOffset += 8;

                ReportExportContents("------------------------------");
                ReportExportContents($"{substructName} is {typeName}");

                List<string> propertyPattern;
                try
                {
                    propertyPattern = Program.GetPattern($"{Program.PatternFolders.property}/{typeName}");
                }
                catch
                {
                    ReportExportContents($"Failed to find a pattern for property type {typeName}");

                    Int32 assumedSize = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);
                    readingContext.currentUexpOffset += 8;

                    ReportExportContents($"Assumed property size {assumedSize}");

                    ReportExportContents($"Assumed property body {BitConverter.ToString(uexp, readingContext.currentUexpOffset + 1, assumedSize)}");

                    throw;
                }

                machineState.Push(new ReadingContext()
                {
                    currentUexpOffset = readingContext.currentUexpOffset,
                    declaredSize = -1,
                    declaredSizeStartOffset = -1,
                    collectionElementCount = -1,

                    pattern = propertyPattern,
                    patternAlphabet = readingContext.patternAlphabet,

                    nextStep = ReadingContext.NextStep.applyPattern,
                    structCategory = ReadingContext.StructCategory.nonExport
                });

                ExecutePushedReadingContext(uasset, uexp, readingContext);

                return true;
            }

            if (readingContext.nextStep == ReadingContext.NextStep.applyPattern)
            {
                if (readingContext.pattern.Count == 0)
                {
                    return false;
                }

                if (readingContext.patternAlphabet.ContainsKey(readingContext.pattern[0]))
                {
                    readingContext.patternAlphabet[readingContext.pattern[0]](uasset, uexp, readingContext);
                }
                else
                {
                    readingContext.currentUexpOffset += 4;
                    readingContext.pattern.TakeArg();
                }

                return true;
            }

            return false;
        }

        public static void ExecutePushedReadingContext(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            IncStructLevel();

            StepsTilEndOfStruct(uasset, uexp);

            DecStructLevel();

            readingContext.currentUexpOffset = machineState.Pop().currentUexpOffset;
            readingContext.nextStep = ReadingContext.NextStep.applyPattern;
        }



        public static void ReportExportContents(string message)
        {
            Console.WriteLine(currentStructLevelIdent + message);
        }

        public static void IncStructLevel()
        {
            currentStructLevel++;

            UpdateCurrentSLIString();
        }

        public static void DecStructLevel()
        {
            currentStructLevel--;

            UpdateCurrentSLIString();
        }

        public static void UpdateCurrentSLIString()
        {
            currentStructLevelIdent = "";

            for (int i = 0; i < currentStructLevel; i++)
            {
                currentStructLevelIdent += structLevelIdent;
            }
        }

        public static void ResetSLIString()
        {
            currentStructLevel = 0;
            currentStructLevelIdent = "";
        }

        public static string FullNameString(byte[] tgtFile, Int32 offset)
        {
            Int32 nameIndex = BitConverter.ToInt32(tgtFile, offset);
            string nameString = Program.runData.nameMap[nameIndex];

            Int32 nameAug = BitConverter.ToInt32(tgtFile, offset + 4);
            if (nameAug != 0)
            {
                nameString += $"_{nameAug}";
            }

            return nameString;
        }
    }

    class ReadingContext
    {
        public Int32 currentUexpOffset;
        public Int32 declaredSize;
        public Int32 declaredSizeStartOffset;
        public Int32 collectionElementCount;

        public List<string> pattern;

        public NextStep nextStep;
        public StructCategory structCategory;

        public Dictionary<string, ExportParsingMachine.PatternElementProcesser> patternAlphabet;

        public enum NextStep
        {
            substructNameAndType,
            applyPattern
        }

        public enum StructCategory
        {
            export,
            nonExport
        }
    }
}
