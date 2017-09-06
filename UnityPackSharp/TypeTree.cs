using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnityPackSharp
{
    public class TypeTree
    {
        private static readonly byte[] StringTable = Encoding.ASCII.GetBytes("AABB\0AnimationClip\0AnimationCurve\0AnimationState\0Array\0Base\0BitField\0bitset\0bool\0char\0ColorRGBA\0Component\0data\0deque\0double\0dynamic_array\0FastPropertyName\0first\0float\0Font\0GameObject\0Generic Mono\0GradientNEW\0GUID\0GUIStyle\0int\0list\0long long\0map\0Matrix4x4f\0MdFour\0MonoBehaviour\0MonoScript\0m_ByteSize\0m_Curve\0m_EditorClassIdentifier\0m_EditorHideFlags\0m_Enabled\0m_ExtensionPtr\0m_GameObject\0m_Index\0m_IsArray\0m_IsStatic\0m_MetaFlag\0m_Name\0m_ObjectHideFlags\0m_PrefabInternal\0m_PrefabParentObject\0m_Script\0m_StaticEditorFlags\0m_Type\0m_Version\0Object\0pair\0PPtr<Component>\0PPtr<GameObject>\0PPtr<Material>\0PPtr<MonoBehaviour>\0PPtr<MonoScript>\0PPtr<Object>\0PPtr<Prefab>\0PPtr<Sprite>\0PPtr<TextAsset>\0PPtr<Texture>\0PPtr<Texture2D>\0PPtr<Transform>\0Prefab\0Quaternionf\0Rectf\0RectInt\0RectOffset\0second\0set\0short\0size\0SInt16\0SInt32\0SInt64\0SInt8\0staticvector\0string\0TextAsset\0TextMesh\0Texture\0Texture2D\0Transform\0TypelessData\0UInt16\0UInt32\0UInt64\0UInt8\0unsigned int\0unsigned long long\0unsigned short\0vector\0Vector2f\0Vector3f\0Vector4f\0m_ScriptingClassIdentifier\0Gradient\0");

        public TypeMetadata TypeMetadata { get; private set; }

        public short Version { get; private set; }
        public bool IsArray { get; private set; }
        public string Type { get;private set; }
        public string Name { get; private set; }
        public int Size { get; private set; }
        public int Index { get; private set; }
        public int Flags { get; private set; }

        public IReadOnlyList<TypeTree> Children { get { return children.AsReadOnly();  } }

        private byte[] data;
        private List<TypeTree> children = new List<TypeTree>();

        internal bool PostAlign
        {
            get
            {
                return (this.Flags & 0x4000) > 0;
            }
        }

        internal TypeTree(TypeMetadata typeMetadata)
        {
            this.TypeMetadata = typeMetadata;
        }

        internal void Load(BinaryReader reader)
        {
            var format = this.TypeMetadata.Asset.Format;
            if (format == 10 || format >= 12)
            {
                LoadBlob(reader);
            }
            else
            {
                throw new NotSupportedException("Not supported format : " + format);
            }
        }

        private void LoadBlob(BinaryReader reader)
        {
            var bigendian = this.TypeMetadata.Asset.bigendian;
            var numNodes = reader.ReadInt32(bigendian);
            var bufferBytes = reader.ReadInt32(bigendian);
            var nodeData = reader.ReadBytes(24 * (int)numNodes);
            this.data = reader.ReadBytes(bufferBytes);

            var parents = new Stack<TypeTree>();
            parents.Push(this);
            using (var nodeReader = new BinaryReader(new MemoryStream(nodeData)))
            {
                for (var i = 0; i < numNodes; i++)
                {
                    var version = nodeReader.ReadInt16(bigendian);
                    var depth = nodeReader.ReadByte();
                    TypeTree current;

                    if (depth == 0)
                    {
                        current = this;
                    }
                    else
                    {
                        while (parents.Count > depth)
                        {
                            parents.Pop();
                        }

                        current = new TypeTree(this.TypeMetadata);
                        parents.Peek().children.Add(current);
                        parents.Push(current);
                    }

                    current.Version = version;
                    current.IsArray = nodeReader.ReadByte() != 0;
                    current.Type = GetString(nodeReader.ReadInt32(bigendian));
                    current.Name = GetString(nodeReader.ReadInt32(bigendian));
                    current.Size = nodeReader.ReadInt32(bigendian);
                    current.Index = nodeReader.ReadInt32(bigendian);
                    current.Flags = nodeReader.ReadInt32(bigendian);
                }
            }
        }

        private string GetString(int offset)
        {
            byte[] stringTable;

            if (offset < 0)
            {
                offset &= 0x7fffffff;
                stringTable = StringTable;
            }
            else if (offset < this.data.Length)
            {
                stringTable = this.data;
            }
            else
            {
                return string.Format("unknown({0})", offset);
            }

            int count = 0;
            for (var i = offset; i < stringTable.Length; i++)
            {
                if (stringTable[i] == 0)
                {
                    break;
                }
                count++;
            }

            if (count > 0)
            {
                return Encoding.UTF8.GetString(stringTable, offset, count);
            }

            return string.Empty;
        }
    }
}
