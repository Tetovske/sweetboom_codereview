using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;
/// <summary>
/// <para>[EN] Class with save functions and some survice functions.</para>
/// <para>[RU] Класс предоставляющий функции сохранения, загрузки всех игровых данных, а также некоторые служебные функции.</para>
/// </summary>
public static class Save {
    
    public static GameSave data; // player progress
    public static GameData gameData;// levels data
    public static ConfigurationSettings configuration;
    static bool d = true;
    public static SoundController sound;
    private static string savePath = "/save.txt", gameDataPath = "/gamedata.txt";
#if UNITY_EDITOR || UNITY_WINDOWS
    private static string fullGameInfoFilePath = $"{Application.streamingAssetsPath}{gameDataPath}";
#endif
#if UNITY_ANDROID
    //private static string fullGameInfoFilePath = $"{Application.streamingAssetsPath}{gameDataPath}";
#endif
    /// <summary>
    /// [EN] Main initialization.
    /// [RU] Главная инициализация.
    /// </summary>
    public static void Init() 
    {
        
    }

    public static GameData InitGameData()
    {
        
        try
        {
#if UNITY_EDITOR
            if (!File.Exists($"{fullGameInfoFilePath}")) Debug.Log("Open Dudle/Level Editor " +
                "to start making levels.");
#endif
            if(d)
            {
                string fileData = "";
#if UNITY_EDITOR
                string[] fileDataAr = File.ReadAllLines(fullGameInfoFilePath);
                foreach (var line in fileDataAr) fileData += line;
#elif UNITY_ANDROID
                Debug.Log("Android");
                string path = "jar:file://" + Application.dataPath + "!/assets/gamedata.txt";
                WWW reader = new WWW(path);
                while (!reader.isDone) { }
                fileData = reader.text;
#endif
                gameData = JsonUtility.FromJson<GameData>(fileData);
                configuration = gameData.settings;

                if (configuration.sortingLevelsInMenu) // Level sorting
                {
                    for (int i = 0; i < gameData.levels.Count - 1; i++)
                    {
                        int min = int.MaxValue;
                        Level minLvl = new Level();
                        for (int j = i; j < gameData.levels.Count; j++)
                        {
                            if (gameData.levels[j].levelNum < min)
                            {
                                min = gameData.levels[j].levelNum;
                                Level l = gameData.levels[i];
                                gameData.levels[i] = gameData.levels[j];
                                gameData.levels[j] = l;
                            }
                        }
                    }
                }
                return gameData;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Sweet Boom Editor] Game data is damaged. Error type: " + e.ToString());
            configuration = null;
            return null;
        }
        return null;
    }
    /*
    public static GameSave Initialize() // Init game data and game save
    {
        if (!File.Exists(fullGameInfoFilePath))
        {
            File.Create(fullGameInfoFilePath);
        }
        else
        {

        }
        return null;
    } */
    public static bool SaveGame()
    {

        
        return false;
    }
    /// <summary>
    /// <para>[EN] Return a random int number between 'from' [inclusive] and 'to' [inclusive]</para>
    /// <para>[RU] Возвращает случаное значение типа int в заданном промежутке (включая передаваемые параметры)</para>
    /// </summary>
    /// <param name="from">beginning of range</param>
    /// <param name="to">end of range</param>
    /// <returns></returns>
    public static int Randomizer(int from, int to) // Randomizer ('from' and 'to' - inclusive)
    {
        float dist = (float)100 / (float)((to - from) + 1);
        while(true)
        {
            int result = UnityEngine.Random.Range(1, 101);
            for (int i = 0; i < ((to - from) + 1); i++) if (result > i * dist && result <= (i + 1) * dist) return from + i;
        }
    }
    [Serializable]
    public class GameSave
    {
        public List<SaveSlot> gameData;
        public DateTime saveTime;
        public int coins;
        public bool isAdvertising;

        public GameSave(List<SaveSlot> save, DateTime saveTime, int coins, bool ad)
        {
            gameData = save;
            this.saveTime = saveTime;
            this.coins = coins;
            isAdvertising = ad;
        }
        [Serializable]
        public class SaveSlot
        {
            public int LevelNum;
            public bool LevelLock;
            public int Score;

            public SaveSlot(int num, bool lvlLock)
            {
                LevelNum = num;
                LevelLock = lvlLock;
            }
            public SaveSlot(int num, bool lvlLock, int score) : this(num, lvlLock)
            {
                this.Score = score;
            }
        }
    }
    public enum BlockID
    {
        nil = 0,
        empty = 1,
        candy = 2,
        ice = 3,
        block = 4
    }
    public enum CandyType
    {
        red,
        green,
        blue,
        yellow,
        orange, 
        purple,
        multi,
        nil = -1
    }
    [Serializable]
    public class GameData // All game data from SweetBoom editor
    {
        public List<Level> levels;
        public int levelCount;
        public ConfigurationSettings settings;
    }

    [Serializable]
    public class Level // Levels class for save added levels by developer
    {
        public int levelNum, fieldWidth, fieldHeight;
        public string comment;
        public Vector3Int[] savedBlock;
        public int[] target;
        public Color[] levelColors;
    }
    [System.Serializable]
    public class ConfigurationSettings
    {
        public bool sortingLevelsInMenu, randomizePositionOfIcons, fps;
        public float distance, size;
    }
}
