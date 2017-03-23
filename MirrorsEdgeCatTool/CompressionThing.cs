using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    public struct CompStr
    {
        public int processLen;
        public int countLen;
        public int countOffset;
        public byte[] plainBytes;
    }

    class CompressionThing
    {
        public byte[] compressedData;
        public byte[] decompressedData;
        public int DecompressedDataLength;

        public CompressionThing()
        {}

        public Liner[] ImportFromExcelFile(string where)
        {
            ExcelPackage pck = new ExcelPackage(File.Open(where, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            ExcelWorksheet ws = pck.Workbook.Worksheets["Satirlar"];

            List<Liner> importedLiners = new List<Liner>();

            uint lastLength = 0;
            for (int i = 2; i <= ws.Dimension.Rows; i++)
            {
                Liner lin = new Liner();
                string str = "";

                if (ws.Cells["B" + (i)].Value != null && ws.Cells["B" + (i)].Value.ToString() != "")
                {
                    str = ws.Cells["B" + (i)].Value.ToString();//take from translated first,                                         
                }
                else if (ws.Cells["A" + (i)].Value != null)
                {
                    str = ws.Cells["A" + (i)].Value.ToString();
                }
                lin.line = str;
                str = ConvertBackUnicodes(str);
                lin.lineBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(str + '\x00');

                lin.grup = uint.Parse(ws.Cells["C" + (i)].Value.ToString());
                lin.id = uint.Parse(ws.Cells["D" + (i)].Value.ToString());
                lin.subPos = lastLength;
                lastLength = (lastLength + (uint)lin.lineBytes.Length) % 65536;
                importedLiners.Add(lin);

            }

            pck.Dispose();

            return importedLiners.ToArray();
        }

        public byte[] compressGermanLZ4(string excelPath)
        {

            Liner[] liners = ImportFromExcelFile(excelPath);
            byte[] allLineBytes = liners.SelectMany(p => p.lineBytes).ToArray();

            MemoryStream compressedMem = new MemoryStream();
            using (BinaryWriter binwr = new BinaryWriter(compressedMem))
            {
                byte[] header = new byte[] { 0x00, 0x90, 0x03, 0x00 };
                int lineCount = liners.Length;
                int idStartPos = 140 + 8;
                int textStartPos = 63964 + 8;
                int unpackedSize = allLineBytes.Length + textStartPos - 8;
                byte[] global = new byte[] { 0x47, 0x6C, 0x6F, 0x62, 0x61, 0x6C };

                binwr.Write(header);
                binwr.Write(unpackedSize);
                binwr.Write(lineCount);
                binwr.Write(idStartPos - 8);
                binwr.Write(textStartPos - 8);
                binwr.Write(global);

                DecompressedDataLength = unpackedSize;

                while (binwr.BaseStream.Position < (idStartPos))
                    binwr.Write((byte)0x0);

                Liner[] linersOrderedByIds = liners.OrderBy(p => p.id).ToArray();
                for (int i = 0; i < linersOrderedByIds.Length; i++)
                {
                    Liner lin = linersOrderedByIds[i];
                    binwr.Write(lin.id);
                    binwr.Write((Int16)lin.subPos);
                    binwr.Write((Int16)lin.grup);
                }

                binwr.Write(allLineBytes, 0, allLineBytes.Length);

            }

            return CompressData(compressedMem.ToArray());

        }

        public byte[] compressLZ4(string excelPath)
        {

            Liner[] liners = ImportFromExcelFile(excelPath);
            byte[] allLineBytes = liners.SelectMany(p => p.lineBytes).ToArray();

            MemoryStream compressedMem = new MemoryStream();
            using (BinaryWriter binwr = new BinaryWriter(compressedMem))
            {
                byte[] header = new byte[] { 0x00,0x01,0x00,0x00, 0x00,0x71, 0x00,0x00, 0x00,0x90,0x03,0x00 };                                                
                int lineCount = liners.Length;                
                int idStartPos = 140 + 16;
                int textStartPos = 63964 + 16;
                int unpackedSize = allLineBytes.Length + textStartPos - 8;
                byte[] global = new byte[] { 0x47, 0x6C, 0x6F, 0x62, 0x61, 0x6C };

                binwr.Write(header);
                binwr.Write(unpackedSize);
                binwr.Write(lineCount);
                binwr.Write(idStartPos - 16);
                binwr.Write(textStartPos - 16);
                binwr.Write(global);

                DecompressedDataLength = unpackedSize;

                while (binwr.BaseStream.Position < (idStartPos))
                    binwr.Write((byte)0);

                Liner[] linersOrderedByIds = liners.OrderBy(p => p.id).ToArray();
                for (int i = 0; i < linersOrderedByIds.Length; i++)
                {
                    Liner lin = linersOrderedByIds[i];
                    binwr.Write(lin.id);
                    binwr.Write((Int16)lin.subPos);
                    binwr.Write((Int16)lin.grup);
                }

                int uncompressedRemaining = 65536 + 8 - (textStartPos);
                for (int i = 0; i < uncompressedRemaining; i++)
                    binwr.Write(allLineBytes[i]);

                int hold = uncompressedRemaining;
                while (hold < allLineBytes.Length)
                {

                    if (hold + 65536 < allLineBytes.Length)
                    {
                        byte[] temp = new byte[65536];
                        Array.Copy(allLineBytes, hold, temp, 0, 65536);
                        byte[] aa = compressBlock(temp, hold);
                        binwr.Write(aa);
                        hold += 65536;
                    }
                    else
                    {
                        int remaining = allLineBytes.Length - hold;
                        byte[] temp = new byte[remaining];
                        Array.Copy(allLineBytes, hold, temp, 0, remaining);
                        binwr.Write(compressBlock(temp, hold));
                        hold += remaining;
                    }

                    Console.WriteLine(hold + " OK...");
                }

            }


            compressedData = compressedMem.ToArray();
            return compressedData;

        }

        public byte[] CompressData(byte[] allBytes)
        {
            MemoryStream compressedMem = new MemoryStream();
            using (BinaryWriter binwr = new BinaryWriter(compressedMem))
            {
                int hold = (int)binwr.BaseStream.Position;
                while (hold < allBytes.Length)
                {                    
                    if (hold + 65536 < allBytes.Length)
                    {
                        byte[] temp = new byte[65536];
                        Array.Copy(allBytes, hold, temp, 0, 65536);
                        byte[] aa = compressBlock(temp, 0);
                        binwr.Write(aa);
                        hold += 65536;
                    }
                    else
                    {
                        int remaining = allBytes.Length - hold;
                        byte[] temp = new byte[remaining];
                        Array.Copy(allBytes, hold, temp, 0, remaining);
                        binwr.Write(compressBlock(temp, 0));
                        hold += remaining;
                    }

                    Console.WriteLine(hold + " CData OK...");
                }

            }
            compressedData = compressedMem.ToArray();
            return compressedData;
        }

        public byte[] compressBlock(byte[] strBytes, int startIndex)
        {
            List<CompStr> compressedStrs = new List<CompStr>();
            int fwdIndex = 0, lastIndex = 0;
            int currentGroup = 0;
            while (fwdIndex < strBytes.Length)
            {
                currentGroup = getGroupNo(fwdIndex + startIndex);
                                
                byte mainCh = strBytes[fwdIndex];
                int enBenzerCount = 0;
                int processLen = 0, countLen = 0, countOffset = 0;

                for (int bckIndex = fwdIndex-1; bckIndex >= 0; bckIndex--)
                {
                    byte backCh = strBytes[bckIndex];
                    int backGroupNo = getGroupNo(bckIndex + startIndex);

                    if (currentGroup != backGroupNo)
                        break;

                    if (mainCh == backCh)
                    {
                        int benzerCount = 0;
                        List<byte> lets = new List<byte>();
                        for (int z = 0; fwdIndex + z < strBytes.Length && strBytes[fwdIndex + z] == strBytes[bckIndex + z]; z++)
                        {
                            benzerCount++;
                        }
                        if (benzerCount > 3 && benzerCount > enBenzerCount)
                        {
                            enBenzerCount = benzerCount;
                            countOffset = fwdIndex - bckIndex;
                            countLen = benzerCount;
                        }
                    }
                }

                if (enBenzerCount != 0)
                {
                    byte[] plainBytes = new byte[fwdIndex - lastIndex];
                    Array.Copy(strBytes, lastIndex, plainBytes, 0, plainBytes.Length);
                    fwdIndex += enBenzerCount;
                    lastIndex = fwdIndex;

                    CompStr cc = new CompStr();
                    cc.processLen = plainBytes.Length;
                    cc.countLen = countLen;
                    cc.countOffset = countOffset;
                    cc.plainBytes = plainBytes;
                    compressedStrs.Add(cc);
                }
                else
                    fwdIndex++;

            }

            CompStr lastcc = new CompStr();
            byte[] lastBytes = new byte[strBytes.Length - lastIndex];
            Array.Copy(strBytes, lastIndex, lastBytes, 0, lastBytes.Length);
            lastcc.processLen = lastBytes.Length;
            lastcc.countLen = 4;
            lastcc.countOffset = 0;
            lastcc.plainBytes = lastBytes;
            compressedStrs.Add(lastcc);

            List<byte> exportBytes = new List<byte>();
            for (var i = 0; i < compressedStrs.Count; i++)
            {
                CompStr cstr = compressedStrs[i];
                byte[] processLen = splitToFFs(cstr.processLen);
                if(cstr.countLen-4 < 0)
                    cstr.countLen = 0;

                byte[] countLen = splitToFFs(cstr.countLen - 4);
                byte[] countOffset = BitConverter.GetBytes((Int16)cstr.countOffset);

                byte pSide = (byte)((processLen[0] << 4) & 0xF0);
                byte cSide = (byte)(countLen[0] & 0x0F);
                byte pcBayt = (byte)(pSide | cSide);
                exportBytes.Add(pcBayt);
                for (var z = 1; z < processLen.Length; z++)
                    exportBytes.Add(processLen[z]);

                byte[] stringBytes = cstr.plainBytes;
                exportBytes.AddRange(stringBytes);

                if (i == compressedStrs.Count - 1)
                    break; //do not write last 0 countOffset

                exportBytes.AddRange(countOffset);
                for (var z = 1; z < countLen.Length; z++)
                    exportBytes.Add(countLen[z]);

            }            

            byte[] compressedSize = BitConverter.GetBytes((Int16)exportBytes.Count);
            Array.Reverse(compressedSize);
            byte[] headerBytes = { 0x09, 0x70 };
            byte[] decompresedSize = BitConverter.GetBytes(strBytes.Length);
            Array.Reverse(decompresedSize);
            exportBytes.InsertRange(0, compressedSize);
            exportBytes.InsertRange(0, headerBytes);
            exportBytes.InsertRange(0, decompresedSize);
            
            return exportBytes.ToArray();
        }

        private int getGroupNo(int index)
        {
            return index / 65536;
        }

        private byte[] splitToFFs(int proc)
        {
            List<byte> ret = new List<byte>();
            if (proc >= 15)
            {
                ret.Add(0xF);
                int sub = proc - 0xF;
                while (sub > 0xFF)
                {
                    ret.Add(0xFF);
                    sub -= 0xFF;
                }
                ret.Add((byte)(sub));
            }
            else
                ret.Add(BitConverter.GetBytes(proc)[0]);

            return ret.ToArray();
        }

        public byte[] decompressLZ4(byte[] data)
        {
            List<byte[]> decodedBytes = new List<byte[]>();

            using (BinaryReader binred = new BinaryReader(new MemoryStream(data)))
            {                
                                                                
                while (binred.BaseStream.Position < binred.BaseStream.Length)
                {
                    uint decompressSize = ReadBigEndianUInt32(binred);
                    int header = binred.ReadInt16();
                    if (header == 0x7100)//uncompressed
                    {
                        binred.ReadInt16();//0x0000
                        decodedBytes.Add(binred.ReadBytes((int)decompressSize));
                        continue;
                    }
                    else if (header != 0x7009)//must be compressed then, if not unknown header
                    {
                        Console.WriteLine("Unknown Header, At:" + binred.BaseStream.Position);
                        return null;
                    }
                    uint compressedSize = ReadBigEndianUInt16(binred);

                    List<byte> decoded = new List<byte>();
                    long startPos = binred.BaseStream.Position;                    
                    while (binred.BaseStream.Position - startPos < compressedSize)
                    {

                        byte proCouLen = binred.ReadByte();
                        int processLen = ReadProcessLength(binred, proCouLen);
                        int countLen = getLast4Bit(proCouLen);

                        byte[] aa = binred.ReadBytes(processLen);
                        decoded.AddRange(aa);
                        
                        if (binred.BaseStream.Position - startPos >= compressedSize)
                            break; //end of block

                        int countOffset = binred.ReadUInt16();

                        if (countLen == 0xF)
                        {
                            countLen += ReadCountLength(binred);
                        }
                        countLen += 4;
                        
                        for (int i = 0; i < countLen; i++)
                        {
                            int index = ( (decoded.Count-1) - countOffset + 1) % decoded.Count;
                            byte b = decoded[ index ];
                            decoded.Add(b);                            
                        }                        
                    }
                    decodedBytes.Add(decoded.ToArray());
                    
                }                         
            }

            decompressedData = decodedBytes.SelectMany(x => x).ToArray();
            return decompressedData;
        }

        public void ExportTextsToExcelFile(string where, byte[] uncompressedData)
        {
            StringChunk strChunk = new StringChunk(uncompressedData);
            Liner[] allLiners = strChunk.allLiners;

            ExcelPackage pck = new ExcelPackage(File.Open(where, FileMode.Create));
            ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Satirlar");
            ws.Cells["A1"].Value = "Orjinal Satır";
            ws.Cells["B1"].Value = "Türkçe Satır";
            ws.Cells["C1"].Value = "Grup";
            ws.Cells["D1"].Value = "Id";
            ws.Cells["E1"].Value = "Pos";
            ws.Cells["A1:E1"].Style.Font.Bold = true;

            for (int i = 0; i < allLiners.Length; i++)
            {
                Liner lin = allLiners[i];
                ws.Cells["A" + (i + 2)].Value = allLiners[i].line;
                ws.Cells["C" + (i + 2)].Value = allLiners[i].grup;
                ws.Cells["D" + (i + 2)].Value = allLiners[i].id;
                ws.Cells["E" + (i + 2)].Value = allLiners[i].subPos;
            }

            pck.Save();
            pck.Dispose();
        }
        
        private string ConvertBackUnicodes(string strLine)
        {
            string str = strLine;
            //if contains unicode escape character, convert back to unicode
            for (var k = 0; k < str.Length - 1; k++)
            {
                if (str[k] == '<' && str[++k] == '*')
                {
                    string code = str.Substring(k + 1, str.IndexOf('>', k) - k - 1);
                    int iCode = int.Parse(code);
                    byte bCode = (byte)iCode;
                    str = str.Replace("<*" + code + ">", "" + (char)bCode);
                }
            }

            return str;
        }

        private int ReadProcessLength(BinaryReader binred, byte procolen)
        {
            int first4 = getFirst4Bit(procolen);
            if (first4 != 0xF)
                return first4;

            return ReadFullLength(binred, first4);
        }

        private int ReadCountLength(BinaryReader binred)
        {
            return ReadFullLength(binred, 0);
        }

        private int ReadFullLength(BinaryReader binred, int firsty)
        {
            int top = 0;
            int aa = top = firsty;
            do
            {
                aa = binred.ReadByte();
                top += aa;
            } while (aa == 0xFF);

            return top;
        }

        private int getFirst4Bit(byte b)
        {
            return b >> 4;
        }

        private int getLast4Bit(byte b)
        {
            return b & 0x0F;
        }

        private UInt16 ReadBigEndianUInt16(BinaryReader binred)
        {
            byte[] bb = binred.ReadBytes(2);
            Array.Reverse(bb);
            return BitConverter.ToUInt16(bb, 0);
        }
        private uint ReadBigEndianUInt32(BinaryReader binred)
        {
            byte[] bb = binred.ReadBytes(4);
            Array.Reverse(bb);
            return BitConverter.ToUInt32(bb, 0);
        }

/*if (str.Contains("dictionary"))
{
    int bigb = 255;
    List<byte> dd = new List<byte>();
    for (int be = 30; be <= bigb; be++)
    {
        List<byte> aa = Encoding.Default.GetBytes(" "+be.ToString()+"-").ToList();
        aa.Add((byte)be);
        if (be!=123)
            dd.AddRange(aa);
    }
    dd.Add(0);
    lin.lineBytes = dd.ToArray();
}*/
    }
}
