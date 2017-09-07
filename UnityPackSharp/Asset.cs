using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityPackSharp
{
    public class Asset
    {
        public string Name { get; private set; }
        public AssetBundle Bundle { get; private set; }
        public int Format { get; private set; }
        public TypeMetadata TypeMetadata { get; private set; }

        public IReadOnlyCollection<ObjectInfo> Objects { get { return objectDict.Values; } }

        internal bool bigendian = true;
        internal bool longObjectIds;
        internal byte[] objectData;
        internal List<object> assetRefs = new List<object>();

        private Dictionary<int, TypeTree> typeDict = new Dictionary<int, TypeTree>();
        private Dictionary<long, ObjectInfo> objectDict = new Dictionary<long, ObjectInfo>();

        public bool IsResource
        {
            get
            {
                return Name.EndsWith(".resource");
            }
        }

        internal Asset(string name, AssetBundle bundle, BinaryReader reader)
        {
            this.Name = name;
            this.Bundle = bundle;
            this.assetRefs.Add(this);

            if (bundle.Signature == AssetBundle.UnityFSSignature)
            {
                LoadFS(reader);
            }
            else
            {
                throw new NotSupportedException("Not supported signature : " + bundle.Signature);
            }
        }

        private void LoadFS(BinaryReader reader)
        {
            if (this.IsResource)
            {
                return;
            }

            var metadataSize = reader.ReadInt32BE();
            var fileSize = reader.ReadInt32BE();
            this.Format = reader.ReadInt32BE();
            var objectDataOffset = reader.ReadInt32BE();

            if (this.Format >= 9)
            {
                this.bigendian = reader.ReadBoolean();
                reader.ReadBytes(3);    // padding
            }

            this.TypeMetadata = new TypeMetadata(this);
            this.TypeMetadata.Load(reader);

            if (7 <= this.Format && this.Format <= 13)
            {
                this.longObjectIds = reader.ReadInt32(this.bigendian) != 0;
            }

            var numObjectInfos = reader.ReadInt32(this.bigendian);
            for (var i = 0; i < numObjectInfos; i++)
            {
                if (this.Format >= 14)
                {
                    reader.Align(4);
                }

                var obj = new ObjectInfo(this);
                obj.Load(reader);
                RegisterObject(obj);
            }

            // Unknown
            if (this.Format >= 15)
            {
                var unkCount = reader.ReadInt32(this.bigendian);
                reader.Align(4);
                reader.ReadBytes(unkCount * 0xc);
            }

            if (this.Format >= 6)
            {
                var numRefs = reader.ReadInt32(this.bigendian);
                for (var i = 0; i < numRefs; i++)
                {
                    var assetRef = new AssetRef(this);
                    assetRef.Load(reader);
                    this.assetRefs.Add(assetRef);
                }
            }

            reader.ReadString();    // unknown

            reader.BaseStream.Seek(objectDataOffset, SeekOrigin.Begin);
            this.objectData = reader.ReadBytes(fileSize - objectDataOffset);
        }

        internal long ReadId(BinaryReader reader)
        {
            if (this.Format >= 14)
            {
                return reader.ReadInt64(this.bigendian);
            }
            else
            {
                return (long)reader.ReadInt32(this.bigendian);
            }
        }

        private void RegisterObject(ObjectInfo obj)
        {
            var tree = this.TypeMetadata.FindType(obj.TypeId);
            if (tree != null)
            {
                this.typeDict[obj.TypeId] = tree;
            }
            else if (!this.typeDict.ContainsKey(obj.TypeId))
            {
                // need structs.dat
                this.typeDict[obj.TypeId] = null;
            }

            if (this.objectDict.ContainsKey(obj.PathId))
            {
                throw new InvalidDataException("Duplicate asset object");
            }

            this.objectDict[obj.PathId] = obj;
        }

        internal TypeTree FindType(int typeId, int classId = 0)
        {
            TypeTree tree;
            if (typeId < 0)
            {
                tree = this.TypeMetadata.FindType(typeId);
                if (tree == null)
                {
                    tree = this.TypeMetadata.FindType(classId);
                }
            }
            else
            {
                this.typeDict.TryGetValue(typeId, out tree);
            }
            return tree;
        }

        public Asset GetAsset(string filePath)
        {
            var environment = this.Bundle.Environment;
            if (filePath.Contains(':'))
            {
                return environment.GetAsset(filePath);
            }

            return environment.GetAssetByFileName(filePath);
        }
    }
}
