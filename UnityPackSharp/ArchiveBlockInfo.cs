using System.IO;

namespace UnityPackSharp
{
    class ArchiveBlockInfo
    {
        public int UncompressedSize { get; private set; }
        public int CompressedSize { get; private set; }
        public CompressionType CompressionType { get; private set; }

        public ArchiveBlockInfo(BinaryReader reader)
        {
            this.UncompressedSize = reader.ReadInt32BE();
            this.CompressedSize = reader.ReadInt32BE();
            var flags = reader.ReadUInt16BE();
            this.CompressionType = (CompressionType)(flags & 0x3f);
        }
    }
}
