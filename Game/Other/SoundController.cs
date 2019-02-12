using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundController : MonoBehaviour {

    public AudioSource clickOpen_, clickClose_, background_, stone_, icecrush_, candycrush_;

    public static AudioSource clickOpen;
    public static AudioSource clickClose;
    public static AudioSource background;
    public static AudioSource stone;
    public static AudioSource icecrush;
    public static AudioSource candycrush;
    
    public static SoundController soundScript;
    public static bool soundActive { get; private set; }
    private static bool _musicActive;
    public static bool musicActive
    {
        get { return _musicActive; }
        private set
        {
            _musicActive = value;
            if (value) { if (background != null && !background.isPlaying) background.Play(); }
            else { if (background != null && background.isPlaying) background.Stop(); }
        }
    }
    private static bool isInited = false;

    private void Awake()
    {
        if(!isInited)
        {
            if (PlayerPrefs.GetString("sound") == "") PlayerPrefs.SetString("sound", "en");
            if (PlayerPrefs.GetString("music") == "") PlayerPrefs.SetString("music", "en");

            clickOpen = clickOpen_;
            clickClose = clickClose_;
            background = background_;
            stone = stone_;
            icecrush = icecrush_;
            candycrush = candycrush_;
            

            if (PlayerPrefs.GetString("sound") == "en") soundActive = true;
            else soundActive = false;

            if (PlayerPrefs.GetString("music") == "en") musicActive = true;
            else musicActive = false;

            isInited = true;
        }
    }
    
    public static void UpdateSoundSettings(SoundSettings set, MusicSettings set2)
    {
        if(set != SoundSettings.current)
        {
            if (set == SoundSettings.enabled) { PlayerPrefs.SetString("sound", "en"); soundActive = true; }
            else if (set == SoundSettings.disabled) { PlayerPrefs.SetString("sound", "dis"); soundActive = false; }
        }
        if (set2 != MusicSettings.current)
        {
            if (set2 == MusicSettings.enabled) { PlayerPrefs.SetString("music", "en"); musicActive = true; }
            else if (set2 == MusicSettings.disabled) { PlayerPrefs.SetString("music", "dis"); musicActive = false; }
        }
    }
    public static void PlaySound(SoundType type)
    {
        if(soundActive)
        {
            switch (type)
            {
                case SoundType.candyCrush:
                    candycrush.Play();
                    break;
                case SoundType.clickClose:
                    clickClose.Play();
                    break;
                case SoundType.clickOpen:
                    clickOpen.Play();
                    break;
                case SoundType.iceCrush:
                    icecrush.Play();
                    break;
                case SoundType.stoneCrush:
                    stone.Play();
                    break;
            }
        }
    }
    public enum SoundSettings
    {
        enabled,
        disabled, 
        current
    }
    public enum MusicSettings
    {
        enabled,
        disabled,
        current
    }
    public enum SoundType
    {
        clickOpen,
        clickClose,
        stoneCrush,
        iceCrush,
        candyCrush
    }
}
