using System;
using System.Collections.Generic;
using System.IO;

namespace UnityPackSharp
{
    public class AssetBundle
    {
        public static readonly string UnityRawSignature = "UnityRaw";
        public static readonly string UnityWebSignature = "UnityWeb";
        public static readonly string UnityFSSignature = "UnityFS";

        public UnityEnvironment Environment { get; private set; }
        public string Signature { get; private set; }
        public int FormatVersion { get; private set; }
        public string UnityVersion { get; private set; }
        public string GeneratorVersion { get; private set; }

        public byte[] Guid { get; private set; }

        public string Name { get; private set; }
        public IReadOnlyList<Asset> Assets { get; private set; }

        private Stream stream;

        internal AssetBundle(UnityEnvironment environment)
        {
            this.Environment = environment;

        }

        internal void Load(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                Load(fs);
            }
        }

        internal void Load(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                Load(new MemoryStream(bytes));
            }
        }

        private void Load(Stream stream)
        {
            this.stream = stream;
            var reader = new BinaryReader(this.stream);
            this.Signature = reader.ReadCString();
            this.FormatVersion = reader.ReadInt32BE();
            this.UnityVersion = reader.ReadCString();
            this.GeneratorVersion = reader.ReadCString();

            if (this.Signature == UnityFSSignature || this.Signature == UnityWebSignature)
            {
                LoadFS(reader);
            }
            else
            {
                throw new NotSupportedException("Not supported signature : " + this.Signature);
            }
        }

        private void LoadFS(BinaryReader reader)
        {
            var fileSize = reader.ReadInt64BE();
            var ciBlockSize = reader.ReadInt32BE();
            var uiBlockSize = reader.ReadInt32BE();
            var flags = reader.ReadUInt32BE();
            var compressionType = (CompressionType)(flags & 0x3f);
            var currentPos = reader.BaseStream.Position;
            var eofMetadata = (flags & 0x80) > 0;
            if (eofMetadata)
            {
                reader.BaseStream.Seek(-ciBlockSize, SeekOrigin.End);
            }

            var metadata = reader.ReadBytes(ciBlockSize);
            switch (compressionType)
            {
                case CompressionType.None:
                    break;
                case CompressionType.LZ4:
                case CompressionType.LZ4HC:
                    metadata = LZ4.LZ4Codec.Decode(metadata, 0, ciBlockSize, uiBlockSize);
                    break;
                default:
                    throw new NotSupportedException("Not supported compression type : " + compressionType);
            }

            if (eofMetadata)
            {
                reader.BaseStream.Seek(currentPos, SeekOrigin.Begin);
            }

            using (var ms = new MemoryStream(metadata))
            {
                LoadFSCore(reader, new BinaryReader(ms));
            }
        }

        private void LoadFSCore(BinaryReader mainReader, BinaryReader metadataReader)
        {
            this.Guid = metadataReader.ReadBytes(16);
            var blocksNum = metadataReader.ReadInt32BE();
            var blocks = new List<ArchiveBlockInfo>(blocksNum);
            for (var i = 0; i < blocksNum; i++)
            {
                blocks.Add(new ArchiveBlockInfo(metadataReader));
            }

            var nodesNum = metadataReader.ReadInt32BE();
            var nodes = new List<Tuple<Int64, Int64,string>>(nodesNum);
            for (var i = 0; i < nodesNum; i++)
            {
                var offset = metadataReader.ReadInt64BE();
                var size = metadataReader.ReadInt64BE();
                metadataReader.ReadUInt32BE();  // status
                var name = metadataReader.ReadCString();
                nodes.Add(Tuple.Create(offset, size, name));
            }

            if (blocks.Count != nodes.Count)
            {
                throw new NotSupportedException("blocks.Count != nodes.Count");
            }

            var assets = new List<Asset>();

            for (var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                var node = nodes[i];
                var name = node.Item3;

                MemoryStream blockStream;
                switch (block.CompressionType)
                {
                    case CompressionType.None:
                        blockStream = new MemoryStream(mainReader.ReadBytes(block.CompressedSize));
                        break;
                    case CompressionType.LZMA:
                        {
                            var properties = mainReader.ReadBytes(5);
                            var decoder = new SevenZip.Compression.LZMA.Decoder();
                            blockStream = new MemoryStream(block.UncompressedSize);
                            decoder.SetDecoderProperties(properties);
                            decoder.Code(mainReader.BaseStream, blockStream, block.CompressedSize - 5, block.UncompressedSize, null);
                            blockStream.Seek(0, SeekOrigin.Begin);
                            break;
                        }
                    case CompressionType.LZ4:
                    case CompressionType.LZ4HC:
                        {
                            var decoded = LZ4.LZ4Codec.Decode(mainReader.ReadBytes(block.CompressedSize), 0, block.UncompressedSize, block.CompressedSize);
                            blockStream = new MemoryStream(decoded);
                            break;
                        }
                    default:
                        throw new NotSupportedException("Not supported compression type : " + block.CompressionType);
                }

                assets.Add(new Asset(name, this, new BinaryReader(blockStream)));
                blockStream.Dispose();
            }

            this.Assets = assets;
            if (this.Assets.Count == 0)
            {
                this.Name = string.Empty;
            }
            else
            {
                this.Name = this.Assets[0].Name;
            }
        }
    }
}
