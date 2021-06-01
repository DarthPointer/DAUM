using System;
using System.Collections.Generic;
using System.IO;
using DRGOffSetterLib;

namespace daum
{
    public class ExportReadOperation : Operation
    {
        delegate void PatternElementProcesser(Span<byte> uasset, Span<byte> uexp, ReadingContext readingContext);

        private static int currentStructLevel = 0;
        private static string structLevelIdent = "  ";
        private static string currentStructLevelIdent = "";

        private static string endOfStructConfigName = "None";

        private static Dictionary<string, PatternElementProcesser> patternElementProcessers = new Dictionary<string, PatternElementProcesser>()
        {
            { "Size", SizePatternElementProcesser },
            { "SizeStart", SizeStartPatternElementProcesser },

            { "Skip", SkipPatternElementProcesser },

            { "Float32", FloatPatternElementProcesser },
            { "ObjectIndex", ObjectIndexPatternElementProcesser },

            { "NTPL", NoneTerminatedPropListPatternElementProcesser }
        };

        private static Stack<ReadingContext> machineState;

        public override string ExecuteAndGetOffSetterAgrs(ref Span<byte> span, List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            useStandardBackup = false;
            doneSomething = false;

            Int32 exportIndex = GetExportIndex(span, args).Value;

            Int32 fisrtExportOffset = DOLib.Int32FromSpanOffset(span, exportOffsetOffset);
            Int32 uexpStructureOffset = DOLib.Int32FromSpanOffset(span, fisrtExportOffset + (exportIndex - 1) * exportDefSize + exportSerialOffsetOffset)
                - DOLib.Int32FromSpanOffset(span, headerSizeOffset);
            Int32 uexpStructureSize = DOLib.Int32FromSpanOffset(span, fisrtExportOffset + (exportIndex - 1) * exportDefSize + exportSerialSizeOffset);

            ResetSLIString();
            machineState = new Stack<ReadingContext>();
            machineState.Push(new ReadingContext()
            {
                currentUexpOffset = uexpStructureOffset,
                declaredSize = uexpStructureSize,
                declaredSizeStartOffset = uexpStructureOffset,
                elementsLeft = -1,

                pattern = new List<string>() { "NTPL" },

                nextStep = NextStep.substructNameAndType,
                structCategory = StructCategory.export
            });

            StepsTilEndOfStruct(span, File.ReadAllBytes(Program.runData.uexpFileName));

            return "";
        }

        private static void StepsTilEndOfStruct(Span<byte> uasset, Span<byte> uexp)
        {
            while (Step(uasset, uexp));
        }

