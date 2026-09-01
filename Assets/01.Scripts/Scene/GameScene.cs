using _01.Scripts.Manager;
using _01.Scripts.UI.Popup;
using UnityEngine;

namespace _01.Scripts.Scene
{
	public class GameScene : BaseScene
	{
		protected override bool Init()
		{
			if (base.Init() == false)
				return false;

			sceneType = Define.Scene.Game;
			Managers.UI.ShowPopupUI<UITitlePopup>();
			Debug.Log("Init");
			return true;
		}
	}
}
