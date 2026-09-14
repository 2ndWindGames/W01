using SWGUnity2DCore.Manager;
using SWGUnity2DCore.Util;
using UnityEngine;

/// <summary>
/// Adds phase-locked rhythm to the gameplay BGM while a combo is active.
/// The original song keeps playing at its normal speed and pitch.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameplayComboMusic : MonoBehaviour
{
    private const string SongPath = "Sounds/BGM/Gameplay_NeonRush";
    private const string DrivePath = "Sounds/BGM/Gameplay_ComboDrive";
    private const string RushPath = "Sounds/BGM/Gameplay_ComboRush";
    private const float FadeSeconds = 0.38f;

    [SerializeField, Range(0f, 1f)] private float driveVolume = 0.23f;
    [SerializeField, Range(0f, 1f)] private float rushVolume = 0.28f;

    private readonly AudioSource[] m_Layers = new AudioSource[2];
    private readonly float[] m_FadeFrom = new float[2];
    private readonly float[] m_FadeTo = new float[2];
    private AudioClip m_SongClip;
    private AudioClip m_DriveClip;
    private AudioClip m_RushClip;
    private AudioSource m_SongSource;
    private float m_FadeElapsed = FadeSeconds;
    private int m_Tier;

    public int GameplayComboTier => m_Tier;

    private void Awake()
    {
        m_SongClip = Resources.Load<AudioClip>(SongPath);
        m_DriveClip = Resources.Load<AudioClip>(DrivePath);
        m_RushClip = Resources.Load<AudioClip>(RushPath);
        if (m_SongClip == null || m_DriveClip == null || m_RushClip == null)
        {
            Debug.LogError("Gameplay combo music clips are missing from Resources/Sounds/BGM.", this);
            enabled = false;
            return;
        }

        m_Layers[0] = CreateLayer("Combo Drive", m_DriveClip);
        m_Layers[1] = CreateLayer("Combo Rush", m_RushClip);
    }

    private AudioSource CreateLayer(string name, AudioClip clip)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.priority = 33;
        source.loop = true;
        source.clip = clip;
        source.volume = 0f;
        return source;
    }

    /// <summary>0 = original song; 1 = 10-combo drive; 2 = 50-combo rush.</summary>
    public void SetTier(int tier)
    {
        tier = Mathf.Clamp(tier, 0, 2);
        if (m_Tier == tier && (tier == 0 || m_SongSource != null)) return;
        m_Tier = tier;
        SetFadeTargets();
        if (tier > 0) FindSongAndSync();
    }

    private void LateUpdate()
    {
        if (m_Layers[0] == null) return;

        // There is no need to search the sound root throughout the lobby.
        if (m_Tier == 0 && m_SongSource == null) return;

        if (m_SongSource == null || !m_SongSource.isPlaying || m_SongSource.clip != m_SongClip)
        {
            m_SongSource = null;
            if (m_Tier > 0) FindSongAndSync();
            if (m_SongSource == null)
            {
                StopLayers();
                return;
            }
        }

        if (m_Tier == 0 && AnotherBgmIsPlaying())
        {
            // The old gameplay song may still be fading out. Its combo layer
            // should not spill into the lobby or result music.
            StopLayers();
            m_SongSource = null;
            return;
        }

        bool muted = Managers.Sound.IsMuted(Define.Sound.Bgm);
        foreach (AudioSource layer in m_Layers) layer.mute = muted;
        if (m_FadeElapsed >= FadeSeconds) return;
        m_FadeElapsed = Mathf.Min(FadeSeconds, m_FadeElapsed + Time.unscaledDeltaTime);
        float t = Mathf.SmoothStep(0f, 1f, m_FadeElapsed / FadeSeconds);
        for (int i = 0; i < m_Layers.Length; i++)
            m_Layers[i].volume = Mathf.Lerp(m_FadeFrom[i], m_FadeTo[i], t);
        if (m_Tier == 0 && m_FadeElapsed >= FadeSeconds)
        {
            StopLayers();
            m_SongSource = null;
        }
    }

    private void FindSongAndSync()
    {
        if (m_SongSource != null || m_SongClip == null) return;
        GameObject soundRoot = GameObject.Find("@SoundRoot");
        if (soundRoot == null) return;
        foreach (AudioSource source in soundRoot.GetComponentsInChildren<AudioSource>())
        {
            if (source.clip != m_SongClip || !source.isPlaying) continue;
            m_SongSource = source;
            double phase = source.clip.samples > 0
                ? source.timeSamples / (double)source.clip.samples
                : 0d;
            foreach (AudioSource layer in m_Layers)
            {
                layer.Stop();
                layer.timeSamples = Mathf.Clamp((int)(phase * layer.clip.samples), 0, layer.clip.samples - 1);
                layer.volume = 0f;
                layer.mute = Managers.Sound.IsMuted(Define.Sound.Bgm);
                layer.Play();
            }
            SetFadeTargets();
            return;
        }
    }

    private bool AnotherBgmIsPlaying()
    {
        GameObject soundRoot = GameObject.Find("@SoundRoot");
        if (soundRoot == null) return false;
        foreach (AudioSource source in soundRoot.GetComponentsInChildren<AudioSource>())
            if (source.name.StartsWith("Bgm") && source.clip != m_SongClip && source.isPlaying)
                return true;
        return false;
    }

    private void SetFadeTargets()
    {
        for (int i = 0; i < m_Layers.Length; i++)
        {
            m_FadeFrom[i] = m_Layers[i] != null ? m_Layers[i].volume : 0f;
            m_FadeTo[i] = m_Tier == i + 1
                ? (i == 0 ? driveVolume : rushVolume)
                : 0f;
        }
        m_FadeElapsed = 0f;
    }

    private void StopLayers()
    {
        foreach (AudioSource layer in m_Layers)
        {
            layer.Stop();
            layer.volume = 0f;
        }
    }

    private void OnDisable()
    {
        if (m_Layers[0] != null) StopLayers();
    }
}
