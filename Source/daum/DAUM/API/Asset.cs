using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DAUM.API
{
    public class Asset
    {
        private string path;

        private string UAssetPath => path + ".uasset";
        private string UExpPath => path + ".uexp";

        private byte[] uasset;
        private byte[] uexp;

        private OffsetData offsetData;

        public string[] nameMap;
        public ImportDefinition[] importMap;
        public ExportDefinition[] exportMap;

        public Asset(string filePath)
        {
            path = filePath;

            uasset = File.ReadAllBytes(UAssetPath);
            uexp = File.ReadAllBytes(UExpPath);

            offsetData = new OffsetData();
            offsetData.ApplyCustomVersionSize(
                BitConverter.ToInt32(uasset, offsetData.customVersionCountOffset) *
                offsetData.customVersionElementSize);

            FillNameMap();
            FillImportMap();
        }

        private void FillNameMap()
        {
            Int32 nameCount = BitConverter.ToInt32(uasset, offsetData.nameCountOffset);
            Int32 currentNameOffset = BitConverter.ToInt32(uasset, offsetData.nameOffsetOffset);

            nameMap = new string[nameCount];

            for (Int32 i = 0; i < nameCount; i++)
            {
                nameMap[i] = CustomValueReader.SizePrefixedStringFromOffsetOffsetAdvance(uasset, ref currentNameOffset);

                currentNameOffset += offsetData.nameHashesSize;
            }
        }

        private void FillImportMap()
        {
        }
    }
}
