using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;
using TMPro;

namespace _01.Scripts.UI.Popup
{
    public class UI_SoundPopup : UI_Popup
    {
        enum Buttons
        {
            btnBgm,
            btnEffect,
            btnClose
        }

        private TextMeshProUGUI m_BgmLabel;
        private TextMeshProUGUI m_EffectLabel;
        
        public override bool Init()
        {
            if (!base.Init())
                return false;
            
            BindButton(typeof(Buttons));
			GameLocalization.ApplyFont(this);
            GetButton((int)Buttons.btnBgm).gameObject.BindEvent(OnClickBgmButton);
            GetButton((int)Buttons.btnEffect).gameObject.BindEvent(OnClickEffectButton);
            GetButton((int)Buttons.btnClose).gameObject.BindEvent(OnClickCloseButton);

            m_BgmLabel = GetButton((int)Buttons.btnBgm).GetComponentInChildren<TextMeshProUGUI>();
            m_EffectLabel = GetButton((int)Buttons.btnEffect).GetComponentInChildren<TextMeshProUGUI>();
			foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				if (label.name == "txtTitle") label.text = GameLocalization.T("SOUND", "사운드");
				else if (label.name == "txtClose") label.text = GameLocalization.T("CLOSE", "닫기");
			}
            RefreshLabels();
			
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
