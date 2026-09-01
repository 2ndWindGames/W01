using _01.Scripts.Scene;
using _01.Scripts.Util.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _01.Scripts.Manager
{
    public class SceneManagerEx
    {
        private Define.Scene m_CurSceneType = Define.Scene.Unknown;

        public Define.Scene CurrentSceneType
        {
            get
            {
                if (m_CurSceneType != Define.Scene.Unknown)
                    return m_CurSceneType;
                return CurrentScene.sceneType;
            }
            set => m_CurSceneType = value;
        }

        private static BaseScene CurrentScene => GameObject.Find("Scene").GetComponent<BaseScene>();

        public void Init()
        {

        }

        public void ChangeScene(Define.Scene type)
        {
            CurrentScene.Clear();

            m_CurSceneType = type;
            SceneManager.LoadScene(GetSceneName(type));
        }

        string GetSceneName(Define.Scene type)
        {
            string name = System.Enum.GetName(typeof(Define.Scene), type);
            if (name == null)
            {
                GameLog.Error($"{GetType().Name} scene name is null", "system", null);
                return string.Empty;
            } 
            
            char[] letters = name.ToLower().ToCharArray();
            letters[0] = char.ToUpper(letters[0]);
            return new string(letters);
        }
    }
}
