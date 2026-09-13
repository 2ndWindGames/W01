using SWGUnity2DCore.Util;
using UnityEngine;

namespace SWGUnity2DCore.Manager
{
    /// <summary>Bounded 2D voices: effects retain their gain/pitch, music fades across scene changes.</summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class SoundPlayback : MonoBehaviour
    {
        private const int EffectVoiceCount = 12;
        private const float MusicFadeSeconds = 0.28f;
        private readonly AudioSource[] m_Music = new AudioSource[2];
        private readonly AudioSource[] m_Effects = new AudioSource[EffectVoiceCount];
        private readonly float[] m_EffectPitches = new float[EffectVoiceCount];
        private readonly double[] m_EffectStarted = new double[EffectVoiceCount];
        private readonly float[] m_FadeFrom = new float[2];
        private readonly float[] m_FadeTo = new float[2];
        private float m_FadeElapsed = MusicFadeSeconds;
        private float m_EffectPitch = 1f;
        private int m_CurrentMusic = -1;
        private bool m_MusicMuted;
        private bool m_EffectsMuted;

        public void Initialize()
        {
            if (m_Music[0] != null) return;
            for (int i = 0; i < m_Music.Length; i++)
                m_Music[i] = CreateSource(i == 0 ? "Bgm" : "Bgm Crossfade", true, 32);
            for (int i = 0; i < m_Effects.Length; i++)
                m_Effects[i] = CreateSource(i == 0 ? "Effect" : $"Effect {i:00}", false, 64);
        }

        private AudioSource CreateSource(string sourceName, bool loop, int priority)
        {
            Transform child = transform.Find(sourceName);
            GameObject go = child != null ? child.gameObject : new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            AudioSource source = go.GetComponent<AudioSource>();
            if (source == null) source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.loop = loop;
            source.priority = priority;
            source.volume = 0f;
            return source;
        }

        public void PlayBgm(AudioClip clip, float volume, float pitch)
        {
            // Result and retry can request the lobby again: preserve its musical phrase.
            if (m_CurrentMusic < 0 || m_Music[m_CurrentMusic].clip != clip || !m_Music[m_CurrentMusic].isPlaying)
            {
                m_CurrentMusic = m_CurrentMusic == 0 ? 1 : 0;
                AudioSource incoming = m_Music[m_CurrentMusic];
                incoming.Stop();
                incoming.clip = clip;
                incoming.volume = 0f;
                incoming.mute = m_MusicMuted;
                incoming.pitch = Mathf.Clamp(pitch, -3f, 3f);
                incoming.Play();
            }
            m_Music[m_CurrentMusic].pitch = Mathf.Clamp(pitch, -3f, 3f);
            for (int i = 0; i < m_Music.Length; i++)
            {
                m_FadeFrom[i] = m_Music[i].volume;
                m_FadeTo[i] = i == m_CurrentMusic ? Mathf.Clamp01(volume) : 0f;
            }
            m_FadeElapsed = 0f;
        }

        private void Update()
        {
            if (m_FadeElapsed >= MusicFadeSeconds) return;
            m_FadeElapsed = Mathf.Min(MusicFadeSeconds, m_FadeElapsed + Time.unscaledDeltaTime);
            float t = Mathf.SmoothStep(0f, 1f, m_FadeElapsed / MusicFadeSeconds);
            for (int i = 0; i < m_Music.Length; i++)
            {
                m_Music[i].volume = Mathf.Lerp(m_FadeFrom[i], m_FadeTo[i], t);
                if (i != m_CurrentMusic && m_FadeElapsed >= MusicFadeSeconds)
                {
                    m_Music[i].Stop();
                    m_Music[i].clip = null;
                }
            }
        }

        public void PlayEffect(AudioClip clip, float volume, float pitch)
        {
            int voice = 0;
            for (int i = 0; i < m_Effects.Length; i++)
            {
                if (!m_Effects[i].isPlaying) { voice = i; break; }
                if (m_EffectStarted[i] < m_EffectStarted[voice]) voice = i;
            }
            AudioSource source = m_Effects[voice];
            source.Stop();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.mute = m_EffectsMuted;
            m_EffectPitches[voice] = pitch;
            source.pitch = Mathf.Clamp(pitch * m_EffectPitch, -3f, 3f);
            m_EffectStarted[voice] = AudioSettings.dspTime;
            source.Play();
        }

        public void SetPitch(Define.Sound type, float pitch)
        {
            if (type == Define.Sound.Bgm)
            {
                foreach (AudioSource source in m_Music) source.pitch = Mathf.Clamp(pitch, -3f, 3f);
            }
            else if (type == Define.Sound.Effect)
            {
                m_EffectPitch = pitch;
                for (int i = 0; i < m_Effects.Length; i++)
                    m_Effects[i].pitch = Mathf.Clamp(m_EffectPitches[i] * pitch, -3f, 3f);
            }
        }

        public void SetMuted(Define.Sound type, bool muted)
        {
            if (type == Define.Sound.Bgm)
            {
                m_MusicMuted = muted;
                foreach (AudioSource source in m_Music) source.mute = muted;
            }
            else if (type == Define.Sound.Effect)
            {
                m_EffectsMuted = muted;
                foreach (AudioSource source in m_Effects) source.mute = muted;
            }
        }

        public bool IsMuted(Define.Sound type) => type == Define.Sound.Bgm ? m_MusicMuted
            : type == Define.Sound.Effect && m_EffectsMuted;

        public void Stop(Define.Sound type)
        {
            if (type != Define.Sound.Bgm && type != Define.Sound.Effect) return;
            foreach (AudioSource source in type == Define.Sound.Bgm ? m_Music : m_Effects)
            {
                source.Stop();
                source.clip = null;
            }
            if (type == Define.Sound.Bgm)
            {
                m_CurrentMusic = -1;
                m_FadeElapsed = MusicFadeSeconds;
            }
        }

        public void StopAll()
        {
            Stop(Define.Sound.Bgm);
            Stop(Define.Sound.Effect);
        }
    }
}
