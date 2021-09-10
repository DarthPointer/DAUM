using System;
using System.Collections.Generic;

namespace daum
{
    public class ExportReadOperation : Operation
    {
        private const string structTypeHeuristicaPatternElementName = "structTypeHeuristica";

        public const string UnknownBytesPatternElementName = "UnknownBytes";
        
        private static Dictionary<string, ExportParsingMachine.PatternElementProcesser> patternElementProcessers =
            new Dictionary<string, ExportParsingMachine.PatternElementProcesser>()
        {
            { ExportParsingMachine.sizePatternElementName, SizePatternElementProcesser },
            { ExportParsingMachine.sizeStartPatternElementName, SizeStartPatternElementProcesser },

            { ExportParsingMachine.skipPatternElementName, SkipPatternElementProcesser },
            { UnknownBytesPatternElementName, UnknownBytesPatternElementProcesser },
            { ExportParsingMachine.skipIfPatternEndsPatternElementName, SkipIfEndPatternElementProcesser },
            { ExportParsingMachine.skipIfPatternShorterThanPatternElemetnName, SkipIfPatternShorterThanPatternElementProcesser },

            { ExportParsingMachine.uint16PatternElementName, UInt16PatternElementProcesser },

            { ExportParsingMachine.int32PatternElementName, IntPatternElementProcesser },
            { ExportParsingMachine.uint32PatternElementName, UIntPatternElementProcesser },

            { ExportParsingMachine.uint64PatternElementName, UInt64PatternElementProcesser },

            { "ByteProp", BytePropPatternElementProcesser },
            { ExportParsingMachine.float32PatternElementName, FloatPatternElementProcesser },
            { ExportParsingMachine.GUIDPatternElementName, GUIDPatternElementProcesser },
            { ExportParsingMachine.SPNTSPatternElementName, SizePrefixedNullTermStringPatternElementProcesser },

            { ExportParsingMachine.boolPatternElementName, BoolPatternElementProcesser },

            { ExportParsingMachine.objectIndexPatternElementName, ObjectIndexPatternElementProcesser },
            { ExportParsingMachine.namePatternElementName, NamePatternElementProcesser },

            { ExportParsingMachine.structTypeNameIndexPatternElementName, StructTypeNameIndexPatternElementProcesser },
            { structTypeHeuristicaPatternElementName, StructTypeHeurisitcaPatternElementProcesser },

            { ExportParsingMachine.arrayElementTypeNameIndexPatternElementName, ArrayElementTypeNameIndexPatternElementProcesser },
            { ExportParsingMachine.elementCountPatternElementName, ElementCountPatternElementProcesser },
            { ExportParsingMachine.arrayRepeatPatternElementName, ArrayRepeatPatternElementProcesser },
            { ExportParsingMachine.structPropertyArrayTypePatternElementName, StructPropertyArrayTypePatternElementProcesser },

            { ExportParsingMachine.propertyNameCopyPatternElementName, Skip8Bytes },
            { ExportParsingMachine.structPropertyNamePatternElementName, Skip8Bytes },

            { ExportParsingMachine.MGTPatternElementName, MapGeneratorTypesPatternElementProcesser },

            { ExportParsingMachine.TPDHPatternElementName, TextPropertyDirtyHackPatternElementProcesser },

            { ExportParsingMachine.NTPLPatternElementName, NoneTerminatedPropListPatternElementProcesser }
        };

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            useStandardBackup = false;
            doneSomething = false;

            Int32 exportIndex = GetExportIndex(Program.runData.uasset, args).Value;

            Int32 fisrtExportOffset = BitConverter.ToInt32(Program.runData.uasset, HeaderOffsets.exportOffsetOffset);
            Int32 uexpStructureOffset = BitConverter.ToInt32(Program.runData.uasset, fisrtExportOffset + (exportIndex - 1)
                * HeaderOffsets.exportDefSize + HeaderOffsets.exportSerialOffsetOffset)
                - BitConverter.ToInt32(Program.runData.uasset, headerSizeOffset);

            Int32 uexpStructureSize = BitConverter.ToInt32(Program.runData.uasset, fisrtExportOffset + (exportIndex - 1) *
                HeaderOffsets.exportDefSize + HeaderOffsets.exportSerialSizeOffset);

            string exportObjectName = ExportParsingMachine.FullNameString(Program.runData.uasset, fisrtExportOffset + (exportIndex - 1) *
                HeaderOffsets.exportDefSize + HeaderOffsets.exportNameOffset);

            Console.WriteLine("--------------------");
            Console.WriteLine($"Export Index: {exportIndex}");
            Console.WriteLine($"Export Object Name {exportObjectName}");
            Console.WriteLine("--------------------");

