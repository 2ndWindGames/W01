using System;
using _01.Scripts.Scene;
using _01.Scripts.Util;
using TMPro;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
	public class UI_GamePopup : UI_Popup
	{
		enum Texts
		{
			txtStatus,
			txtScoreValue,
			txtBestValue,
			txtTimeValue,
			txtStart,
		}
		
		enum Buttons
		{
			btnStart
		}
		
		private GameScene mGameScene;

		public bool IsInitialized { get; private set; }
		
		public override bool Init()
		{
			if (!base.Init())
				return false;


			if (IsInitialized)
			{
				return true;
			} 
			
			Initialize();
			return true;
		}

		public void Initialize()
		{
			BindText(typeof(Texts));
			BindButton(typeof(Buttons));

			var curScene = Manager.SceneManagerEx.CurrentScene;
			mGameScene = curScene.GetComponent<GameScene>();
			if (mGameScene == null)
			{
				return;
			}
			
			GetButton((int)Buttons.btnStart).gameObject.BindEvent(mGameScene.StartRound);

			GetText((int)Texts.txtBestValue).text = mGameScene.bestScore.ToString("00");

			
			IsInitialized = true;
		}
		
		
		public Button GetButtonStart() => GetButton((int)Buttons.btnStart);

		public void BindEventStartButton(Action action)
		{
			GetButton((int)Buttons.btnStart).gameObject.BindEvent(action);
		} 
		
		public TextMeshProUGUI GetTextTime() => GetText((int)Texts.txtTimeValue);
		public TextMeshProUGUI GetTextBest() => GetText((int)Texts.txtBestValue);
		public TextMeshProUGUI GetTextStatus() => GetText((int)Texts.txtStatus);
		public TextMeshProUGUI GetTextScore() => GetText((int)Texts.txtScoreValue);
	}
}
