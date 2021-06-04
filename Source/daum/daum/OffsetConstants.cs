﻿using System;

namespace daum
{
    static class OffsetConstants
    {
        // ----------------------------------------- 
        // NameMap
        public const Int32 nameOffsetOffset = 45;
        public const Int32 nameCountOffset = 41;

        public const Int32 stringSizeDesignationSize = 4;
        public const Int32 nameHashesSize = 4;

        // ----------------------------------------- 
        // ImportMap
        public const Int32 importOffsetOffset = 69;
        public const Int32 importCountOffset = 65;

        public const Int32 importNameOffset = 20;
        public const Int32 importPackageOffset = 0;
        public const Int32 importClassOffset = 8;
        public const Int32 importOuterIndexOffset = 16;
        public const Int32 importDefSize = 28;
    }
}
