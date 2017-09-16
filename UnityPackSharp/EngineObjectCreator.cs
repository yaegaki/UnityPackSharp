using System.Collections.Generic;
using UnityPackSharp.Engine;

namespace UnityPackSharp
{
    class EngineObjectCreator : IEngineObjectCreator
    {
        public static EngineObjectCreator Instance { get; } = new EngineObjectCreator();

        public EngineObject CreateEngineObject(TypeTree typeTree, Dictionary<string, object> dict)
        {
            switch (typeTree.Type)
            {
                case "TextAsset":
                    return new TextAsset(typeTree, dict);
                case "GameObject":
                    return new GameObject(typeTree, dict);
                case "Transform":
                    return new Transform(typeTree, dict);
                default:
                    return null;
            }
        }
    }
}
