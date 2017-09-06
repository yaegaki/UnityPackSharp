using System;
using System.IO;
using System.Text;

namespace UnityPackSharp
{
    static class BinaryReaderExtensions
    {
        public static string ReadCString(this BinaryReader reader)
        {
            var sb = new StringBuilder();
            while (true)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }

                sb.Append((char)b);
            }

            return sb.ToString();
        }

        public static Int16 ReadInt16(this BinaryReader reader, bool bingendian)
        {
            if (bingendian)
            {
                return reader.ReadInt16BE();
            }
            else
            {
                return reader.ReadInt16();
            }
        }

        public static Int16 ReadInt16BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public static UInt16 ReadUInt16(this BinaryReader reader, bool bingendian)
        {
            if (bingendian)
            {
                return reader.ReadUInt16BE();
            }
            else
            {
                return reader.ReadUInt16();
            }
        }

        public static UInt16 ReadUInt16BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static Int32 ReadInt32(this BinaryReader reader, bool bigendian)
        {
            if (bigendian)
            {
                return reader.ReadInt32BE();
            }
            else
            {
                return reader.ReadInt32();
            }
        }

        public static Int32 ReadInt32BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static UInt32 ReadUInt32(this BinaryReader reader, bool bigendian)
        {
            if (bigendian)
            {
                return reader.ReadUInt32BE();
            }
            else
            {
                return reader.ReadUInt32();
            }
        }

        public static UInt32 ReadUInt32BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static UInt64 ReadUInt64(this BinaryReader reader, bool bigendian)
        {
            if (bigendian)
            {
                return reader.ReadUInt64BE();
            }
            else
            {
                return reader.ReadUInt64();
            }
        }

        public static UInt64 ReadUInt64BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static Int64 ReadInt64(this BinaryReader reader, bool bigendian)
        {
            if (bigendian)
            {
                return reader.ReadInt64BE();
            }
            else
            {
                return reader.ReadInt64();
            }
        }

        public static Int64 ReadInt64BE(this BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static void Align(this BinaryReader reader, int align)
        {
            var m = (int)(reader.BaseStream.Position % align);
            if (m != 0)
            {
                reader.ReadBytes(align - m);
            }
        }
    }
}
