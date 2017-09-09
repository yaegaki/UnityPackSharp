using System;
using System.Linq;
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

            using (var dataStream = new MemoryStream(blocks.Sum(b => b.UncompressedSize)))
            {
                foreach (var block in blocks)
                {
                    switch (block.CompressionType)
                    {
                        case CompressionType.None:
                            mainReader.BaseStream.CopyTo(dataStream, block.CompressedSize);
                            break;
                        case CompressionType.LZMA:
                            {
                                var properties = mainReader.ReadBytes(5);
                                var decoder = new SevenZip.Compression.LZMA.Decoder();
                                decoder.SetDecoderProperties(properties);
                                decoder.Code(mainReader.BaseStream, dataStream, block.CompressedSize - 5, block.UncompressedSize, null);
                                break;
                            }
                        case CompressionType.LZ4:
                        case CompressionType.LZ4HC:
                            {
                                var b = mainReader.ReadBytes(block.CompressedSize);
                                var decoded = LZ4.LZ4Codec.Decode(b, 0, block.CompressedSize, block.UncompressedSize);
                                dataStream.Write(decoded, 0, decoded.Length);
                                break;
                            }
                        default:
                            throw new NotSupportedException("Not supported compression type : " + block.CompressionType);
                    }
                }

                var assets = new List<Asset>(nodes.Count);

                foreach (var node in nodes)
                {
                    var offset = node.Item1;
                    var size = node.Item2;
                    var name = node.Item3;
                    dataStream.Seek(offset, SeekOrigin.Begin);

                    assets.Add(new Asset(name, this, new BinaryReader(dataStream)));
                }

                this.Assets = assets;
            }

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
