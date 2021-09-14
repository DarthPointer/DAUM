using System;
using System.Collections.Generic;
using System.Text;

namespace daum
{
    partial class ExportChangeOperation : Operation
    {
        private static ECOCustomRunDara customRunDara;

        private readonly static Dictionary<string, Action<List<string>>> replaceModeAdditionalKeys = new Dictionary<string, Action<List<string>>>()
        {
            { "-r", (args) => customRunDara.reportSearchSteps = true },
            { "-utf16", (args) => customRunDara.newStringValEncoding = ECOCustomRunDara.NewStringValEncoding.utf16 },
            { "-nullstr", (args) => customRunDara.nullString = true }
        };

        private readonly static Dictionary<string, ECOCustomRunDara.Mode> modes = new Dictionary<string, ECOCustomRunDara.Mode>()
        {
            { "-a", ECOCustomRunDara.Mode.add },
            { "-r", ECOCustomRunDara.Mode.rewrite },
            { "-d", ECOCustomRunDara.Mode.delete }
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
                { ExportParsingMachine.MGTPatternElementName, MapGeneratorTypesContextSearcher },

                { ExportParsingMachine.propertyNameCopyPatternElementName, Skip8Bytes },
                { ExportParsingMachine.structPropertyNamePatternElementName, Skip8Bytes },

                { ExportParsingMachine.arrayRepeatPatternElementName, ArrayRepeatContextSearcher},
                { ExportParsingMachine.elementCountPatternElementName, ElementCountContextSearcher },

                { skipContextPatternElementName, SkipContextContextSearcher },
                { ExportParsingMachine.skipPatternElementName, SkipContextSearcher },
                { ExportParsingMachine.skipIfPatternEndsPatternElementName, SkipIfEndContextSearcher },
                { ExportParsingMachine.skipIfPatternShorterThanPatternElemetnName, SkipIfPatternShorterThanContextSearcher },

                { ExportParsingMachine.GUIDPatternElementName, ValueContextSearcher },
                { ExportParsingMachine.float32PatternElementName, ValueContextSearcher },
                { ExportParsingMachine.SPNTSPatternElementName, ValueContextSearcher },
                { ExportParsingMachine.boolPatternElementName, ValueContextSearcher },
                { ExportParsingMachine.namePatternElementName, ValueContextSearcher },
                { ExportParsingMachine.objectIndexPatternElementName, ValueContextSearcher },
                { ExportParsingMachine.uint16PatternElementName, ValueContextSearcher },
                { ExportParsingMachine.int32PatternElementName, ValueContextSearcher },
                { ExportParsingMachine.uint32PatternElementName, ValueContextSearcher },
                { ExportParsingMachine.uint64PatternElementName, ValueContextSearcher },

                { ExportParsingMachine.TPDHPatternElementName, TextPropertyDirtyHackContextSearcher }
            };

