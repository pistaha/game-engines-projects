using UnityEngine;

public static class VRAudioSettings
{
    public const string MusicVolumeKey = "VR_MUSIC_VOLUME";
    public const string SfxVolumeKey = "VR_SFX_VOLUME";

    public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
    public static float SfxVolume => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

    public static void SaveMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        ApplySceneAudioVolumes();
    }

    public static void SaveSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
        ApplySceneAudioVolumes();
    }

    public static void ApplySceneAudioVolumes()
    {
        AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
                continue;

            string groupName = source.outputAudioMixerGroup != null ? source.outputAudioMixerGroup.name : string.Empty;
            source.volume = groupName.Contains("Music") ? MusicVolume : SfxVolume;
        }
    }
}
