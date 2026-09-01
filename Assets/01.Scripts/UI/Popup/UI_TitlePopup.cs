using _01.Scripts.Manager;
using _01.Scripts.Util;
using UnityEngine;
using static Define;

namespace _01.Scripts.UI.Popup
{
	public class UITitlePopup : UI_Popup
	{
		enum Texts
		{
			TouchToStartText,
			StartButtonText,
			ContinueButtonText,
			CollectionButtonText,
			//DataResetConfirmText
		}

		enum Buttons
		{
			StartButton,
			ContinueButton,
			CollectionButton
		}

		public override bool Init()
		{
			if (!base.Init())
				return false;

			BindText(typeof(Texts));
			BindButton(typeof(Buttons)); 

			GetButton((int)Buttons.StartButton).gameObject.BindEvent(OnClickStartButton);
			GetButton((int)Buttons.ContinueButton).gameObject.BindEvent(OnClickContinueButton);
			GetButton((int)Buttons.CollectionButton).gameObject.BindEvent(OnClickCollectionButton);


			// GetText((int)Texts.StartButtonText).text = Managers.GetText(Define.StartButtonText);
			// GetText((int)Texts.ContinueButtonText).text = Managers.GetText(Define.ContinueButtonText);
			// GetText((int)Texts.CollectionButtonText).text = Managers.GetText(Define.CollectionButtonText);

			Managers.Sound.Clear();
			Managers.Sound.Play(Sound.Effect, "Sound_MainTitle");
			return true;
		}

		void OnClickStartButton()
		{
			Debug.Log("OnClickStartButton");
			Managers.Sound.Play(Sound.Effect, "Sound_FolderItemClick");

			// ������ �ִ��� Ȯ��
			// if (Managers.Game.LoadGame())
			// {
			// 	Managers.UI.ShowPopupUI<UI_ConfirmPopup>().SetInfo(() =>
			// 	{
			// 		Managers.Game.Init();
			// 		Managers.Game.SaveGame();
			//
			// 		Managers.UI.ClosePopupUI(this); // UI_TitlePopup
			// 		Managers.UI.ShowPopupUI<UI_NamePopup>();
			// 	}, Managers.GetText(Define.DataResetConfirm));
			// }
			// else
			// {
			// 	Managers.Game.Init();
			// 	Managers.Game.SaveGame();
			//
			// 	Managers.UI.ClosePopupUI(this); // UI_TitlePopup
			// 	Managers.UI.ShowPopupUI<UI_NamePopup>();
			// }		
		}

		void OnClickContinueButton()
		{
			Debug.Log("OnClickContinueButton");
			Managers.Sound.Play(Sound.Effect, ("Sound_FolderItemClick"));
			// Managers.Game.Init();
			// Managers.Game.LoadGame();

			Managers.UI.ClosePopupUI(this);
			// Managers.UI.ShowPopupUI<UI_PlayPopup>();
		}

		void OnClickCollectionButton()
		{
			Managers.Sound.Play(Sound.Effect, ("Sound_FolderItemClick"));
			// Managers.Game.Init();
			// Managers.Game.LoadGame();
			//
			// Debug.Log("OnClickCollectionButton");
			// Managers.UI.ShowPopupUI<UI_CollectionPopup>();
		}
	}
}
