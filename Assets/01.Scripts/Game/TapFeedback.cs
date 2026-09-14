using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using UnityEngine;

namespace _01.Scripts.Game
{
    /// <summary>One contact produces one cue, one haptic and a bounded visual burst.</summary>
    public sealed class TapFeedback : MonoBehaviour
    {
        private const int Capacity = 10;
        private sealed class Burst
        {
            public Transform Root;
            public SpriteRenderer Ring;
            public readonly SpriteRenderer[] Sparks = new SpriteRenderer[6];
            public float Age;
            public Color Color;
            public bool Failure;
            public float Duration = .26f;
        }
        private readonly Burst[] m_Bursts = new Burst[Capacity];
        private int m_Next;
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
            burst.Color = new Color(1f, .12f, .24f);
            burst.Root.position = target.transform.position;
            burst.Ring.sprite = target.GetSprite(target.Type);
            burst.Root.gameObject.SetActive(true);
            Animate(burst);
        }

        public void SetPaused(bool paused) => m_Paused = paused;

        public void Clear()
        {
            foreach (Burst burst in m_Bursts)
                if (burst != null) burst.Root.gameObject.SetActive(false);
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
        }

        private static void Animate(Burst burst)
        {
            float t = burst.Age / burst.Duration;
            burst.Ring.transform.localScale = Vector3.one * (burst.Failure ? Mathf.Lerp(.95f, .15f, t) : Mathf.Lerp(.82f, 1.45f, t));
            burst.Ring.color = burst.Failure ? new Color(1f, .15f, .28f, 1f - t) : new Color(1f, 1f, 1f, .7f * (1f - t));
            for (int i = 0; i < burst.Sparks.Length; i++)
            {
                float angle = i * Mathf.PI / 3f + .25f;
                var spark = burst.Sparks[i];
                spark.transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(.3f, burst.Failure ? 1.15f : .78f, t);
                spark.transform.localScale = Vector3.one * Mathf.Lerp(.095f, .025f, t);
                spark.color = new Color(burst.Color.r, burst.Color.g, burst.Color.b, 1f - t);
            }
        }
    }
}
