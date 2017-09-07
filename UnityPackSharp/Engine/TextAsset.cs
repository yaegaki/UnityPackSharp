using System.Collections.Generic;

namespace UnityPackSharp.Engine
{
    public class TextAsset : EngineObject
    {
        public string text
        {
            get
            {
                return GetString("m_Script");
            }
        }

        public byte[] bytes
        {
            get
            {
                return Get<byte[]>("m_Script");
            }
        }

        public TextAsset(TypeTree typeTree, Dictionary<string, object> dictionary)
            : base(typeTree, dictionary)
        {
        }
    }
}
