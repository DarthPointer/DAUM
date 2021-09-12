using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace daum.OffSetter
{
    abstract class OffSetterBlock
    {
        protected abstract ReadOnlyCollection<Int32> GetAffectedGlobalOffsets();

        public void OffSet(byte[] span, Dictionary<RequiredOffSettingData, Int32> args)
        {
            if (args.ContainsKey(RequiredOffSettingData.SizeChange))
            {
                foreach (Int32 offset in GetAffectedGlobalOffsets())
                {
                   DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.SizeChange], offset);
                }
            }

            LocalOffSet(span, args);
        }

        protected abstract void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, Int32> args);

        public abstract void PreviousBlocksOffSet(byte[] span, Int32 sizeChange);
    }

    [Block(humanReadableBlockName = "names map", offSettingArguments = RequiredOffSettingData.SizeChange | RequiredOffSettingData.CountChange)]
    class NamesMap : OffSetterBlock
    {
        protected override ReadOnlyCollection<Int32> GetAffectedGlobalOffsets()
        {
            return Array.AsReadOnly(new Int32[] { Program.runData.headerOffsets.totalHeaderSizeOffset,
            Program.runData.headerOffsets.exportOffsetOffset, Program.runData.headerOffsets.importOffsetOffset,
            Program.runData.headerOffsets.dependsOffsetOffset, Program.runData.headerOffsets.assetRegistryDataOffsetOffset,
            Program.runData.headerOffsets.bulkDataOffsetOffset, Program.runData.headerOffsets.preloadDependencyOffsetOffset});
        }

        protected override void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, int> args)
        {
            foreach (Int32 offset in new Int32[] { Program.runData.headerOffsets.nameCountOffset, Program.runData.headerOffsets.nameCountOffset2 })
            {
                DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.CountChange], offset);
            }
        }

        public override void PreviousBlocksOffSet(byte[] span, int sizeChange) { }
    }

    [Block(humanReadableBlockName = "imports map", offSettingArguments = RequiredOffSettingData.SizeChange | RequiredOffSettingData.CountChange)]
    class ImportsMap : OffSetterBlock
    {
        protected override ReadOnlyCollection<Int32> GetAffectedGlobalOffsets()
        {
            return Array.AsReadOnly(new Int32[] { Program.runData.headerOffsets.totalHeaderSizeOffset,
            Program.runData.headerOffsets.exportOffsetOffset, Program.runData.headerOffsets.dependsOffsetOffset,
            Program.runData.headerOffsets.assetRegistryDataOffsetOffset, Program.runData.headerOffsets.bulkDataOffsetOffset,
            Program.runData.headerOffsets.preloadDependencyOffsetOffset});
        }

        protected override void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, int> args)
        {
            DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.CountChange], Program.runData.headerOffsets.importCountOffset);
        }

        public override void PreviousBlocksOffSet(byte[] span, int sizeChange) { }
    }

    [Block(humanReadableBlockName = "exports map", offSettingArguments = RequiredOffSettingData.SizeChange | RequiredOffSettingData.CountChange)]
    class ExportsMap : OffSetterBlock
    {
        protected override ReadOnlyCollection<Int32> GetAffectedGlobalOffsets()
        {
            return Array.AsReadOnly(new Int32[] { Program.runData.headerOffsets.totalHeaderSizeOffset,
            Program.runData.headerOffsets.dependsOffsetOffset, Program.runData.headerOffsets.assetRegistryDataOffsetOffset,
            Program.runData.headerOffsets.bulkDataOffsetOffset, Program.runData.headerOffsets.preloadDependencyOffsetOffset});
        }

        protected override void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, int> args)
        {
            DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.CountChange], Program.runData.headerOffsets.exportCountOffset);
            DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.CountChange], Program.runData.headerOffsets.exportCountOffset2);
        }

        public override void PreviousBlocksOffSet(byte[] span, int sizeChange) { }
    }

    [Block(humanReadableBlockName = "exported (.uexp) data", offSettingArguments = RequiredOffSettingData.SizeChange | RequiredOffSettingData.SizeChangeOffset)]
    class Exports : OffSetterBlock
    {
        protected override ReadOnlyCollection<Int32> GetAffectedGlobalOffsets()
        {
            return Array.AsReadOnly(new Int32[] { Program.runData.headerOffsets.bulkDataOffsetOffset });
        }

        protected override void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, int> args)
        {
            SerialOffsetting(span, args[RequiredOffSettingData.SizeChange], args[RequiredOffSettingData.SizeChangeOffset]);
        }

        public override void PreviousBlocksOffSet(byte[] span, int sizeChange)
        {
            SerialOffsetting(span, sizeChange, 0);
        }

        #region serial offsetting tools

        private static void SerialOffsetting(byte[] span, Int32 sizeChange, Int32 sizeChangeOffset)
        {
            Int32 exportsCount = BitConverter.ToInt32(span, Program.runData.headerOffsets.exportCountOffset);
            Int32 exportsMapOffset = BitConverter.ToInt32(span, Program.runData.headerOffsets.exportOffsetOffset);

            Int32 currentReferenceOffset = exportsMapOffset;
            bool applyOffsetChange = false;
            int processedReferences = 0;

            while (processedReferences < exportsCount)
            {

                if (!applyOffsetChange)
                {
                    RelativeSizeChangeDirection direction = CheckOffsetAffected(span, sizeChangeOffset, currentReferenceOffset);
                    if (direction == RelativeSizeChangeDirection.here)
                    {
                        ApplySerialSizeChange(span, sizeChange, currentReferenceOffset);
                        applyOffsetChange = true;
                    }

                    if (direction == RelativeSizeChangeDirection.before)
                    {
                        ApplySerialOffsetChange(span, sizeChange, currentReferenceOffset);
                        applyOffsetChange = true;
                    }
                }
                else
                {
                    ApplySerialOffsetChange(span, sizeChange, currentReferenceOffset);
                }

                currentReferenceOffset += Program.runData.headerOffsets.exportDefSize;
                processedReferences++;
            }
        }

        private static RelativeSizeChangeDirection CheckOffsetAffected(byte[] span, Int32 sizeChangeSerialOffset, Int32 referenceOffset)
        {
            Int32 serialOffset = BitConverter.ToInt32(span, referenceOffset + Program.runData.headerOffsets.exportSerialOffsetOffset);

            if (serialOffset > sizeChangeSerialOffset) return RelativeSizeChangeDirection.before;
            else if (serialOffset == sizeChangeSerialOffset) return RelativeSizeChangeDirection.here;
            else /*if (serialOffset < sizeChangeSerialOffset)*/ return RelativeSizeChangeDirection.after;
        }

        private static void ApplySerialSizeChange(byte[] span, Int32 sizeChange, Int32 referenceOffset)
        {
            DAUMLib.AddToInt32ByOffset(span, sizeChange, referenceOffset + Program.runData.headerOffsets.exportSerialSizeOffset);
        }

        private static void ApplySerialOffsetChange(byte[] span, Int32 sizeChange, Int32 referenceOffset)
        {
            DAUMLib.AddToInt32ByOffset(span, sizeChange, referenceOffset + Program.runData.headerOffsets.exportSerialOffsetOffset);
        }

        private enum RelativeSizeChangeDirection
        {
            after = 0,
            here = 1,
            before = 2
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class BlockAttribute : Attribute
    {
        public RequiredOffSettingData offSettingArguments;
        public string humanReadableBlockName;
    }

    public enum RequiredOffSettingData
    {
        None = 0,
        SizeChange = 1,
        SizeChangeOffset = 2,
        CountChange = 4
    }
}
