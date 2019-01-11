using System;
using System.IO;
using EventStore.Core.Exceptions;

namespace EventStore.Core.Index
{
    public class PTableFooter
    {
        private const int Size = 128;
        public readonly FileType FileType;
        public readonly byte Version;
        public readonly uint NumMidpointsCached;

        public static int GetSize(byte version){
            if(version>=PTableVersions.IndexV4)
                return Size;
            return 0;
        }

        public PTableFooter(byte version, uint numMidpointsCached)
        {
            FileType = FileType.PTableFile;
            Version = version;
            NumMidpointsCached = numMidpointsCached;
        }

        public byte[] AsByteArray()
        {
            var array = new byte[Size];
            array[0] = (byte) FileType.PTableFile;
            array[1] = Version;
            uint numMidpoints = NumMidpointsCached;
            for(int i=0;i<4;i++){
                array[i+2] = (byte) (numMidpoints & 0xFF);
                numMidpoints >>= 8;
            }

            return array;
        }

        public static PTableFooter FromStream(Stream stream)
        {
            var type = stream.ReadByte();
            if (type != (int)FileType.PTableFile)
                ThrowHelper.ThrowCorruptIndexException_CorruptedPTable();
            var version = stream.ReadByte();
            if (version == -1)
                ThrowHelper.ThrowCorruptIndexException_CouldntReadVersionOfPTableFromFooter();
            if(!(version >= PTableVersions.IndexV4))
                ThrowHelper.ThrowCorruptIndexException_PTableFooterWithVersionLess4Found();

            byte[] buffer = new byte[4];
            stream.Read(buffer,0,4);
            uint numMidpointsCached = BitConverter.ToUInt32(buffer,0);

            return new PTableFooter((byte)version, numMidpointsCached);
        }
    }
}