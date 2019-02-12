using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.IO;


public class LevelManager : MonoBehaviour {

    public static string saveFolderName = "/gamedata.txt";
    [Header("Do not touch this fields")]
    GameObject levelField;
    private static GameObject[] levels;
    public GameObject[] star;
    public GameObject levelInfo;
    public TextMeshProUGUI levelNumTxt;
    [SerializeField]
    private GameObject levelInfoAnim;
    public static Save.GameData gameData; // MAIN GAME DATA
    public static Save.ConfigurationSettings configuration;
    static bool isInit;
    static LevelManager lvlManagerScript;

    private Menu.FaderMethods function;
    private static int curLevel;

    private void Awake()
    {
        gameData = Save.InitGameData();
    }

    private void Start()
    {
        
    }

    public static void UpdateLevels()
    {
        try
        {
            levels = GameObject.FindGameObjectsWithTag("Level");
            for (int i = 0; i < levels.Length; i++)
            {
                levels[i].transform.Find("LevelNumber").GetComponent<TextMeshProUGUI>().text =
                    gameData.levels[i].levelNum.ToString();
                levels[i].GetComponent<LevelController>().levelID = gameData.levels[i].levelNum;
                levels[i].transform.Find("stars").gameObject.SetActive(true);
            }
        }
        catch(Exception e)
        {
            Debug.LogError("[Sweet Boom Editor] Game data is damaged. Can't load levels.");
        }
        // Set star progress ...
    }

    public void LevelSelected(int levelNum)
    {
        curLevel = levelNum;
        SoundController.PlaySound(SoundController.SoundType.clickOpen);
        levelInfo.gameObject.SetActive(true);
        foreach (GameObject lvl in levels) lvl.gameObject.GetComponent<BoxCollider2D>().enabled = false;
        levelNumTxt.text = $"Level {levelNum}";
        levelInfoAnim.GetComponent<Animator>().SetTrigger("open");
    }
    public void ButtonClick()
    {
        switch (EventSystem.current.currentSelectedGameObject.name)
        {
            case "#exit":
                levelInfo.gameObject.SetActive(false);
                foreach (GameObject lvl in levels) lvl.gameObject.GetComponent<BoxCollider2D>().enabled = true;
                SoundController.PlaySound(SoundController.SoundType.clickClose);
                break;
            case "#play_btn":
                break;
        }

    }
    public void Play()
    {
        SoundController.PlaySound(SoundController.SoundType.clickOpen);
        function = () =>
        {
            SceneConnect.OpenScene(curLevel);
            SceneManager.LoadScene(1);
        };
        StartCoroutine(Menu.Fader(function, Menu.FaderFunctions.toBlack));
    }
}
