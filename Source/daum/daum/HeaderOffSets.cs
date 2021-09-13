using System;

namespace daum
{
    public class HeaderOffsets
    {
        // ----------------------------------------- 
        // NameMap
        public Int32 nameOffsetOffset = 45;
        public Int32 nameCountOffset = 41;

        public Int32 stringSizeDesignationSize = 4;
        public Int32 nameHashesSize = 4;

        // ----------------------------------------- 
        // ImportMap
        public Int32 importOffsetOffset = 69;
        public Int32 importCountOffset = 65;

        public Int32 importNameOffset = 20;
        public Int32 importPackageOffset = 0;
        public Int32 importClassOffset = 8;
        public Int32 importOuterIndexOffset = 16;
        public Int32 importDefSize = 28;

        // ----------------------------------------- 
        // ExportMap
        public Int32 exportOffsetOffset = 61;
        public Int32 exportCountOffset = 57;

        public Int32 exportClassOffset = 0;
        public Int32 exportSuperOffset = 4;
        public Int32 exportTemplateOffset = 8;
        public Int32 exportOuterOffset = 12;
        public Int32 exportNameOffset = 16;
        public Int32 exportObjectFlagsOffset = 24;
        public Int32 exportSerialSizeOffset = 28;
        public Int32 exportSerialOffsetOffset = 36;

        public Int32 exportOtherDataOffset = 44;
        public int exportOtherDataInt32Count = 15;

        public Int32 exportDefSize = 104;



        // -----------------------------------------
        // Extra 

        public Int32 customVersionCountOffset = 20;

        public Int32 customVersionElementSize = 20;

        public Int32 totalHeaderSizeOffset = 24;

        public Int32 dependsOffsetOffset = 73;

        public Int32 exportCountOffset2 = 113;

        public Int32 nameCountOffset2 = 117;

        public Int32 assetRegistryDataOffsetOffset = 165;

        public Int32 bulkDataOffsetOffset = 169;

        public Int32 preloadDependencyOffsetOffset = 189;


        public void ApplyCustomVersionSize(int size)
        {
            nameOffsetOffset += size;
            nameCountOffset += size;
            importOffsetOffset += size;
            importCountOffset += size;
            exportOffsetOffset += size;
            exportCountOffset += size;

            totalHeaderSizeOffset += size;
            dependsOffsetOffset += size;
            exportCountOffset2 += size;
            nameCountOffset2 += size;
            assetRegistryDataOffsetOffset += size;
            bulkDataOffsetOffset += size;
            preloadDependencyOffsetOffset += size;
        }
    }
}
