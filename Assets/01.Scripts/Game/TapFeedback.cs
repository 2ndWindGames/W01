using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using TMPro;
using UnityEngine;

namespace _01.Scripts.Game
{
    /// <summary>One contact produces one cue, one haptic and a bounded visual burst.</summary>
    public sealed class TapFeedback : MonoBehaviour
    {
        private const int Capacity = 10;
        private const int ScoreCapacity = 12;
        private const float ScoreDuration = .72f;
        private sealed class Burst
        {
            public Transform Root;
            public SpriteRenderer Ring;
            public readonly SpriteRenderer[] Sparks = new SpriteRenderer[6];
            public float Age;
            public Color Color;
            public bool Failure;
            public float Duration = .26f;
            public float SizeScale = 1f;
        }
        private sealed class ScoreFloat
        {
            public Transform Root;
            public TextMeshPro Label;
            public Vector3 StartPosition;
            public Color Color;
            public float Age;
            public float Direction;
            public float VerticalDirection;
        }
        private readonly Burst[] m_Bursts = new Burst[Capacity];
        private readonly ScoreFloat[] m_Scores = new ScoreFloat[ScoreCapacity];
        private int m_Next;
        private int m_NextScore;
        private bool m_Paused;

        public static string Cue(TapTargetType type, bool fever) => type == TapTargetType.Bomb ? "SFX/Target_Bomb"
            : type == TapTargetType.Quick ? (fever ? "SFX/Fever_Quick" : "SFX/Target_Quick")
            : type == TapTargetType.TimeBonus ? (fever ? "SFX/Fever_TimeBonus" : "SFX/Target_TimeBonus")
            : fever ? "SFX/Fever_Tap" : "SFX/Target_NeonTap";

        public static void PlayCue(TapTargetType type, bool fever)
        {
            Managers.Sound.Play(Define.Sound.Effect, Cue(type, fever), type == TapTargetType.Bomb ? .75f : .66f);
            TapHaptics.Play(type, fever);
        }

        public void Initialize(Sprite particle)
        {
            for (int i = 0; i < Capacity; i++)
            {
                var burst = new Burst();
                burst.Root = new GameObject("Tap Burst " + i).transform;
                burst.Root.SetParent(transform, false);
                burst.Ring = Renderer("Echo", burst.Root, null, 220);
                for (int j = 0; j < burst.Sparks.Length; j++)
                    burst.Sparks[j] = Renderer("Spark " + j, burst.Root, particle, 221);
                burst.Root.gameObject.SetActive(false);
                m_Bursts[i] = burst;
            }
            for (int i = 0; i < ScoreCapacity; i++)
            {
                var score = new ScoreFloat();
                score.Root = new GameObject("Earned Score " + i).transform;
                score.Root.SetParent(transform, false);
                score.Label = new GameObject("Score").AddComponent<TextMeshPro>();
                score.Label.transform.SetParent(score.Root, false);
                score.Label.transform.localScale = Vector3.one * .1f;
                score.Label.rectTransform.sizeDelta = new Vector2(18f, 6f);
                score.Label.alignment = TextAlignmentOptions.Center;
                score.Label.textWrappingMode = TextWrappingModes.NoWrap;
                score.Label.fontSize = 35f;
                score.Label.fontStyle = FontStyles.Bold;
                GameLocalization.ApplyFont(score.Label);
                score.Label.outlineColor = new Color32(5, 9, 24, 255);
                score.Label.outlineWidth = .18f;
                var meshRenderer = score.Label.GetComponent<MeshRenderer>();
                meshRenderer.sortingOrder = 230;
                score.Root.gameObject.SetActive(false);
                m_Scores[i] = score;
            }
            // Decompressed short clips are ready before the first target is touched.
            foreach (TapTargetType type in System.Enum.GetValues(typeof(TapTargetType)))
            {
                Managers.Sound.GetAudioClipLength(Cue(type, false));
                Managers.Sound.GetAudioClipLength(Cue(type, true));
            }
        }

        private static SpriteRenderer Renderer(string name, Transform parent, Sprite sprite, int order)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        public void Show(CircleTarget target, bool fever)
        {
            PlayCue(target.Type, fever);
            Burst burst = m_Bursts[m_Next];
            m_Next = (m_Next + 1) % Capacity;
            burst.Age = 0f;
            burst.Failure = false;
            burst.Duration = .26f;
            burst.SizeScale = target.NominalScale / .82f;
            burst.Root.position = target.transform.position;
            burst.Root.gameObject.SetActive(true);
            burst.Ring.sprite = target.GetSprite(target.Type);
            burst.Color = target.Type == TapTargetType.Bomb ? new Color(1f, .2f, .3f)
                : target.Type == TapTargetType.TimeBonus ? new Color(1f, .82f, .22f)
                : fever ? new Color(1f, .35f, .9f)
                : target.Type == TapTargetType.Quick ? new Color(.65f, .45f, 1f) : new Color(.2f, .9f, 1f);
            Animate(burst);
        }