            ExportParsingMachine.ResetSLIString();
            ExportParsingMachine.machineState = new Stack<ReadingContext>();
            ExportParsingMachine.machineState.Push(new ReadingContext()
            {
                currentUexpOffset = uexpStructureOffset,
                declaredSize = uexpStructureSize,
                declaredSizeStartOffset = uexpStructureOffset,
                collectionElementCount = -1,

                pattern = new List<string>() { ExportParsingMachine.NTPLPatternElementName },
                patternAlphabet = patternElementProcessers,

                structCategory = ReadingContext.StructCategory.export
            });

            ExportParsingMachine.StepsTilEndOfStruct(Program.runData.uasset, Program.runData.uexp);

            return "";
        }

        

        private static void SizePatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.declaredSize = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 4;
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Size: {readingContext.declaredSize}");
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

            ExportParsingMachine.ReportExportContents($"Size Start Offset: {readingContext.declaredSizeStartOffset}");
        }

        private static void UInt16PatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Int Value: {BitConverter.ToUInt16(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 2;
        }

        private static void IntPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Int Value: {BitConverter.ToInt32(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 4;
        }

        private static void UIntPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Int Value: {BitConverter.ToUInt32(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 4;
        }

        private static void UInt64PatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Int Value: {BitConverter.ToUInt64(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 8;
        }

        private static void BoolPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Bool Value: {BitConverter.ToBoolean(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 1;
        }

        private static void FloatPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Float Value: {BitConverter.ToSingle(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 4;
        }

        private static void GUIDPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();
            ExportParsingMachine.ReportExportContents(ExportParsingMachine.GUIDFromUexpOffsetToString(ref readingContext.currentUexpOffset));
        }

        private static void SizePrefixedNullTermStringPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string value = Program.SizePrefixedStringFromOffsetOffsetAdvance(uexp, ref readingContext.currentUexpOffset);

            ExportParsingMachine.ReportExportContents($"String: {value}");
        }

        private static void BytePropPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Bytes Value: {BitConverter.ToString(uexp, readingContext.currentUexpOffset, readingContext.declaredSize)}");

            readingContext.currentUexpOffset += readingContext.declaredSize;
        }

        private static void UnknownBytesPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 count = Int32.Parse(readingContext.pattern.TakeArg());

            ExportParsingMachine.ReportExportContents($"Unknown Bytes: {BitConverter.ToString(uexp, readingContext.currentUexpOffset, count)}");

            readingContext.currentUexpOffset += count;
        }

        private static void NamePatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ExportParsingMachine.ReportExportContents($"Name: {ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 8;
        }

        private static void StructTypeNameIndexPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();
            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            ExportParsingMachine.ReportExportContents($"Structure Type: {typeName}");

            readingContext.currentUexpOffset += 8;

            if (Program.PatternExists($"{Program.PatternFolders.structure}/{typeName}"))
            {
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{typeName}"));
            }
            else if (Program.config.enablePatternReadingHeuristica)
            {
                readingContext.pattern.Add(structTypeHeuristicaPatternElementName);
                readingContext.pattern.Add(ExportParsingMachine.skipIfPatternEndsPatternElementName);
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
                    ExportParsingMachine.ReportExportContents("Heuristica failed to give assumed structure pattern");
                    break;

                case PatternHeuristica.HeuristicaStatus.NonCriticalFailure:
                    ExportParsingMachine.ReportExportContents("Heuristica failed to find a meaningful pattern, boilerplate is provided");
                    break;

                case PatternHeuristica.HeuristicaStatus.Success:
                    ExportParsingMachine.ReportExportContents("Heuristica proposed a structure pattern, applying it");
                    break;
            }
        }

        private static void ArrayElementTypeNameIndexPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            ExportParsingMachine.ReportExportContents($"Array Element Type: {typeName}");

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

            ExportParsingMachine.ReportExportContents($"Elements Count: {elementCount}");
        }

        private static void ArrayRepeatPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 scaledElementSize;

            // Some element types have no context-free size determination apart from assumed elements total size and count.
            // Also ignore it if we have 0 elements because it is pointless and causes exception.
            if (readingContext.pattern[0] == ExportParsingMachine.scaledArrayElementsPatternElementName &&
                readingContext.collectionElementCount != 0)
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

                if (element == ExportParsingMachine.arrayRepeatEndPatternElementName) break;

                repeatedPattern.Add(element);
            }

            for (int i = 0; i < readingContext.collectionElementCount; i++)
            {
                ExportParsingMachine.ReportExportContents($"Element {i}");

                ExportParsingMachine.machineState.Push(new ReadingContext()
                {
                    currentUexpOffset = readingContext.currentUexpOffset,

                    pattern = new List<string>(repeatedPattern),
                    patternAlphabet = readingContext.patternAlphabet,

                    structCategory = ReadingContext.StructCategory.nonExport,

                    declaredSize = scaledElementSize
                });

                ExportParsingMachine.ExecutePushedReadingContext(uasset, uexp, readingContext);
            }
        }

        private static void StructPropertyArrayTypePatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            ExportParsingMachine.ReportExportContents($"Element structure type: {typeName}");

            if (Program.PatternExists($"{Program.PatternFolders.structure}/{typeName}"))
            {
                readingContext.pattern.Add(ExportParsingMachine.arrayRepeatPatternElementName);
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{typeName}"));
            }
            else if (Program.config.enablePatternReadingHeuristica && readingContext.collectionElementCount != 0)
            {
                readingContext.pattern.Add(structTypeHeuristicaPatternElementName);
                readingContext.pattern.Add(ExportParsingMachine.skipIfPatternShorterThanPatternElemetnName);
                readingContext.pattern.Add("2");
                readingContext.pattern.Add(ExportParsingMachine.arrayRepeatPatternElementName);
            }
        }

        private static void Skip8Bytes(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.currentUexpOffset += 8;
        }

        private static void MapGeneratorTypesPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string tKey = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            string tVal = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            ExportParsingMachine.ReportExportContents($"<{tKey}, {tVal}>");

            if (Program.PatternExists($"{Program.PatternFolders.body}/{tKey}") && Program.PatternExists($"{Program.PatternFolders.body}/{tVal}"))
            {
                List<string> keyPattern = Program.GetPattern($"{Program.PatternFolders.body}/{tKey}");
                List<string> valPattern = Program.GetPattern($"{Program.PatternFolders.body}/{tVal}");

                if (keyPattern.TakeArg() == ExportParsingMachine.arrayRepeatPatternElementName &&
                    valPattern.TakeArg() == ExportParsingMachine.arrayRepeatPatternElementName)
                {
                    readingContext.pattern.Add(ExportParsingMachine.elementCountPatternElementName);
                    readingContext.pattern.Add(ExportParsingMachine.arrayRepeatPatternElementName);
                    readingContext.pattern.AddRange(keyPattern);
                    readingContext.pattern.Add(ExportParsingMachine.arrayRepeatEndPatternElementName);

                    readingContext.pattern.Add(ExportParsingMachine.elementCountPatternElementName);
                    readingContext.pattern.Add(ExportParsingMachine.arrayRepeatPatternElementName);
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
            ExportParsingMachine.ReportExportContents("Text Property support is postponed. ETA depends on readability of UE shitcode.");
        }

        private static void SkipIfEndPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            if (readingContext.pattern.Count == 0)
            {
                readingContext.currentUexpOffset = readingContext.declaredSizeStartOffset + readingContext.declaredSize;

                ExportParsingMachine.ReportExportContents("Skipping structure due to lack of pattern");
            }
        }

        private static void SkipIfPatternShorterThanPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 minimalCountToProceed = Int32.Parse(readingContext.pattern.TakeArg());
            if (readingContext.pattern.Count < minimalCountToProceed)
            {
                readingContext.currentUexpOffset = readingContext.declaredSizeStartOffset + readingContext.declaredSize;
                readingContext.pattern.Clear();

                ExportParsingMachine.ReportExportContents("Skipping structure due to lack of pattern");
            }
        }

        private static void ObjectIndexPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();
            string valueStr = ExportParsingMachine.ObjectByIndexFullNameString(uasset, uexp, readingContext);

            ExportParsingMachine.ReportExportContents($"Object: {valueStr}");
        }

        private static void NoneTerminatedPropListPatternElementProcesser(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            string substructName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            if (substructName == ExportParsingMachine.endOfStructConfigName)
            {
                readingContext.pattern.TakeArg();
                return;
            }

            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            ExportParsingMachine.ReportExportContents("------------------------------");
            ExportParsingMachine.ReportExportContents($"{substructName} is {typeName}");

            List<string> propertyPattern;
            try
            {
                propertyPattern = Program.GetPattern($"{Program.PatternFolders.property}/{typeName}");
            }
            catch
            {
                ExportParsingMachine.ReportExportContents($"Failed to find a pattern for property type {typeName}");

                Int32 assumedSize = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);
                readingContext.currentUexpOffset += 8;

                ExportParsingMachine.ReportExportContents($"Assumed property size {assumedSize}");

                ExportParsingMachine.ReportExportContents($"Assumed property body {BitConverter.ToString(uexp, readingContext.currentUexpOffset + 1, assumedSize)}");

                throw;
            }

            ExportParsingMachine.machineState.Push(new ReadingContext()
            {
                currentUexpOffset = readingContext.currentUexpOffset,
                declaredSize = -1,
                declaredSizeStartOffset = -1,
                collectionElementCount = -1,

                pattern = propertyPattern,
                patternAlphabet = readingContext.patternAlphabet,

                structCategory = ReadingContext.StructCategory.nonExport
            });

            ExportParsingMachine.ExecutePushedReadingContext(uasset, uexp, readingContext);
        }
    }
}
