using System;
using System.Threading.Tasks;
using _01.Scripts.UI.Intro;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;
using UnityEngine;

namespace _01.Scripts.UI.Popup
{
	public class UI_IntroPopup : UI_Popup
	{
		enum Buttons
		{
			btn_start,
			btn_setting,
			btn_sound,
			btn_rank,
			btn_easteregg,
		}

		public override bool Init()
		{
			if (!base.Init())
				return false;

			BindButton(typeof(Buttons));

			GetButton((int)Buttons.btn_start).gameObject.BindEvent(OnClickBtnStart);
			GetButton((int)Buttons.btn_setting).gameObject.BindEvent(OnClickBtnSetting);
			GetButton((int)Buttons.btn_sound).gameObject.BindEvent(OnClickBtnSound);
			GetButton((int)Buttons.btn_rank).gameObject.BindEvent(OnClickBtnRank);
			GetButton((int)Buttons.btn_easteregg).gameObject.BindEvent(OnClickBtnEasterEgg);

			var motionAnimator = GetComponent<IntroMotionAnimator>();
			if (motionAnimator == null)
				motionAnimator = gameObject.AddComponent<IntroMotionAnimator>();
			motionAnimator.Initialize();

			return true;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnStart()
		{
			GameLog.Debug("OnClickBtnStart");
			Managers.Scene.ChangeScene(_01.Scripts.Scene.W01SceneType.Game);
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnSetting()
		{
			GameLog.Debug("OnClickBtnSetting");
			
			var popUp = Managers.UI.FindPopup<UI_SettingPopup>();
			if (popUp == null)
			{
				Managers.UI.ShowPopupUI<UI_SettingPopup>();	
				Managers.Rank.GetTopNPlayers(10);
			}
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnSound()
		{
			GameLog.Debug("OnClickBtnSound");
			
			var popUp = Managers.UI.FindPopup<UI_SoundPopup>();
			if (popUp == null)
			{
				Managers.UI.ShowPopupUI<UI_SoundPopup>();	
			}

		}
		
		private void OnClickBtnRank()
		{
			GameLog.Debug("OnClickBtnRank");
			
			var popUp = Managers.UI.FindPopup<UI_RankPopup>();
			if (popUp == null)
			{
				Managers.UI.ShowPopupUI<UI_RankPopup>();	
			}
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnEasterEgg()
		{
			GameLog.Debug("OnClickBtnEasterEgg");
		}
	}
}
