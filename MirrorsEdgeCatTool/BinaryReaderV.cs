using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorsEdgeCatTool
{
    class BinaryReaderV : BinaryReader
    {        
        public BinaryReaderV(string filePath) : base(File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
        }

        public uint ReadBigEndUInt32()
        {
            byte[] bb = this.ReadBytes(4);
            uint val = (uint)( bb[0] << 24 | bb[1] << 16 | bb[2] << 8 | bb[3] );
            return val;
        }

        public ushort ReadBigEndUInt16()
        {
            byte[] bb = this.ReadBytes(2);
            ushort val = (ushort)( bb[0] << 8 | bb[1]);
            return val;
        }
    }
}
