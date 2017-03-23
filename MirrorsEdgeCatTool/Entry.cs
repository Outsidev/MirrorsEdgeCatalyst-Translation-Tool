using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    class Entry
    {
        public Dictionary<string, dynamic> subTypes;
        public Entry(BinaryReader binred)
        {
            subTypes = new Dictionary<string, dynamic>();
            byte entryType = binred.ReadByte();
            if (entryType == 0x82)
            {
                int entrySize = Tools.Read128(binred);
                long entryOffset = binred.BaseStream.Position;
                while (binred.BaseStream.Position - entryOffset < entrySize - 1)
                {
                    Tuple<string, dynamic> tup = Tools.GetTypeObj(binred);
                    if (tup != null)
                        subTypes.Add(tup.Item1, tup.Item2);
                }
            }
        }

        public string GetIdAsString()
        {
            return BitConverter.ToString(subTypes["id"]).Replace("-", string.Empty);
        }
    }
}
