using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAUM.API
{
    public class NameReference
    {
        public int nameMapIndex;
        public int aug;

        public string ToString(Asset contextAsset)
        {
            return $"{contextAsset.nameMap[nameMapIndex]} : {aug}";
        }
    }
}
