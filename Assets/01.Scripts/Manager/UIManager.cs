using System.Collections.Generic;
using System.Linq;
using _01.Scripts.UI.Scene;
using UnityEngine;
using GameObject = UnityEngine.GameObject;

namespace _01.Scripts.Manager
{
	public class UIManager
	{
		private int m_Order = -20;

		private readonly Stack<UI_Popup> m_PopupStack = new();

		private UI_Scene SceneUI { get; set; }

		private GameObject Root
		{
			get
			{
				GameObject root = GameObject.Find("@UI_Root");
				if (root == null)
					root = new GameObject { name = "@UI_Root" };

				return root;
			}
		}

		public void SetCanvas(GameObject go, bool sort = true)
		{
			var canvas = Utils.GetOrAddComponent<Canvas>(go);
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.overrideSorting = true;

			if (sort)
			{
				canvas.sortingOrder = m_Order;
				m_Order++;
			}
			else
			{
				canvas.sortingOrder = 0;
			}
		}

		public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
		{
			if (string.IsNullOrEmpty(name))
				name = typeof(T).Name;

			var prefab = Managers.Resource.Load<UnityEngine.GameObject>($"Prefabs/UI/SubItem/{name}");

			GameObject go = Managers.Resource.Instantiate(prefab);
			if (parent != null)
				go.transform.SetParent(parent);

			go.transform.localScale = Vector3.one;
			go.transform.localPosition = prefab.transform.position;

			return Utils.GetOrAddComponent<T>(go);
		}

		public T ShowSceneUI<T>(string name = null) where T : UI_Scene
		{
			if (string.IsNullOrEmpty(name))
				name = typeof(T).Name;

			GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");
			T sceneUI = Utils.GetOrAddComponent<T>(go);
			SceneUI = sceneUI;

			go.transform.SetParent(Root.transform);

			return sceneUI;
		}

		public T ShowPopupUI<T>(string name = null, Transform parent = null) where T : UI_Popup
		{
			if (string.IsNullOrEmpty(name))
				name = typeof(T).Name;

			GameObject prefab = Managers.Resource.Load<GameObject>($"Prefabs/UI/Popup/{name}");

			GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");
			T popup = Utils.GetOrAddComponent<T>(go);
			m_PopupStack.Push(popup);

			if (parent != null)
				go.transform.SetParent(parent);
			else if (SceneUI != null)
				go.transform.SetParent(SceneUI.transform);
			else
				go.transform.SetParent(Root.transform);

			go.transform.localScale = Vector3.one;
			go.transform.localPosition = prefab.transform.position;

			return popup;
		}

		public T FindPopup<T>() where T : UI_Popup
		{
			return m_PopupStack.FirstOrDefault(x => x.GetType() == typeof(T)) as T;
		}

		public T PeekPopupUI<T>() where T : UI_Popup
		{
			if (m_PopupStack.Count == 0)
				return null;

			return m_PopupStack.Peek() as T;
		}

		public void ClosePopupUI(UI_Popup popup)
		{
			if (m_PopupStack.Count == 0)
				return;

			if (m_PopupStack.Peek() != popup)
			{
				Debug.Log("Close Popup Failed!");
				return;
			}

			ClosePopupUI();
		}

		public void ClosePopupUI()
		{
			if (m_PopupStack.Count == 0)
				return;

			UI_Popup popup = m_PopupStack.Pop();
			Managers.Resource.Destroy(popup.gameObject);
			popup = null;
			m_Order--;
		}

		public void CloseAllPopupUI()
		{
			while (m_PopupStack.Count > 0)
				ClosePopupUI();
		}

		public void Clear()
		{
			CloseAllPopupUI();
			SceneUI = null;
		}
	}
}
