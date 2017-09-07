using System.Collections.Generic;
using UnityPackSharp.Engine;

namespace UnityPackSharp
{
    public interface IEngineObjectCreator
    {
        EngineObject CreateEngineObject(TypeTree typeTree, Dictionary<string, object> dict);
    }
}
