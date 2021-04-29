using System;
using System.Collections.Generic;
using DRGOffSetterLib;

namespace daum
{
    public abstract class Operation
    {
        public abstract string ExecuteAndGetOffSetterAgrs(ref Span<byte> span, List<string> args);

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
    }

    public abstract class MapOperation : Operation
    {
        private static string addOpKey = "-a";
        private static string replaceKey = "-r";
        protected static string byIndexKey = "-i";
        protected static string skipKey = "-s";

        protected static Int32 nameOffsetOffset = 45;
        protected static Int32 nameCountOffset = 41;

        protected static Int32 importOffsetOffset = 69;
        protected static Int32 importCountOffset = 65;

        protected static Int32 exportOffsetOffset = 61;

        protected static Int32 stringSizeDesignationSize = 4;

        protected abstract Int32 nextBlockOffsetOffset { get; }
        protected abstract Int32 thisBlockOffsetOffset { get; }
        protected abstract Int32 thisBlockRecordCountOffset { get; }

        public override string ExecuteAndGetOffSetterAgrs(ref Span<byte> span, List<string> args)
        {
            string opKey = args.TakeArg();
            if (opKey == addOpKey)
            {
                return AddOperation(ref span, args, DOLib.Int32FromSpanOffset(span, nextBlockOffsetOffset));
            }
            if (opKey == replaceKey)
            {
                Int32? replaceOffset;
                if (args[0] == byIndexKey)
                {
                    args.TakeArg();
                    replaceOffset = FindByIndex(span, args,
                        DOLib.Int32FromSpanOffset(span, thisBlockOffsetOffset), DOLib.Int32FromSpanOffset(span, thisBlockRecordCountOffset));
                }
                else
                {
                    replaceOffset = FindByName(span, args,
                        DOLib.Int32FromSpanOffset(span, thisBlockOffsetOffset), DOLib.Int32FromSpanOffset(span, thisBlockRecordCountOffset));
                }

                if (replaceOffset.HasValue) return ReplaceOperation(ref span, args, replaceOffset.Value);
                else throw new KeyNotFoundException("Key specifying record to replace is not present in a map");
            }

            throw new ArgumentException("Arguments are invalid for replacement operation");
        }

        protected abstract Int32? FindByName(Span<byte> span, List<string> args, Int32 mapOffset, Int32 mapRecordsCount);

        protected abstract Int32? FindByIndex(Span<byte> span, List<string> args, Int32 mapOffset, Int32 mapRecordsCount);

        protected abstract string ReplaceOperation(ref Span<byte> span, List<string> args, Int32 replaceAtOffset);

        protected abstract string AddOperation(ref Span<byte> span, List<string> args, Int32 addAtOffset);

        protected static string StringFromNameDef(Span<byte> span, Int32 offset)
        {
            Int32 size = DOLib.Int32FromSpanOffset(span, offset);

            Span<byte> scope = span.Slice(offset + stringSizeDesignationSize, size - 1);
            string result = "";

            foreach (char i in scope)
            {
                result += i;
            }

            return result;
        }
    }

    public class NameDefOperation : MapOperation
    {
        protected override Int32 nextBlockOffsetOffset => importOffsetOffset;
        protected override Int32 thisBlockOffsetOffset => nameOffsetOffset;
        protected override Int32 thisBlockRecordCountOffset => nameCountOffset;

        protected override string AddOperation(ref Span<byte> span, List<string> args, Int32 addAtOffset)
        {
            Span<byte> insert = MakeNameDef(args[0]);
            span = Insert(span, insert, addAtOffset);

            return $" -n {insert.Length} 1";
        }

        protected override Int32? FindByName(Span<byte> span, List<string> args, Int32 mapOffset, Int32 mapRecordsCount)
        {
            Int32 currentNameOffset = mapOffset;
            string name = args.TakeArg();

            for (int processedRecords = 0; processedRecords < mapRecordsCount; processedRecords++)
            {
                string storedName = StringFromNameDef(span, currentNameOffset);
                if (storedName == name)
                {
                    return currentNameOffset;
                }

                currentNameOffset += 9 + storedName.Length;
            }

            return null;
        }

        protected override Int32? FindByIndex(Span<byte> span, List<string> args, Int32 mapOffset, Int32 mapRecordsCount)
        {
            Int32 index = Int32.Parse(args.TakeArg());

            if (index < mapRecordsCount)
            {
                Int32 currentNameOffset = mapOffset;

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentNameOffset += DOLib.Int32FromSpanOffset(span, currentNameOffset) + 8;
                }

                return currentNameOffset;
            }

            return null;
        }

