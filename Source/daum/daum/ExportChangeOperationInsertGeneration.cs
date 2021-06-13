using System;
using System.Collections.Generic;
using DRGOffSetterLib;

namespace daum
{
    partial class ExportChangeOperation
    {
        private delegate byte[] AppendPatternData(byte[] originalArray, List<string> pattern, List<string> data);

        private const string propertyNamePatternElementName = "PropertyName";
        private const string propertyTypePatternElementName = "PropertyType";

        private static readonly Dictionary<string, AppendPatternData> patternFillers = new Dictionary<string, AppendPatternData>()
        {
            { propertyNamePatternElementName, PropertyNameFiller },
            { propertyTypePatternElementName, PropertyTypeFiller },

            { ExportParsingMachine.sizePatternElementName, NullSizeFiller },
            { ExportParsingMachine.sizeStartPatternElementName, InsertSizeStartRecorder },

            { ExportParsingMachine.boolPatternElementName, NullValueFiller },
            { ExportParsingMachine.uint16PatternElementName, NullValueFiller },
            { ExportParsingMachine.int32PatternElementName, NullValueFiller },
            { ExportParsingMachine.uint32PatternElementName, NullValueFiller },
            { ExportParsingMachine.uint64PatternElementName, NullValueFiller },
            { ExportParsingMachine.float32PatternElementName, NullValueFiller },


            { ExportParsingMachine.skipPatternElementName, SkipFiller }
        };

        private static readonly Dictionary<string, Int32> defaultValueSizes = new Dictionary<string, int>()
        {
            { ExportParsingMachine.boolPatternElementName, 1 },
            { ExportParsingMachine.uint16PatternElementName, 2 },
            { ExportParsingMachine.int32PatternElementName, 4 },
            { ExportParsingMachine.uint32PatternElementName, 4 },
            { ExportParsingMachine.uint64PatternElementName, 8 },

            { ExportParsingMachine.float32PatternElementName, 4 }
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

        private static byte[] SkipFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();
            Int32 skipSize = Int32.Parse(pattern.TakeArg());

            byte[] insert = new byte[skipSize];

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] PropertyNameFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            Int32 nameIndex = GetNameIndex(data).Value;
            Int32 nameAug = Int32.Parse(data.TakeArg());

            byte[] insert = new byte[8];

            BitConverter.GetBytes(nameIndex).CopyTo(insert, 0);
            BitConverter.GetBytes(nameAug).CopyTo(insert, 4);

            return Insert(originalArray, insert, originalArray.Length);
        }

        private static byte[] PropertyTypeFiller(byte[] originalArray, List<string> pattern, List<string> data)
        {
            pattern.TakeArg();

            Int32 nameIndex = GetNameIndex(data).Value;
            Int32 nameAug = Int32.Parse(data.TakeArg());

            string nameString = Program.runData.nameMap[nameIndex];
            pattern.AddRange(Program.GetPattern($"{Program.PatternFolders.property}/{nameString}"));

            byte[] insert = new byte[8];

            BitConverter.GetBytes(nameIndex).CopyTo(insert, 0);
            BitConverter.GetBytes(nameAug).CopyTo(insert, 4);

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
    }
}
