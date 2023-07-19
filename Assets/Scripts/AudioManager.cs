using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _instance;

    public SoundClip[] soundClips;

    public static AudioManager getInstance()
    {
        return _instance ? _instance : null;
    }

    void Awake()
    {
        _instance = this;
    }

    public AudioSource GetSource(string name)
    {
        SoundClip clip = Array.Find(soundClips, (i) => i.name == name);
        return clip != null ? clip.source : null;
    }
}

[Serializable]
public class SoundClip
{
    public string name;
    public AudioSource source;
}
