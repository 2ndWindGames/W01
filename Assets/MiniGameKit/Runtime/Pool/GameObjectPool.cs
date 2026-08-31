using System.Collections.Generic;
using UnityEngine;

namespace MiniGameKit
{
    /// <summary>
    /// A single-prefab pool. Loading and asset lookup are intentionally outside this class.
    /// </summary>
    public sealed class GameObjectPool : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_Prefab;

        [SerializeField]
        private Transform m_Parent;

        [SerializeField]
        [Min(0)]
        private int m_InitialSize;

        private readonly List<GameObject> m_Instances = new List<GameObject>();

        private void Awake()
        {
            if (m_Parent == null)
            {
                m_Parent = transform;
            }

            Prewarm();
        }

        public void Initialize(GameObject prefab, Transform parent = null, int initialSize = 0)
        {
            DespawnAll();

            m_Prefab = prefab;
            m_Parent = parent != null ? parent : transform;
            m_InitialSize = Mathf.Max(0, initialSize);
            Prewarm();
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            if (m_Prefab == null)
            {
                Debug.LogError("GameObjectPool requires a prefab.", this);
                return null;
            }

            GameObject instance = FindInactiveInstance();
            if (instance == null)
            {
                instance = CreateInstance();
            }

            instance.transform.SetParent(m_Parent, false);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null || !m_Instances.Contains(instance))
            {
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(m_Parent, false);
        }

        public void DespawnAll()
        {
            for (int i = 0; i < m_Instances.Count; i++)
            {
                if (m_Instances[i] == null)
                {
                    continue;
                }

                m_Instances[i].SetActive(false);
                if (m_Parent != null)
                {
                    m_Instances[i].transform.SetParent(m_Parent, false);
                }
            }
        }

        private void Prewarm()
        {
            if (m_Prefab == null)
            {
                return;
            }

            while (m_Instances.Count < m_InitialSize)
            {
                CreateInstance();
            }
        }

        private GameObject FindInactiveInstance()
        {
            for (int i = 0; i < m_Instances.Count; i++)
            {
                if (m_Instances[i] != null && !m_Instances[i].activeSelf)
                {
                    return m_Instances[i];
                }
            }

            return null;
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Instantiate(m_Prefab, m_Parent);
            instance.name = m_Prefab.name;
            instance.SetActive(false);
            m_Instances.Add(instance);
            return instance;
        }

        private void OnDestroy()
        {
            m_Instances.Clear();
        }
    }
}
