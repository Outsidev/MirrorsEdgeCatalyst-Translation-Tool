using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    class Sb : MainEntry
    {

        public Sb(string _filePath, long offset = 0) : base(_filePath)
        {            
            ReadFile(_filePath,offset);
        } 
             

        public override string[] GetFileDirectories()
        {
            List<string> paths = new List<string>();
                        
            foreach (Entry bundle in mainEntry.subTypes["bundles"])
            {
                foreach (Entry subEnt in bundle.subTypes["ebx"])
                {
                    paths.Add(Path.Combine(bundle.subTypes["path"],subEnt.subTypes["name"]));
                }
                foreach (Entry subEnt in bundle.subTypes["dbx"])
                {
                    paths.Add(Path.Combine(bundle.subTypes["path"], subEnt.subTypes["name"]));
                }
            }
            return paths.ToArray();
        }
    }
}
