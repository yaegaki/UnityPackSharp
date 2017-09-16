using System.IO;
using UnityPackSharp.Engine;

namespace UnityPackSharp
{
    public class ObjectPointer
    {
        public Asset Source { get; private set; }
        public TypeTree TypeTree { get; private set; }
        public int FileId { get; private set; }
        public long PathId { get; private set; }

        public bool IsNull { get { return PathId == 0; } }

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

        public EngineObject GetEngineObject()
        {
            return GetEngineObject<EngineObject>();
        }

        public T GetEngineObject<T>()
            where T : EngineObject
        {
            var asset = GetAsset();
            if (asset == null)
            {
                throw new System.Exception("not resolved");
            }

            var obj = asset.GetObjectByPathId(this.PathId);
            if (obj == null)
            {
                throw new System.Exception("not resolved");
            }

            return obj.ReadEngineObject<T>();
        }
    }
}
