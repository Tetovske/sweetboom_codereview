using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class Menu : MonoBehaviour {

    [Header("Choose amount of sectors")]
    [SerializeField]
    private Levels[] levelsZones;

    public GameObject settingsPanel, fader;
    private static GameObject faderS;
    [SerializeField]
    private Canvas menu, levels;
    [HideInInspector]
    public delegate void FaderMethods();
    FaderMethods function;
    [HideInInspector]
    public event Action gameStarted;
    [SerializeField]
    private Animator settingsPopup;
    public TextMeshProUGUI fpsCountertxt;

    private Save.GameData gameData;

    private SoundController sound;

    [SerializeField] private Slider soundSlider, musicslider;
    private static bool firstOpen = true;

    private void Awake()
    {
        if (firstOpen) firstOpen = false;
        else
        {
            menu.gameObject.SetActive(false);
            levels.gameObject.SetActive(true);
        }
        try
        {
            gameData = LevelManager.gameData;
            if (gameData != null) if (gameData.settings.fps) fpsCountertxt.gameObject.SetActive(true);
        }
        catch { }
    }
    private void Start()
    {
        faderS = fader;
        StartCoroutine(Fader(null, FaderFunctions.fromBlack));
    }
    private void Update()
    {
        
    }
    private void FixedUpdate()
    {
        if(Save.configuration != null) if(Save.configuration.fps) fpsCountertxt.text = $"fps: {(int)(1 / Time.deltaTime)}";
    }
    private void SettingsInit()
    {
        if (SoundController.musicActive) musicslider.value = musicslider.maxValue;
        else musicslider.value = musicslider.minValue;

        if (SoundController.soundActive) soundSlider.value = soundSlider.maxValue;
        else soundSlider.value = soundSlider.minValue;
    }
    public void ButtonClicked()
    {
        
        switch(EventSystem.current.currentSelectedGameObject.name)
        {
            case "#ok_btn":
                settingsPanel.gameObject.SetActive(false);
                SoundController.PlaySound(SoundController.SoundType.clickOpen);
                break;

            case "#play_btn":
                SoundController.PlaySound(SoundController.SoundType.clickOpen);
                function = () =>
                {
                    menu.gameObject.SetActive(false);
                    levels.gameObject.SetActive(true);
                    LevelManager.UpdateLevels();
                };
                StartCoroutine(Fader(function, FaderFunctions.toBlackAndBack));
                break;

            case "#settings_btn":
                settingsPanel.gameObject.SetActive(true);
                SoundController.PlaySound(SoundController.SoundType.clickOpen);
                settingsPopup.SetTrigger("open");
                SettingsInit();
                break;

            case "#close_btn":
                settingsPanel.gameObject.SetActive(false);
                SoundController.PlaySound(SoundController.SoundType.clickClose);
                break;

            case "#rate_btn":
                SoundController.PlaySound(SoundController.SoundType.clickOpen);
                break;

            case "#backmenu_btn":
                SoundController.PlaySound(SoundController.SoundType.clickClose);
                function = () =>
                {
                    menu.gameObject.SetActive(true);
                    levels.gameObject.SetActive(false);
                };
                StartCoroutine(Fader(function, FaderFunctions.toBlackAndBack));
                break;
        }
    }
    // MENU
    public void GameStart()
    {
        
    }
    void GameBegins()
    {
        gameStarted();
    }
    public enum FaderFunctions
    {
        toBlackAndBack = 1,
        toBlack = 2,
        fromBlack = 3
    }

    public static IEnumerator Fader(FaderMethods function, FaderFunctions functionIndex) // 
    {
        switch(functionIndex)
        {
            case FaderFunctions.toBlackAndBack:
            faderS.gameObject.SetActive(true);
            while (faderS.GetComponent<Image>().color.a < 1)
            {
                faderS.GetComponent<Image>().color = new Color(0, 0, 0, faderS.GetComponent<Image>().color.a + 0.05f);
                yield return new WaitForSeconds(0.0001f * Time.deltaTime);
            }
            function?.Invoke();
            while (faderS.GetComponent<Image>().color.a > 0)
            {
                faderS.GetComponent<Image>().color = new Color(0, 0, 0, faderS.GetComponent<Image>().color.a - 0.05f);
                yield return new WaitForSeconds(0.0001f * Time.deltaTime);
            }
            faderS.gameObject.SetActive(false);
                yield return 0;
                break;
            case FaderFunctions.toBlack:
                faderS.gameObject.SetActive(true);
                faderS.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                while (faderS.GetComponent<Image>().color.a < 1)
                {
                    faderS.GetComponent<Image>().color = new Color(0, 0, 0, faderS.GetComponent<Image>().color.a + 0.05f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                function?.Invoke();
                break;
            case FaderFunctions.fromBlack:
                faderS.gameObject.SetActive(true);
                faderS.GetComponent<Image>().color = new Color(0, 0, 0, 1);
                while (faderS.GetComponent<Image>().color.a > 0)
                {
                    faderS.GetComponent<Image>().color = new Color(0, 0, 0, faderS.GetComponent<Image>().color.a - 0.05f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                function?.Invoke();
                faderS.gameObject.SetActive(false);
                break;
        }
        
    }
    
    public void OnSliderChanged() 
    {
        var slider = EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
        switch(EventSystem.current.currentSelectedGameObject.name)
        {
            case "#SoundSlider":
                if (slider.value == slider.maxValue)
                {
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.enabled, SoundController.MusicSettings.current);
                }
                else if (slider.value == slider.minValue)
                {
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.disabled, SoundController.MusicSettings.current);
                }
                break;
            case "#MusicSlider":
                if(slider.value == slider.maxValue)
                {
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.current, SoundController.MusicSettings.enabled);
                }
                else if(slider.value == slider.minValue)
                {
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.current, SoundController.MusicSettings.disabled);
                }
                break;
        }
    }

    [System.Serializable]
    class Levels
    {
        public int amountOfLevels;
    }
    [System.Serializable]
    public class SaveData
    {
        public int levelNum;
        public int amountOfStars;
        public string nameOfLevel;
    }

}

