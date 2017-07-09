using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    class Program
    {

        public static Toc[] tocFiles;
        public static Sb[]  sbFiles;
        public static Cat[] catFiles;        

        public static string CurrentPath;
        public static string GamePath;
        public static string MainFolder = "Patch\\";
        static void Main(string[] args)
        {
            CurrentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //args = new string[] { "import", @"C:\Games\Mirror's Edge - Catalyst", "en.kindaorg.xlsx", "en" };
            if (args[0] == "export")
            {
                //gameLoc, lang
                GamePath = args[1];
                string lang = args[2];

                Console.WriteLine(":Export:");
                Console.WriteLine("GamePath: " + Path.Combine(GamePath, MainFolder));
                Console.WriteLine("Lang: " + lang);

                ReadTocSbCatFiles();
                ExportLanguageSubtitle(lang);
            }
            else if (args[0] == "import")
            {
                //gameLoc, lang, excelFileLoc,
                GamePath = args[1];
                string lang = args[2];
                string excelLoc = args[3];

                Console.WriteLine(":Import:");
                Console.WriteLine("GamePath: " + Path.Combine(GamePath, MainFolder));
                Console.WriteLine("Excel Location: " + excelLoc);
                Console.WriteLine("Lang: " + lang);

                ReadTocSbCatFiles();
                ImportLanguage(lang, excelLoc);
            }
            else if (args[0] == "about")
            {
                Console.WriteLine("Mirrors Edge Catalyst Translation Tool");
                Console.WriteLine("Burak Sey - 2017.03");
            }

            Console.WriteLine("DONE!");
        }

        static void ReadTocSbCatFiles()
        {
            string mainPath = Path.Combine(GamePath, MainFolder, "Win32\\");
            string[] allTocPaths = Directory.GetFiles(mainPath, "*.toc", SearchOption.AllDirectories);
            string[] allSbPaths = Directory.GetFiles(mainPath, "*.sb", SearchOption.AllDirectories);
            string[] allCatPaths = Directory.GetFiles(mainPath, "*.cat", SearchOption.AllDirectories);

            tocFiles = new Toc[allTocPaths.Length];
            sbFiles = new Sb[allSbPaths.Length];
            catFiles = new Cat[allCatPaths.Length];


            for (int i = 0; i < allTocPaths.Length; i++)
            {
                string path = allTocPaths[i];
                tocFiles[i] = new Toc(path);
            }
            //Console.WriteLine("Tocs Done.");

            /*for (int i = 0; i < allSbPaths.Length; i++)
            {
                string path = allSbPaths[i];
                sbFiles[i] = new Sb(path);
            }
            Console.WriteLine("Sb Done.");*/

            for (int i = 0; i < allCatPaths.Length; i++)
            {
                string path = allCatPaths[i];
                catFiles[i] = new Cat(path);
            }
            //Console.WriteLine("Cats Done.");
        }

        static void ImportLanguage(string lang, string excelPath)
        {
            CompressionThing compressor = new CompressionThing();

            Toc langToc = tocFiles.Where(p => Path.GetFileNameWithoutExtension(p.filePath) == lang).First();
            Entry ent1 = langToc.mainEntry.subTypes["chunks"][0];
            Entry ent2 = langToc.mainEntry.subTypes["chunks"][1];

            Kat kat1 = Tools.GetCatSettings(catFiles, ent1.subTypes["sha1"]);
            Kat kat2 = Tools.GetCatSettings(catFiles, ent2.subTypes["sha1"]);
            Entry bigEnt = kat1.size > kat2.size ? ent1 : ent2;
            //bigger one is subtitle one
            byte[] sha1     = bigEnt.subTypes["sha1"];
            byte[] id       = bigEnt.subTypes["id"];
            string idStr    = BitConverter.ToString(id).Replace("-", "");
            Kat deKat       = Tools.GetCatSettings(catFiles, sha1);

            Console.WriteLine("Compressing..");
            byte[] compressedNewBytes;
            if (lang == "de" || lang == "zh" || lang == "ja" )
                compressedNewBytes = compressor.compressGermanLZ4(excelPath);
            else
                compressedNewBytes = compressor.compressLZ4(excelPath);

            Console.WriteLine("OK.");


            string cas1path = Path.Combine(GamePath, MainFolder, @"Win32\gameconfigurations\initialinstallpackage\cas_01.cas");
            byte[] cas1 = File.ReadAllBytes(cas1path);
            int z = 0;
            if(compressedNewBytes.Length > (uint)deKat.size)
            {
                Console.WriteLine("Yeni dosyanın boyutu ("+ compressedNewBytes.Length +") Orjinal boyuttan ("+ deKat.size +") daha büyük!\nİptal.. ");
                return;
            }

            for (uint i = deKat.offset; i < deKat.offset + deKat.size; i++)
            {
                if (z < compressedNewBytes.Length)
                {
                    cas1[i] = compressedNewBytes[z];
                    z++;
                }
                else
                {
                    cas1[i] = 0;
                }
            }
            Console.WriteLine("Writing to Cas1 File..");
            BackupAndWriteBytes(cas1path, cas1);
            Console.WriteLine("OK.");

            //for new decompressedSize, if not edited game refuses to open -except polish

            int dataSizeOffset = -1;//454368;//420703;//420813;
            string cas2path = Path.Combine(GamePath, MainFolder, @"Win32\gameconfigurations\initialinstallpackage\cas_02.cas");
            byte[] cas2 = File.ReadAllBytes(cas2path);
            dataSizeOffset = Tools.SearchIdInCasFile(cas2, id);           

            if (dataSizeOffset == -1)
            {
                Console.WriteLine("Skip Cas2..");
                return;
            }                

            dataSizeOffset += 16;//size offset comes after id start offset 
            byte[] dataLen = BitConverter.GetBytes(compressor.DecompressedDataLength);
            for (int i = 0; i < 4; i++)
            {
                cas2[i + dataSizeOffset] = dataLen[i];
            }

            Console.WriteLine("Writing to Cas2 File..");
            BackupAndWriteBytes(cas2path, cas2);
            Console.WriteLine("OK.");

        }

        static void BackupAndWriteBytes(string path, byte[] data)
        {
            if (!File.Exists(path + ".backupOrginal"))
                File.Copy(path, path + ".backupOrginal");
            File.WriteAllBytes(path, data);
        }


        static void ExportLanguageSubtitle(string lang, string exportpath="")
        {
            byte[] data = ExportChunk(lang);
            CompressionThing deCompressor = new CompressionThing();
            data = deCompressor.decompressLZ4(data);

            //File.WriteAllBytes(Path.Combine(CurrentPath,"en.bin.GOODY"), data);
            if(exportpath == "")
                exportpath = Tools.EnumarateFileName(CurrentPath, lang);

            deCompressor.ExportTextsToExcelFile(exportpath, data);

        }

        static byte[] ExportChunk(string lang)
        {
            Toc langToc = tocFiles.Where(p => Path.GetFileNameWithoutExtension(p.filePath) == lang).First();

            Entry ent1 = langToc.mainEntry.subTypes["chunks"][0];
            Entry ent2 = langToc.mainEntry.subTypes["chunks"][1];

            Kat kat1 = Tools.GetCatSettings(catFiles, ent1.subTypes["sha1"]);
            Kat kat2 = Tools.GetCatSettings(catFiles, ent2.subTypes["sha1"]);
            Kat bigKat = kat1.size > kat2.size ? kat1 : kat2;

            byte[] datMan = Tools.GetDataFromKat(bigKat);//bigger chunk is subtitle file
            return datMan;
        }

        static Kat GetKatFromSb(string aPath, string bPath, string subType)
        {
            string tocPath = "";
            Entry tocEntry = null;
            Toc mainToc = null;
            foreach (Toc ent in tocFiles)
            {
                foreach (Entry en in ent.mainEntry.subTypes["bundles"])
                {
                    if (en.subTypes["id"] == aPath)
                    {
                        mainToc = ent;
                        tocPath = ent.filePath;
                        tocPath = tocPath.Substring(0, tocPath.LastIndexOf('.'));
                        tocEntry = en;
                        break;
                    }
                }
            }
            
            string sbPath = Path.Combine(GamePath+MainFolder, tocPath + ".sb");
            Sb mainSbFile = new Sb(sbPath);
            Sb sbFile = new Sb(sbPath, (long)tocEntry.subTypes["offset"]);
            Entry sbEntry = null;
            foreach (Entry eb in sbFile.mainEntry.subTypes[subType])
            {
                if (eb.subTypes["name"] == bPath)
                {
                    sbEntry = eb;
                    break;
                }
            }

            return Tools.GetCatSettings(catFiles, sbEntry.subTypes["sha1"]);

        }
        
        static byte[] ExportThing(string aPath, string bPath)
        {
            string tocPath = "";
            Entry tocEntry = null;
            Toc mainToc = null;
            foreach (Toc ent in tocFiles)
            {
                foreach(Entry en in ent.mainEntry.subTypes["bundles"])
                {
                    if(en.subTypes["id"] == aPath)
                    {
                        mainToc = ent;
                        tocPath = ent.filePath;
                        tocPath = tocPath.Substring(0,tocPath.LastIndexOf('.'));
                        tocEntry = en;
                        break;
                    }
                }
            }

            string sbPath = Path.Combine(GamePath, MainFolder, tocPath + ".sb");
            Sb sbFile = new Sb(sbPath, (long)tocEntry.subTypes["offset"]);
            Entry sbEntry = null;
            foreach (Entry eb in sbFile.mainEntry.subTypes["res"])
            {
                if (eb.subTypes["name"] == bPath)
                {
                    sbEntry = eb;
                    break;

                }
            }
            return  Tools.GetDataFromSha1(catFiles, sbEntry.subTypes["sha1"]);                        
        }

        static void ImportThing(string aPath, string bPath, byte[] data)
        {
            string tocPath = "";
            Entry tocEntry = null;
            Toc mainToc = null;
            foreach (Toc ent in tocFiles)
            {
                foreach (Entry en in ent.mainEntry.subTypes["bundles"])
                {
                    if (en.subTypes["id"] == aPath)
                    {
                        mainToc = ent;
                        tocPath = ent.filePath;
                        tocPath = tocPath.Substring(0, tocPath.LastIndexOf('.'));
                        tocEntry = en;
                        break;
                    }
                }
            }

            string sbPath = Path.Combine(GamePath, MainFolder, tocPath + ".sb");
            Sb sbFile = new Sb(sbPath, (long)tocEntry.subTypes["offset"]);
            Entry sbEntry = null;
            foreach (Entry eb in sbFile.mainEntry.subTypes["res"])
            {
                if (eb.subTypes["name"] == bPath)
                {
                    sbEntry = eb;
                    break;
                }
            }

            Kat katSettings = Tools.GetCatSettings(catFiles, sbEntry.subTypes["sha1"]);
            string cas1path = Path.Combine(GamePath, MainFolder, Path.GetDirectoryName(katSettings.motherCat.filePath)+"\\cas_0"+katSettings.casNo+".cas");
            byte[] cas1 = File.ReadAllBytes(cas1path);
            int z = 0;
            /*if (data.Length > (uint)katSettings.size)
            {
                Console.WriteLine("Yeni dosyanın boyutu (" + data.Length + ") Orjinal boyuttan (" + katSettings.size + ") daha büyük!\nİptal.. ");
                return;
            }*/

            for (uint i = katSettings.offset; i < katSettings.offset + katSettings.size; i++)
            {
                if (z < data.Length)
                {
                    cas1[i] = data[z];
                    z++;
                }
                else
                {
                    cas1[i] = 0;
                }
            }
            Console.WriteLine("Writing to Cas1 File..");
            BackupAndWriteBytes(cas1path, cas1);
            Console.WriteLine("OK.");
        }
        
        static void ExportPaths(string where)
        {
            List<string> pathNames = new List<string>();
            foreach (Sb sb in sbFiles)
            {
                foreach (Entry bundle in sb.mainEntry.subTypes["bundles"])
                {
                    string path = bundle.subTypes["path"];
                    foreach (Entry ebxEntry in bundle.subTypes["ebx"])
                    {
                        string name = ebxEntry.subTypes["name"];
                        pathNames.Add(path + ">>" + name);
                    }
                } 
            }

            File.WriteAllLines(where, pathNames.ToArray());
        }
        
               
    }
}
