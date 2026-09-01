using System;
using _01.Scripts.Manager;
using _01.Scripts.Util;
using _01.Scripts.Util.Logging;
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
			GetButton((int)Buttons.btn_easteregg).gameObject.BindEvent(OnClickBtnEasterEgg);
			
			return true;
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnStart()
		{
			GameLog.Debug("OnClickBtnStart");
			
			Managers.Scene.ChangeScene(Define.Scene.Game);
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnSetting()
		{
			GameLog.Debug("OnClickBtnSetting");
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnSound()
		{
			GameLog.Debug("OnClickBtnSound");
		}
		
		// ReSharper disable Unity.PerformanceAnalysis
		private void OnClickBtnEasterEgg()
		{
			GameLog.Debug("OnClickBtnEasterEgg");
		}
	}
}
