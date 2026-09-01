using _01.Scripts.Manager;
using UnityEngine;
using UnityEngine.Serialization;

namespace _01.Scripts.Scene
{
    public class BaseScene : MonoBehaviour
    {
        [FormerlySerializedAs("SceneType")] public Define.Scene sceneType = Define.Scene.Unknown;

        private bool m_Init;

        private void Start()
        {
            Init();
        }

        protected virtual bool Init()
        {
            if (m_Init)
                return false;

            m_Init = true;
            GameObject go = GameObject.Find("EventSystem");
            if (go == null)
                Managers.Resource.Instantiate("UI/EventSystem").name = "@EventSystem";

            return true;
        }

        public virtual void Clear() { }
    }
}
