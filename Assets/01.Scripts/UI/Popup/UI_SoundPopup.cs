using System.Threading.Tasks;
using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using SWGUnity2DCore.Util.Logging;

namespace _01.Scripts.UI.Popup
{
    public class UI_SoundPopup : UI_Popup
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
			
            return true;
        }

        private void OnClickCloseButton()
        {
            GameLog.Debug("OnClickCloseButton");
            Managers.UI.ClosePopupUI(this);
        }
    }
}