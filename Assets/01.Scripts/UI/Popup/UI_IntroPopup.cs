using System;
using System.Threading.Tasks;
using _01.Scripts.UI.Intro;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;
using TMPro;
using UnityEngine;

namespace _01.Scripts.UI.Popup
{
	public class UI_IntroPopup : UI_Popup
	{
		enum Buttons
		{
			btn_start,
			btn_sound,
			btn_rank,
			btn_easteregg,
		}

		public override bool Init()
		{
			if (!base.Init())
				return false;

			BindButton(typeof(Buttons));
			GameLocalization.ApplyFont(this);

			GetButton((int)Buttons.btn_start).gameObject.BindEvent(OnClickBtnStart);
			GetButton((int)Buttons.btn_sound).gameObject.BindEvent(OnClickBtnSound);
			GetButton((int)Buttons.btn_rank).gameObject.BindEvent(OnClickBtnRank);
			GetButton((int)Buttons.btn_easteregg).gameObject.BindEvent(OnClickBtnEasterEgg);
			SetButtonLabel(Buttons.btn_start, GameLocalization.T("TOUCH TO START", "터치해서 시작"));
			SetButtonLabel(Buttons.btn_sound, GameLocalization.T("SOUND", "사운드"));
			SetButtonLabel(Buttons.btn_rank, GameLocalization.T("RANKING", "랭킹"));
			foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				if (label.name != "txt_right") continue;
				label.text = GameLocalization.T(
					"© 2026 SECONDWINDGAMES",
					"© 2026 SECONDWINDGAMES");
				label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 90f);
				label.raycastTarget = false;
				break;
			}

			var motionAnimator = GetComponent<IntroMotionAnimator>();
			if (motionAnimator == null)
				motionAnimator = gameObject.AddComponent<IntroMotionAnimator>();
			motionAnimator.Initialize();

			return true;
		}

		private void SetButtonLabel(Buttons button, string value)
		{
			var label = GetButton((int)button).GetComponentInChildren<TextMeshProUGUI>(true);
			if (label != null) label.text = value;
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnStart()
		{
			GameLog.Debug("OnClickBtnStart");
			Managers.Scene.ChangeScene(_01.Scripts.Scene.W01SceneType.Game);
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
