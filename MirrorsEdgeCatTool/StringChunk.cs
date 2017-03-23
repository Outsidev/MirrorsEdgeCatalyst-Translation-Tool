using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{

    struct Liner
    {
        public uint id;
        public uint subPos;
        public uint grup;
        public string line;
        public byte[] lineBytes;
    }

    class StringChunk
    {
        public Liner[] allLiners;
        BinaryReader binred;
        uint mainOffset = 8;
        
        public StringChunk(byte[] languageData)
        {
            readData(languageData);
        }

        private void readData(byte[] data)
        {
            using (binred = new BinaryReader(new MemoryStream(data)))
            {

                binred.BaseStream.Position = 4; //skip header
                uint extractedSize = binred.ReadUInt32();
                uint lineCount = binred.ReadUInt32();
                uint idStartPos = binred.ReadUInt32() + mainOffset;
                uint textStartPos = binred.ReadUInt32() + mainOffset;
                binred.ReadBytes(6);//Globals               

                allLiners = new Liner[lineCount];

                binred.BaseStream.Position = idStartPos;
                for (int i = 0; i < lineCount; i++)
                {
                    Liner lin = new Liner();
                    lin.id = binred.ReadUInt32();
                    lin.subPos = binred.ReadUInt16();
                    lin.grup = binred.ReadUInt16();
                    long tempPos = binred.BaseStream.Position;

                    binred.BaseStream.Position = textStartPos + lin.grup * 65536 + lin.subPos;
                    lin.lineBytes = Tools.ReadTillNull(binred);

                    //x00 null, x0A newline, xA0 hardspace
                    lin.line = Encoding.GetEncoding("ISO-8859-1").GetString(lin.lineBytes).TrimEnd('\x00');
                    for (var k = 0; k < lin.line.Length; k++)
                    {
                        byte chB = (byte)lin.line[k];
                        if ( (chB > 129 && chB < 209) || chB < 32)
                        {
                            lin.line = lin.line.Replace(lin.line[k].ToString(), "<*" + chB.ToString() + ">");
                        }
                    }
                    allLiners[i] = lin;
                    binred.BaseStream.Position = tempPos;
                }

                allLiners = allLiners.OrderBy(p => p.grup).ThenBy(p => p.subPos).ToArray();

            }
        }

    }
}
