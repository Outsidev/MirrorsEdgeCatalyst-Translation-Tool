using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    class Patcher
    {

        public Patcher()
        {

        }

        public void PatchCasFile(string file, Cat katFile, Entry engEnt)
        {
            uint addedByte = 342680, addSize=0;
            bool passed = false;
            Kat engCat = new Kat();
            uint newOffset = 31797010;
            foreach (var kat  in katFile.katList)
            {
                if(Tools.CompareByteArrays(kat.sha1, engEnt.subTypes["sha1"]))
                {
                    engCat = kat;
                    break;
                }
            }

            using (BinaryReader binred = new BinaryReader(File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                using(BinaryWriter binwr = new BinaryWriter(File.Open(file+".PATCHED",FileMode.Create)))
                {
                    binwr.Write(binred.ReadBytes(580));
                    for (int i = 0; i < katFile.katList.Count; i++)
                    {
                        Kat kat = katFile.katList[i];
                        byte[] sha1 = binred.ReadBytes(20);
                        binwr.Write(sha1);

                        uint offset = binred.ReadUInt32();
                        UInt64 size = binred.ReadUInt64();
                        uint casNo = binred.ReadUInt32();

                        if (!passed && Tools.CompareByteArrays(sha1, engCat.sha1))
                        {
                            passed = true;
                            binwr.Write(newOffset);
                            binwr.Write(size);
                            binwr.Write(casNo);
                            continue;
                        }

                        if (casNo == engCat.casNo && offset > engCat.offset)
                            binwr.Write(offset - addedByte);
                        else
                            binwr.Write(offset);                        

                        binwr.Write(size);
                        binwr.Write(casNo);
                    }
                    long remaining = binred.BaseStream.Length-binred.BaseStream.Position;
                    binwr.Write(binred.ReadBytes((int)remaining));
                }
            }
        }
    }
}
