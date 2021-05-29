using System;
using System.Collections.Generic;
using System.IO;
using DRGOffSetterLib;

namespace daum
{
    delegate void UexpValRead(Span<byte> uasset, Span<byte> uexp, ref int offset);

    public class ExportReadOperation : Operation
    {
        private static string endOfStructConfigName = "None";

        private static Dictionary<string, UexpValRead> primitiveTyperReaders = new Dictionary<string, UexpValRead>()
        {
            { "float32", ReadFloat32Value },
            { "objectProp", ReadObjectPropValue }
        };

        public override string ExecuteAndGetOffSetterAgrs(ref Span<byte> span, List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            useStandardBackup = false;
            doneSomething = false;

            Int32 exportIndex = GetExportIndex(span, args).Value;

            Int32 fisrtExportOffset = DOLib.Int32FromSpanOffset(span, exportOffsetOffset);
            Int32 uexpStructureOffset = DOLib.Int32FromSpanOffset(span, fisrtExportOffset + (exportIndex - 1) * exportDefSize + exportSerialOffsetOffset)
                - DOLib.Int32FromSpanOffset(span, headerSizeOffset);

            ReadProperties(span, File.ReadAllBytes(Program.runData.fileName.Substring(0, Program.runData.fileName.LastIndexOf('.') + 1) + "uexp"),
                uexpStructureOffset);

            return "";
        }

        private static void ReadProperties(Span<byte> uasset, Span<byte> uexp, Int32 offset)
        {
            while (ReadProperty(uasset, uexp, ref offset)) ;

            return;
        }

        private static bool ReadProperty(Span<byte> uasset, Span<byte> uexp, ref Int32 offset)
        {
            string propertyName = ReadNameForm(uasset, uexp, ref offset);

            if (propertyName == endOfStructConfigName)
            {
                offset += 4;
                return false;
            }

            string typeName = ReadNameForm(uasset, uexp, ref offset);

            Console.WriteLine("-----------------------------");
            Console.WriteLine($"{propertyName} is {typeName}");

            string propertyPatternFilename = Program.runData.toolDir + $"PropertyPatterns\\{ typeName }";
            if (File.Exists(propertyPatternFilename))
            {
                string patternString = File.ReadAllText(propertyPatternFilename);
                List<string> parsedPattern = Program.ParseCommandString(patternString);
                while (parsedPattern.Count > 0)
                {
                    PatternElementRead(uasset, uexp, ref offset, parsedPattern);
                }

                return true;
            }

            return false;
        }

        private static string ReadNameForm(Span<byte> uasset, Span<byte> uexp, ref Int32 offset)
        {
            string nameString = NameString(uasset, DOLib.Int32FromSpanOffset(uexp, offset));
            offset += 4;
            Int32 nameAug = DOLib.Int32FromSpanOffset(uexp, offset);
            offset += 4;

            if (nameAug != 0)
            {
                nameString += $"_{ nameAug }";
            }

            return nameString;
        }

        private static void PatternElementRead(Span<byte> uasset, Span<byte> uexp, ref Int32 offset, List<string> patternArgs)
        {
            const string skipPattern = "skip";

            string patternElement = patternArgs.TakeArg();

            if (patternElement == skipPattern)
            {
                offset += Int32.Parse(patternArgs.TakeArg());
                return;
            }
            else if (primitiveTyperReaders.ContainsKey(patternElement))
            {
                primitiveTyperReaders[patternElement](uasset, uexp, ref offset);
                return;
            }
            else
            {
                offset += 4;
                return;
            }
        }

        private static void ReadFloat32Value(Span<byte> uasset, Span<byte> uexp, ref Int32 offset)
        {
            Span<byte> scope = uexp.Slice(offset, 4);
            offset += 4;

            Console.WriteLine($"Float Value {BitConverter.ToSingle(scope)}");
        }

        private static void ReadObjectPropValue(Span<byte> uasset, Span<byte> uexp, ref Int32 offset)
        {
            Span<byte> scope = uexp.Slice(offset, 4);
            offset += 4;

            Int32 index = BitConverter.ToInt32(scope);

            if (index == 0)
            {
                Console.WriteLine("ObjectProperty is null");
                return;
            }
            else if (index > 0)
            {
                Console.WriteLine($"ObjectProperty Value Export:{NameString(uasset, NameIndexFromExportDef(uasset, FindExportDefOffset(uasset, index).Value))}");
            }
            else
            {
                Console.WriteLine($"ObjectProperty Value Import:{NameString(uasset, NameIndexFromImportDef(uasset, FindImportDefOffset(uasset, index).Value))}");
            }
        }
    }
}
