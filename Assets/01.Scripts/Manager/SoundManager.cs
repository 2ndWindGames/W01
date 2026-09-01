using System.Collections.Generic;
using UnityEngine;

namespace _01.Scripts.Manager
{
    public class SoundManager
    {
        private readonly AudioSource[] m_AudioSources = new AudioSource[(int)Define.Sound.Max];
        private readonly Dictionary<string, AudioClip> m_AudioClips = new Dictionary<string, AudioClip>();

        private GameObject m_SoundRoot = null;

        public void Init()
        {
		    if (m_SoundRoot == null)
		    {
			    m_SoundRoot = GameObject.Find("@SoundRoot");
			    if (m_SoundRoot == null)
			    {
				    m_SoundRoot = new GameObject { name = "@SoundRoot" };
				    Object.DontDestroyOnLoad(m_SoundRoot);

				    string[] soundTypeNames = System.Enum.GetNames(typeof(Define.Sound));
				    for (int count = 0; count < soundTypeNames.Length - 1; count++)
				    {
					    GameObject go = new GameObject { name = soundTypeNames[count] };
					    m_AudioSources[count] = go.AddComponent<AudioSource>();
					    go.transform.parent = m_SoundRoot.transform;
				    }

				    m_AudioSources[(int)Define.Sound.Bgm].loop = true;
			    }
		    }
	    }

        public void Clear()
        {
            foreach (var audioSource in m_AudioSources)
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
            if (!path.Contains("Sound/"))
                path = $"Sound/{path}";

            audioSource.volume = volume;

            switch (type)
            {
                case Define.Sound.Bgm:
                {
                    AudioClip audioClip = Managers.Resource.Load<AudioClip>(path);
                    if (audioClip == null)
                        return false;

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
                        return false;

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
            m_AudioClips.Add(path, audioClip);
            return audioClip;
        }
    }
}

