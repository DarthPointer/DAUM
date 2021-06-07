using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using DRGOffSetterLib;

namespace daum
{
    public class ExportReadOperation : Operation
    {
        delegate void PatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext);

        private static int currentStructLevel = 0;
        private static string structLevelIdent = "  ";
        private static string currentStructLevelIdent = "";

        public const string endOfStructConfigName = "None";

        private const string arrayRepeatPatternElementName = "ArrayRepeat";
        private const string arrayRepeatEndPatternElementName = "ArrayRepeatEnd";
        private const string elementCountPatternElementName = "ElementCount";

        private const string scaledArrayElementsPatternElementName = "ScaledArrayElements";

        private const string structTypeHeuristicaPatternElementName = "structTypeHeuristica";

        public const string UnknownBytesPatternElementName = "UnknownBytes";
        private const string SkipIfPatternEndsPatternElementName = "SkipIfPatternEnds";

        private static Dictionary<string, PatternElementProcesser> patternElementProcessers = new Dictionary<string, PatternElementProcesser>()
        {
            { "Size", SizePatternElementProcesser },
            { "SizeStart", SizeStartPatternElementProcesser },

            { "Skip", SkipPatternElementProcesser },
            { UnknownBytesPatternElementName, UnknownBytesPatternElementProcesser },

            { "Int32", IntPatternElementProcesser },
            { "UInt32", UIntPatternElementProcesser },
            { "UInt64", UInt64PatternElementProcesser },
            { "ByteProp", BytePropPatternElementProcesser },
            { "Float32", FloatPatternElementProcesser },
            { "GUID", GUIDPatternElementProcesser },
            { "SPNTS", SizePrefixedNullTermStringPatternElementProcesser },

            { "Bool", BoolPatternElementProcesser },

            { "ObjectIndex", ObjectIndexPatternElementProcesser },
            { "Name", NamePatternElementProcesser },

            { "StructTypeNameIndex", StructTypeNameIndexPatternElementProcesser },
            { structTypeHeuristicaPatternElementName, StructTypeHeurisitcaPatternElementProcesser },

            { "ArrayElementTypeNameIndex", ArrayElementTypeNameIndexPatternElementProcesser },
            { elementCountPatternElementName, ElementCountPatternElementProcesser },
            { arrayRepeatPatternElementName, ArrayRepeatPatternElementProcesser },
            { "StructPropertyArrayType", StructPropertyArrayTypePatternElementProcesser },

            { "MapGeneratorTypes", MapGeneratorTypesPatternElementProcesser },

            { "TextPropertyDirtyHack", TextPropertyDirtyHackPatternElementProcesser },

            { SkipIfPatternEndsPatternElementName, SkipIfEndPatternElementProcesser },

            { "NTPL", NoneTerminatedPropListPatternElementProcesser }
        };

        private static Stack<ReadingContext> machineState;

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            useStandardBackup = false;
            doneSomething = false;

            Int32 exportIndex = GetExportIndex(Program.runData.uasset, args).Value;

            Console.WriteLine("--------------------");
            Console.WriteLine($"Export Index: {exportIndex}");
            Console.WriteLine("--------------------");

            Int32 fisrtExportOffset = BitConverter.ToInt32(Program.runData.uasset, exportOffsetOffset);
            Int32 uexpStructureOffset = BitConverter.ToInt32(Program.runData.uasset, fisrtExportOffset + (exportIndex - 1) * exportDefSize + exportSerialOffsetOffset)
                - BitConverter.ToInt32(Program.runData.uasset, headerSizeOffset);
            Int32 uexpStructureSize = BitConverter.ToInt32(Program.runData.uasset, fisrtExportOffset + (exportIndex - 1) * exportDefSize + exportSerialSizeOffset);

