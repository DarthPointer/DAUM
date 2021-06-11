using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DRGOffSetterLib;

namespace daum
{
    class ExportChangeOperation : Operation
    {
        private static ECOCustomRunDara customRunDara;

        readonly static Dictionary<string, Action<List<string>>> replaceModeAdditionalKeys = new Dictionary<string, Action<List<string>>>()
        {
            { "-r", (args) => customRunDara.reportSearchSteps = true }
        };

        private static Dictionary<string, Func<List<string>, string>> modes = new Dictionary<string, Func<List<string>, string>>()
        {
            { "-r", ReplaceMode }
        };

        private static Dictionary<string, ExportParsingMachine.PatternElementProcesser> contextSearchProcessers =
            new Dictionary<string, ExportParsingMachine.PatternElementProcesser>()
            {
                { ExportParsingMachine.NTPLPatternElementName, NTPLContextSearcher },

                { ExportParsingMachine.sizePatternElementName, SizeContextSearcher },
                { ExportParsingMachine.sizeStartPatternElementName, SizeStartContextSearcher },

                { ExportParsingMachine.arrayElementTypeNameIndexPatternElementName, ArrayElementTypeNameIndexContextSearcher },
                { ExportParsingMachine.structTypeNameIndexPatternElementName, StructTypeNameIndexContextSearcher },
                { ExportParsingMachine.structPropertyArrayTypePatternElementName, StructPropertyArrayTypeContextSearcher },

                { ExportParsingMachine.arrayRepeatPatternElementName, ArrayRepeatContextSearcher},
                { ExportParsingMachine.elementCountPatternElementName, ElementCountContextSearcher },

                { skipContextPatternElementName, SkipContextContextSearcher },
                { ExportParsingMachine.skipPatternElementName, SkipContextSearcher },
                { ExportParsingMachine.skipIfPatternEndsPatternElementName, SkipIfEndContextSearcher },
                { ExportParsingMachine.skipIfPatternShorterThanPatternElemetnName, SkipIfPatternShorterThanContextSearcher },

                { ExportParsingMachine.GUIDPatternElementName, ValueContextSearcher },
                { ExportParsingMachine.float32PatternElementName, ValueContextSearcher }
            };

