using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityPackSharp.Engine
{
    public class Transform : EngineObject, IEnumerable<Transform>
    {
        private GameObject _gameObject;
        public GameObject gameObject
        {
            get
            {
                if (_gameObject == null)
                {
                    _gameObject = Get<ObjectPointer>("m_GameObject").GetEngineObject<GameObject>();
                }

                return _gameObject;
            }
        }

        public Transform parent
        {
            get
            {
                var ptr = Get<ObjectPointer>("m_Father");
                if (ptr.IsNull)
                {
                    return null;
                }

                return ptr.GetEngineObject<Transform>();
            }
        }

        public Transform root
        {
            get
            {
                var target = this;
                while (true)
                {
                    var _parent = target.parent;
                    if (_parent == null)
                    {
                        return target;
                    }

                    target = _parent;
                }
            }
        }

        private IReadOnlyList<Transform> children;

        public Transform(TypeTree typeTree, Dictionary<string, object> dict)
            : base(typeTree, dict)
        {
        }


        #region IEnumerable<Transform>

        public IEnumerator<Transform> GetEnumerator()
        {
            if (this.children == null)
            {
                var _children = Get<object[]>("m_Children");
                if (_children == null)
                {
                    this.children = Array.Empty<Transform>();
                }
                else
                {
                    this.children = _children
                        .OfType<ObjectPointer>()
                        .Select(c => c.GetEngineObject<Transform>())
                        .Where(t => t != null)
                        .ToArray();
                }
            }

            return this.children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
