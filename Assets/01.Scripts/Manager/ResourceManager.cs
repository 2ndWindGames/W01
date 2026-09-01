using System.Collections.Generic;
using UnityEngine;

namespace _01.Scripts.Manager
{
    public class ResourceManager
    {
        private readonly Dictionary<string, Sprite> m_Sprites = new();

        public void Init()
        {
        }

        public T Load<T>(string path) where T : Object
        {
            if (typeof(T) == typeof(Sprite))
            {
                if (m_Sprites.TryGetValue(path, out var sprite))
                    return sprite as T;

                var sp = Resources.Load<Sprite>(path);
                m_Sprites.Add(path, sp);
                return sp as T;
            }

            return Resources.Load<T>(path);
        }

        public GameObject Instantiate(string path, Transform parent = null)
        {
            var prefab = Load<GameObject>($"Prefabs/{path}");
            if (prefab != null) 
                return Instantiate(prefab, parent);
            
            Debug.Log($"Failed to load prefab : {path}");
            return null;

        }

        public GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            GameObject go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            return go;
        }

        public void Destroy(GameObject go)
        {
            if (go == null)
                return;

            Object.Destroy(go);
        }
    }
}

