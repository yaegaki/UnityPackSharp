using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityPackSharp.Engine;

namespace UnityPackSharp
{
    public class ObjectInfo
    {
        public Asset Asset { get; private set; }
        public int TypeId { get; private set; }
        public long PathId { get; private set; }
        public int ClassId { get; private set; }

        public TypeTree TypeTree
        {
            get
            {
                return Asset.FindType(TypeId);
            }
        }

        private int dataOffset;
        private int size;

        internal ObjectInfo(Asset asset)
        {
            this.Asset = asset;
        }

        internal void Load(BinaryReader reader)
        {
            var bigendian = this.Asset.bigendian;
            var format = this.Asset.Format;

            if (this.Asset.longObjectIds)
            {
                this.PathId = reader.ReadInt64(bigendian);
            }
            else
            {
                this.PathId = this.Asset.ReadId(reader);
            }

            this.dataOffset = reader.ReadInt32(bigendian);
            this.size = reader.ReadInt32(bigendian);
            if (format < 17)
            {
                this.TypeId = reader.ReadInt32(bigendian);
                this.ClassId = (int)reader.ReadInt16(bigendian);
            }
            else
            {
                var typeId = reader.ReadInt32(bigendian);
                this.ClassId = this.Asset.TypeMetadata.FindClassId(typeId);
                this.TypeId = this.ClassId;
            }

            if (format <= 10)
            {
                reader.ReadInt16(bigendian);    // isDestroyed
            }

            if (format >= 11 && format <= 16)
            {
                reader.ReadInt16(bigendian);    // unknown
            }

            if (format >= 15 && format <= 16)
            {
                reader.ReadByte();  // unknown
            }
        }

        public EngineObject ReadEngineObject()
        {
            var typeTree = this.TypeTree;
            if (typeTree == null)
            {
                return null;
            }
            using (var ms = new MemoryStream(this.Asset.objectData))
            {
                ms.Seek(this.dataOffset, SeekOrigin.Begin);
                return ReadValue(typeTree, new BinaryReader(ms)) as EngineObject;
            }
        }

        private object ReadValue(TypeTree typeTree, BinaryReader reader)
        {
            var beforePos = reader.BaseStream.Position;
            object result = null;
            var needAlign = false;
            var typeName = typeTree.Type;
            var bigendian = this.Asset.bigendian;
            switch (typeName)
            {
                case "bool":
                    result = reader.ReadBoolean();
                    break;
                case "SInt8":
                    result = reader.ReadSByte();
                    break;
                case "UInt8":
                    result = reader.ReadByte();
                    break;
                case "SInt16":
                    result = reader.ReadInt16(bigendian);
                    break;
                case "UInt16":
                    result = reader.ReadUInt16(bigendian);
                    break;
                case "SInt64":
                    result = reader.ReadInt64(bigendian);
                    break;
                case "UInt64":
                    result = reader.ReadUInt64(bigendian);
                    break;
                case "UInt32":
                case "unsigned int":
                    result = reader.ReadUInt32(bigendian);
                    break;
                case "SInt32":
                case "int":
                    result = reader.ReadInt32(bigendian);
                    break;
                case "float":
                    reader.Align(4);
                    result = reader.ReadSingle();
                    break;
                case "string":
                    {
                        var size = reader.ReadInt32(bigendian);
                        result = reader.ReadBytes(size);
                        needAlign = typeTree.Children[0].PostAlign;
                    }
                    break;
                default:
                    {
                        var firstChild = typeTree.IsArray ? typeTree : typeTree.Children[0];

                        if (typeName.StartsWith("PPtr<"))
                        {
                            var ptr = new ObjectPointer(this.Asset, typeTree);
                            ptr.Load(reader);
                            result = ptr;
                        }
                        else if (firstChild != null && firstChild.IsArray)
                        {
                            needAlign = firstChild.IsArray;
                            size = reader.ReadInt32(bigendian);
                            var arrayType = firstChild.Children[1];
                            if (arrayType.Type == "char" || arrayType.Type == "UInt8")
                            {
                                result = reader.ReadBytes(size).OfType<object>().ToArray();
                            }
                            else
                            {
                                var array = new object[size];
                                for (var i = 0; i < size; i++)
                                {
                                    array[i] = ReadValue(arrayType, reader);
                                }
                                result = array;
                            }
                        }
                        else if (typeName == "pair")
                        {
                            var first = ReadValue(typeTree.Children[0], reader);
                            var second = ReadValue(typeTree.Children[1], reader);
                            result = new KeyValuePair<object, object>(first, second);
                        }
                        else
                        {
                            var dict = new Dictionary<string, object>(typeTree.Children.Count);
                            foreach (var child in typeTree.Children)
                            {
                                dict[child.Name] = ReadValue(child, reader);
                            }
                            result = new EngineObject(typeTree, dict);
                        }
                    }
                    break;
            }

            var afterPos = reader.BaseStream.Position;
            var actualSize = afterPos - beforePos;
            if (typeTree.Size > 0 &&  actualSize < typeTree.Size)
            {
                throw new NotSupportedException(string.Format("Expected ReadValue {0}, Actual {1}", typeTree.Size, actualSize));
            }

            if (needAlign || typeTree.PostAlign)
            {
                reader.Align(4);
            }

            return result;
        }
    }
}
