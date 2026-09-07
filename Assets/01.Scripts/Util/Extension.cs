using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.Util
{
	public static class Extension
	{
		/// <summary>
		/// Collider2D 내부의 임의 지점을 콜라이더 Transform 기준 로컬 좌표로 반환합니다.
		/// </summary>
		public static Vector2 GetRandomPointInside(
			this Collider2D collider, float margin = 0f, int maxAttempts = 256)
		{
			if (collider == null)
				throw new ArgumentNullException(nameof(collider));
			if (!collider.enabled || !collider.gameObject.activeInHierarchy)
				throw new InvalidOperationException($"{collider.name}: 활성화된 Collider2D가 필요합니다.");
			if (margin < 0f)
				throw new ArgumentOutOfRangeException(nameof(margin), "마진은 0 이상이어야 합니다.");
			if (maxAttempts <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxAttempts));

			Bounds bounds = collider.bounds;
			float minX = bounds.min.x + margin;
			float maxX = bounds.max.x - margin;
			float minY = bounds.min.y + margin;
			float maxY = bounds.max.y - margin;

			if (minX > maxX || minY > maxY)
				throw new InvalidOperationException(
					$"{collider.name}: 마진 {margin}이 Collider2D 크기보다 큽니다.");

			for (int i = 0; i < maxAttempts; i++)
			{
				Vector2 worldPoint = new Vector2(
					UnityEngine.Random.Range(minX, maxX),
					UnityEngine.Random.Range(minY, maxY));

				if (IsInsideWithMargin(collider, worldPoint, margin))
					return collider.transform.InverseTransformPoint(worldPoint);
			}

			throw new InvalidOperationException(
				$"{collider.name}: Collider2D 내부 좌표를 찾지 못했습니다. 면적이 없는 EdgeCollider2D인지 확인하세요.");
		}

		/// <summary>
		/// Collider2D 내부의 임의 지점을 월드 좌표로 반환합니다.
		/// </summary>
		public static Vector2 GetRandomPointInsideWorld(
			this Collider2D collider, float margin = 0f, int maxAttempts = 256)
		{
			Vector2 localPoint = collider.GetRandomPointInside(margin, maxAttempts);
			Vector3 worldPoint = collider.transform.TransformPoint(localPoint);
			return new Vector2(worldPoint.x, worldPoint.y);
		}

		private static bool IsInsideWithMargin(Collider2D collider, Vector2 point, float margin)
		{
			if (!collider.OverlapPoint(point))
				return false;
			if (margin <= Mathf.Epsilon)
				return true;

			const int directionCount = 16;
			for (int i = 0; i < directionCount; i++)
			{
				float angle = i * Mathf.PI * 2f / directionCount;
				Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * margin;
				if (!collider.OverlapPoint(point + offset))
					return false;
			}

			return true;
		}

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
