using System.IO;

namespace UnityPackSharp
{
    public class ObjectPointer
    {
        public Asset Source { get; private set; }
        public TypeTree TypeTree { get; private set; }
        public int FileId { get; private set; }
        public long PathId { get; private set; }

        internal ObjectPointer(Asset source, TypeTree typeTree)
        {
            this.TypeTree = TypeTree;
            this.Source = source;
        }

        internal void Load(BinaryReader reader)
        {
            this.FileId = reader.ReadInt32(this.Source.bigendian);
            this.PathId = this.Source.ReadId(reader);
        }

        public Asset GetAsset()
        {
            if (this.FileId >= this.Source.assetRefs.Count)
            {
                return null;
            }

            var obj = this.Source.assetRefs[this.FileId];
            var asset = obj as Asset;
            if (asset == null)
            {
                var assetRef = obj as AssetRef;
                if (assetRef != null)
                {
                    asset = assetRef.Resolve();
                }
            }

            return asset;
        }
    }
}
