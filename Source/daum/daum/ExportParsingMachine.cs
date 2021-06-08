﻿using System;
using System.Collections.Generic;
using DRGOffSetterLib;

namespace daum
{
    static class ExportParsingMachine
    {
        public delegate void PatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext);

        public static Stack<ReadingContext> machineState;



        public const string endOfStructConfigName = "None";

        public const string NTPLPatternElementName = "NTPL";

        public const string skipPatternElementName = "Skip";
        public const string skipIfPatternEndsPatternElementName = "SkipIfPatternEnds";

        public const string sizeStartPatternElementName = "SizeStart";
        public const string sizePatternElementName = "Size";

        public const string arrayElementTypeNameIndexPatternElementName = "ArrayElementTypeNameIndex";
        public const string structTypeNameIndexPatternElementName = "StructTypeNameIndex";

        public const string float32PatternElementName = "Float32";
        public const string GUIDPatternElementName = "GUID";



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

        public static void ExecutePushedReadingContext(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            IncStructLevel();

            StepsTilEndOfStruct(uasset, uexp);

            DecStructLevel();

            readingContext.currentUexpOffset = machineState.Pop().currentUexpOffset;
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

        public static string GUIDFromOffsetToString(ref Int32 offset)
        {
            string Quad(Int32 StartPos)
            {
                return BitConverter.ToString(Program.runData.uexp, StartPos + 3, 1) + BitConverter.ToString(Program.runData.uexp, StartPos + 2, 1) +
                    BitConverter.ToString(Program.runData.uexp, StartPos + 1, 1) + BitConverter.ToString(Program.runData.uexp, StartPos + 0, 1);
            }

            string guid1 = Quad(offset + 0);
            string guid2 = Quad(offset + 4);
            string guid3 = Quad(offset + 8);
            string guid4 = Quad(offset + 12);

            offset += 16;

            return $"GUID: {guid1}-{guid2}-{guid3}-{guid4}";
        }
    }

    class ReadingContext
    {
        public Int32 currentUexpOffset;
        public Int32 declaredSize;
        public Int32 declaredSizeStartOffset;
        public Int32 collectionElementCount;

        public Int32 contextDeclaredSizeOffset;
        public Int32 contextCollectionElementCountOffset;

        public List<string> pattern;
        public List<string> targetContext;

        public StructCategory structCategory;

        public Dictionary<string, ExportParsingMachine.PatternElementProcesser> patternAlphabet;

        public enum StructCategory
        {
            export,
            nonExport
        }
    }
}
