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

        public GameObject targetPrefab;
    }
}
