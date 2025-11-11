using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using MirraGames.SDK;

public class SoundController : Singleton<SoundController>
{
    [SerializeField] private Sound[] sounds;

#if GAME_SING
    [SerializeField] private List<AudioSource> m_OthersMusic = new();
    [SerializeField] private AudioSource m_MyMusic;

    [SerializeField] private float m_OthersMusicVolume = 0.25f;
    [SerializeField] private float m_MyMusicVolume = 0.25f;
    private AudioClip m_MutedOthersClip;
    private bool m_OthersMuted = false;
#endif

    private float m_MasterVolume = 1f;
    [SerializeField] private Slider m_MasterVolumeSlider;

    private List<string> m_SoundMuters = new();

    public void OnSliderValueChanged()
    {
        m_MasterVolume = m_MasterVolumeSlider.value;

        if (m_SoundMuters.Count <= 0)
        {
            MirraSDK.Audio.Volume = m_MasterVolume;
        }
    }

    private void Awake()
    {
#if GAME_SING
        Assert.IsTrue(m_OthersMusic.Count > 0);
        Assert.IsNotNull(m_MyMusic);
        TogglePlayerMusic(true);
        ToggleOthersMusic(true);
#endif

        Assert.IsNotNull(m_MasterVolumeSlider);
        transform.SetParent(null);

        foreach (Sound s in sounds)
        {
            s.Sources = new AudioSource[s.AudioSourcesCount];    

            for (int i = 0; i < s.AudioSourcesCount; ++i)
            {
                AudioSource Source = gameObject.AddComponent<AudioSource>();

                s.Sources[i] = Source;

                Source.clip = s.clip;
                Source.volume = s.volume;
                Source.pitch = s.pitch;
                Source.loop = s.loop;
                Source.playOnAwake = s.isPlayOnAwake;
            }
        }

        Play("Background");
    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            return;
        }

        AudioSource BestSource = null;
        float BestSourcePosition = float.MaxValue;

        for (int i = 0; i < s.Sources.Length; ++i)
        {
            AudioSource Source = s.Sources[i];

            if (!Source.isPlaying)
            {
                BestSource = Source;
                break;
            }

            if (Source.time < BestSourcePosition)
            {
                BestSource = Source;
                BestSourcePosition = Source.time;
            }
        }

        if (BestSource != null)
        {
            BestSource.pitch = Mathf.Clamp(s.pitch + UnityEngine.Random.Range(-s.PitchDeviation, s.PitchDeviation), 0.1f, 3f);

            BestSource.Play();
        }
    }

    public Sound GetSound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        return s;
    }

    public void MakeClickSound()
    {
        Play("ButtonClick");
    }

#if false
    public void MuteSpecificOtherMusic(AudioClip Clip)
    {
        if (m_MutedOthersClip != null && m_MutedOthersClip != Clip)
        {
            SetSpecificOtherMusicVolume(m_MutedOthersClip, m_OthersMuted ? 0f : m_OthersMusicVolume);
        }

        m_MutedOthersClip = Clip;

        SetSpecificOtherMusicVolume(Clip, 0f);
    }

    private void SetSpecificOtherMusicVolume(AudioClip Clip, float Volume)
    {
        if (Clip == null)
        {
            return;
        }

        for (int i = 0; i < m_OthersMusic.Count; ++i)
        {
            var Source = m_OthersMusic[i];

            if (Source.clip == Clip)
            {
                Source.volume = Volume;
            }
        }
    }

    private void SetChannelVolume(TypeOfSound Type, float Volume)
    {
        for (int i = 0; i < sounds.Length; ++i)
        {
            var Sound = sounds[i];

            if (Sound.typeOfSound != Type)
            {
                continue;
            }

            for (int n = 0; n < Sound.Sources.Length; ++n)
            {
                Sound.Sources[n].volume = Volume;
            }
        }
    }

    public void TogglePlayerMusic(bool Toggle)
    {
        m_MyMusic.volume = Toggle ? m_MyMusicVolume : 0f;
    }

    public void ToggleOthersMusic(bool Toggle)
    {
        m_OthersMuted = !Toggle;

        float Volume = Toggle ? m_OthersMusicVolume : 0f;

        for (int i = 0; i < m_OthersMusic.Count; ++i)
        {
            if (m_MutedOthersClip == m_OthersMusic[i].clip)
            {
                continue;
            }

            m_OthersMusic[i].volume = Volume;
        }
    }
#endif

    private void OnApplicationFocus(bool focus)
    {       
        Silence(!focus);       
    }

    private void OnApplicationPause(bool pause)
    {
        Silence(pause);
    }

    void Silence(bool silence)
    {
        if (silence)
        {
            MuteGame("app_focus_silence");
        }
        else
        {
            UnmuteGame("app_focus_silence");
        }
    }

    public void MuteGame(string MuteId)
    {
        MirraSDK.Audio.Volume = 0;
        MirraSDK.Audio.Pause = true;

        if (!m_SoundMuters.Contains(MuteId))
        {
            m_SoundMuters.Add(MuteId);
        }
    }

    public void UnmuteGame(string MuteId)
    {
        m_SoundMuters.Remove(MuteId);

        if (m_SoundMuters.Count > 0)
        {
            return;
        }

        MirraSDK.Audio.Volume = Mathf.Clamp01(m_MasterVolume);
        MirraSDK.Audio.Pause = false;
    }
}
