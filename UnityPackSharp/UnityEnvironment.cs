using System;
using System.Collections.Generic;
using UnityPackSharp.Engine;

namespace UnityPackSharp
{
    public class UnityEnvironment
    {
        private Dictionary<string, AssetBundle> assetBundleDict = new Dictionary<string, AssetBundle>();
        private List<IEngineObjectCreator> engineObjectCreators = new List<IEngineObjectCreator>()
        {
            EngineObjectCreator.Instance,
        };


        public AssetBundle LoadAssetBundle(string path)
        {
            var ab = new AssetBundle(this);
            ab.Load(path);
            this.assetBundleDict[ab.Name] = ab;

            return ab;
        }

        public AssetBundle LoadAssetBundle(byte[] bytes)
        {
            var ab = new AssetBundle(this);
            ab.Load(bytes);
            this.assetBundleDict[ab.Name] = ab;

            return ab;
        }

        public Asset GetAsset(string filePath)
        {
            throw new NotImplementedException();
            
            /*
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            var uri = new Uri(filePath);
            if (uri.Scheme == "archive")
            {
            }
            return null;
            */
        }

        public Asset GetAssetByFileName(string filePath)
        {
            throw new NotImplementedException();
        }

        public void RegisterEngineObjectCreator(IEngineObjectCreator creator)
        {
            this.engineObjectCreators.Add(creator);
        }

        internal EngineObject CreateEngineObject(TypeTree typeTree, Dictionary<string, object> dict)
        {
            foreach (var creator in this.engineObjectCreators)
            {
                var obj = creator.CreateEngineObject(typeTree, dict);
                if (obj != null)
                {
                    return obj;
                }
            }

            return new EngineObject(typeTree, dict);
        }
    }
}
