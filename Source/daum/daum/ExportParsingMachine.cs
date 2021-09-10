using System;
using System.Collections.Generic;

namespace daum
{
    static class ExportParsingMachine
    {
        public delegate void PatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext);

        public static Stack<ReadingContext> machineState;

        public const string arrayRepeatPatternElementName = "ArrayRepeat";
        public const string arrayRepeatEndPatternElementName = "ArrayRepeatEnd";
        public const string elementCountPatternElementName = "ElementCount";

        public const string scaledArrayElementsPatternElementName = "ScaledArrayElements";

        public const string endOfStructConfigName = "None";

        public const string NTPLPatternElementName = "NTPL";

        public const string skipPatternElementName = "Skip";
        public const string skipIfPatternEndsPatternElementName = "SkipIfPatternEnds";
        public const string skipIfPatternShorterThanPatternElemetnName = "SkipIfPatternShorterThan";

        public const string sizeStartPatternElementName = "SizeStart";
        public const string sizePatternElementName = "Size";

        public const string arrayElementTypeNameIndexPatternElementName = "ArrayElementTypeNameIndex";
        public const string structTypeNameIndexPatternElementName = "StructTypeNameIndex";
        public const string structPropertyArrayTypePatternElementName = "StructPropertyArrayType";

        public const string propertyNameCopyPatternElementName = "PropertyNameCopy";
        public const string structPropertyNamePatternElementName = "StructPropertyName";

        public const string MGTPatternElementName = "MapGeneratorTypes";

        public const string float32PatternElementName = "Float32";
        public const string GUIDPatternElementName = "GUID";
        public const string SPNTSPatternElementName = "SPNTS";
        public const string boolPatternElementName = "Bool";
        public const string objectIndexPatternElementName = "ObjectIndex";
        public const string namePatternElementName = "Name";
        public const string uint16PatternElementName = "UInt16";
        public const string int32PatternElementName = "Int32";
        public const string uint32PatternElementName = "UInt32";
        public const string uint64PatternElementName = "UInt64";

        public const string TPDHPatternElementName = "TextPropertyDirtyHack";



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

            ReadingContext finishedContext = machineState.Pop();
            readingContext.currentUexpOffset = finishedContext.currentUexpOffset;

            if (finishedContext.contextReturnProcesser != null) finishedContext.contextReturnProcesser(readingContext, finishedContext);
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

        public static string ObjectByIndexFullNameString(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            Int32 index = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);

            readingContext.currentUexpOffset += 4;

            string valueStr;

            if (index == 0)
            {
                valueStr = "null";
            }
            else if (index < 0)
            {
                valueStr = $"Import:{ImportByIndexFullNameString(uasset, uexp, index)}";
            }
            else
            {
                valueStr = $"Export:{ExportByIndexFullNameString(uasset, uexp, index)}";
            }

            return valueStr;
        }

        private static string ImportByIndexFullNameString(byte[] uasset, byte[] uexp, Int32 importIndex)
        {
            importIndex = -1 * importIndex - 1;
            Int32 firstImportOffset = BitConverter.ToInt32(uasset, HeaderOffsets.importOffsetOffset);
            return ExportParsingMachine.FullNameString(uasset, firstImportOffset + importIndex * HeaderOffsets.importDefSize + HeaderOffsets.importNameOffset);
        }

        private static string ExportByIndexFullNameString(byte[] uasset, byte[] uexp, Int32 exportIndex)
        {
            exportIndex = exportIndex - 1;
            Int32 firstExportOffset = BitConverter.ToInt32(uasset, HeaderOffsets.exportOffsetOffset);
            return ExportParsingMachine.FullNameString(uasset, firstExportOffset + exportIndex * HeaderOffsets.exportDefSize + HeaderOffsets.exportNameOffset);
        }

        public static string GUIDFromUexpOffsetToString(ref Int32 offset)
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

        public static List<string> ParseContext(string contextString)
        {
            contextString = contextString.TrimEnd('/');
            if (contextString.Length > 0)
            {
                return new List<string>(contextString.Split('/'));
            }
            else
            {
                return new List<string>();
            }
        }
    }

    class ReadingContext
    {
        public delegate void ContextReturnProcesser(ReadingContext upperContext, ReadingContext finishedContext);

        public Int32 currentUexpOffset;
        public Int32 declaredSize;
        public Int32 declaredSizeStartOffset;
        public Int32 collectionElementCount;

        public Int32 sizeChange = 0;

        public Int32 contextDeclaredSizeOffset = 0;
        public Int32 contextCollectionElementCountOffset;

        public List<string> pattern;
        public List<string> targetContext;

        public StructCategory structCategory;

        public Dictionary<string, ExportParsingMachine.PatternElementProcesser> patternAlphabet;

        public ContextReturnProcesser contextReturnProcesser = null;

        public enum StructCategory
        {
            export,
            nonExport
        }
    }
}
