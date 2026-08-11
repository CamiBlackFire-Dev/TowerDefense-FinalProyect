using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Volumes")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSavedVolumes();

        CreateAudioSources();
    }

    private void LoadSavedVolumes()
    {
        if (PlayerPrefs.HasKey("MasterVolume"))
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (PlayerPrefs.HasKey("MusicVolume"))
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (PlayerPrefs.HasKey("SFXVolume"))
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    private void CreateAudioSources()
    {
        GameObject musicObject = new GameObject("MusicSource");
        musicObject.transform.SetParent(transform);
        musicSource = musicObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        UpdateMusicVolume();

        GameObject sfxObject = new GameObject("SFXSource");
        sfxObject.transform.SetParent(transform);
        sfxSource = sfxObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        UpdateSFXVolume();
    }

    private void UpdateMusicVolume()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;
    }

    private void UpdateSFXVolume()
    {
        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;

        musicSource.Stop();
        musicSource.clip = clip;
        UpdateMusicVolume();
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip, bool randomizePitch = false)
    {
        if (clip == null) return;

        if (randomizePitch)
        {
            sfxSource.pitch = Random.Range(0.9f, 1.1f);
        }
        else
        {
            sfxSource.pitch = 1f;
        }

        sfxSource.PlayOneShot(clip, sfxSource.volume);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
        UpdateSFXVolume();
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateMusicVolume();
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateSFXVolume();
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateMusicVolume();
            UpdateSFXVolume();
        }
    }
}