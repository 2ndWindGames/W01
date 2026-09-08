using System.Collections.Generic;
using UnityEngine;

namespace _01.Scripts.Manager
{
    public class SoundManager
    {
		private const string ResourcesRoot = "Sounds/";
        private readonly AudioSource[] m_AudioSources = new AudioSource[(int)Define.Sound.Max];
        private readonly Dictionary<string, AudioClip> m_AudioClips = new Dictionary<string, AudioClip>();

        private GameObject m_SoundRoot = null;

        public void Init()
        {
			if (m_SoundRoot == null)
			{
				m_SoundRoot = GameObject.Find("@SoundRoot");
				if (m_SoundRoot == null)
					m_SoundRoot = new GameObject { name = "@SoundRoot" };

				Object.DontDestroyOnLoad(m_SoundRoot);
			}

			string[] soundTypeNames = System.Enum.GetNames(typeof(Define.Sound));
			for (int count = 0; count < soundTypeNames.Length - 1; count++)
			{
				Transform child = m_SoundRoot.transform.Find(soundTypeNames[count]);
				GameObject go = child != null ? child.gameObject : new GameObject(soundTypeNames[count]);
				go.transform.SetParent(m_SoundRoot.transform, false);
				m_AudioSources[count] = go.GetComponent<AudioSource>();
				if (m_AudioSources[count] == null)
					m_AudioSources[count] = go.AddComponent<AudioSource>();
				m_AudioSources[count].playOnAwake = false;
			}

			m_AudioSources[(int)Define.Sound.Bgm].loop = true;
	    }

        public void Clear()
        {
            foreach (var audioSource in m_AudioSources)
				if (audioSource != null)
					audioSource.Stop();
            m_AudioClips.Clear();
        }

        public void SetPitch(Define.Sound type, float pitch = 1.0f)
	    {
		    var audioSource = m_AudioSources[(int)type];
            if (audioSource == null)
                return;

            audioSource.pitch = pitch;
	    }

        public bool Play(Define.Sound type, string path, float volume = 1.0f, float pitch = 1.0f)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            AudioSource audioSource = m_AudioSources[(int)type];
			if (audioSource == null)
			{
				Debug.LogError($"SoundManager AudioSource가 초기화되지 않았습니다: {type}");
				return false;
			}

			if (path.StartsWith("Sound/"))
				path = ResourcesRoot + path.Substring("Sound/".Length);
			else if (!path.StartsWith(ResourcesRoot))
				path = ResourcesRoot + path;

            audioSource.volume = volume;

            switch (type)
            {
                case Define.Sound.Bgm:
                {
                    AudioClip audioClip = Managers.Resource.Load<AudioClip>(path);
                    if (audioClip == null)
					{
						Debug.LogError($"BGM을 찾을 수 없습니다: Resources/{path}");
                        return false;
					}

                    if (audioSource.isPlaying)
                        audioSource.Stop();

                    audioSource.clip = audioClip;
                    audioSource.pitch = pitch;
                    audioSource.Play();
                    return true;
                }
                case Define.Sound.Effect:
                {
                    AudioClip audioClip = GetAudioClip(path);
                    if (audioClip == null)
					{
						Debug.LogError($"효과음을 찾을 수 없습니다: Resources/{path}");
                        return false;
					}

                    audioSource.pitch = pitch;
                    audioSource.PlayOneShot(audioClip);
                    return true;
                }
                case Define.Sound.Max:
                default:
                {
                    // if (type == Define.Sound.Speech)
                    // {
                    //     AudioClip audioClip = GetAudioClip(path);
                    //     if (audioClip == null)
                    //         return false;
                    //
                    //     if (audioSource.isPlaying)
                    //         audioSource.Stop();
                    //
                    //     audioSource.clip = audioClip;
                    //     audioSource.pitch = pitch;
                    //     audioSource.Play();
                    //     return true;
                    // }

                    break;
                }
            }

            return false;
        }

        public void Stop(Define.Sound type)
	    {
            AudioSource audioSource = m_AudioSources[(int)type];
            audioSource.Stop();
        }

	    public float GetAudioClipLength(string path)
        {
            AudioClip audioClip = GetAudioClip(path);
            return audioClip == null ? 0.0f : audioClip.length;
        }

        private AudioClip GetAudioClip(string path)
        {
            if (m_AudioClips.TryGetValue(path, out var audioClip))
                return audioClip;

			audioClip = Managers.Resource.Load<AudioClip>(path);
			if (audioClip != null)
				m_AudioClips.Add(path, audioClip);
            return audioClip;
        }
    }
}

