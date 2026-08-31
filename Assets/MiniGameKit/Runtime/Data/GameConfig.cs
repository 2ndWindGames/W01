using UnityEngine;

namespace MiniGameKit
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MiniGameKit/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Min(0f)]
        public float roundDuration = 30f;

        [Min(1)]
        public int scorePerTap = 1;

        [Min(1)]
        public int initialTargetCount = 1;

        [Header("Difficulty")]
        [Min(0.2f)]
        public float startingTargetLifetime = 1.8f;

        [Min(0.2f)]
        public float minimumTargetLifetime = 0.65f;

        [Range(0.25f, 1.5f)]
        public float minimumTargetScale = 0.58f;

        [Header("Fever")]
        [Min(2)]
        public int feverCombo = 10;

        [Min(1f)]
        public float feverDuration = 5f;

        [Min(1)]
        public int feverTargetCount = 2;

        public GameObject targetPrefab;
    }
}
