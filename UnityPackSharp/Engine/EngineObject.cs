using System.Collections.Generic;
using System.Text;

namespace UnityPackSharp.Engine
{
    [System.Diagnostics.DebuggerDisplay("Type:{TypeTree.Type} Name:{Name}")]
    public class EngineObject
    {
        public TypeTree TypeTree { get; private set; }
        private Dictionary<string, object> dictionary;

        public string Name
        {
            get
            {
                return GetString("m_Name");
            }
        }

        public EngineObject(TypeTree typeTree, Dictionary<string, object> dictionary)
        {
            this.TypeTree = typeTree;
            this.dictionary = dictionary;
        }

        public IReadOnlyCollection<string> ParameterKeys
        {
            get
            {
                return dictionary.Keys;
            }
        }

        public T Get<T>(string name)
        {
            object obj;
            if (this.dictionary.TryGetValue(name, out obj))
            {
                if (obj is T)
                {
                    return (T)obj;
                }
            }

            return default(T);
        }

        public string GetString(string name)
        {
            var bytes = Get<byte[]>(name);
            if (bytes != null)
            {
                return Encoding.UTF8.GetString(bytes);
            }

            return string.Empty;
        }
    }
}
