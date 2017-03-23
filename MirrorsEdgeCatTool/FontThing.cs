using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    struct FontTables
    {
        public string tag;
        public uint checkSum;
        public uint offset;
        public uint length;
        public byte[] data;
    }

    struct CMap
    {
        public UInt16 version;
        public UInt16 numTables;
        public EncodingRecord[] encodingRecs;
    }

    struct EncodingRecord
    {
        public UInt16 platformID;
        public UInt16 encodingID;
        public uint offset;

    }

    class FontThing
    {
        public Dictionary<string, FontTables> fontDict;
        public Dictionary<string, byte[]> fontData;
        public FontTables[] fontTables;
        public FontTables[] orderedfontTables;
        public int fileLength = 0;

        uint header;
        UInt16 numTables;
        UInt16 searchRange;                
        UInt16 entrySelector;
        UInt16 rangeShift;

        public FontThing(string fontPath)
        {
            fontDict = new Dictionary<string, FontTables>();
            fontData = new Dictionary<string, byte[]>();
            fontTables = readFontFile(fontPath);
            orderedfontTables = fontTables.OrderBy(p => p.offset).ToArray();
        }

        public void WriteNewFont(FontThing font2)
        {
            string expPath = @"C:\Users\Burk\Desktop\mirrorsEdgeCatShit\MEC\europeFontNEW.otf";
            using (BinaryWriter binwrit = new BinaryWriter(File.Open(expPath, FileMode.Create)))
            {
                binwrit.Write(GetBigEnd32(header));
                binwrit.Write(GetBigEnd16(numTables));
                binwrit.Write(GetBigEnd16(searchRange));
                binwrit.Write(GetBigEnd16(entrySelector));
                binwrit.Write(GetBigEnd16(rangeShift));
                Dictionary<string,long> topPos = new Dictionary<string, long>();
                for (int i = 0; i < fontTables.Length; i++)
                {
                    string nam = fontTables[i].tag;
                    FontTables ft = this.fontDict[nam];
                    FontTables ft2 = font2.fontDict[nam];

                    binwrit.Write(Encoding.ASCII.GetBytes(ft.tag));
                    binwrit.Write(GetBigEnd32(ft.checkSum));
                    topPos.Add(nam,binwrit.BaseStream.Position);
                    binwrit.Write(GetBigEnd32(ft.offset));
                    binwrit.Write(GetBigEnd32(ft.length));
                }
                var orderFont = fontTables.OrderBy(p => p.offset).ToArray();
                Dictionary<string, int> addNullTags = new Dictionary<string, int>();
                addNullTags.Add("head", 2);
                addNullTags.Add("maxp", 2);
                addNullTags.Add("CFF ", 1);
                for (int i = 0; i < topPos.Count; i++)
                {
                    //veriyi yaz
                    uint dataStartPos = (uint)binwrit.BaseStream.Position;
                    FontTables ft = orderFont[i];
                    binwrit.Write(ft.data);
                    if (addNullTags.ContainsKey(ft.tag))
                    {
                        for (int k = 0; k < addNullTags[ft.tag]; k++)
                            binwrit.Write((byte)0);
                    }                        

                    //geri git yukarıyı guncelle
                    int hold = (int)binwrit.BaseStream.Position;
                    binwrit.BaseStream.Position = topPos[ft.tag];
                    binwrit.Write(GetBigEnd32(dataStartPos));
                    binwrit.Write(GetBigEnd32(ft.length));
                    binwrit.BaseStream.Position = hold;
                }

            }
            
        }

        private FontTables[] readFontFile(string filePath)
        {
            using( BinaryReaderV binred = new BinaryReaderV(filePath))
            {
                fileLength = (int)binred.BaseStream.Length;
                header = binred.ReadBigEndUInt32();
                if (header != 0x4f54544f && header != 0x00010000) //cff or ttf
                    return null;

                numTables      = binred.ReadBigEndUInt16();
                searchRange    = binred.ReadBigEndUInt16();//?                
                entrySelector  = binred.ReadBigEndUInt16();//?                
                rangeShift     = binred.ReadBigEndUInt16();//?

                FontTables[] fontTables = new FontTables[numTables];                
                for (int i = 0; i < numTables; i++)
                {
                    FontTables ft = new FontTables();
                    string tagName = Encoding.ASCII.GetString(binred.ReadBytes(4));
                    ft.tag      = tagName;
                    ft.checkSum = binred.ReadBigEndUInt32();                                       
                    ft.offset   = binred.ReadBigEndUInt32();                                       
                    ft.length   = binred.ReadBigEndUInt32();                                        

                    long hold = binred.BaseStream.Position;
                    binred.BaseStream.Position = ft.offset;
                    byte[] bb = binred.ReadBytes((int)ft.length);
                    ft.data = bb;
                    binred.BaseStream.Position = hold;                    

                    fontTables[i] = ft;
                    fontDict.Add(ft.tag, ft);
                }

                //readCmap(binred, fontDict["cmap"]);

                return fontTables;//.OrderBy(p => p.offset).ToArray();                               
            }
        }

        private byte[] GetBigEnd32(uint aa)
        {
            byte[] zz = BitConverter.GetBytes(aa);
            Array.Reverse(zz);
            return zz;
        }
        private byte[] GetBigEnd16(UInt16 aa)
        {
            byte[] zz = BitConverter.GetBytes(aa);
            Array.Reverse(zz);
            return zz;
        }

        private void readCmap(BinaryReaderV binred, FontTables cmapTable)
        {
            binred.BaseStream.Position = cmapTable.offset;
            CMap cmap = new CMap();
            cmap.version = binred.ReadBigEndUInt16();
            cmap.numTables = binred.ReadBigEndUInt16();
            cmap.encodingRecs = new EncodingRecord[cmap.numTables];
            for (int i = 0; i < cmap.numTables; i++)
            {
                EncodingRecord er = new EncodingRecord();
                er.platformID = binred.ReadBigEndUInt16();
                er.encodingID = binred.ReadBigEndUInt16();
                er.offset = binred.ReadBigEndUInt32();
            }


        }
    }
}
