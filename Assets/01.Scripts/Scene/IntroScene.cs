using _01.Scripts.Manager;
using _01.Scripts.UI.Popup;
using UnityEngine;

namespace _01.Scripts.Scene
{
	public class IntroScene : BaseScene
	{
		protected override bool Init()
		{
			if (!base.Init())
				return false;

			sceneType = Define.Scene.Intro;
			Managers.UI.ShowPopupUI<UI_IntroPopup>();
			Debug.Log("Init");
			return true;
		}
	}
}
