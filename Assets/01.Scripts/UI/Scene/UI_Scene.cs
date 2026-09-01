using _01.Scripts.Manager;

namespace _01.Scripts.UI.Scene
{
	public class UI_Scene : UI_Base
	{
		public override bool Init()
		{
			if (!base.Init())
				return false;

			Managers.UI.SetCanvas(gameObject, false);
			return true;
		}
	}
}