        protected override string ReplaceOperation(ref Span<byte> span, List<string> args, Int32 replaceAtOffset)
        {
            string newName = args.TakeArg();

            Int32 oldNameStoredSize = DOLib.Int32FromSpanOffset(span, replaceAtOffset);
            Int32 sizeChange = newName.Length + 1 - oldNameStoredSize;

            span = Remove(span, replaceAtOffset, oldNameStoredSize + 8);
            span = Insert(span, MakeNameDef(newName), replaceAtOffset);

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
        private static Int32 importPackageOffset = 0;
        private static Int32 importClassOffset = 8;
        private static Int32 importOuterIndexOffset = 16;
        private static Int32 importNameOffset = 20;

        private static Int32 importDefSize = 28;

        protected override Int32 nextBlockOffsetOffset => exportOffsetOffset;

        protected override Int32 thisBlockOffsetOffset => importOffsetOffset;
        protected override Int32 thisBlockRecordCountOffset => importCountOffset;

        protected override string AddOperation(ref Span<byte> span, List<string> args, int addAtOffset)
        {
            Int32 package = GetNameIndex(span, args).Value;
            Int32 _class = GetNameIndex(span, args).Value;
            Int32 outerIndex = GetImportIndex(span, args).Value;
            Int32 name = GetNameIndex(span, args).Value;

            span = Insert(span, MakeImportDef(package, _class, outerIndex, name), addAtOffset);

            return $" -i {importDefSize} 1";
        }

        protected override string ReplaceOperation(ref Span<byte> span, List<string> args, int replaceAtOffset)
        {
            Int32? package = GetNameIndex(span, args);
            Int32? _class = GetNameIndex(span, args);
            Int32? outerIndex = GetImportIndex(span, args);
            Int32? name = GetNameIndex(span, args);

            if (package != null) DOLib.WriteInt32IntoOffset(span, package.Value, replaceAtOffset + importPackageOffset);
            if (_class != null) DOLib.WriteInt32IntoOffset(span, _class.Value, replaceAtOffset + importClassOffset);
            if (outerIndex != null) DOLib.WriteInt32IntoOffset(span, outerIndex.Value, replaceAtOffset + importOuterIndexOffset);
            if (name != null) DOLib.WriteInt32IntoOffset(span, name.Value, replaceAtOffset + importNameOffset);

            return "";
        }

        protected override Int32? FindByName(Span<byte> span, List<string> args, int mapOffset, int mapRecordsCount)
        {
            Int32 nameIndex = (Int32)FindNameIndex(span, args.TakeArg());

            Int32 currentImportOffset = mapOffset;

            for (int processedRecords = 0; processedRecords < mapRecordsCount; processedRecords++)
            {
                Int32 recordNameIndex = NameIndexFromImportDef(span, currentImportOffset);
                if (recordNameIndex == nameIndex)
                {
                    return currentImportOffset;
                }

                currentImportOffset += importDefSize;
            }

            return null;
        }

        protected override int? FindByIndex(Span<byte> span, List<string> args, int mapOffset, int mapRecordsCount)
        {
            Int32 index = -1*Int32.Parse(args.TakeArg()) - 1;

            if (index < mapRecordsCount)
            {
                Int32 currentNameOffset = mapOffset;

                for (Int32 currentIndex = 0; currentIndex < index; currentIndex++)
                {
                    currentNameOffset += importDefSize;
                }

                return currentNameOffset;
            }

            return null;
        }

        private static Span<byte> MakeImportDef(Int32 package, Int32 _class, Int32 outerIndex, Int32 name)
        {
            Span<byte> result = new Span<byte>(new byte[importDefSize]);

            DOLib.WriteInt32IntoOffset(result, package, importPackageOffset);
            DOLib.WriteInt32IntoOffset(result, _class, importClassOffset);
            DOLib.WriteInt32IntoOffset(result, outerIndex, importOuterIndexOffset);
            DOLib.WriteInt32IntoOffset(result, name, importNameOffset);

            return result;
        }

        private static Int32? GetNameIndex(Span<byte> span, List<string> args)
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

        private static Int32? FindNameIndex(Span<byte> span, string name)
        {
            Int32 currentNameOffset = DOLib.Int32FromSpanOffset(span, nameOffsetOffset);

            for (int processedRecords = 0; processedRecords < DOLib.Int32FromSpanOffset(span, nameCountOffset); processedRecords++)
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

        private static Int32? GetImportIndex(Span<byte> span, List<string> args)
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

        private static Int32? FindImportIndex(Span<byte> span, string name)
        {
            Int32 nameIndex = (Int32)FindNameIndex(span, name);

            Int32 currentImportOffset = DOLib.Int32FromSpanOffset(span, importOffsetOffset);

            for (int processedRecords = 0; processedRecords < DOLib.Int32FromSpanOffset(span, importCountOffset); processedRecords++)
            {
                Int32 recordNameIndex = NameIndexFromImportDef(span, currentImportOffset);
                if (recordNameIndex == nameIndex)
                {
                    return -1*processedRecords -1;
                }

                currentImportOffset += importDefSize;
            }

            return null;
        }

        private static Int32 NameIndexFromImportDef(Span<byte> span, Int32 offset)
        {
            return DOLib.Int32FromSpanOffset(span, offset + importNameOffset);
        }
    }
}
