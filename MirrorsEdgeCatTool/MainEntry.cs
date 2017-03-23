using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    class MainEntry
    {
        public string filePath;
        public Entry mainEntry;

        public MainEntry(string _filePath)
        {
            filePath = _filePath;
            if(filePath.Contains("Win32"))
                filePath = _filePath.Substring(_filePath.IndexOf("Win32"));
            ReadFile(_filePath);
        }

        public void ReadFile(string path, long offset=0)
        {
            using (BinaryReader binred = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                binred.BaseStream.Position = offset;
                mainEntry = new Entry(binred);
            }
        }

        public virtual string[] GetFileDirectories()
        {
            List<string> paths = new List<string>();
            foreach (Entry bundle in mainEntry.subTypes["bundles"])
            {
                paths.Add(bundle.subTypes["name"]);
            }
            return paths.ToArray();
        }
    }
}