        private static Dictionary<string, PrimitiveTypeData> primitiveTypes = new Dictionary<string, PrimitiveTypeData>()
        {
            { ExportParsingMachine.GUIDPatternElementName, new PrimitiveTypeData() {
                reader = ExportParsingMachine.GUIDFromUexpOffsetToString,
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
            } },

            { ExportParsingMachine.SPNTSPatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    return Program.SizePrefixedStringFromOffsetOffsetAdvance(Program.runData.uexp, ref offset);
                },
                writer = (ref Int32 offset, string value) =>
                {
                    Int32 initialStringSize = BitConverter.ToInt32(Program.runData.uexp, offset);
                    Int32 newStringSize;

                    Program.runData.uexp = Remove(Program.runData.uexp, offset+4, initialStringSize < 0 ? initialStringSize*-2 : initialStringSize);

                    byte[] insert;
                    if (customRunDara.nullString)
                    {
                        insert = new byte[0];
                        newStringSize = 0;
                        DAUMLib.WriteInt32IntoOffset(Program.runData.uexp, newStringSize, offset);
                    }
                    else if (customRunDara.newStringValEncoding == ECOCustomRunDara.NewStringValEncoding.utf8)
                    {
                        insert = Encoding.UTF8.GetBytes(value);
                        insert = Insert(insert, new byte[]{0}, insert.Length);
                        newStringSize = insert.Length;
                        DAUMLib.WriteInt32IntoOffset(Program.runData.uexp, newStringSize, offset);
                    }
                    else //if (customRunDara.newStringValEncoding == ECOCustomRunDara.NewStringValEncoding.utf16)
                    {
                        insert = Encoding.Unicode.GetBytes(value);
                        insert = Insert(insert, new byte[]{0, 0}, insert.Length);
                        newStringSize = insert.Length;
                        DAUMLib.WriteInt32IntoOffset(Program.runData.uexp, newStringSize/-2, offset);
                    }

                    Program.runData.uexp = Insert(Program.runData.uexp, insert, offset + 4);
                    customRunDara.sizeChange = newStringSize - initialStringSize;
                    ExportParsingMachine.machineState.Peek().sizeChange = customRunDara.sizeChange;

                    offset += 4 + newStringSize;
                },
                skip = (ref Int32 offset) =>
                {
                    Int32 count = BitConverter.ToInt32(Program.runData.uexp, offset);
                    offset +=4;
                    offset += count > 0 ? count : -2 * count;
                }
            } },
            { ExportParsingMachine.boolPatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = BitConverter.ToBoolean(Program.runData.uexp, offset).ToString(); offset++; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    BitConverter.GetBytes(bool.Parse(value)).CopyTo(Program.runData.uexp, offset); offset++;
                },
                ConstantSize = 1
            } },
            { ExportParsingMachine.uint16PatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = BitConverter.ToUInt16(Program.runData.uexp, offset).ToString(); offset += 2; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    BitConverter.GetBytes(UInt16.Parse(value)).CopyTo(Program.runData.uexp, offset); offset += 2;
                },
                ConstantSize = 2
            } },
            { ExportParsingMachine.int32PatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = BitConverter.ToInt32(Program.runData.uexp, offset).ToString(); offset += 4; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    BitConverter.GetBytes(Int32.Parse(value)).CopyTo(Program.runData.uexp, offset); offset += 4;
                },
                ConstantSize = 4
            } },
            { ExportParsingMachine.uint32PatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = BitConverter.ToUInt32(Program.runData.uexp, offset).ToString(); offset += 4; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    BitConverter.GetBytes(UInt32.Parse(value)).CopyTo(Program.runData.uexp, offset); offset += 4;
                },
                ConstantSize = 4
            } },
            { ExportParsingMachine.uint64PatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = BitConverter.ToUInt64(Program.runData.uexp, offset).ToString(); offset += 8; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    BitConverter.GetBytes(UInt64.Parse(value)).CopyTo(Program.runData.uexp, offset); offset += 8;
                },
                ConstantSize = 8
            } },
            { ExportParsingMachine.namePatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = ExportParsingMachine.FullNameString(Program.runData.uexp, offset); offset += 8; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    List<string> args = Program.ParseCommandString(value);
                    Int32 nameIndex = GetNameIndex(args).Value;
                    Int32 nameAug = Int32.Parse(args.TakeArg());
                    BitConverter.GetBytes(nameIndex).CopyTo(Program.runData.uexp, offset); offset += 4;
                    BitConverter.GetBytes(nameAug).CopyTo(Program.runData.uexp, offset); offset += 4;
                },
                ConstantSize = 8
            } },
            { ExportParsingMachine.objectIndexPatternElementName, new PrimitiveTypeData()
            {
                reader = (ref Int32 offset) =>
                {
                    string result = ExportParsingMachine.FullNameString(Program.runData.uexp, offset); offset += 4; return result;
                },
                writer = (ref Int32 offset, string value) =>
                {
                    Int32 objectIndex = GetImportExportIndex(Program.runData.uasset, Program.ParseCommandString(value)).Value;
                    BitConverter.GetBytes(objectIndex).CopyTo(Program.runData.uexp, offset); offset += 4;
                },
                ConstantSize = 4
            } }
        };

        

        private const string skipContextPatternElementName = "SkipContext";

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            customRunDara = new ECOCustomRunDara();

            string offsetterArgs = Execute(args);

            doneSomething = customRunDara.taskComplete;
            useStandardBackup = customRunDara.taskComplete;

            if (!customRunDara.taskComplete)
            {
                Console.WriteLine("Export Change Operation did not complete its task. Probably, a wrong target context was specified.");
            }

            return offsetterArgs;
        }


        private static string Execute(List<string> args)
        {
            customRunDara.mode = modes[args.TakeArg()];

            byte[] uasset = Program.runData.uasset;
            byte[] uexp = Program.runData.uexp;

            Int32 exportIndex = GetExportIndex(uasset, args).Value;
            Int32 exportDefOffset = BitConverter.ToInt32(uasset, Program.runData.headerOffsets.exportOffsetOffset) + (exportIndex - 1) * Program.runData.headerOffsets.exportDefSize;

            customRunDara.changedExportSerialOffset = BitConverter.ToInt32(uasset, exportDefOffset + Program.runData.headerOffsets.exportSerialOffsetOffset);

            Int32 exportOffset = BitConverter.ToInt32(uasset, exportDefOffset + Program.runData.headerOffsets.exportSerialOffsetOffset) -
                BitConverter.ToInt32(uasset, Program.runData.headerOffsets.totalHeaderSizeOffset);
            Int32 exportSize = BitConverter.ToInt32(uasset, exportDefOffset + Program.runData.headerOffsets.exportSerialSizeOffset);

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

                targetContext = ExportParsingMachine.ParseContext(targetContext),

                structCategory = ReadingContext.StructCategory.export,

                contextReturnProcesser = ContextReturnProcesser
            });

            ExportParsingMachine.StepsTilEndOfStruct(Program.runData.uasset, Program.runData.uexp);

            return $" -e {customRunDara.sizeChange} {customRunDara.changedExportSerialOffset}";
        }

        private static void NTPLContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            if (customRunDara.mode == ECOCustomRunDara.Mode.rewrite || readingContext.targetContext.Count > 0)
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
                if (substructName == targetPropertyName && readingContext.targetContext.Count == 1 && customRunDara.mode == ECOCustomRunDara.Mode.delete)
                {
                    customRunDara.deleteStartOffset = readingContext.currentUexpOffset - 16;
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

                    structCategory = ReadingContext.StructCategory.nonExport,

                    contextReturnProcesser = ContextReturnProcesser
                });

                ExportParsingMachine.ExecutePushedReadingContext(uasset, uexp, readingContext);

                if (substructName == targetPropertyName && readingContext.targetContext.Count == 1 && customRunDara.mode == ECOCustomRunDara.Mode.delete)
                {
                    customRunDara.deleteEndOffset = readingContext.currentUexpOffset;

                    readingContext.sizeChange = customRunDara.deleteStartOffset - customRunDara.deleteEndOffset;
                    customRunDara.sizeChange = readingContext.sizeChange;

                    ExportParsingMachine.machineState.Peek().sizeChange = customRunDara.sizeChange;

                    Program.runData.uexp = Remove(uexp, customRunDara.deleteStartOffset, -customRunDara.sizeChange);

                    readingContext.pattern.Clear();
                    readingContext.targetContext.Clear();

                    customRunDara.taskComplete = true;
                }
            }
            else if (customRunDara.mode == ECOCustomRunDara.Mode.add && readingContext.targetContext.Count == 0)
            {
                byte[] insert = GenerateInsert(Program.ParseCommandString(customRunDara.newValue), new List<string>()
                { propertyNamePatternElementName, propertyTypePatternElementName });

                readingContext.sizeChange = insert.Length;
                customRunDara.sizeChange = insert.Length;

                ExportParsingMachine.machineState.Peek().sizeChange = customRunDara.sizeChange;

                Program.runData.uexp = Insert(uexp, insert, readingContext.currentUexpOffset);

                readingContext.pattern.Clear();
                readingContext.targetContext.Clear();

                customRunDara.taskComplete = true;
            }
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
            bool thisArrayIsExtended = false;
            Int32 deleteElementIndex = -1;
            Int32 targetIndex = -1;

            if (readingContext.targetContext.Count > 1)
            {
                if (readingContext.targetContext[0] == "Array")
                {
                    Int32 skipsLeft = Int32.Parse(readingContext.targetContext[1]);
                    if (skipsLeft == 0)
                    {
                        if (readingContext.targetContext.Count == 3 && customRunDara.mode == ECOCustomRunDara.Mode.delete)
                        {
                            readingContext.targetContext.TakeArg();
                            readingContext.targetContext.TakeArg();
                            deleteElementIndex = Int32.Parse(readingContext.targetContext.TakeArg());
                        }
                        else if (readingContext.targetContext.Count == 2 && customRunDara.mode == ECOCustomRunDara.Mode.add)
                        {
                            thisArrayIsExtended = true;

                            readingContext.targetContext.TakeArg();
                            readingContext.targetContext.TakeArg();
                        }
                        else if (readingContext.targetContext.Count > 2)
                        {
                            thisArrayIsTarget = true;

                            readingContext.targetContext.TakeArg();
                            readingContext.targetContext.TakeArg();

                            targetIndex = Int32.Parse(readingContext.targetContext.TakeArg());

                            if (targetIndex < 0)
                            {
                                targetIndex = readingContext.collectionElementCount + targetIndex;
                            }
                        }
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
                if (customRunDara.taskComplete) break;

                if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"Element {i}");

                ExportParsingMachine.machineState.Push(new ReadingContext()
                {
                    currentUexpOffset = readingContext.currentUexpOffset,

                    pattern = new List<string>(repeatedPattern),
                    patternAlphabet = readingContext.patternAlphabet,

                    targetContext = thisArrayIsTarget && (i == targetIndex) ? readingContext.targetContext : new List<string>(){ "Pattern Blocker" },

                    structCategory = ReadingContext.StructCategory.nonExport,

                    declaredSize = scaledElementSize,

                    contextReturnProcesser = ContextReturnProcesser
                });

                if (i == deleteElementIndex)
                {
                    customRunDara.deleteStartOffset = readingContext.currentUexpOffset;
                }

                ExportParsingMachine.ExecutePushedReadingContext(uasset, uexp, readingContext);

                if (i == deleteElementIndex)
                {
                    customRunDara.deleteEndOffset = readingContext.currentUexpOffset;

                    customRunDara.sizeChange = customRunDara.deleteStartOffset - customRunDara.deleteEndOffset;
                    readingContext.sizeChange = customRunDara.sizeChange;

                    DAUMLib.AddToInt32ByOffset(uexp, -1, readingContext.contextCollectionElementCountOffset);

                    Program.runData.uexp = Remove(uexp, customRunDara.deleteStartOffset, -customRunDara.sizeChange);

                    customRunDara.taskComplete = true;
                    readingContext.pattern.Clear();
                    readingContext.targetContext.Clear();

                    break;
                }
            }

            if (thisArrayIsExtended)
            {
                byte[] insert = GenerateInsert(Program.ParseCommandString(customRunDara.newValue), repeatedPattern);
                customRunDara.sizeChange = insert.Length;
                readingContext.sizeChange = customRunDara.sizeChange;

                DAUMLib.AddToInt32ByOffset(uexp, 1, readingContext.contextCollectionElementCountOffset);

                Program.runData.uexp = Insert(uexp, insert, readingContext.currentUexpOffset);

                customRunDara.taskComplete = true;
                readingContext.pattern.Clear();
                readingContext.targetContext.Clear();
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

        private static void MapGeneratorTypesContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            string tKey = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            string tVal = ExportParsingMachine.FullNameString(uexp, readingContext.currentUexpOffset);
            readingContext.currentUexpOffset += 8;

            if (customRunDara.reportSearchSteps) ExportParsingMachine.ReportExportContents($"<{tKey}, {tVal}>");

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

        private static void Skip8Bytes(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            readingContext.pattern.TakeArg();

            readingContext.currentUexpOffset += 8;
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


            if (readingContext.targetContext.Count == 2 && customRunDara.mode == ECOCustomRunDara.Mode.rewrite)
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

                        customRunDara.taskComplete = true;
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

        private static void TextPropertyDirtyHackContextSearcher(byte[] uasset, byte[] uexp, ReadingContext readingContext)
        {
            /* TextProperty pattern has to be `Size 0 Skip 1 SizeStart TPDH`
             * Tgt context should be TextProperty/0/X/Type/0
             * Pattern gets `Skip X Type`.
            */
            readingContext.pattern.TakeArg();

            if (readingContext.targetContext.Count > 0)
            {
                if (readingContext.targetContext[0] == "TextProperty")
                {
                    Int32 skipsLeft = Int32.Parse(readingContext.targetContext[1]);

                    if (skipsLeft == 0)
                    {
                        if (customRunDara.reportSearchSteps)
                        {
                            ExportParsingMachine.ReportExportContents($"Applying offset bruteforce for TextProperty at {readingContext.currentUexpOffset}");
                        }

                        readingContext.targetContext.TakeArg();
                        readingContext.targetContext.TakeArg();

                        // Proceed to needed offset
                        readingContext.pattern.Add(ExportParsingMachine.skipPatternElementName);
                        readingContext.pattern.Add(readingContext.targetContext.TakeArg());

                        // Value is there
                        readingContext.pattern.Add(readingContext.targetContext[0]);
                    }
                }
            }
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



        private static void ContextReturnProcesser(ReadingContext upperContext, ReadingContext finishedContext)
        {
            upperContext.sizeChange += finishedContext.sizeChange;
            if (finishedContext.contextDeclaredSizeOffset != 0)
            {
                DAUMLib.AddToInt32ByOffset(Program.runData.uexp, finishedContext.sizeChange, finishedContext.contextDeclaredSizeOffset);
            }

            if (customRunDara.taskComplete)
            {
                upperContext.targetContext.Clear();
                upperContext.pattern.Clear();
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

            public bool taskComplete = false;

            public Int32 changedExportSerialOffset = 0;
            public Int32 sizeChange = 0;

            public Int32 insertDeclaredSizeOffset = -1;
            public Int32 insertSizeStartOffset = -1;

            public Int32 deleteStartOffset = -1;
            public Int32 deleteEndOffset = -1;

            public NewStringValEncoding newStringValEncoding = NewStringValEncoding.utf8;
            public bool nullString = false;

            public Mode mode;

            public enum NewStringValEncoding
            {
                utf8,
                utf16
            }

            public enum Mode
            {
                add,
                rewrite,
                delete
            }
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
