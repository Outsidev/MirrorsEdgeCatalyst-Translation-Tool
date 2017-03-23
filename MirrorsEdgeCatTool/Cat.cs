using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    struct Kat
    {
        public Cat motherCat;
        public byte[] sha1;
        public uint offset;
        public UInt64 size;
        public uint casNo;
    }

    class Cat
    {
        public string filePath;
        public List<Kat> katList;
        public Cat(string _filePath)
        {
            filePath = _filePath;
            katList = new List<Kat>();
            ReadFile(filePath);
        }

        public void ReadFile(string path)
        {
            using(BinaryReader binred = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                binred.BaseStream.Position = 572;
                int catCount = binred.ReadInt32();
                int dafak = binred.ReadInt32();
                for(int i=0; i<catCount; i++)
                {
                    Kat kat = new Kat();
                    kat.sha1 = binred.ReadBytes(20);
                    kat.offset = binred.ReadUInt32();
                    kat.size = binred.ReadUInt64();
                    kat.casNo = binred.ReadUInt32();
                    kat.motherCat = this;
                    katList.Add(kat);
                }
            }
        }

        public string GetCasPath(uint casNo)
        {
            string casPa = Path.Combine(Path.GetDirectoryName(filePath), "cas_0"+casNo+".cas");
            return casPa;
        }

    }
}
