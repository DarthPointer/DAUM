using System;
using System.Collections.Generic;

namespace daum
{
    partial class ExportChangeOperation
    {
        private delegate byte[] AppendPatternData(byte[] originalArray, List<string> pattern, List<string> data);

        private const string propertyNamePatternElementName = "PropertyName";
        private const string propertyTypePatternElementName = "PropertyType";

        private static Int32 newPropertyName;

        private static readonly Dictionary<string, AppendPatternData> patternFillers = new Dictionary<string, AppendPatternData>()
        {
            { propertyNamePatternElementName, PropertyNameFiller },
            { propertyTypePatternElementName, PropertyTypeFiller },

            { ExportParsingMachine.sizePatternElementName, NullSizeFiller },
            { ExportParsingMachine.sizeStartPatternElementName, InsertSizeStartRecorder },

            { ExportParsingMachine.structTypeNameIndexPatternElementName, StructTypeNameIndexFiller },
            { ExportParsingMachine.arrayElementTypeNameIndexPatternElementName, ArrayTypeNameIndexFiller },
            { ExportParsingMachine.structPropertyArrayTypePatternElementName, StructPropertyArrayTypeFiller },

            { ExportParsingMachine.propertyNameCopyPatternElementName, PropertyNameCopyFiller },
            { ExportParsingMachine.structPropertyNamePatternElementName, StructPropertyNameFiller },

            { ExportParsingMachine.elementCountPatternElementName, NullElementCountFiller },
            { ExportParsingMachine.arrayRepeatPatternElementName, NullArrayFiller },

            { ExportParsingMachine.boolPatternElementName, NullValueFiller },
            { ExportParsingMachine.uint16PatternElementName, NullValueFiller },
            { ExportParsingMachine.int32PatternElementName, NullValueFiller },
            { ExportParsingMachine.uint32PatternElementName, NullValueFiller },
            { ExportParsingMachine.uint64PatternElementName, NullValueFiller },
            { ExportParsingMachine.float32PatternElementName, NullValueFiller },
            { ExportParsingMachine.GUIDPatternElementName, NullValueFiller },
            { ExportParsingMachine.namePatternElementName, NullValueFiller },
            { ExportParsingMachine.objectIndexPatternElementName, NullValueFiller },

            { ExportParsingMachine.SPNTSPatternElementName, NullValueFiller },

            { ExportParsingMachine.TPDHPatternElementName, TextPropertyRawBodyInput },

            { ExportParsingMachine.NTPLPatternElementName, NullNTPLFiller },

            { ExportParsingMachine.skipPatternElementName, SkipFiller },
            { ExportParsingMachine.skipIfPatternEndsPatternElementName, ExceptionIfPatternEnds }
        };

        private static readonly Dictionary<string, Int32> defaultValueSizes = new Dictionary<string, int>()
        {
            { ExportParsingMachine.boolPatternElementName, 1 },

            { ExportParsingMachine.uint16PatternElementName, 2 },
            { ExportParsingMachine.int32PatternElementName, 4 },
            { ExportParsingMachine.uint32PatternElementName, 4 },
            { ExportParsingMachine.uint64PatternElementName, 8 },

            { ExportParsingMachine.float32PatternElementName, 4 },

            { ExportParsingMachine.GUIDPatternElementName, 16 },

            { ExportParsingMachine.namePatternElementName, 8 },
            { ExportParsingMachine.objectIndexPatternElementName, 4 },

            { ExportParsingMachine.SPNTSPatternElementName, 4 }
        };

        private static byte[] GenerateInsert(List<string> args, List<string> pattern)
        {
            List<string> dataArgs = args;

            byte[] insert = new byte[0];

            while (pattern.Count > 0)
            {
                if (patternFillers.ContainsKey(pattern[0]))
                {
                    insert = patternFillers[pattern[0]](insert, pattern, dataArgs);
                }
                else
                {
                    insert = Insert(insert, BitConverter.GetBytes(Int32.Parse(pattern.TakeArg())), insert.Length);
                }
            }

            if (customRunDara.insertDeclaredSizeOffset != -1)
            {
                BitConverter.GetBytes(insert.Length - customRunDara.insertSizeStartOffset).CopyTo(insert, customRunDara.insertDeclaredSizeOffset);
            }

            return insert;
        }

        private static byte[] NullValueFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            Int32 skipSize = defaultValueSizes[pattern.TakeArg()];

            byte[] insert = new byte[skipSize];

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] TextPropertyRawBodyInput(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            byte[] insert = RawBytesFromString(data.TakeArg());

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] NullNTPLFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            byte[] insert = new byte[8];
            BitConverter.GetBytes(Array.IndexOf(Program.runData.nameMap, ExportParsingMachine.endOfStructConfigName)).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] SkipFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();
            Int32 skipSize = Int32.Parse(pattern.TakeArg());

            byte[] insert = new byte[skipSize];

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] ExceptionIfPatternEnds(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            if (pattern.Count == 0)
            {
                throw new FormatException("Found mid-property pattern end during creation process, aborted");
            }

            return originalArray;
        }

        private static byte[] PropertyNameFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            Int32 nameIndex = newPropertyName = GetNameIndex(data).Value;

            byte[] insert = new byte[8];
            BitConverter.GetBytes(nameIndex).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] PropertyTypeFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            Int32 nameIndex = GetNameIndex(data).Value;

            string nameString = Program.runData.nameMap[nameIndex];
            pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.property}/{nameString}"));

            byte[] insert = new byte[8];
            BitConverter.GetBytes(nameIndex).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] NullSizeFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            customRunDara.insertDeclaredSizeOffset = originalArray.Length;

            byte[] insert = new byte[4];

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] InsertSizeStartRecorder(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            customRunDara.insertSizeStartOffset = originalArray.Length;

            return originalArray;
        }

        private static byte[] StructTypeNameIndexFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            Int32 structType = GetNameIndex(data).Value;

            pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{Program.runData.nameMap[structType]}"));

            byte[] insert = new byte[8];
            BitConverter.GetBytes(structType).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] ArrayTypeNameIndexFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            Int32 elementType = GetNameIndex(data).Value;

            pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.body}/{Program.runData.nameMap[elementType]}"));

            byte[] insert = new byte[8];
            BitConverter.GetBytes(elementType).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] StructPropertyArrayTypeFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            Int32 elementType = GetNameIndex(data).Value;

            pattern.Add(ExportParsingMachine.arrayRepeatPatternElementName);
            pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.structure}/{Program.runData.nameMap[elementType]}"));

            byte[] insert = new byte[8];
            BitConverter.GetBytes(elementType).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] PropertyNameCopyFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            byte[] insert = new byte[8];
            BitConverter.GetBytes(newPropertyName).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] StructPropertyNameFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            byte[] insert = new byte[8];
            BitConverter.GetBytes(FindNameIndex("StructProperty").Value).CopyTo(insert, 0);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] NullElementCountFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            byte[] insert = new byte[4];
            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] NullArrayFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            while (pattern.Count != 0 && pattern[0] != ExportParsingMachine.arrayRepeatEndPatternElementName)
            {
                pattern.TakeArg();
            }

            return originalArray;
        }

        private static byte[] RawBytesFromString(string str)
        {
            string[] array = str.Split(' ');
            if (array.Length == 1 && array[0] == "")
            {
                array = new string[0];
            }

            List<byte> result = new List<byte>();

            foreach (string bytestr in array)
            {
                result.Add(byte.Parse(bytestr, style: System.Globalization.NumberStyles.HexNumber));
            }

            return result.ToArray();
        }
    }
}
