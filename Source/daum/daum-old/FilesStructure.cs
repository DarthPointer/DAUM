using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace daum
{

    [JsonObject]
    class FilesStructure
    {
        public static FilesStructure instance;
        public static FileStructure currentFile;

        public Dictionary<string, FileStructure> files = new Dictionary<string, FileStructure>();
    }

    [JsonObject]
    class FileStructure
    {
        public NameMapStamp nameMap;
        public ImportMapStamp importMap;
        public ExportMapStamp exportMap;

        public ExportExpansion[] exportsExpansion;
    }

    [JsonObject]
    class NameMapStamp
    {
        public NameStamp[] names;
    }

    class NameStamp
    {
        public int thisIndex;
        public string name;
    }

    [JsonObject]
    class ImportMapStamp
    {
        public ImportStamp[] imports;
    }

    [JsonObject]
    class ImportStamp
    {
        public int thisIndex;
        public string package;
        public string _class;
        public string outer;
        public string name;
    }

    [JsonObject]
    class ExportMapStamp
    {
        public ExportDefinitionStamp[] exports;
    }

    [JsonObject]
    class ExportDefinitionStamp
    {
        public int thisIndex;
        public string _class;
        public Int32 super;
        public string template;
        public string outer;
        public string name;
        public Int32 serialSize;
        public Int32 serialOffset;
        public Int32 flags;

        public Int32[] otherData;
    }

    [JsonObject]
    class ExportExpansion
    {
        public int thisIndex;
        public string name;

        public List<IPropertyElement> properties;
    }

    [JsonObject]
    class Property : IPropertyElement
    {
        public string name;
        public string type;
        public Int32 size;
        public Int32 sizeStartOffset;

        public List<IPropertyElement> contents;
    }

    [JsonObject]
    interface IPropertyElement
    {
    }

    [JsonObject]
    class SimpleElement<T> : IPropertyElement
    {
        public string name;
        public T value;
    }

    [JsonObject]
    class ArrayElement : IPropertyElement
    {
        public int index;
        public List<IPropertyElement> contents;
    }

    //[JsonObject]
    //abstract class StrcutProperty : Property
    //{
    //    public string structType;
    //}

    //[JsonObject]
    //class NTPLStructProperty : Property
    //{
    //    public List<Property> propertyList;
    //}

    //[JsonObject]
    //class ArrayProperty : Property
    //{
    //    public string elementType;
    //    public Int32 elementCount;
    //}

    //[JsonObject]
    //class ObjectProperty : Property
    //{
    //    public string objectName;
    //}
}