        private static Dictionary<string, PrimitiveTypeData> primitiveTypes = new Dictionary<string, PrimitiveTypeData>()
        {
            { ExportParsingMachine.GUIDPatternElementName, new PrimitiveTypeData() {
                reader = ExportParsingMachine.GUIDFromOffsetToString,
                writer = WriteGUID,
                ConstantSize = 16
            } },
            { ExportParsingMachine.float32PatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = BitConverter.ToSingle(Program.runData.uexp, offset).ToString(); offset += 4; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    BitConverter.GetBytes(float.Parse(value)).CopyTo(Program.runData.uexp, offset); offset +=4;
                },
                ConstantSize = 4
            } }
        };

        

        private const string skipContextPatternElementName = "SkipContext";

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            doneSomething = true;
            useStandardBackup = true;

            customRunDara = new ECOCustomRunDara();

            return modes[args.TakeArg()](args);
        }


        private static string ReplaceMode(List<string> args)
        {
            byte[] uasset = Program.runData.uasset;
            byte[] uexp = Program.runData.uexp;

            Int32 exportIndex = GetExportIndex(uasset, args).Value;
            Int32 exportDefOffset = BitConverter.ToInt32(uasset, exportOffsetOffset) + (exportIndex - 1) * exportDefSize;
            Int32 exportOffset = BitConverter.ToInt32(uasset, exportDefOffset + exportSerialOffsetOffset) -
                BitConverter.ToInt32(uasset, headerSizeOffset);
            Int32 exportSize = BitConverter.ToInt32(uasset, exportDefOffset + exportSerialSizeOffset);

            string targetContext = args.TakeArg();
            customRunDara.newValue = args.TakeArg();

            while (args.Count > 0)
            {
                replaceModeAdditionalKeys[args.TakeArg()](args);
            }

            ExportParsingMachine.ResetSLIString();

            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents(
                $"Searching for {targetContext} in export at {exportOffset} to set new value {customRunDara.newValue}");

            ExportParsingMachine.machineState = new Stack<ReadingContext>();
            ExportParsingMachine.machineState.Push(new ReadingContext()
            {
                currentUexpOffset = exportOffset,
                declaredSize = exportSize,
                declaredSizeStartOffset = exportOffset,
                collectionElementCount = -1,

                pattern = new List<string>() { "NTPL" },
                patternAlphabet = contextSearchProcessers,

                targetContext = new List<string>(targetContext.Split('/')),

                structCategory = ReadingContext.StructCategory.export
            });

            ExportParsingMachine.StepsTilEndOfStruct(Program.runData.uasset, Program.runData.uexp);

            return "";
        }

        private static void NTPLContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            string targetPropertyName = readingContext.targetContext[0];

            string substructName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            if (substructName == ExportParsingMachine.endOfStructConfigName)
            {
                readingContext.pattern.TakeArg();
                return;
            }

            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            if (customRunDara.reportSearchSteps)
            {
                ExportParsingMachine.ReportExportContents("------------------------------");
                ExportParsingMachine.ReportExportContents($"{substructName} is {typeName}");
            }

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

            if (substructName != targetPropertyName)
            {
                propertyPattern.Insert(propertyPattern.IndexOf(ExportParsingMachine.sizeStartPatternElementName) + 1,
                    skipContextPatternElementName);
            }

            List<string> targetSubContext = new List<string>(readingContext.targetContext);
            targetSubContext.RemoveAt(0);

            ExportParsingMachine.machineState.Push(new ReadingContext()
            {
                currentUexpOffset = readingContext.currentUexpOffset,
                declaredSize = -1,
                declaredSizeStartOffset = -1,
                collectionElementCount = -1,

                targetContext = targetSubContext,

                pattern = propertyPattern,
                patternAlphabet = readingContext.patternAlphabet,

                structCategory = ReadingContext.StructCategory.nonExport
            });

            ExportParsingMachine.ExecutePushedReadingContext(uasset, uexp, readingContext);
        }

        private static void SizeContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.declaredSize = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);
            readingContext.contextDeclaredSizeOffset = readingContext.currentUexpOffset;

            if (customRunDara.reportSearchSteps)
            {
                ExportParsingMachine.ReportExportContents($"Size is {readingContext.declaredSize}, stored at {readingContext.contextDeclaredSizeOffset}");
            }

            readingContext.currentUexpOffset += 4;
        }

        private static void SizeStartContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.declaredSizeStartOffset = readingContext.currentUexpOffset;
            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"Context Size start at {readingContext.currentUexpOffset}");
        }

        private static void ArrayElementTypeNameIndexContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"Array Element Type: {typeName}");

            if (Program.PatternExists($"{Program.PatternFolders.body}/{typeName}"))
            {
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.body}/{typeName}"));
            }
        }

        private static void StructTypeNameIndexContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();
            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"Structure Type: {typeName}");

            readingContext.currentUexpOffset += 8;

            if (Program.PatternExists($"{Program.PatternFolders.structure}/{typeName}"))
            {
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{typeName}"));
            }
        }

        private static void ArrayRepeatContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            bool thisArrayIsTarget = false;
            Int32 targetIndex = -1;

            if (readingContext.targetContext.Count > 2)
            {
                if (readingContext.targetContext[0] == "Array")
                {
                    Int32 skipsLeft = Int32.Parse(readingContext.targetContext[1]);
                    if (skipsLeft == 0)
                    {
                        thisArrayIsTarget = true;

                        readingContext.targetContext.TakeArg();
                        readingContext.targetContext.TakeArg();

                        targetIndex = Int32.Parse(readingContext.targetContext.TakeArg());
                    }
                    else
                    {
                        skipsLeft--;
                        readingContext.targetContext[1] = skipsLeft.ToString();
                    }
                }
            }

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
                if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"Element {i}");

                ExportParsingMachine.machineState.Push(new ReadingContext()
                {
                    currentUexpOffset = readingContext.currentUexpOffset,

                    pattern = new List<string>(repeatedPattern),
                    patternAlphabet = readingContext.patternAlphabet,

                    targetContext = thisArrayIsTarget && (i == targetIndex) ? readingContext.targetContext : new List<string>(){ "Pattern Blocker" },

                    structCategory = ReadingContext.StructCategory.nonExport,

                    declaredSize = scaledElementSize
                });

                ExportParsingMachine.ExecutePushedReadingContext(uasset, uexp, readingContext);
            }
        }

        private static void ElementCountContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.collectionElementCount = BitConverter.ToInt32(uexp, readingContext.currentUexpOffset);
            readingContext.contextCollectionElementCountOffset = readingContext.currentUexpOffset;

            readingContext.currentUexpOffset += 4;
        }

        private static void StructPropertyArrayTypeContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string typeName = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"Element structure type: {typeName}");

            if (Program.PatternExists($"{Program.PatternFolders.structure}/{typeName}"))
            {
                readingContext.pattern.Add(ExportParsingMachine.arrayRepeatPatternElementName);
                readingContext.pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{typeName}"));
            }
            // Heuristics not allowed yet for replacement
            //else if (Program.config.enablePatternReadingHeuristica && readingContext.collectionElementCount != 0)
            //{
            //    readingContext.pattern.Add(structTypeHeuristicaPatternElementName);
            //    readingContext.pattern.Add(SkipIfPatternShorterThanPatternElemetnName);
            //    readingContext.pattern.Add("2");
            //    readingContext.pattern.Add(ExportParsingMachine.arrayRepeatPatternElementName);
            //}
        }

        private static void SkipContextContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents("Skipping context");

            readingContext.pattern.Clear();
            readingContext.currentUexpOffset = readingContext.declaredSizeStartOffset + readingContext.declaredSize;
        }

        private static void SkipContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            Int32 skipLength = Int32.Parse(readingContext.pattern.TakeArg());
            readingContext.currentUexpOffset += skipLength;
        }

        private static void ValueContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            string primitiveTypeName = readingContext.pattern.TakeArg();
            PrimitiveTypeData primitiveType = primitiveTypes[primitiveTypeName];


            if (readingContext.targetContext.Count == 2)
            {
                Int32 skipsLeft = Int32.Parse(readingContext.targetContext[1]);

                if (primitiveTypeName == readingContext.targetContext[0])
                {
                    if (skipsLeft == 0)
                    {
                        if (customRunDara.reportSearchSteps)
                        {
                            ExportParsingMachine.ReportExportContents($"Found replacement target at {readingContext.currentUexpOffset}");
                        }
                        readingContext.pattern.Clear();
                        readingContext.targetContext.Clear();

                        primitiveType.writer(ref readingContext.currentUexpOffset, customRunDara.newValue);
                        return;
                    }
                    else
                    {
                        skipsLeft--;
                        readingContext.targetContext[1] = skipsLeft.ToString();
                    }
                }
            }

            primitiveType.skip(ref readingContext.currentUexpOffset);
            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"Skipping {primitiveTypeName} value");
        }

        private static void SkipIfEndContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            if (readingContext.pattern.Count == 0)
            {
                readingContext.currentUexpOffset = readingContext.declaredSizeStartOffset + readingContext.declaredSize;

                ExportParsingMachine.ReportExportContents("Skipping structure due to lack of pattern");
            }
        }

        private static void SkipIfPatternShorterThanContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
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



        private static void WriteGUID(ref Int32 offset, string value)
        {
            byte[] newVal = Guid.Parse(value).ToByteArray();

            newVal.CopyTo(Program.runData.uexp, offset);
            offset += 16;
        }



        private class ECOCustomRunDara
        {
            public bool reportSearchSteps = false;
            public string newValue = "";
        }

        private class PrimitiveTypeData
        {
            public delegate string Reader(ref Int32 offset);
            public delegate void Writer(ref Int32 offset, string value);
            public delegate void Skip(ref Int32 offset);

            public Int32 ConstantSize
            {
                set
                {
                    skip = (ref Int32 offset) => offset += value;
                }
            }

            public Reader reader;
            public Writer writer;
            public Skip skip;
        }
    }
}
