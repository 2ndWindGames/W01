using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.Util
{
	public static class Extension
	{
		public static T GetOrAddComponent<T>(this GameObject go) where T : UnityEngine.Component
		{
			return Utils.GetOrAddComponent<T>(go);
		}

		public static void BindEvent(this GameObject go, Action action, Define.UIEvent type = Define.UIEvent.Click)
		{
			UI_Base.BindEvent(go, action, type);
		}

		static readonly System.Random Rand = new System.Random();

		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = Rand.Next(n + 1);
				(list[k], list[n]) = (list[n], list[k]);
			}
		}

		public static T GetRandom<T>(this IList<T> list)
		{
			int index = Rand.Next(list.Count);
			return list[index];
		}

		public static void ResetVertical(this ScrollRect scrollRect)
		{
			scrollRect.verticalNormalizedPosition = 1;
		}

		public static void ResetHorizontal(this ScrollRect scrollRect)
		{
			scrollRect.horizontalNormalizedPosition = 1;
		}
	}
}