            ResetSLIString();
            machineState = new Stack<ReadingContext>();
            machineState.Push(new ReadingContext()
            {
                currentUexpOffset = uexpStructureOffset,
                declaredSize = uexpStructureSize,
                declaredSizeStartOffset = uexpStructureOffset,
                collectionElementCount = -1,

                pattern = new List<string>() { "NTPL" },

                nextStep = ReadingContext.NextStep.substructNameAndType,
                structCategory = ReadingContext.StructCategory.export
            });

            StepsTilEndOfStruct(Program.runData.uasset, File.ReadAllBytes(Program.runData.uexpFileName));

            return "";
        }

        private static void StepsTilEndOfStruct(byte[] uasset, byte[] uexp)
        {
            while (Step(uasset, uexp));
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

                machineState.Push(new ReadingContext()
                {
                    currentUexpOffset = readingContext.currentUexpOffset,
                    declaredSize = -1,
                    declaredSizeStartOffset = -1,
                    collectionElementCount = -1,

                    pattern = Program.GetPattern($"{Program.PatternFolders.property}/{typeName}"),

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

                if (patternElementProcessers.ContainsKey(readingContext.pattern[0]))
                {
                    patternElementProcessers[readingContext.pattern[0]](uasset, uexp, readingContext);
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

        private static void SizePatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.declaredSize = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 4;
            readingContext.pattern.TakeArg();

            ReportExportContents($"Size: {readingContext.declaredSize}");
        }

        private static void SkipPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.currentUexpOffset += Int32.Parse(readingContext.pattern.TakeArg());
        }

        private static void SizeStartPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();
            
            readingContext.declaredSizeStartOffset = readingContext.currentUexpOffset;

            ReportExportContents($"Size Start Offset: {readingContext.declaredSizeStartOffset}");
        }

        private static void IntPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Int Value: {BitConverter.ToInt32(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 4;
        }

        private static void UIntPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Int Value: {BitConverter.ToUInt32(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 4;
        }

        private static void UInt64PatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Int Value: {BitConverter.ToUInt64(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 8;
        }

        private static void BoolPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Bool Value: {BitConverter.ToBoolean(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 1;
        }

        private static void FloatPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Float Value: {BitConverter.ToSingle(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 4;
        }

        private static void GUIDPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string Quad(Int32 StartPos)
            {
                return BitConverter.ToString(uexp, StartPos + 3, 1) + BitConverter.ToString(uexp, StartPos + 2, 1) +
                    BitConverter.ToString(uexp, StartPos + 1, 1) + BitConverter.ToString(uexp, StartPos + 0, 1);
            }

            string guid1 = Quad(readingContext.currentUexpOffset + 0);
            string guid2 = Quad(readingContext.currentUexpOffset + 4);
            string guid3 = Quad(readingContext.currentUexpOffset + 8);
            string guid4 = Quad(readingContext.currentUexpOffset + 12);

            ReportExportContents($"GUID: {guid1}-{guid2}-{guid3}-{guid4}");

            readingContext.currentUexpOffset += 16;
        }

        private static void SizePrefixedNullTermStringPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string value = Program.SizePrefixedStringFromOffsetOffsetAdvance(uexp, ref readingContext.currentUexpOffset);

            ReportExportContents($"String: {value}");
        }

        private static void BytePropPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Bytes Value: {BitConverter.ToString(uexp, readingContext.currentUexpOffset, readingContext.declaredSize)}");

            readingContext.currentUexpOffset += readingContext.declaredSize;
        }

        private static void UnknownBytesPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 count = Int32.Parse(readingContext.pattern.TakeArg());

            ReportExportContents($"Unknown Bytes: {BitConverter.ToString(uexp, readingContext.currentUexpOffset, count)}");

            readingContext.currentUexpOffset += count;
        }

        private static void NamePatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Name: {FullNameString(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 8;
        }

        private static void StructTypeNameIndexPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();
            string typeName = FullNameString(uexp, readingContext.currentUexpOffset);
            ReportExportContents($"Structure Type: {typeName}");

            readingContext.currentUexpOffset += 8;

            if (Program.PatternExists($"{Program.PatternFolders.structure}/{typeName}"))
            {
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{typeName}"));
            }
            else if (Program.config.enablePatternReadingHeuristica)
            {
                readingContext.pattern.Add(structTypeHeuristicaPatternElementName);
                readingContext.pattern.Add(SkipIfPatternEndsPatternElementName);
            }
        }

        private static void StructTypeHeurisitcaPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.pattern.AddRange(PatternHeuristica.AssumedStructPattern(Program.runData, readingContext,
                out PatternHeuristica.HeuristicaStatus heuristicaStatus));

            switch (heuristicaStatus)
            {
                case PatternHeuristica.HeuristicaStatus.Failure:
                    ReportExportContents("Heuristica failed to give assumed structure pattern");
                    break;

                case PatternHeuristica.HeuristicaStatus.NonCriticalFailure:
                    ReportExportContents("Heuristica failed to find a meaningful pattern, boilerplate is provided");
                    break;

                case PatternHeuristica.HeuristicaStatus.Success:
                    ReportExportContents("Heuristica proposed a structure pattern, applying it");
                    break;
            }
        }

        private static void ArrayElementTypeNameIndexPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string typeName = FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            ReportExportContents($"Array Element Type: {typeName}");

            if (Program.PatternExists($"{Program.PatternFolders.body}/{typeName}"))
            {
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.body}/{typeName}"));
            }
        }

        private static void ElementCountPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 elementCount = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 4;

            readingContext.collectionElementCount = elementCount;

            ReportExportContents($"Elements Count: {elementCount}");
        }

        private static void ArrayRepeatPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 scaledElementSize;

            // Some element types have no context-free size determination apart from assumed elements total size and count.
            // Also ignore it if we have 0 elements because it is pointless and causes exception.
            if (readingContext.pattern[0] == scaledArrayElementsPatternElementName && readingContext.collectionElementCount != 0)
            {
                readingContext.pattern.TakeArg();
                scaledElementSize = (readingContext.declaredSizeStartOffset + readingContext.declaredSize -
                    readingContext.currentUexpOffset) /
                    (readingContext.collectionElementCount);
            }
            else
            {
                scaledElementSize = -1;
            }

            List<string> repeatedPattern = new List<string>();


            // Passing all the stuff to repeat in cycle which is all past ArrayRepeat and til ArrayRepeatEnd or end of pattern
            while (readingContext.pattern.Count > 0)
            {
                string element = readingContext.pattern.TakeArg();

                if (element == arrayRepeatEndPatternElementName) break;

                repeatedPattern.Add(element);
            }

            for (int i = 0; i < readingContext.collectionElementCount; i++)
            {
                ReportExportContents($"Element {i}");

                machineState.Push(new ReadingContext()
                {
                    currentUexpOffset = readingContext.currentUexpOffset,
                    pattern = new List<string>(repeatedPattern),

                    nextStep = ReadingContext.NextStep.applyPattern,
                    structCategory = ReadingContext.StructCategory.nonExport,

                    declaredSize = scaledElementSize
                });

                ExecutePushedReadingContext(uasset, uexp, readingContext);
            }
        }

        private static void StructPropertyArrayTypePatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string typeName = FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            ReportExportContents($"Element structure type: {typeName}");

            if (Program.PatternExists($"{Program.PatternFolders.structure}/{typeName}"))
            {
                readingContext.pattern.Add(arrayRepeatPatternElementName);
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{typeName}"));
            }
        }

        private static void MapGeneratorTypesPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string tKey = FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            string tVal = FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            ReportExportContents($"<{tKey}, {tVal}>");

            if (Program.PatternExists($"{Program.PatternFolders.body}/{tKey}") && Program.PatternExists($"{Program.PatternFolders.body}/{tVal}"))
            {
                List<string> keyPattern = Program.GetPattern($"{Program.PatternFolders.body}/{tKey}");
                List<string> valPattern = Program.GetPattern($"{Program.PatternFolders.body}/{tVal}");

                if (keyPattern.TakeArg() == arrayRepeatPatternElementName && valPattern.TakeArg() == arrayRepeatPatternElementName)
                {
                    readingContext.pattern.Add(elementCountPatternElementName);
                    readingContext.pattern.Add(arrayRepeatPatternElementName);
                    readingContext.pattern.AddRange(keyPattern);
                    readingContext.pattern.Add(arrayRepeatEndPatternElementName);

                    readingContext.pattern.Add(elementCountPatternElementName);
                    readingContext.pattern.Add(arrayRepeatPatternElementName);
                    readingContext.pattern.AddRange(keyPattern);
                    readingContext.pattern.AddRange(valPattern);
                }
            }
        }

        enum CharType
        {
            oneByte,
            unicode
        }

        private static void TextPropertyDirtyHackPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            //Epic Games probably like it when you have to fuck your brain with TexProperty having a body prefix which varies in SIZE between types.
            //I don't. I hope the author of that idea got a proper remedy.

            readingContext.currentUexpOffset = readingContext.declaredSizeStartOffset + readingContext.declaredSize;
            ReportExportContents("Text Property support is postponed. ETA depends on readability of UE shitcode.");
        }

        private static void ExecutePushedReadingContext(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            IncStructLevel();

            StepsTilEndOfStruct(uasset, uexp);

            DecStructLevel();

            readingContext.currentUexpOffset = machineState.Pop().currentUexpOffset;
            readingContext.nextStep = ReadingContext.NextStep.applyPattern;
        }

        private static void SkipIfEndPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            if (readingContext.pattern.Count == 0)
            {
                readingContext.currentUexpOffset = readingContext.declaredSizeStartOffset + readingContext.declaredSize;

                ReportExportContents("Skipping structure due to lack of pattern");
            }
        }

        private static void ObjectIndexPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

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

            ReportExportContents($"Object: {valueStr}");
        }

        private static void NoneTerminatedPropListPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            if (FullNameString(uexp, readingContext.currentUexpOffset) != endOfStructConfigName)
            {
                readingContext.nextStep = ReadingContext.NextStep.substructNameAndType;
            }
            else
            {
                readingContext.pattern.TakeArg();
                readingContext.currentUexpOffset += 8;
            }
        }

        private static void IncStructLevel()
        {
            currentStructLevel++;

            UpdateCurrentSLIString();
        }

        private static void DecStructLevel()
        {
            currentStructLevel--;

            UpdateCurrentSLIString();
        }

        private static void UpdateCurrentSLIString()
        {
            currentStructLevelIdent = "";

            for (int i = 0; i < currentStructLevel; i++)
            {
                currentStructLevelIdent += structLevelIdent;
            }
        }

        private static void ResetSLIString()
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

        private static string ImportByIndexFullNameString(byte[] uasset, byte[] uexp, Int32 importIndex)
        {
            importIndex = -1*importIndex - 1;
            Int32 firstImportOffset = BitConverter.ToInt32(uasset, OffsetConstants.importOffsetOffset);
            return FullNameString(uasset, firstImportOffset + importIndex * OffsetConstants.importDefSize + OffsetConstants.importNameOffset);
        }

        private static string ExportByIndexFullNameString(byte[] uasset, byte[] uexp, Int32 exportIndex)
        {
            exportIndex = exportIndex - 1;
            Int32 firstExportOffset = BitConverter.ToInt32(uasset, exportOffsetOffset);
            return FullNameString(uasset, firstExportOffset + exportIndex * exportDefSize + exportNameOffset);
        }

        private static void ReportExportContents(string message)
        {
            Console.WriteLine(currentStructLevelIdent + message);
        }

        public class ReadingContext
        {
            public Int32 currentUexpOffset;
            public Int32 declaredSize;
            public Int32 declaredSizeStartOffset;
            public Int32 collectionElementCount;

            public List<string> pattern;

            public NextStep nextStep;
            public StructCategory structCategory;

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
}