        public void ShowMiss(CircleTarget target, int lostCombo)
        {
            Managers.Sound.Play(Define.Sound.Effect,
                lostCombo >= 5 ? "SFX/Combo_Break" : "SFX/Target_Miss", lostCombo >= 5 ? .8f : .65f);
            TapHaptics.PlayFailure();
            Burst burst = m_Bursts[m_Next];
            m_Next = (m_Next + 1) % Capacity;
            burst.Age = 0f;
            burst.Failure = true;
            burst.Duration = lostCombo >= 5 ? .52f : .38f;
            burst.SizeScale = target.NominalScale / .82f;
            burst.Color = new Color(1f, .12f, .24f);
            burst.Root.position = target.transform.position;
            burst.Ring.sprite = target.GetSprite(target.Type);
            burst.Root.gameObject.SetActive(true);
            Animate(burst);
        }

        /// <summary>Shows the actual points awarded and any time bonus next to the hit target.</summary>
        public void ShowScore(CircleTarget target, int points, int bonusSeconds = 0)
        {
            if (target == null || points <= 0) return;
            ScoreFloat score = m_Scores[m_NextScore];
            m_NextScore = (m_NextScore + 1) % ScoreCapacity;
            if (score == null) return;

            // Keep the number beside the neon ring, including near viewport edges.
            Vector3 targetPosition = target.transform.position;
            Camera camera = Camera.main;
            Vector3 viewport = camera != null ? camera.WorldToViewportPoint(targetPosition) : new Vector3(.5f, .5f);
            score.Direction = viewport.x > .68f ? -1f : 1f;
            score.VerticalDirection = viewport.y > .78f ? -1f : 1f;
            float scoreOffset = target.NominalScale / .82f;
            score.StartPosition = targetPosition + new Vector3(score.Direction * .46f * scoreOffset,
                score.VerticalDirection * .38f * scoreOffset, -.04f);
            score.Root.position = score.StartPosition;
            score.Root.localScale = Vector3.one;
            score.Age = 0f;
            score.Color = bonusSeconds > 0 ? new Color(1f, .89f, .56f)
                : points >= 20 ? new Color(1f, .88f, .36f)
                : points >= 6 ? new Color(.56f, 1f, .76f) : new Color(.72f, .96f, 1f);
            score.Label.rectTransform.sizeDelta = new Vector2(18f, bonusSeconds > 0 ? 10f : 6f);
            score.Label.text = bonusSeconds > 0
                ? "+" + points + "\n<size=55%><color=#FFD76C>+" + bonusSeconds + "s</color></size>"
                : "+" + points;
            SpriteRenderer targetRenderer = target.GetComponent<SpriteRenderer>();
            if (targetRenderer != null)
                score.Label.GetComponent<MeshRenderer>().sortingLayerID = targetRenderer.sortingLayerID;
            score.Root.gameObject.SetActive(true);
            Animate(score);
        }

        public void SetPaused(bool paused) => m_Paused = paused;

        public void Clear()
        {
            foreach (Burst burst in m_Bursts)
                if (burst != null) burst.Root.gameObject.SetActive(false);
            foreach (ScoreFloat score in m_Scores)
                if (score != null) score.Root.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (m_Paused) return;
            foreach (Burst burst in m_Bursts)
            {
                if (burst == null || !burst.Root.gameObject.activeSelf) continue;
                burst.Age += Time.deltaTime;
                if (burst.Age >= burst.Duration) burst.Root.gameObject.SetActive(false);
                else Animate(burst);
            }
            foreach (ScoreFloat score in m_Scores)
            {
                if (score == null || !score.Root.gameObject.activeSelf) continue;
                score.Age += Time.deltaTime;
                if (score.Age >= ScoreDuration) score.Root.gameObject.SetActive(false);
                else Animate(score);
            }
        }

        private static void Animate(ScoreFloat score)
        {
            float t = Mathf.Clamp01(score.Age / ScoreDuration);
            float pop = Mathf.Lerp(.72f, 1.15f, Mathf.Clamp01(t / .14f));
            score.Root.localScale = Vector3.one * Mathf.Lerp(pop, .96f, Mathf.Clamp01((t - .2f) / .8f));
            score.Root.position = score.StartPosition + new Vector3(score.Direction * .08f * t,
                score.VerticalDirection * .56f * t, 0f);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - .42f) / .58f));
            score.Label.color = new Color(score.Color.r, score.Color.g, score.Color.b, alpha);
        }

        private static void Animate(Burst burst)
        {
            float t = burst.Age / burst.Duration;
            burst.Ring.transform.localScale = Vector3.one * burst.SizeScale
                * (burst.Failure ? Mathf.Lerp(.95f, .15f, t) : Mathf.Lerp(.82f, 1.45f, t));
            burst.Ring.color = burst.Failure ? new Color(1f, .15f, .28f, 1f - t) : new Color(1f, 1f, 1f, .7f * (1f - t));
            for (int i = 0; i < burst.Sparks.Length; i++)
            {
                float angle = i * Mathf.PI / 3f + .25f;
                var spark = burst.Sparks[i];
                spark.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * burst.SizeScale
                    * Mathf.Lerp(.3f, burst.Failure ? 1.15f : .78f, t);
                spark.transform.localScale = Vector3.one * burst.SizeScale * Mathf.Lerp(.095f, .025f, t);
                spark.color = new Color(burst.Color.r, burst.Color.g, burst.Color.b, 1f - t);
            }
        }
    }
}
