using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{    
    class Toc : MainEntry
    {

        public Toc(string _filePath) : base(_filePath)
        {
            ReadFile(_filePath, 556);
        }

        public override string[] GetFileDirectories()
        {
            List<string> paths = new List<string>();
            foreach (Entry bundle in mainEntry.subTypes["bundles"])
            {
                paths.Add(bundle.subTypes["id"]);
            }
            return paths.ToArray();
        }
    }
}
