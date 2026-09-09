using System.Threading.Tasks;
using _01.Scripts.Manager;
using _01.Scripts.Util;
using _01.Scripts.Util.Logging;
using TMPro;
using UnityEngine;

namespace _01.Scripts.UI.Popup
{
    public class UI_SettingPopup : UI_Popup
    {
        enum Buttons
        {
            btnClose
        }
        
        public override bool Init()
        {
            if (!base.Init())
                return false;
			
            BindButton(typeof(Buttons));
            GetButton((int)Buttons.btnClose).gameObject.BindEvent(OnClickCloseButton);

            if (Managers.Ads.PrivacyOptionsRequired)
            {
                var close = GetButton((int)Buttons.btnClose);
                var privacy = Object.Instantiate(close, close.transform.parent);
                privacy.name = "btnAdsPrivacy";
                privacy.onClick.RemoveAllListeners();
                var handler = privacy.GetComponent<UI_EventHandler>();
                if (handler != null)
                {
                    handler.OnClickHandler = null;
                    handler.OnPressedHandler = null;
                    handler.OnPointerDownHandler = null;
                    handler.OnPointerUpHandler = null;
                }
                var label = privacy.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "광고 개인정보 설정";
                var rect = (RectTransform)privacy.transform;
                rect.anchoredPosition += Vector2.up * (rect.rect.height + 20);
                privacy.onClick.AddListener(Managers.Ads.ShowPrivacyOptions);
            }
            
            return true;
        }
        
        private void OnClickCloseButton()
        {
            GameLog.Debug("OnClickCloseButton");
            Managers.UI.ClosePopupUI(this);
        }
    }
}
