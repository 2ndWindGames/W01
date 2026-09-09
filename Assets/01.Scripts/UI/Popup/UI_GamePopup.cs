using System;
using System.Threading.Tasks;
using _01.Scripts.Scene;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
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
			txtComboValue,
			txtResultValue
		}
		
		enum Buttons
		{
			btnBack,
			btnStart,
			btnRetry
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

			var curScene = SceneManagerEx.CurrentScene;
			mGameScene = curScene.GetComponent<GameScene>();
			if (mGameScene == null)
			{
				return;
			}
			
			GetButton((int)Buttons.btnBack).gameObject.BindEvent(ClickBackButton);
			GetButton((int)Buttons.btnStart).gameObject.BindEvent(mGameScene.StartRound);
			GetButton((int)Buttons.btnRetry).gameObject.BindEvent(mGameScene.RetryRound);
			GetText((int)Texts.txtBestValue).text = mGameScene.bestScore.ToString("00");
			
			IsInitialized = true;
		}

		private void ClickBackButton()
		{
			if (Managers.Ads.IsShowingInterstitial) return;
			Managers.Scene.ChangeScene(_01.Scripts.Scene.W01SceneType.Intro);
		}


		public Button GetButtonStart() => GetButton((int)Buttons.btnStart);
		public Button GetButtonRetry() => GetButton((int)Buttons.btnRetry);

		public void BindEventStartButton(Action action)
		{
			GetButton((int)Buttons.btnStart).gameObject.BindEvent(action);
		}
		
		public void BindEventRetryButton(Action action)
		{
			GetButton((int)Buttons.btnRetry).gameObject.BindEvent(action);
		} 
		
		public TextMeshProUGUI GetTextTime() => GetText((int)Texts.txtTimeValue);
		public TextMeshProUGUI GetTextBest() => GetText((int)Texts.txtBestValue);
		public TextMeshProUGUI GetTextStatus() => GetText((int)Texts.txtStatus);
		public TextMeshProUGUI GetTextScore() => GetText((int)Texts.txtScoreValue);
		public TextMeshProUGUI GetTextCombo() => GetText((int)Texts.txtComboValue);
		public TextMeshProUGUI GetTextResult() => GetText((int)Texts.txtResultValue);
	}
}
