using System.Collections.Generic;

namespace UnityPackSharp.Engine
{
    public class GameObject : EngineObject
    {
        private Transform _transform;
        public Transform transform
        {
            get
            {
                if (_transform == null)
                {
                    var components = Get<object[]>("m_Component");
                    foreach (var component in components)
                    {
                        ObjectPointer ptr;
                        // old version
                        if (component is KeyValuePair<object, object>)
                        {
                            ptr = (ObjectPointer)(((KeyValuePair<object, object>)component).Value);
                        }
                        // new version
                        else if (component is EngineObject)
                        {
                            ptr = ((EngineObject)component).Get<ObjectPointer>("component");
                        }
                        else
                        {
                            continue;
                        }

                        _transform = ptr.GetEngineObject<Transform>();
                        if (_transform != null)
                        {
                            break;
                        }
                    }
                }

                return _transform;
            }
        }
        public GameObject(TypeTree typeTree, Dictionary<string, object> dict)
            : base(typeTree, dict)
        {

        }
    }
}
