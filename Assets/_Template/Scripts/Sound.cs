using UnityEngine.Audio;
using UnityEngine;
using System;

public enum TypeOfSound
{
    Music = 1,
    SFX = 2,

#if GAME_SING
    SingPlayerMusic = 100,
    SingOthersMusic = 101,
#endif
}

[Serializable]
public class Sound 
{
    public string name;
    public TypeOfSound typeOfSound;
    public AudioClip clip;

    [Range(0f,1f)]
    public float volume;
    [Range(.1f, 3f)]
    public float pitch;
    [Range(0f, 3f)]
    public float PitchDeviation = 0.1f;
    public bool loop;
    public bool isPlayOnAwake;

    [Range(1, 6)]
    public int AudioSourcesCount = 1;

    [HideInInspector]
    public AudioSource[] Sources;
}
