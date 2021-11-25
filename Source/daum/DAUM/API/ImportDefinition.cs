using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAUM.API
{
    public class ImportDefinition
    {
        public NameReference packageName;
        public NameReference className;
        public Int32 outerIndex;
        public NameReference importName;

        public string ToString(Asset contextAsset)
        {
            return importName.ToString(contextAsset);
        }
    }
}
