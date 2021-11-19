using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAUM.API
{
    public class Asset
    {
        string path;

        string UAssetPath => path + ".uasset";
        string UExpPath => path + ".uexp";

        byte[] uasset;
        byte[] uexp;
    }
}
