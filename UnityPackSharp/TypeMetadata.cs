using System.Collections.Generic;
using System.IO;

namespace UnityPackSharp
{
    public class TypeMetadata
    {
        public Asset Asset { get; private set; }

        public string GeneratorVersion { get; private set; }
        public uint TargetPlatform { get; private set; }

        private List<int> classIds;
        private Dictionary<int, byte[]> hashDict;
        private Dictionary<int, TypeTree> typeTreeDict;

        internal TypeMetadata(Asset asset)
        {
            this.Asset = asset;
        }

        internal void Load(BinaryReader reader)
        {
            var bigendian = this.Asset.bigendian;
            var format = this.Asset.Format;

            this.GeneratorVersion = reader.ReadCString();
            this.TargetPlatform = reader.ReadUInt32(bigendian);


            if (format >= 13)
            {
                var hasTypeTree = reader.ReadBoolean();
                var numTypes = reader.ReadInt32(bigendian);
                this.classIds = new List<int>(numTypes);
                this.hashDict = new Dictionary<int, byte[]>(numTypes);
                if (hasTypeTree)
                {
                    this.typeTreeDict = new Dictionary<int, TypeTree>(numTypes);
                }

                for (var i = 0; i < numTypes; i++)
                {
                    var classId = reader.ReadInt32(bigendian);
                    if (format >= 17)
                    {
                        reader.ReadByte();  // unknown
                        var scriptId = reader.ReadInt16(bigendian);

                        if (classId == 114)
                        {
                            if (scriptId >= 0)
                            {
                                classId = -1 - scriptId;
                            }
                            else
                            {
                                classId = -1;
                            }
                        }
                    }

                    this.classIds.Add(classId);
                    byte[] hash;
                    if (classId < 0)
                    {
                        hash = reader.ReadBytes(0x20);
                    }
                    else
                    {
                        hash = reader.ReadBytes(0x10);
                    }
                    this.hashDict[classId] = hash;

                    if (hasTypeTree)
                    {
                        var tree = new TypeTree(this);
                        tree.Load(reader);
                        this.typeTreeDict[classId] = tree;
                    }
                }
            }
            else
            {
                this.typeTreeDict = new Dictionary<int, TypeTree>();
                var numFields = reader.ReadInt32(bigendian);
                for (var i = 0; i < numFields; i++)
                {
                    var classId = reader.ReadInt32(bigendian);
                    var tree = new TypeTree(this);
                    tree.Load(reader);
                    this.typeTreeDict[classId] = tree;
                }
            }
        }

        internal TypeTree FindType(int typeId)
        {
            TypeTree tree;
            this.typeTreeDict.TryGetValue(typeId, out tree);
            return tree;
        }

        internal int FindClassId(int typeId)
        {
            return this.classIds[typeId];
        }
    }
}
