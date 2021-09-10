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
            return Array.AsReadOnly(new Int32[] { HeaderOffsets.totalHeaderSizeOffset,
            HeaderOffsets.exportOffsetOffset, HeaderOffsets.importOffsetOffset,
            HeaderOffsets.dependsOffsetOffset, HeaderOffsets.assetRegistryDataOffsetOffset,
            HeaderOffsets.bulkDataOffsetOffset, HeaderOffsets.preloadDependencyOffsetOffset});
        }

        protected override void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, int> args)
        {
            foreach (Int32 offset in new Int32[] { HeaderOffsets.nameCountOffset, HeaderOffsets.nameCountOffset2 })
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
            return Array.AsReadOnly(new Int32[] { HeaderOffsets.totalHeaderSizeOffset,
            HeaderOffsets.exportOffsetOffset, HeaderOffsets.dependsOffsetOffset,
            HeaderOffsets.assetRegistryDataOffsetOffset, HeaderOffsets.bulkDataOffsetOffset,
            HeaderOffsets.preloadDependencyOffsetOffset});
        }

        protected override void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, int> args)
        {
            DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.CountChange], HeaderOffsets.importCountOffset);
        }

        public override void PreviousBlocksOffSet(byte[] span, int sizeChange) { }
    }

    [Block(humanReadableBlockName = "exports map", offSettingArguments = RequiredOffSettingData.SizeChange | RequiredOffSettingData.CountChange)]
    class ExportsMap : OffSetterBlock
    {
        protected override ReadOnlyCollection<Int32> GetAffectedGlobalOffsets()
        {
            return Array.AsReadOnly(new Int32[] { HeaderOffsets.totalHeaderSizeOffset,
            HeaderOffsets.dependsOffsetOffset, HeaderOffsets.assetRegistryDataOffsetOffset,
            HeaderOffsets.bulkDataOffsetOffset, HeaderOffsets.preloadDependencyOffsetOffset});
        }

        protected override void LocalOffSet(byte[] span, Dictionary<RequiredOffSettingData, int> args)
        {
            DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.CountChange], HeaderOffsets.exportCountOffset);
            DAUMLib.AddToInt32ByOffset(span, args[RequiredOffSettingData.CountChange], HeaderOffsets.exportCountOffset2);
        }

        public override void PreviousBlocksOffSet(byte[] span, int sizeChange) { }
    }

    [Block(humanReadableBlockName = "exported (.uexp) data", offSettingArguments = RequiredOffSettingData.SizeChange | RequiredOffSettingData.SizeChangeOffset)]
    class Exports : OffSetterBlock
    {
        protected override ReadOnlyCollection<Int32> GetAffectedGlobalOffsets()
        {
            return Array.AsReadOnly(new Int32[] { HeaderOffsets.bulkDataOffsetOffset });
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
            Int32 exportsCount = BitConverter.ToInt32(span, HeaderOffsets.exportCountOffset);
            Int32 exportsMapOffset = BitConverter.ToInt32(span, HeaderOffsets.exportOffsetOffset);

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

                currentReferenceOffset += HeaderOffsets.exportDefSize;
                processedReferences++;
            }
        }

        private static RelativeSizeChangeDirection CheckOffsetAffected(byte[] span, Int32 sizeChangeSerialOffset, Int32 referenceOffset)
        {
            Int32 serialOffset = BitConverter.ToInt32(span, referenceOffset + HeaderOffsets.exportSerialOffsetOffset);

            if (serialOffset > sizeChangeSerialOffset) return RelativeSizeChangeDirection.before;
            else if (serialOffset == sizeChangeSerialOffset) return RelativeSizeChangeDirection.here;
            else /*if (serialOffset < sizeChangeSerialOffset)*/ return RelativeSizeChangeDirection.after;
        }

        private static void ApplySerialSizeChange(byte[] span, Int32 sizeChange, Int32 referenceOffset)
        {
            DAUMLib.AddToInt32ByOffset(span, sizeChange, referenceOffset + HeaderOffsets.exportSerialSizeOffset);
        }

        private static void ApplySerialOffsetChange(byte[] span, Int32 sizeChange, Int32 referenceOffset)
        {
            DAUMLib.AddToInt32ByOffset(span, sizeChange, referenceOffset + HeaderOffsets.exportSerialOffsetOffset);
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
