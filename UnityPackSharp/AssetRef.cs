using System.IO;

namespace UnityPackSharp
{
    public class AssetRef
    {
        public Asset Source { get; private set; }
        public Asset Asset { get; private set; }
        public string AssetPath { get; private set; }
        public int Type { get; private set; }
        public string FilePath { get; private set; }

        private byte[] guid;

        internal AssetRef(Asset source)
        {
            this.Source = source;
        }

        internal void Load(BinaryReader reader)
        {
            this.AssetPath = reader.ReadCString();
            this.guid = reader.ReadBytes(16);
            this.Type = reader.ReadInt32(this.Source.bigendian);
            this.FilePath = reader.ReadCString();
        }

        public Asset Resolve()
        {
            return this.Source.GetAsset(this.FilePath);
        }
    }
}
