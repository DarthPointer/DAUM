using System;
using System.Collections.Generic;
using System.IO;

namespace daum
{
    public abstract class Operation
    {
        protected static string byIndexKey = "-i";
        protected static string indexOfExportKey = "-e";

        public abstract string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup);

        protected static byte[] Insert(byte[] target, byte[] insert, Int32 offset)
        {
            Span<byte> spanStart = target.AsSpan(0, offset);
            Span<byte> spanEnd = target.AsSpan(offset);

            byte[] newArray = new byte[target.Length + insert.Length];

            spanStart.CopyTo(newArray.AsSpan(0));
            insert.CopyTo(newArray.AsSpan(offset));
            spanEnd.CopyTo(newArray.AsSpan(offset + insert.Length));

            return newArray;
        }

        protected static byte[] Remove(byte[] target, Int32 offset, Int32 size)
        {
            Span<byte> spanStart = target.AsSpan(0, offset);
            Span<byte> spanEnd = target.AsSpan(offset + size);

            byte[] newArray = new byte[target.Length - size];

            spanStart.CopyTo(newArray.AsSpan(0));
            spanEnd.CopyTo(newArray.AsSpan(offset));

            return newArray;
        }

        protected static byte[] StringToBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                bytes[i] = (byte)str[i];
            }

            return bytes;
        }

        public static string SizePrefixedStringFromOffset(byte[] uasset, Int32 offset)
        {
            return Program.SizePrefixedStringFromOffsetOffsetAdvance(uasset, ref offset);
        }

        protected static Int32? FindNameIndex(string name)
        {
            return Array.IndexOf(Program.runData.nameMap, name);
        }

        protected static Int32? GetNameIndex(List<string> args)
        {
            string arg0 = args.TakeArg();

            if (arg0 == byIndexKey)
            {
                return Int32.Parse(args.TakeArg());
            }
            else if (arg0 == "-s")
            {
                return null;
            }
            else
            {
                return (Int32)FindNameIndex(arg0);
            }
        }

        protected static Int32? FindImportDefOffset(byte[] uasset, Int32 index)
        {
            index = -1 * index - 1;

            if (index < BitConverter.ToInt32(uasset, Program.runData.headerOffsets.importCountOffset))
            {
                Int32 currentImportOffset = BitConverter.ToInt32(uasset, Program.runData.headerOffsets.importOffsetOffset);

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentImportOffset += Program.runData.headerOffsets.importDefSize;
                }

                return currentImportOffset;
            }

            return null;
        }

        protected static Int32? GetImportIndex(byte[] uasset, List<string> args)
        {
            if (args[0] == byIndexKey)
            {
                args.TakeArg();
                return Int32.Parse(args.TakeArg());
            }
            else if (args[0] == "-s")
            {
                args.TakeArg();
                return null;
            }
            else
            {
                return (Int32)FindImportIndex(uasset, args);
            }
        }

        protected static Int32? FindImportIndex(byte[] uasset, List<string> args)
        {
            Int32 nameIndex = (Int32)FindNameIndex(args.TakeArg());
            Int32 nameAug = Int32.Parse(args.TakeArg());

            Int32 currentImportOffset = BitConverter.ToInt32(uasset, Program.runData.headerOffsets.importOffsetOffset);

            for (int processedRecords = 0; processedRecords < BitConverter.ToInt32(uasset, Program.runData.headerOffsets.importCountOffset); processedRecords++)
            {
                Int32 recordNameIndex = NameIndexFromImportDef(uasset, currentImportOffset);
                Int32 redordNameAug = BitConverter.ToInt32(uasset, currentImportOffset + Program.runData.headerOffsets.importNameOffset + 4);
                if (recordNameIndex == nameIndex && redordNameAug == nameAug)
                {
                    return -1 * processedRecords - 1;
                }

                currentImportOffset += Program.runData.headerOffsets.importDefSize;
            }

            return null;
        }

        protected static Int32 NameIndexFromImportDef(byte[] uasset, Int32 offset)
        {
            return BitConverter.ToInt32(uasset, offset + Program.runData.headerOffsets.importNameOffset);
        }

        protected static Int32? FindExportDefOffset(byte[] uasset, Int32 index)
        {
            index -= 1;

            if (index < BitConverter.ToInt32(uasset, Program.runData.headerOffsets.exportCountOffset))
            {
                Int32 currentExportDefOffset = BitConverter.ToInt32(uasset, Program.runData.headerOffsets.exportOffsetOffset);

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentExportDefOffset += Program.runData.headerOffsets.exportDefSize;
                }

                return currentExportDefOffset;
            }

            return null;
        }

        protected static Int32? GetExportIndex(byte[] uasset, List<string> args)
        {
            string arg0 = args.TakeArg();

            if (arg0 == byIndexKey)
            {
                return Int32.Parse(args.TakeArg());
            }
            else
            {
                return FindExportIndex(uasset, arg0, Int32.Parse(args.TakeArg()));
            }
        }

        protected static Int32? FindExportIndex(byte[] uasset, string name, Int32 nameAug)
        {
            Int32 nameIndex = FindNameIndex(name).Value;

            Int32 currentExportDefIndex = 1;
            Int32 currentExportDefOffset = BitConverter.ToInt32(uasset, Program.runData.headerOffsets.exportOffsetOffset);
            for (int processedRecords = 0; processedRecords < BitConverter.ToInt32(uasset, Program.runData.headerOffsets.exportCountOffset); processedRecords++)
            {
                if (BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportNameOffset) == nameIndex &&
                    BitConverter.ToInt32(uasset, currentExportDefOffset + Program.runData.headerOffsets.exportNameOffset + 4) == nameAug)
                {
                    return currentExportDefIndex;
                }

                currentExportDefIndex++;
                currentExportDefOffset += Program.runData.headerOffsets.exportDefSize;
            }

            return null;
        }

        protected static Int32 NameIndexFromExportDef(byte[] uasset, Int32 offset)
        {
            return BitConverter.ToInt32(uasset, offset + Program.runData.headerOffsets.exportNameOffset);
        }

        protected static Int32? GetImportExportIndex(byte[] uasset, List<string> args)
        {
            string arg0 = args[0];

            if (arg0 == byIndexKey)
            {
                args.TakeArg();
                return Int32.Parse(args.TakeArg());
            }
            else if (arg0 == indexOfExportKey)
            {
                args.TakeArg();
                return GetExportIndex(uasset, args);
            }
            else
            {
                return GetImportIndex(uasset, args);
            }
        }
    }

    public abstract class MapOperation : Operation
    {
        private static string addOpKey = "-a";
        private static string replaceOpKey = "-r";
        protected static string skipKey = "-s";

        protected abstract Int32 nextBlockOffsetOffset { get; }
        protected abstract Int32 thisBlockOffsetOffset { get; }
        protected abstract Int32 thisBlockRecordCountOffset { get; }

        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething,  out bool useStandardBackup)
        {
            doneSomething = true;

            string opKey = args.TakeArg();
            if (opKey == addOpKey)
            {
                return AddOperation(args, BitConverter.ToInt32(Program.runData.uasset, nextBlockOffsetOffset), out useStandardBackup);
            }
            if (opKey == replaceOpKey)
            {
                Int32? replaceOffset;
                if (args[0] == byIndexKey)
                {
                    args.TakeArg();
                    replaceOffset = FindByIndex(args,
                        BitConverter.ToInt32(Program.runData.uasset, thisBlockOffsetOffset),
                        BitConverter.ToInt32(Program.runData.uasset, thisBlockRecordCountOffset));
                }
                else
                {
                    replaceOffset = FindByName(args,
                        BitConverter.ToInt32(Program.runData.uasset, thisBlockOffsetOffset),
                        BitConverter.ToInt32(Program.runData.uasset, thisBlockRecordCountOffset));
                }

                if (replaceOffset.HasValue) return ReplaceOperation(args, replaceOffset.Value, out useStandardBackup);
                else throw new KeyNotFoundException("Key specifying record to replace is not present in a map");
            }

            throw new ArgumentException("Arguments are invalid for replacement operation");
        }

        protected abstract Int32? FindByName(List<string> args, Int32 mapOffset, Int32 mapRecordsCount);

        protected abstract Int32? FindByIndex(List<string> args, Int32 mapOffset, Int32 mapRecordsCount);

        protected abstract string ReplaceOperation(List<string> args, Int32 replaceAtOffset, out bool useStandardBackup);

        protected abstract string AddOperation(List<string> args, Int32 addAtOffset, out bool useStandardBackup);
    }

    public class NameDefOperation : MapOperation
    {
        protected override Int32 nextBlockOffsetOffset => Program.runData.headerOffsets.importOffsetOffset;
        protected override Int32 thisBlockOffsetOffset => Program.runData.headerOffsets.nameOffsetOffset;
        protected override Int32 thisBlockRecordCountOffset => Program.runData.headerOffsets.nameCountOffset;

        protected override string AddOperation(List<string> args, Int32 addAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;

            byte[] insert = MakeNameDef(args[0]);
            Program.runData.uasset = Insert(Program.runData.uasset, insert, addAtOffset);

            Int32 newNameMapCount = Program.runData.nameMap.Length + 1;
            Array.Resize(ref Program.runData.nameMap, newNameMapCount);
            Program.runData.nameMap[newNameMapCount - 1] = args.TakeArg();

            return $" -n {insert.Length} 1";
        }

        protected override Int32? FindByName(List<string> args, Int32 mapOffset, Int32 mapRecordsCount)
        {
            Int32 currentNameOffset = mapOffset;
            string name = args.TakeArg();

            for (int processedRecords = 0; processedRecords < mapRecordsCount; processedRecords++)
            {
                string storedName = SizePrefixedStringFromOffset(Program.runData.uasset, currentNameOffset);
                if (storedName == name)
                {
                    return currentNameOffset;
                }

                currentNameOffset += 9 + storedName.Length;
            }

            return null;
        }

        protected override Int32? FindByIndex(List<string> args, Int32 mapOffset, Int32 mapRecordsCount)
        {
            Int32 index = Int32.Parse(args.TakeArg());

            if (index < mapRecordsCount)
            {
                Int32 currentNameOffset = mapOffset;

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentNameOffset += BitConverter.ToInt32(Program.runData.uasset, currentNameOffset) + 8;
                }

                return currentNameOffset;
            }

            return null;
        }

        protected override string ReplaceOperation(List<string> args, Int32 replaceAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;
            string newName = args.TakeArg();

            Int32 oldNameStoredSize = BitConverter.ToInt32(Program.runData.uasset, replaceAtOffset);
            Int32 sizeChange = newName.Length + 1 - oldNameStoredSize;

            string origName = SizePrefixedStringFromOffset(Program.runData.uasset, replaceAtOffset);
            Program.runData.nameMap[Array.IndexOf(Program.runData.nameMap, origName)] = newName;

            Program.runData.uasset = Remove(Program.runData.uasset, replaceAtOffset, oldNameStoredSize + 8);
            Program.runData.uasset = Insert(Program.runData.uasset, MakeNameDef(newName), replaceAtOffset);

            return $" -n {sizeChange} 0";
        }

        private static byte[] MakeNameDef(string name)
        {
            byte[] result = new byte[Program.runData.headerOffsets.nameHashesSize + Program.runData.headerOffsets.stringSizeDesignationSize + 1];
            result = Insert(result, StringToBytes(name), 4);

            DAUMLib.WriteInt32IntoOffset(result, name.Length + 1, 0);

            return result;
        }
    }

    public class ImportDefOperation : MapOperation
    {
        protected override Int32 nextBlockOffsetOffset => Program.runData.headerOffsets.exportOffsetOffset;

        protected override Int32 thisBlockOffsetOffset => Program.runData.headerOffsets.importOffsetOffset;
        protected override Int32 thisBlockRecordCountOffset => Program.runData.headerOffsets.importCountOffset;

        protected override string AddOperation(List<string> args, int addAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;

            Int32 package = GetNameIndex(args).Value;
            Int32 _class = GetNameIndex(args).Value;
            Int32 outerIndex = GetImportIndex(Program.runData.uasset, args).Value;
            Int32 name = GetNameIndex(args).Value;
            Int32 nameAug = Int32.Parse(args.TakeArg());

            Program.runData.uasset = Insert(Program.runData.uasset, MakeImportDef(package, _class, outerIndex, name, nameAug), addAtOffset);

            Int32 newImportMapCount = Program.runData.importMap.Length + 1;
            Array.Resize(ref Program.runData.importMap, newImportMapCount);
            Program.runData.importMap[newImportMapCount - 1] = new Program.ImportData()
            {
                packageName = new Program.NameEntry(package, 0),
                className = new Program.NameEntry(_class, 0),
                outerIndex = outerIndex,
                importName = new Program.NameEntry(name, nameAug)
            };

            return $"-i {Program.runData.headerOffsets.importDefSize} 1";
        }

        protected override string ReplaceOperation(List<string> args, int replaceAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;

            Int32? package = GetNameIndex(args);
            Int32? _class = GetNameIndex(args);
            Int32? outerIndex = GetImportIndex(Program.runData.uasset, args);
            Int32? name = GetNameIndex(args);
            Int32? nameAug = GetOptionalInt(args);

            Int32 replacementIndex = (replaceAtOffset - BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.importOffsetOffset)) / Program.runData.headerOffsets.importDefSize;

            if (package != null)
            {
                DAUMLib.WriteInt32IntoOffset(Program.runData.uasset, package.Value, replaceAtOffset + Program.runData.headerOffsets.importPackageOffset);
                Program.runData.importMap[replacementIndex].packageName.nameIndex = package.Value;
            }
            if (_class != null)
            {
                DAUMLib.WriteInt32IntoOffset(Program.runData.uasset, _class.Value, replaceAtOffset + Program.runData.headerOffsets.importClassOffset);
                Program.runData.importMap[replacementIndex].className.nameIndex = _class.Value;
            }
            if (outerIndex != null)
            {
                DAUMLib.WriteInt32IntoOffset(Program.runData.uasset, outerIndex.Value, replaceAtOffset + Program.runData.headerOffsets.importOuterIndexOffset);
                Program.runData.importMap[replacementIndex].outerIndex = outerIndex.Value;
            }
            if (name != null)
            {
                Program.runData.importMap[replacementIndex].importName.nameIndex = name.Value;
                DAUMLib.WriteInt32IntoOffset(Program.runData.uasset, name.Value, replaceAtOffset + Program.runData.headerOffsets.importNameOffset);
            }
            if (nameAug != null)
            {
                Program.runData.importMap[replacementIndex].importName.nameAug = nameAug.Value;
                DAUMLib.WriteInt32IntoOffset(Program.runData.uasset, nameAug.Value, replaceAtOffset + Program.runData.headerOffsets.importNameOffset + 4);
            }


            return "";
        }

        private static Int32? GetOptionalInt(List<string> args)
        {
            string arg0 = args.TakeArg();

            if (arg0 == "-s")
            {
                return null;
            }
            else
            {
                return Int32.Parse(arg0);
            }
        }

        protected override Int32? FindByName(List<string> args, int mapOffset, int mapRecordsCount)
        {
            Int32 nameIndex = (Int32)FindNameIndex(args.TakeArg());

            Int32 currentImportOffset = mapOffset;

            for (int processedRecords = 0; processedRecords < mapRecordsCount; processedRecords++)
            {
                Int32 recordNameIndex = NameIndexFromImportDef(Program.runData.uasset, currentImportOffset);
                if (recordNameIndex == nameIndex)
                {
                    return currentImportOffset;
                }

                currentImportOffset += Program.runData.headerOffsets.importDefSize;
            }

            return null;
        }

        protected override int? FindByIndex(List<string> args, int mapOffset, int mapRecordsCount)
        {
            Int32 index = -1*Int32.Parse(args.TakeArg()) - 1;

            if (index < mapRecordsCount)
            {
                Int32 currentNameOffset = mapOffset;

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentNameOffset += Program.runData.headerOffsets.importDefSize;
                }

                return currentNameOffset;
            }

            return null;
        }

        private static byte[] MakeImportDef(Int32 package, Int32 _class, Int32 outerIndex, Int32 name, Int32 nameAug)
        {
            byte[] result = new byte[Program.runData.headerOffsets.importDefSize];

            DAUMLib.WriteInt32IntoOffset(result, package, Program.runData.headerOffsets.importPackageOffset);
            DAUMLib.WriteInt32IntoOffset(result, _class, Program.runData.headerOffsets.importClassOffset);
            DAUMLib.WriteInt32IntoOffset(result, outerIndex, Program.runData.headerOffsets.importOuterIndexOffset);
            DAUMLib.WriteInt32IntoOffset(result, name, Program.runData.headerOffsets.importNameOffset);
            DAUMLib.WriteInt32IntoOffset(result, nameAug, Program.runData.headerOffsets.importNameOffset + 4);

            return result;
        }
    }

    public class ExportDefOperation : MapOperation
    {
        private static Int32 relativeClassOffset = Program.runData.headerOffsets.exportClassOffset;
        private static Int32 relativeSuperOffset = Program.runData.headerOffsets.exportSuperOffset;
        private static Int32 relativeTemlateOffset = Program.runData.headerOffsets.exportTemplateOffset;
        private static Int32 relativeOuterOffset = Program.runData.headerOffsets.exportTemplateOffset;
        private static Int32 relativeObjectNameOffset = Program.runData.headerOffsets.exportNameOffset;
        private static Int32 relativeObjectFlagsOffset = Program.runData.headerOffsets.exportObjectFlagsOffset;
        private static Int32 relativeSerialSizeOffset = Program.runData.headerOffsets.exportSerialSizeOffset;
        private static Int32 relativeSerialOffsetOffset = Program.runData.headerOffsets.exportSerialOffsetOffset;
        private static Int32 relativeOtherDataOffset = Program.runData.headerOffsets.exportOtherDataOffset;

        private static int otherDataInt32Count = Program.runData.headerOffsets.exportOtherDataInt32Count;
        private static Int32 exportDefinitionSize = Program.runData.headerOffsets.exportDefSize;

        protected override int nextBlockOffsetOffset => Program.runData.headerOffsets.dependsOffsetOffset;

        protected override int thisBlockOffsetOffset => Program.runData.headerOffsets.exportOffsetOffset;
        protected override int thisBlockRecordCountOffset => Program.runData.headerOffsets.exportCountOffset;

        protected override string AddOperation(List<string> args, int addAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;

            Int32 _class = GetImportExportIndex(Program.runData.uasset, args).Value;
            Int32 super = Int32.Parse(args.TakeArg());
            Int32 template = GetImportExportIndex(Program.runData.uasset, args).Value;
            Int32 outer = GetImportExportIndex(Program.runData.uasset, args).Value;
            Int32 name = GetNameIndex(args).Value;
            Int32 nameAug = Int32.Parse(args.TakeArg());
            Int32 flags = Int32.Parse(args.TakeArg());

            Int32 serialOffset = BitConverter.ToInt32(Program.runData.uasset, addAtOffset - Program.runData.headerOffsets.exportDefSize + relativeSerialOffsetOffset) +
                BitConverter.ToInt32(Program.runData.uasset, addAtOffset - Program.runData.headerOffsets.exportDefSize + relativeSerialSizeOffset);
            List<Int32> other = new List<int>();
            for (int i = 0; i < otherDataInt32Count; i++)
            {
                other.Add(Int32.Parse(args.TakeArg()));
            }

            Program.runData.uasset = Insert(Program.runData.uasset, MakeExportDef(_class, super, template, outer, name, nameAug, flags, 0, serialOffset, other), addAtOffset);

            Program.CallOffSetterWithArgs("-edef 104 1");

            Int32 newExportSerialOffset = BitConverter.ToInt32(Program.runData.uasset, addAtOffset + relativeSerialOffsetOffset);
            Int32 newExportFileOffset = newExportSerialOffset - BitConverter.ToInt32(Program.runData.uasset, Program.runData.headerOffsets.totalHeaderSizeOffset);

            byte[] stubExport = new byte[12];
            DAUMLib.WriteInt32IntoOffset(stubExport, FindNameIndex("None").Value, 0);
            Program.runData.uexp = Insert(Program.runData.uexp, stubExport, newExportFileOffset);

            Program.CallOffSetterWithArgs($"-e 12 {newExportSerialOffset}");

            return "";
        }

        protected override int? FindByIndex(List<string> args, int mapOffset, int mapRecordsCount)
        {
            throw new NotImplementedException();
        }

        protected override int? FindByName(List<string> args, int mapOffset, int mapRecordsCount)
        {
            throw new NotImplementedException();
        }

        protected override string ReplaceOperation(List<string> args, int replaceAtOffse, out bool useStandardBackupt)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_class"></param>
        /// <param name="super"></param>
        /// <param name="template"></param>
        /// <param name="outer"></param>
        /// <param name="name"></param>
        /// <param name="nameAug">Name Augmentation. Seems to be a number sometimes used to distinguish objects sharing same "base" name</param>
        /// <param name="flags"></param>
        /// <param name="size">aka SerialSize, usually set to 0 in DAUM, actual value set after by OffSetter</param>
        /// <param name="serialOffset"></param>
        /// <param name="other">other is assumed to be 15 elements, less elements will cause exception!</param>
        /// <returns>byte[] with ExportDefinition </returns>
        private static byte[] MakeExportDef(Int32 _class, Int32 super, Int32 template, Int32 outer, Int32 name, Int32 nameAug, Int32 flags,
            Int32 size, Int32 serialOffset, List<Int32> other)
        {
            byte[] result = new byte[exportDefinitionSize];

            DAUMLib.WriteInt32IntoOffset(result, _class, relativeClassOffset);
            DAUMLib.WriteInt32IntoOffset(result, super, relativeSuperOffset);
            DAUMLib.WriteInt32IntoOffset(result, template, relativeTemlateOffset);
            DAUMLib.WriteInt32IntoOffset(result, outer, relativeOuterOffset);
            DAUMLib.WriteInt32IntoOffset(result, name, relativeObjectNameOffset);
            DAUMLib.WriteInt32IntoOffset(result, nameAug, relativeObjectNameOffset + 4);
            DAUMLib.WriteInt32IntoOffset(result, flags, relativeObjectFlagsOffset);
            DAUMLib.WriteInt32IntoOffset(result, size, relativeSerialSizeOffset);
            DAUMLib.WriteInt32IntoOffset(result, serialOffset, relativeSerialOffsetOffset);
            Int32 currentOtherOffset = relativeOtherDataOffset;
            for (int i = 0; i < otherDataInt32Count; i++)
            {
                DAUMLib.WriteInt32IntoOffset(result, other[i], currentOtherOffset);
                currentOtherOffset += 4;
            }

            return result;
        }
    }
}
