using SWGUnity2DCore.UI;
using UnityEngine;

namespace _01.Scripts.UI.SubItem
{
    public class UI_RankingItem : UI_Base
    {
        enum Texts
        {
            txtRank,
            txtNicName,
            txtScore
        }
        
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
			
            IsInitialized = true;
        }

        public void SetProfile(int entryRank, string nickname, double entryScore)
        {
            GetText((int)Texts.txtRank).text = entryRank.ToString("00");
            GetText((int)Texts.txtNicName).text = nickname;
            GetText((int)Texts.txtScore).text = entryScore.ToString("000");
        }
    }
}