        private static bool Step(Span<byte> uasset, Span<byte> uexp)
        {
            ReadingContext readingContext = machineState.Peek();

            if (readingContext.nextStep == NextStep.substructNameAndType)
            {
                string substructName = FullNameString(uasset, uexp, readingContext.currentUexpOffset);
                readingContext.currentUexpOffset += 8;

                if (substructName == endOfStructConfigName)
                {
                    return false;
                }

                string typeName = FullNameString(uasset, uexp, readingContext.currentUexpOffset);
                readingContext.currentUexpOffset += 8;

                ReportExportContents("------------------------------");
                ReportExportContents($"{substructName} is {typeName}");

                machineState.Push(new ReadingContext()
                {
                    currentUexpOffset = readingContext.currentUexpOffset,
                    declaredSize = -1,
                    declaredSizeStartOffset = -1,
                    elementsLeft = -1,

                    pattern = Program.ParseCommandString(File.ReadAllText(Program.runData.toolDir + $"PropertyPatterns/{typeName}")),

                    nextStep = NextStep.applyPattern,
                    structCategory = StructCategory.nonExport
                });

                IncStructLevel();

                StepsTilEndOfStruct(uasset, uexp);

                DecStructLevel();

                readingContext.currentUexpOffset = machineState.Pop().currentUexpOffset;
                readingContext.nextStep = NextStep.applyPattern;

                return true;
            }

            if (readingContext.nextStep == NextStep.applyPattern)
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

        private static void SizePatternElementProcesser(Span<byte> uasset, Span<byte> uexp, ReadingContext readingContext)
        {
            readingContext.declaredSize = BitConverter.ToInt32(uexp.ToArray(), readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 4;
            readingContext.pattern.TakeArg();

            ReportExportContents($"Size: {readingContext.declaredSize}");
        }

        private static void SkipPatternElementProcesser(Span<byte> uasset, Span<byte> uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.currentUexpOffset += Int32.Parse(readingContext.pattern.TakeArg());
        }

        private static void SizeStartPatternElementProcesser(Span<byte> uasset, Span<byte> uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();
            
            readingContext.declaredSizeStartOffset = readingContext.currentUexpOffset;
        }

        private static void FloatPatternElementProcesser(Span<byte> uasset, Span<byte> uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            ReportExportContents($"Float Value: {BitConverter.ToSingle(uexp.ToArray(), readingContext.currentUexpOffset)}");

            readingContext.currentUexpOffset += 4;
        }

        private static void ObjectIndexPatternElementProcesser(Span<byte> uasset, Span<byte> uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 index = BitConverter.ToInt32(uexp.ToArray(), readingContext.currentUexpOffset);

            readingContext.currentUexpOffset += 4;

            string valueStr;

            if (index == 0)
            {
                valueStr = "null";
            }
            else if (index < 0)
            {
                valueStr = ImportByIndexFullNameString(uasset, uexp, index);
            }
            else
            {
                valueStr = ExportByIndexFullNameString(uasset, uexp, index);
            }

            ReportExportContents($"Object: {valueStr}");
        }

        private static void NoneTerminatedPropListPatternElementProcesser(Span<byte> uasset, Span<byte> uexp, ReadingContext readingContext)
        {
            if (FullNameString(uasset, uexp, readingContext.currentUexpOffset) != endOfStructConfigName)
            {
                readingContext.nextStep = NextStep.substructNameAndType;
            }
            else
            {
                readingContext.pattern.TakeArg();
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

        private static string FullNameString(Span<byte> uasset, Span<byte> tgtFile, Int32 uexpOffset)
        {
            Int32 nameIndex = BitConverter.ToInt32(tgtFile.ToArray(), uexpOffset);
            string nameString = NameString(uasset, nameIndex);

            Int32 nameAug = BitConverter.ToInt32(tgtFile.ToArray(), uexpOffset + 4);
            if (nameAug != 0)
            {
                nameString += $"_{nameAug}";
            }

            return nameString;
        }

        private static string ImportByIndexFullNameString(Span<byte> uasset, Span<byte> uexp, Int32 importIndex)
        {
            importIndex = -1*importIndex - 1;
            Int32 firstImportOffset = BitConverter.ToInt32(uasset.ToArray(), importOffsetOffset);
            return FullNameString(uasset, uasset, firstImportOffset + importIndex * importDefSize + importNameOffset);
        }

        private static string ExportByIndexFullNameString(Span<byte> uasset, Span<byte> uexp, Int32 exportIndex)
        {
            exportIndex = exportIndex - 1;
            Int32 firstExportOffset = BitConverter.ToInt32(uasset.ToArray(), exportOffsetOffset);
            return FullNameString(uasset, uasset, firstExportOffset + exportIndex * exportDefSize + exportNameOffset);
        }

        private static void ReportExportContents(string message)
        {
            Console.WriteLine(currentStructLevelIdent + message);
        }

        private class ReadingContext
        {
            public Int32 currentUexpOffset;
            public Int32 declaredSize;
            public Int32 declaredSizeStartOffset;
            public Int32 elementsLeft;

            public List<string> pattern;

            public NextStep nextStep;
            public StructCategory structCategory;
        }

        private enum NextStep
        {
            substructNameAndType,
            applyPattern
        }

        private enum StructCategory
        {
            export,
            nonExport
        }
    }
}
