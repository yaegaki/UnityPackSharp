using System;
using System.Collections.Generic;

namespace UnityPackSharp
{
    public class UnityEnvironment
    {
        private Dictionary<string, AssetBundle> assetBundleDict = new Dictionary<string, AssetBundle>();

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
    }
}
