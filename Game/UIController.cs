using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject fader;
    [SerializeField] private RectTransform alertPanelRect, losePanelRect, winPanelRect, pausePanelRect;

    [SerializeField] private GameObject[] stars;
    [SerializeField] private Slider soundSlider, musicSlider;

    [SerializeField] private Image _headerProgressSlider;
    [SerializeField] private GameObject[] headerStars;
    private GameObject[] headerStars_enabled = new GameObject[3];
    [Header("Name of enabled star gameobject (change with object name).")] [SerializeField] private string starGameobjectName;
    private bool blockHidingOfMessage = false;
    [Header("Scripts:")] [SerializeField] private GameObject gameControllerScript;

    // --------------------------------------------------------------------------------------------------------------
    public float headerProgressSlider
    {
        get
        {
            return _headerProgressSlider.fillAmount;
        }
        set
        {
            _headerProgressSlider.fillAmount = value;
        }
    }
    private Func<object> func; // if returns 0 - end, 1 - not...

    private void Awake()
    {

    }
    private void Start()
    {
        InitUI();
    }
    private void Update()
    {
       
    }
    public void ButtonClick()
    {
        SoundController.PlaySound(SoundController.SoundType.clickOpen);
        switch (EventSystem.current.currentSelectedGameObject.name)
        {
            case "#QuitButton":
                // TODO
                StartCoroutine(ShowMessage("", false, ShowUI.pause));
                StartCoroutine(GameController.GameSceneFader(() => SceneManager.LoadScene(0), GameController.FaderFunctions.toBlack));
                break;
            case "#ResumeButton":
                StartCoroutine(ShowMessage("", false, ShowUI.pause));
                break;
            case "#PauseButton":
                UpdateSliders();
                StartCoroutine(ShowMessage("", true, ShowUI.pause));
                break;
        }
    }
    private void UpdateSliders()
    {
        if (SoundController.musicActive) musicSlider.value = musicSlider.maxValue;
        else musicSlider.value = musicSlider.minValue;

        if (SoundController.soundActive) soundSlider.value = soundSlider.maxValue;
        else soundSlider.value = soundSlider.minValue;
    }
    public void OnSliderValueChanged()
    {
        var slider = EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
        switch (EventSystem.current.currentSelectedGameObject.name)
        {
            case "#SoundSlider":
                if (slider.value == slider.maxValue)
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.enabled, SoundController.MusicSettings.current);
                else if (slider.value == slider.minValue)
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.disabled, SoundController.MusicSettings.current);
                break;
            case "#MusicSlider":
                if (slider.value == slider.maxValue)
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.current, SoundController.MusicSettings.enabled);
                else if (slider.value == slider.minValue)
                    SoundController.UpdateSoundSettings(SoundController.SoundSettings.current, SoundController.MusicSettings.disabled);
                break;
        }

    }
    public enum ShowUI
    {
        defaultAlert,
        gameWin,
        gameLose,
        pause
    }
    /// <summary>
    /// <para>[EN] (Coroutine) Shows a popup UI with dimming background.</para>
    /// <para>[RU] (Корутина) Показывает всплывающее окно с затемнением фона. </para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="duration"></param>
    /// <param name="ui"></param>
    /// <returns></returns>
    public IEnumerator ShowMessage(string message, int duration, ShowUI ui)
    {
        RectTransform uiToShow = CheckUI(ui);
        uiToShow.Find("#AlertTxt").GetComponent<TextMeshProUGUI>().text = message;
        float yPos = uiToShow.GetComponent<RectTransform>().localPosition.y, target = 0, scalePercent = 0.85f;
        StartCoroutine(GameController.GameSceneFader(null, GameController.FaderFunctions.imperfectBlack));
        int res = 1;
        while (res != 0)
        {
            if (yPos > target) yPos = Mathf.Lerp(yPos, target, scalePercent * Time.deltaTime * 10);
            else if (yPos == target || yPos < target) yPos = Mathf.Lerp(yPos, target - 1503, scalePercent * Time.deltaTime * 10);
            uiToShow.localPosition = new Vector3(uiToShow.localPosition.x, yPos, 0);
            if (yPos < target + 1 && yPos > target)
            {
                uiToShow.localPosition = new Vector3(uiToShow.localPosition.x, target, 0);
                yPos = 0;
                yield return new WaitForSeconds(duration);
            }
            else if (yPos < target - 1500)
            {
                uiToShow.localPosition = new Vector3(uiToShow.localPosition.x, target + 1500, 0);
                StartCoroutine(GameController.GameSceneFader(null, GameController.FaderFunctions.fromImperfectBlack));
                res = 0;
            }
            yield return new WaitForEndOfFrame();
        }
    }
    /// <summary>
    /// <para>[EN] (Coroutine) Shows a popup UI with dimming background.</para>
    /// <para>[RU] (Корутина) Показывает всплывающее окно с затемнением фона. </para>
    /// </summary>
    /// <param name="message"></param>
    /// <param name="show"></param>
    /// <param name="ui"></param>
    /// <param name="p">Stars count for 'gameWin' UI</param>
    /// <returns></returns>
    public IEnumerator ShowMessage(string message, bool show, ShowUI ui, params object[] p)
    {
        RectTransform uiToShow = CheckUI(ui);
        if (blockHidingOfMessage) yield return new WaitWhile(() => blockHidingOfMessage);
        if((show && !fader.activeSelf) || (!show && fader.activeSelf))
        {
            float yPos = uiToShow.GetComponent<RectTransform>().localPosition.y, target = 0, scalePercent = 0.85f;
            if (show)
            {
                try { uiToShow.Find("#AlertTxt").GetComponent<TextMeshProUGUI>().text = message; }
                catch { }
                StartCoroutine(GameController.GameSceneFader(null, GameController.FaderFunctions.imperfectBlack));
                target = 0;
                StartCoroutine(BlockVariable(1));
            }
            else target -= 1500;
            int res = 1;
            while (res != 0)
            {

                yPos = Mathf.Lerp(yPos, target, scalePercent * Time.deltaTime * 10);
                uiToShow.localPosition = new Vector3(uiToShow.localPosition.x, yPos, 0);
                if (yPos < target + 1.5f && yPos > target - 1.5f)
                {
                    uiToShow.localPosition = new Vector3(uiToShow.localPosition.x, target, 0);
                    if(!show) StartCoroutine(GameController.GameSceneFader(null, GameController.FaderFunctions.fromImperfectBlack));
                    if (show && ui == ShowUI.gameWin) StartCoroutine(StarsShow((int)p[0]));
                    res = 0;
                }
                yield return new WaitForEndOfFrame();
            }
        }
    }
    public IEnumerator StarsShow(int starCount)
    {
        for(int i = 0; i < starCount; i++)
        {
            stars[i].gameObject.SetActive(true);
            SoundController.PlaySound(SoundController.SoundType.clickOpen);
            yield return new WaitForSeconds(0.5f);
        }
    }
    private RectTransform CheckUI(ShowUI ui)
    {
        switch(ui)
        {
            case ShowUI.defaultAlert: return alertPanelRect; 
            case ShowUI.gameWin: return winPanelRect;
            case ShowUI.gameLose: return losePanelRect;
            case ShowUI.pause: return pausePanelRect;
        }
        throw new Exception("[Sweet Boom Editor] Undefined ui");
    }
    private IEnumerator BlockVariable(int duration)
    {
        blockHidingOfMessage = true;
        yield return new WaitForSeconds(duration);
        blockHidingOfMessage = false;
    }

    private void InitUI()
    {
        for (int i = 0; i < headerStars.Length; i++) headerStars_enabled[i] = headerStars[i].transform.Find(starGameobjectName).gameObject;
        headerProgressSlider = 0;
    }
}
