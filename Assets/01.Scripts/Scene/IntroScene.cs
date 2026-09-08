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
			Managers.Sound.Play(Define.Sound.Bgm, "BGM/Intro_NeonAwakening", 0.42f);
			Debug.Log("Init");
			return true;
		}
	}
}
