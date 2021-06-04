using System;
using System.Collections.Generic;
using DRGOffSetterLib;
using System.IO;

namespace daum
{
    public abstract class Operation
    {
        protected static string byIndexKey = "-i";
        protected static string indexOfExportKey = "-e";

        protected static Int32 exportOffsetOffset = 61;
        protected static Int32 exportCountOffset = 57;

        protected static Int32 exportNameOffset = 16;
        protected static Int32 exportDefSize = 104;

        protected static Int32 exportSerialOffsetOffset = 36;
        protected static Int32 exportSerialSizeOffset = 28;

        protected static Int32 dependsOffsetOffset = 73;

        protected static Int32 headerSizeOffset = 24;

        public abstract string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup);

        protected static Span<byte> Insert(Span<byte> span, Span<byte> insert, Int32 offset)
        {
            Span<byte> spanStart = span.Slice(0, offset);
            Span<byte> spanEnd = span.Slice(offset);

            byte[] newArray = new byte[span.Length + insert.Length];

            spanStart.ToArray().CopyTo(newArray, 0);
            insert.ToArray().CopyTo(newArray, offset);
            spanEnd.ToArray().CopyTo(newArray, offset + insert.Length);

            return newArray;
        }

        protected static Span<byte> Remove(Span<byte> span, Int32 offset, Int32 size)
        {
            Span<byte> spanStart = span.Slice(0, offset);
            Span<byte> spanEnd = span.Slice(offset + size);

            byte[] newArray = new byte[span.Length - size];

            spanStart.ToArray().CopyTo(newArray, 0);
            spanEnd.ToArray().CopyTo(newArray, offset);

            return newArray;
        }

        protected static Span<byte> StringToBytes(string str)
        {
            byte[] bytes = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                bytes[i] = (byte)str[i];
            }

