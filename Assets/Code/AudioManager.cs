using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class AudioEvent : EventArgs
{
	public AudioEvent(string key, Vector3 pos, float volume = 1f)
	{
		Key = key;
		Pos = pos;
		Volume = volume;
	}

	public string Key;
	public Vector3 Pos;
	public float Volume;
}

public class AudioManager : MonoBehaviour
{
	[System.Serializable]
	public class AudioEntry
	{
		public string key;
		public List<AudioClip> clips;
	}

	[System.Serializable]
	public class MusicEntry
	{
		public string key;
		public AudioClip clip;
	}

	public void SetMusicPitch(float pitch)
	{
		if (m_musicSource != null)
		{
			m_musicSource.pitch = Mathf.Max(0.001f, pitch);
		}
	}

	public List<AudioEntry> m_audio;
	public List<MusicEntry> m_music;
	private AudioSource m_musicSource;
	private MusicEntry m_playingMusic;
	private MusicEntry m_queuedMusic;
	private bool m_randomizeTime;
	private float m_maxVolume;

	void Awake()
	{
		var go = this.gameObject;
		m_musicSource = go.AddComponent<AudioSource>();
		m_musicSource.spatialize = false;
		m_musicSource.loop = true;
		StartCoroutine(MusicRoutine());
	}

	void Start()
	{
		EventManager.Instance.RegisterEvent<AudioEvent>(OnAudio);
	}

	void OnDestroy()
	{
		EventManager.Instance.UnregisterEvent<AudioEvent>(OnAudio);
	}

	void OnAudio(AudioEvent args)
	{
		var audio = m_audio.FirstOrDefault(a => a.key == args.Key);
		if (audio != null)
		{
			var go = new GameObject();
			var src =go.AddComponent<AudioSource>();
			var dst = go.AddComponent<DestroyInTime>();
			dst.time = 4f;
			src.PlayOneShot(audio.clips[UnityEngine.Random.Range(0, audio.clips.Count)], args.Volume);
			go.transform.position = args.Pos;
		}
	}

	[ContextMenu("SDFG")]
	void test()
	{
		EventManager.Instance.SendEvent(new AudioEvent("hit", Vector3.zero));
	}

	public void PlayMusic(string name, bool randomizeTime, float maxVolume = 1.0f)
	{
		if (m_playingMusic != null && m_playingMusic.key == name)
		{
			return;
		}

		var music = m_music.FirstOrDefault(a => a.key == name);
		if (music != null)
		{
			m_queuedMusic = music;
			m_randomizeTime = randomizeTime;
			m_maxVolume = maxVolume;
		}
	}

	IEnumerator MusicRoutine()
	{
		while (true)
		{
			// Wait for queued music
			while (m_queuedMusic == null)
				yield return null;

			// Fade out
			if (m_playingMusic != null)
			{
				while (m_musicSource.volume > 0)
				{
					m_musicSource.volume -= Time.deltaTime*2;
					yield return null;
				}
				m_musicSource.Stop();
			}

			// Switch music
			m_musicSource.clip = m_queuedMusic.clip;
			if (m_randomizeTime)
			{
				m_musicSource.time = UnityEngine.Random.Range(0, m_musicSource.clip.length);
			}
			m_musicSource.Play();
			m_playingMusic = m_queuedMusic;
			m_queuedMusic = null;

			// Fade in
			while (m_musicSource.volume < m_maxVolume)
			{
				m_musicSource.volume += Time.deltaTime*2;
				yield return null;
			}
			m_musicSource.volume = m_maxVolume;
		}
	}
}
