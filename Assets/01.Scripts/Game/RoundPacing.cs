using UnityEngine;
using SWGUnity2DCore;

namespace _01.Scripts.Game
{
    public static class RoundPacing
    {
        public static float Progress(float elapsed, GameConfig config) =>
            Mathf.Clamp01(elapsed / Mathf.Max(1f, config.difficultyRampSeconds));

        public static float Lifetime(float elapsed, TapTargetType type, bool fever, GameConfig config)
        {
            float progress = Mathf.Pow(Progress(elapsed, config), 0.85f);
            float lifetime = Mathf.Lerp(config.startingTargetLifetime, config.minimumTargetLifetime, progress);
            if (type == TapTargetType.Quick) lifetime *= 0.7f;
            if (fever) lifetime *= config.feverTargetLifetimeMultiplier;
            return Mathf.Max(config.minimumReactionSeconds, lifetime);
        }

        public static int Stage(float elapsed, GameConfig config) =>
            elapsed >= Mathf.Max(config.scanStartSeconds + 1f, config.surgeStartSeconds) ? 3
                : elapsed >= config.scanStartSeconds ? 2 : 1;
    }
}
