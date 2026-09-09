using _01.Scripts.Scene;
using SWGUnity2DCore.Scene;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWGUnity2DCore.Manager
{
    public class SceneManagerEx
    {
        private _01.Scripts.Scene.W01SceneType m_CurSceneType = _01.Scripts.Scene.W01SceneType.Unknown;

        public _01.Scripts.Scene.W01SceneType CurrentSceneType
        {
            get
            {
                if (m_CurSceneType != _01.Scripts.Scene.W01SceneType.Unknown)
                    return m_CurSceneType;
                return CurrentScene.sceneType;
            }
            set => m_CurSceneType = value;
        }

        public static BaseScene CurrentScene => GameObject.Find("Scene").GetComponent<BaseScene>();

        public void Init()
        {

        }

        public void ChangeScene(_01.Scripts.Scene.W01SceneType type)
        {
            CurrentScene.Clear();

            m_CurSceneType = type;
            SceneManager.LoadScene(GetSceneName(type));
        }

        string GetSceneName(_01.Scripts.Scene.W01SceneType type)
        {
            string name = System.Enum.GetName(typeof(_01.Scripts.Scene.W01SceneType), type);
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