            return bytes;
        }

        protected static Int32? FindNameDefOffset(Span<byte> span, Int32 index)
        {
            if (index < DOLib.Int32FromSpanOffset(span, OffsetConstants.nameCountOffset))
            {
                Int32 currentNameOffset = DOLib.Int32FromSpanOffset(span, OffsetConstants.nameOffsetOffset);

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentNameOffset += DOLib.Int32FromSpanOffset(span, currentNameOffset) + 8;
                }

                return currentNameOffset;
            }

            return null;
        }

        protected static string NameString(Span<byte> span, Int32 index)
        {
            Int32 currentNameOffset = DOLib.Int32FromSpanOffset(span, OffsetConstants.nameOffsetOffset);

            for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
            {
                currentNameOffset += DOLib.Int32FromSpanOffset(span, currentNameOffset) + OffsetConstants.stringSizeDesignationSize + OffsetConstants.nameHashesSize;
            }

            return StringFromNameDef(span, currentNameOffset);
        }

        protected static string StringFromNameDef(Span<byte> span, Int32 offset)
        {
            Int32 size = DOLib.Int32FromSpanOffset(span, offset);

            Span<byte> scope = span.Slice(offset + OffsetConstants.stringSizeDesignationSize, size - 1);
            offset += OffsetConstants.stringSizeDesignationSize;
            string result = "";

            foreach (char i in scope)
            {
                result += i;
            }

            return Program.StringFromOffset(span, offset, size);
        }

        protected static Int32? FindNameIndex(Span<byte> span, string name)
        {
            Int32 currentNameOffset = DOLib.Int32FromSpanOffset(span, OffsetConstants.nameOffsetOffset);

            for (int processedRecords = 0; processedRecords < DOLib.Int32FromSpanOffset(span, OffsetConstants.nameCountOffset); processedRecords++)
            {
                string storedName = StringFromNameDef(span, currentNameOffset);
                if (storedName == name)
                {
                    return processedRecords;
                }

                currentNameOffset += 9 + storedName.Length;
            }

            return null;
        }

        protected static Int32? GetNameIndex(Span<byte> span, List<string> args)
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
                return (Int32)FindNameIndex(span, arg0);
            }
        }

        protected static Int32? FindImportDefOffset(Span<byte> span, Int32 index)
        {
            index = -1 * index - 1;

            if (index < DOLib.Int32FromSpanOffset(span, OffsetConstants.importCountOffset))
            {
                Int32 currentImportOffset = DOLib.Int32FromSpanOffset(span, OffsetConstants.importOffsetOffset);

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentImportOffset += OffsetConstants.importDefSize;
                }

                return currentImportOffset;
            }

            return null;
        }

        protected static Int32? GetImportIndex(Span<byte> span, List<string> args)
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
                return (Int32)FindImportIndex(span, arg0);
            }
        }

        protected static Int32? FindImportIndex(Span<byte> span, string name)
        {
            Int32 nameIndex = (Int32)FindNameIndex(span, name);

            Int32 currentImportOffset = DOLib.Int32FromSpanOffset(span, OffsetConstants.importOffsetOffset);

            for (int processedRecords = 0; processedRecords < DOLib.Int32FromSpanOffset(span, OffsetConstants.importCountOffset); processedRecords++)
            {
                Int32 recordNameIndex = NameIndexFromImportDef(span, currentImportOffset);
                if (recordNameIndex == nameIndex)
                {
                    return -1 * processedRecords - 1;
                }

                currentImportOffset += OffsetConstants.importDefSize;
            }

            return null;
        }

        protected static Int32 NameIndexFromImportDef(Span<byte> span, Int32 offset)
        {
            return DOLib.Int32FromSpanOffset(span, offset + OffsetConstants.importNameOffset);
        }

        protected static Int32? FindExportDefOffset(Span<byte> span, Int32 index)
        {
            index -= 1;

            if (index < DOLib.Int32FromSpanOffset(span, exportCountOffset))
            {
                Int32 currentExportDefOffset = DOLib.Int32FromSpanOffset(span, exportOffsetOffset);

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentExportDefOffset += exportDefSize;
                }

                return currentExportDefOffset;
            }

            return null;
        }

        protected static Int32? GetExportIndex(Span<byte> span, List<string> args)
        {
            string arg0 = args.TakeArg();

            if (arg0 == byIndexKey)
            {
                return Int32.Parse(args.TakeArg());
            }
            else
            {
                return FindExportIndex(span, arg0, Int32.Parse(args.TakeArg()));
            }
        }

        protected static Int32? FindExportIndex(Span<byte> span, string name, Int32 nameAug)
        {
            Int32 nameIndex = FindNameIndex(span, name).Value;

            Int32 currentExportDefIndex = 1;
            Int32 currentExportDefOffset = DOLib.Int32FromSpanOffset(span, exportOffsetOffset);
            for (int processedRecords = 0; processedRecords < DOLib.Int32FromSpanOffset(span, exportCountOffset); processedRecords++)
            {
                if (DOLib.Int32FromSpanOffset(span, currentExportDefOffset + exportNameOffset) == nameIndex &&
                    DOLib.Int32FromSpanOffset(span, currentExportDefOffset + exportNameOffset + 4) == nameAug)
                {
                    return currentExportDefIndex;
                }

                currentExportDefIndex++;
                currentExportDefOffset += exportDefSize;
            }

            return null;
        }

        protected static Int32 NameIndexFromExportDef(Span<byte> span, Int32 offset)
        {
            return DOLib.Int32FromSpanOffset(span, offset + exportNameOffset);
        }

        protected static Int32? GetImportExportIndex(Span<byte> span, List<string> args)
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
                return GetExportIndex(span, args);
            }
            else
            {
                return GetImportIndex(span, args);
            }
        }
    }

    public class OffSetterCall : Operation
    {
        public override string ExecuteAndGetOffSetterAgrs(List<string> args, out bool doneSomething, out bool useStandardBackup)
        {
            useStandardBackup = false;
            doneSomething = true;

            Program.CallOffSetterWithArgs(' ' + string.Join(' ', args));
            Program.runData.uasset = File.ReadAllBytes(Program.runData.uassetFileName);
            return "";
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
                return AddOperation(args, DOLib.Int32FromSpanOffset(Program.runData.uasset, nextBlockOffsetOffset), out useStandardBackup);
            }
            if (opKey == replaceOpKey)
            {
                Int32? replaceOffset;
                if (args[0] == byIndexKey)
                {
                    args.TakeArg();
                    replaceOffset = FindByIndex(args,
                        DOLib.Int32FromSpanOffset(Program.runData.uasset, thisBlockOffsetOffset),
                        DOLib.Int32FromSpanOffset(Program.runData.uasset, thisBlockRecordCountOffset));
                }
                else
                {
                    replaceOffset = FindByName(args,
                        DOLib.Int32FromSpanOffset(Program.runData.uasset, thisBlockOffsetOffset),
                        DOLib.Int32FromSpanOffset(Program.runData.uasset, thisBlockRecordCountOffset));
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
        protected override Int32 nextBlockOffsetOffset => OffsetConstants.importOffsetOffset;
        protected override Int32 thisBlockOffsetOffset => OffsetConstants.nameOffsetOffset;
        protected override Int32 thisBlockRecordCountOffset => OffsetConstants.nameCountOffset;

        protected override string AddOperation(List<string> args, Int32 addAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;

            Span<byte> insert = MakeNameDef(args[0]);
            Program.runData.uasset = Insert(Program.runData.uasset, insert, addAtOffset).ToArray();
            Program.runData.nameMap[Program.runData.nameMap.Length] = args.TakeArg();

            return $" -n {insert.Length} 1";
        }

        protected override Int32? FindByName(List<string> args, Int32 mapOffset, Int32 mapRecordsCount)
        {
            Int32 currentNameOffset = mapOffset;
            string name = args.TakeArg();

            for (int processedRecords = 0; processedRecords < mapRecordsCount; processedRecords++)
            {
                string storedName = StringFromNameDef(Program.runData.uasset, currentNameOffset);
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
                    currentNameOffset += DOLib.Int32FromSpanOffset(Program.runData.uasset, currentNameOffset) + 8;
                }

                return currentNameOffset;
            }

            return null;
        }

        protected override string ReplaceOperation(List<string> args, Int32 replaceAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;
            string newName = args.TakeArg();

            Int32 oldNameStoredSize = DOLib.Int32FromSpanOffset(Program.runData.uasset, replaceAtOffset);
            Int32 sizeChange = newName.Length + 1 - oldNameStoredSize;

            string origName = StringFromNameDef(Program.runData.uasset, replaceAtOffset);
            Program.runData.nameMap[Array.IndexOf(Program.runData.uasset, origName)] = newName;

            Program.runData.uasset = Remove(Program.runData.uasset, replaceAtOffset, oldNameStoredSize + 8).ToArray();
            Program.runData.uasset = Insert(Program.runData.uasset, MakeNameDef(newName), replaceAtOffset).ToArray();

            return $" -n {sizeChange} 0";
        }

        private static Span<byte> MakeNameDef(string name)
        {
            Span<byte> result = new Span<byte>(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            result = Insert(result, StringToBytes(name), 4);

            DOLib.WriteInt32IntoOffset(result, name.Length + 1, 0);

            return result;
        }
    }

    public class ImportDefOperation : MapOperation
    {
        protected override Int32 nextBlockOffsetOffset => exportOffsetOffset;

        protected override Int32 thisBlockOffsetOffset => OffsetConstants.importOffsetOffset;
        protected override Int32 thisBlockRecordCountOffset => OffsetConstants.importCountOffset;

        protected override string AddOperation(List<string> args, int addAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;

            Int32 package = GetNameIndex(Program.runData.uasset, args).Value;
            Int32 _class = GetNameIndex(Program.runData.uasset, args).Value;
            Int32 outerIndex = GetImportIndex(Program.runData.uasset, args).Value;
            Int32 name = GetNameIndex(Program.runData.uasset, args).Value;

            Program.runData.uasset = Insert(Program.runData.uasset, MakeImportDef(package, _class, outerIndex, name), addAtOffset).ToArray();
            Program.runData.importMap[Program.runData.importMap.Length] = new Program.ImportData()
            {
                packageName = package,
                className = _class,
                outerIndex = outerIndex,
                importName = name
            };

            return $" -i {OffsetConstants.importDefSize} 1";
        }

        protected override string ReplaceOperation(List<string> args, int replaceAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = true;

            Int32? package = GetNameIndex(Program.runData.uasset, args);
            Int32? _class = GetNameIndex(Program.runData.uasset, args);
            Int32? outerIndex = GetImportIndex(Program.runData.uasset, args);
            Int32? name = GetNameIndex(Program.runData.uasset, args);

            Int32 replacementIndex = (replaceAtOffset - BitConverter.ToInt32(Program.runData.uasset, OffsetConstants.importOffsetOffset)) / OffsetConstants.importDefSize;

            if (package != null)
            {
                DOLib.WriteInt32IntoOffset(Program.runData.uasset, package.Value, replaceAtOffset + OffsetConstants.importPackageOffset);
                Program.runData.importMap[replacementIndex].packageName = package.Value;
            }
            if (_class != null)
            {
                DOLib.WriteInt32IntoOffset(Program.runData.uasset, _class.Value, replaceAtOffset + OffsetConstants.importClassOffset);
                Program.runData.importMap[replacementIndex].className = _class.Value;
            }
            if (outerIndex != null)
            {
                DOLib.WriteInt32IntoOffset(Program.runData.uasset, outerIndex.Value, replaceAtOffset + OffsetConstants.importOuterIndexOffset);
                Program.runData.importMap[replacementIndex].outerIndex = outerIndex.Value;
            }
            if (name != null)
            {
                Program.runData.importMap[replacementIndex].importName = name.Value;
                DOLib.WriteInt32IntoOffset(Program.runData.uasset, name.Value, replaceAtOffset + OffsetConstants.importNameOffset);
            }


            return "";
        }

        protected override Int32? FindByName(List<string> args, int mapOffset, int mapRecordsCount)
        {
            Int32 nameIndex = (Int32)FindNameIndex(Program.runData.uasset, args.TakeArg());

            Int32 currentImportOffset = mapOffset;

            for (int processedRecords = 0; processedRecords < mapRecordsCount; processedRecords++)
            {
                Int32 recordNameIndex = NameIndexFromImportDef(Program.runData.uasset, currentImportOffset);
                if (recordNameIndex == nameIndex)
                {
                    return currentImportOffset;
                }

                currentImportOffset += OffsetConstants.importDefSize;
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
                    currentNameOffset += OffsetConstants.importDefSize;
                }

                return currentNameOffset;
            }

            return null;
        }

        private static Span<byte> MakeImportDef(Int32 package, Int32 _class, Int32 outerIndex, Int32 name)
        {
            Span<byte> result = new Span<byte>(new byte[OffsetConstants.importDefSize]);

            DOLib.WriteInt32IntoOffset(result, package, OffsetConstants.importPackageOffset);
            DOLib.WriteInt32IntoOffset(result, _class, OffsetConstants.importClassOffset);
            DOLib.WriteInt32IntoOffset(result, outerIndex, OffsetConstants.importOuterIndexOffset);
            DOLib.WriteInt32IntoOffset(result, name, OffsetConstants.importNameOffset);

            return result;
        }
    }

    public class ExportDefOperation : MapOperation
    {
        private static Int32 relativeClassOffset = 0;
        private static Int32 relativeSuperOffset = 4;
        private static Int32 relativeTemlateOffset = 8;
        private static Int32 relativeOuterOffset = 12;
        private static Int32 relativeObjectNameOffset = exportNameOffset;
        private static Int32 relativeObjectFlagsOffset = 24;
        private static Int32 relativeSerialSizeOffset = exportSerialSizeOffset;
        private static Int32 relativeSerialOffsetOffset = exportSerialOffsetOffset;
        private static Int32 relativeOtherDataOffset = 44;

        private static int otherDataInt32Count = 15;
        private static Int32 exportDefinitionSize = exportDefSize;

        protected override int nextBlockOffsetOffset => dependsOffsetOffset;

        protected override int thisBlockOffsetOffset => exportOffsetOffset;
        protected override int thisBlockRecordCountOffset => exportCountOffset;

        protected override string AddOperation(List<string> args, int addAtOffset, out bool useStandardBackup)
        {
            useStandardBackup = false;

            Int32 _class = GetImportExportIndex(Program.runData.uasset, args).Value;
            Int32 super = Int32.Parse(args.TakeArg());
            Int32 template = GetImportExportIndex(Program.runData.uasset, args).Value;
            Int32 outer = GetImportExportIndex(Program.runData.uasset, args).Value;
            Int32 name = GetNameIndex(Program.runData.uasset, args).Value;
            Int32 nameAug = Int32.Parse(args.TakeArg());
            Int32 flags = Int32.Parse(args.TakeArg());

            Int32 serialOffset = DOLib.Int32FromSpanOffset(Program.runData.uasset, addAtOffset - exportDefSize + relativeSerialOffsetOffset) +
                DOLib.Int32FromSpanOffset(Program.runData.uasset, addAtOffset - exportDefSize + relativeSerialSizeOffset);
            List<Int32> other = new List<int>();
            for (int i = 0; i < otherDataInt32Count; i++)
            {
                other.Add(Int32.Parse(args.TakeArg()));
            }

            Program.runData.uasset = Insert(Program.runData.uasset, MakeExportDef(_class, super, template, outer, name, nameAug, flags, 0, serialOffset, other), addAtOffset).ToArray();

            if (File.Exists(Program.runData.uassetFileName + ".AddExportDefBackup")) File.Delete(Program.runData.uassetFileName + ".AddExportDefBackup");
            Directory.Move(Program.runData.uassetFileName, Program.runData.uassetFileName + ".AddExportDefBackup");

            File.WriteAllBytes(Program.runData.uassetFileName, Program.runData.uasset);
            Program.CallOffSetterWithArgs(" -edef 104 1 -r -m");

            Program.runData.uasset = File.ReadAllBytes(Program.runData.uassetFileName);

            Span<byte> uexp = File.ReadAllBytes(Program.runData.uexpFileName);

            Int32 newExportSerialOffset = DOLib.Int32FromSpanOffset(Program.runData.uasset, addAtOffset + relativeSerialOffsetOffset);
            Int32 newExportFileOffset = newExportSerialOffset - DOLib.Int32FromSpanOffset(Program.runData.uasset, headerSizeOffset);

            Span<byte> stubExport = new Span<byte>(new byte[12]);
            DOLib.WriteInt32IntoOffset(stubExport, FindNameIndex(Program.runData.uasset, "None").Value, 0);
            uexp = Insert(uexp, stubExport, newExportFileOffset);

            if (File.Exists(Program.runData.uexpFileName + ".AddStubExportBackup")) File.Delete(Program.runData.uexpFileName + ".AddStubExportBackup");
            File.Move(Program.runData.uexpFileName, Program.runData.uexpFileName + ".AddStubExportBackup");

            File.WriteAllBytes(Program.runData.uexpFileName, uexp.ToArray());

            Program.CallOffSetterWithArgs($" -e 12 {newExportSerialOffset} -r -m");

            Program.runData.uasset = File.ReadAllBytes(Program.runData.uassetFileName);

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
        /// <returns>Span with ExportDefinition </returns>
        private static Span<byte> MakeExportDef(Int32 _class, Int32 super, Int32 template, Int32 outer, Int32 name, Int32 nameAug, Int32 flags,
            Int32 size, Int32 serialOffset, List<Int32> other)
        {
            Span<byte> result = new Span<byte>(new byte[exportDefinitionSize]);

            DOLib.WriteInt32IntoOffset(result, _class, relativeClassOffset);
            DOLib.WriteInt32IntoOffset(result, super, relativeSuperOffset);
            DOLib.WriteInt32IntoOffset(result, template, relativeTemlateOffset);
            DOLib.WriteInt32IntoOffset(result, outer, relativeOuterOffset);
            DOLib.WriteInt32IntoOffset(result, name, relativeObjectNameOffset);
            DOLib.WriteInt32IntoOffset(result, nameAug, relativeObjectNameOffset + 4);
            DOLib.WriteInt32IntoOffset(result, flags, relativeObjectFlagsOffset);
            DOLib.WriteInt32IntoOffset(result, size, relativeSerialSizeOffset);
            DOLib.WriteInt32IntoOffset(result, serialOffset, relativeSerialOffsetOffset);
            Int32 currentOtherOffset = relativeOtherDataOffset;
            for (int i = 0; i < otherDataInt32Count; i++)
            {
                DOLib.WriteInt32IntoOffset(result, other[i], currentOtherOffset);
                currentOtherOffset += 4;
            }

            return result;
        }
    }
}
