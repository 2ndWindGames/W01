using UnityEngine;

namespace SWGUnity2DCore
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
        public float startingTargetLifetime = 1.55f;

        [Min(0.2f)]
        public float minimumTargetLifetime = 0.55f;

        [Min(1f)] public float difficultyRampSeconds = 26f;
        [Min(0.2f)] public float minimumReactionSeconds = 0.38f;

        [Range(0.25f, 1.5f)]
        public float minimumTargetScale = 0.8f;

        [Header("Fever")]
        [Min(2)]
        public int feverCombo = 10;

        [Min(1f)]
        public float feverDuration = 5f;

        [Min(1)]
        public int feverTargetCount = 2;

        [Min(2)]
        public int feverScoreMultiplier = 3;

        [Min(0f)]
        public float feverBonusTimePerTap = 0.12f;

        [Min(1f)]
        public float feverMaximumDuration = 8f;

        [Range(0.4f, 1f)]
        public float feverTargetLifetimeMultiplier = 0.78f;

        public GameObject targetPrefab;
    }
}
