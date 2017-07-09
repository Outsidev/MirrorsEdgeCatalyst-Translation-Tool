using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    class Tools
    {        
        public static int Read128(BinaryReader binred)
        {
            int result=0, i = 0;
            while (true)
            {
                byte bayt = binred.ReadByte();
                result |= (bayt & 127) << i;
                if (bayt >> 7 == 0)
                    return result;
                i += 7; 
            }
        }

        public static byte[] ReadTillNull(BinaryReader binred)
        {
            List<byte> bb8 = new List<byte>();
            byte bb;
            while(binred.BaseStream.Position < binred.BaseStream.Length && (bb = binred.ReadByte()) != 0x0)
            {
                bb8.Add(bb);
            }

            return bb8.ToArray();
        }

        public static string EnumarateFileName(string currentPath, string lang)
        {
            string exportPath = Path.Combine(currentPath, lang + ".xlsx");
            //if file exist, enumarete it
            int existingFileCount = Directory.GetFiles(currentPath, lang + "*.xlsx", SearchOption.TopDirectoryOnly).Length;
            if (existingFileCount > 0)
                exportPath = Path.Combine(currentPath, lang + "_" + existingFileCount + ".xlsx");

            return exportPath;
        }

        public static BinaryReader BinaryReadShare(string path)
        {
            return new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public static int SearchIdInCasFile(byte[] cas2, byte[] id)
        {
            int idStartOffset = -1;
            for (int i = 0; i < cas2.Length; i++)
            {
                bool find = false;
                if (id[0] == cas2[i])
                {
                    find = true;
                    for (int j = 0; j < 16; j++)
                    {
                        if (id[j] != cas2[i + j])
                        {
                            find = false;
                            break;
                        }
                    }
                }
                if (find)
                {
                    idStartOffset = i;
                    break;
                }
            }

            return idStartOffset;
        }

        public static byte[] GetDataFromSha1(Cat[] catFiles, byte[] mySha1)
        {
            Kat katFile = new Kat();
            string katPath = "";
            foreach (Cat kat in catFiles)
            {
                foreach (Kat ka in kat.katList)
                {
                    if (CompareByteArrays(ka.sha1, mySha1))
                    {
                        katPath = kat.filePath;
                        katFile = ka;
                        break;
                    }
                }
            }

            return GetDataFromKat(katFile);
        }

        public static byte[] GetDataFromKat(Kat katFile)
        {
            byte[] data = new byte[katFile.size];
            string casPath = Path.Combine(Path.GetDirectoryName(katFile.motherCat.filePath), "cas_0" + katFile.casNo + ".cas");
            using (BinaryReader binred = new BinaryReader(File.Open(casPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                binred.BaseStream.Position = katFile.offset;
                binred.Read(data, 0, data.Length);
            }
            return data;
        }

        public static Tuple<string, dynamic> GetTypeObj(BinaryReader binred)
        {
            char[] trimchars = { (char)0x1, (char)0x0 };
            string name = "";
            dynamic value = null;
            byte tip = binred.ReadByte();
            if (tip == 0x0)
                return null;

            byte g;             
            while ( (g = binred.ReadByte()) != 0x0) name +=(char)g;            
            if(tip == 0x1)
            {//list*bundles-chunks
                List<Entry> entries = new List<Entry>();
                int entrySize = Read128(binred);
                long entryOffset = binred.BaseStream.Position;
                while (binred.BaseStream.Position - entryOffset < entrySize - 1)
                {
                    Entry subEnt = new Entry(binred);
                    entries.Add(subEnt);
                    binred.ReadByte();//0x0 pass
                }
                value = entries;

            }
            else if(tip == 0x2)
            {//*meta
                value = Read128(binred);
            }
            else if (tip == 0x6)
            {//cas no?
                value = binred.ReadByte();
            }
            else if(tip == 0x7)
            {//string*id-name-path
                int pathSize = binred.ReadByte();
                char[] kars = binred.ReadChars(pathSize);
                value = new string(kars,0, pathSize);                
                value = (value as string).Trim(trimchars);
            }
            else if(tip == 0x9)
            {//offset
                value = binred.ReadUInt64();
            }
            else if(tip == 0x8)
            {//size-magicsalt
                value = binred.ReadUInt32();                  
            }else if(tip == 0x10)
            {//sha1
                value = binred.ReadBytes(20);
            }
            else if(tip == 0xf)
            {//id?
                value = binred.ReadBytes(16);
            }
            else if(tip == 0x13)
            {//resmeta
                int siz = Read128(binred);
                value = binred.ReadBytes(siz);
            }

            return Tuple.Create<string, dynamic>(name, value);            

        }

        public static List<string> GetDirectories(MainEntry[] entryFiles)
        {
            List<string> everyDirectory = new List<string>();
            foreach (MainEntry ent in entryFiles)
            {
                everyDirectory.AddRange(ent.GetFileDirectories());
            }

            return everyDirectory;
        }

        public static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        public static Kat GetCatSettings(Cat[] catFiles, byte[] sha1)
        {
            Kat katFile = new Kat();
            foreach (Cat kat in catFiles)
            {
                foreach (Kat ka in kat.katList)
                {
                    if (CompareByteArrays(ka.sha1, sha1))
                    {
                        katFile = ka;
                        break;
                    }
                }
            }

            return katFile;
        }

        public static string GetLanguageShortCode(string lang)
        {
            switch (lang.ToLower())
            {
                case "english":
                    return "en";
                case "polish":
                    return "pl";
                case "italian":
                    return "it";
                case "spanish":
                    return "es";
                case "french":
                    return "fr";
                case "japanese":
                    return "jp";
                case "brazilianportuguese":
                    return "br";
                case "german":
                    return "de";
                case "russian":
                    return "ru";
                case "traditionalchinese":
                    return "tc";
                default:
                    return "NOPE";
            }
        }
    }
}
