using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
    public class UI_SoundPopup : UI_Popup
    {
        enum Buttons
        {
            btnBgm,
            btnEffect,
            btnClose,
            btnPrivacy
        }

        private TextMeshProUGUI m_BgmLabel;
        private TextMeshProUGUI m_EffectLabel;
        private Button m_PrivacyButton;
        private AdsManager m_AdsManager;
        
        public override bool Init()
        {
            if (!base.Init())
                return false;
            
            BindButton(typeof(Buttons));
			GameLocalization.ApplyFont(this);
            PopupPresentation.Prepare(transform);
            GetButton((int)Buttons.btnBgm).gameObject.BindEvent(OnClickBgmButton);
            GetButton((int)Buttons.btnEffect).gameObject.BindEvent(OnClickEffectButton);
            GetButton((int)Buttons.btnClose).gameObject.BindEvent(OnClickCloseButton);
            m_PrivacyButton = GetButton((int)Buttons.btnPrivacy);
            m_AdsManager = Managers.Ads;
            m_PrivacyButton.gameObject.BindEvent(OnClickPrivacyButton);

            m_BgmLabel = GetButton((int)Buttons.btnBgm).GetComponentInChildren<TextMeshProUGUI>();
            m_EffectLabel = GetButton((int)Buttons.btnEffect).GetComponentInChildren<TextMeshProUGUI>();
			foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				if (label.name == "txtTitle") label.text = GameLocalization.T("SETTINGS", "설정");
				else if (label.name == "txtClose") label.text = GameLocalization.T("CLOSE", "닫기");
				else if (label.name == "txtPrivacy") label.text = GameLocalization.T("PRIVACY OPTIONS", "개인정보 설정");
			}
            RefreshLabels();
			RefreshPrivacyButton();
			
            return true;
        }

        private void OnClickBgmButton()
        {
            Managers.SetBgmEnabled(!Managers.IsBgmEnabled);
            RefreshLabels();
        }

        private void OnClickEffectButton()
        {
            Managers.SetEffectEnabled(!Managers.IsEffectEnabled);
            RefreshLabels();
        }

        private void Update() => RefreshPrivacyButton();

        private void RefreshPrivacyButton()
        {
            if (m_PrivacyButton == null || m_AdsManager == null) return;
            bool required = m_AdsManager.PrivacyOptionsRequired;
            if (m_PrivacyButton.gameObject.activeSelf != required)
                m_PrivacyButton.gameObject.SetActive(required);
        }

        private void OnClickPrivacyButton()
        {
            if (m_AdsManager != null && m_AdsManager.PrivacyOptionsRequired)
                m_AdsManager.ShowPrivacyOptions();
        }

        private void RefreshLabels()
        {
            if (m_BgmLabel != null)
                m_BgmLabel.text = $"BGM : {(Managers.IsBgmEnabled ? "ON" : "OFF")}";
            if (m_EffectLabel != null)
				m_EffectLabel.text = $"{GameLocalization.T("SFX", "효과음")} : {(Managers.IsEffectEnabled ? "ON" : "OFF")}";
        }

        private void OnClickCloseButton()
        {
            GameLog.Debug("OnClickCloseButton");
            Managers.UI.ClosePopupUI(this);
        }
    }
}
