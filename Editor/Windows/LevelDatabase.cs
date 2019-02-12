using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;

public class LevelDatabase : Editor {

    public static GameData data;
    public static string saveFolderName = "/gamedata.txt";

    /// <summary>
    /// Initialize editor
    /// </summary>
    /// <returns></returns>
    public static void Initialization() // Load saved levels from json
    {
        try
        {
            if (!File.Exists($"{Application.streamingAssetsPath}{saveFolderName}"))
            {
                File.Create($"{Application.streamingAssetsPath}{saveFolderName}");
                List<Level> levels = new List<Level>();
                data = new GameData(levels, 0);
                Debug.Log("[Sweet Boom Editor] Gamedata folder created.");
            }
            else
            {
                try
                {
                    string[] fileDataAr = File.ReadAllLines($"{Application.streamingAssetsPath}{saveFolderName}");
                    string fileData = ""; 
                    foreach (var line in fileDataAr) fileData += line;
                    data = JsonUtility.FromJson<GameData>(fileData);
                    if (data == null)
                    {
                        List<Level> levels = new List<Level>();
                        data = new GameData(levels, 0);
                    }
                    else
                    {
                        NewLevelWin.popupLevelID = new int[data.levelCount];
                        NewLevelWin.selectLevelPopupInfo = new string[data.levelCount];
                        for (int i = 0; i < data.levels.Count; i++)
                        {
                            NewLevelWin.selectLevelPopupInfo[i] = $"{data.levels[i].levelNum}: {data.levels[i].comment}";
                            NewLevelWin.popupLevelID[i] = data.levels[i].levelNum;
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.Log(e.Message);
                    EditLevels.ConsoleLog("Initialization error.");
                    data.levelCount = 0;
                }
            }
        }
        catch(Exception e) { Debug.LogError($"Sweet Boom editor initialization failed. {e.Message}"); }
        
    }
    /// <summary>
    /// Returns list of saved levels
    /// </summary>
    /// <param name="lvlNum"></param>
    /// <returns></returns>
    public static Level GetSave(int lvlNum)
    {
        foreach (Level lvl in data.levels) if (lvl.levelNum == lvlNum) { return lvl; }
        return null;
    }
    /// <summary>
    /// Save or replace level by number
    /// </summary>
    /// <param name="saveLevelNum"></param>
    /// <param name="newLevel"></param>
    public static void SaveLevel(int saveLevelNum, Level newLevel)
    {
        bool flag = false;

        if (data != null && data.levels != null)
        {
            for (int i = 0; i < data.levels.Count; i++)
            {
                if (data.levels[i].levelNum == saveLevelNum)
                {
                    EditLevels.ConsoleLog($"Level {saveLevelNum} replaced!");
                    data.levels.Remove(data.levels[i]);
                    data.levelCount--;
                    flag = true;
                }
            }
        }
        data.levels.Add(newLevel);

        data.levelCount++;
        if(!flag) EditLevels.ConsoleLog($"Level {saveLevelNum} saved!");
        SaveData();
    }
    /// <summary>
    /// Delete level by number
    /// </summary>
    /// <param name="lvlNum"></param>
    public static void DeleteLevel(int lvlNum)
    {
        try
        {
            for (int i = 0; i < data.levels.Count; i++)
            {
                if(data.levels[i].levelNum == lvlNum)
                {
                    data.levels.Remove(data.levels[i]);
                    data.levelCount--;
                }
            }
            EditLevels.ConsoleLog($"Level {lvlNum} deleted");
            SaveData();
        }
        catch(Exception e) { EditLevels.ConsoleLog("Deleting failed."); Debug.Log($"Deleting failed: {e.Message}"); }
    }
    /// <summary>
    /// Save all current level data
    /// </summary>
    public static void SaveData() 
    {
        try
        {
            string save = JsonUtility.ToJson(data);
            string[] jsonSave = new string[(save.Length / 300) + 1];
            for (int i = 0; i < jsonSave.Length; i++) jsonSave[i] = "";
            int stringCounter = 0, c = 0;
            for(int i = 0; i < save.Length; i++)
            {
                jsonSave[c] += save[i];
                stringCounter++;
                if (stringCounter > 300) { c++; stringCounter = 0; }
            }
            if (!File.Exists($"{Application.streamingAssetsPath}{saveFolderName}")) Debug.Log("[Sweet Boom Editor] Gamedata folder created.");
            File.WriteAllLines($"{Application.streamingAssetsPath}{saveFolderName}", jsonSave);
        }
        catch(Exception e)
        {
            Debug.Log(e.Message);
        }
        try
        {
            NewLevelWin.popupLevelID = new int[data.levelCount];
            NewLevelWin.selectLevelPopupInfo = new string[data.levelCount];
            for (int i = 0; i < data.levelCount; i++)
            {
                NewLevelWin.selectLevelPopupInfo[i] = $"{data.levels[i].levelNum}: {data.levels[i].comment}";
                NewLevelWin.popupLevelID[i] = data.levels[i].levelNum;
            }
        }
        catch (Exception e) { EditLevels.ConsoleLog($"Failed: {e.Message}"); }
    }
}
public enum CandyColorID
{
    green,
    blue,
    orange,
    red,
    yellow
}
[Serializable]
public class GameData // All game data from SweetBoom editor
{
    public List<Level> levels;
    public int levelCount;
    public ConfigurationSettings settings;

    public GameData(List<Level> lvls, int levelCount)
    {
        this.levels = lvls;
        this.levelCount = levelCount;
    }
    public GameData(List<Level> lvls, int levelCount, ConfigurationSettings settings) : this(lvls, levelCount)
    {
        this.settings = settings;
    }
}

[Serializable]
public class Level // Levels class for save added levels by developer
{
    public int levelNum, fieldWidth, fieldHeight;
    public string comment;
    public Vector3Int[] savedBlock;
    public int[] target;
    public Color[] levelColors;

    public Level(int levelNum_, int fieldWidth_, int fieldHeight_, string comment_, int[,] blocks, int[] trg, Color[] levelColors)
    {
        levelNum = levelNum_;
        fieldWidth = fieldWidth_;
        fieldHeight = fieldHeight_;
        comment = comment_;
        this.levelColors = levelColors;
        target = trg;

        savedBlock = new Vector3Int[fieldWidth_ * fieldHeight_];
        int counter = 0;
        for (int i = 0; i < fieldHeight_; i++)
        {
            for (int y = 0; y < fieldWidth_; y++)
            {
                savedBlock[counter] = new Vector3Int(i, y, blocks[i, y]);
                counter++;
            }
        }
    }
}
[Serializable]
public class ConfigurationSettings // Settings for SweetBoom editor
{
    public bool sortingLevelsInMenu, randomizePositionOfIcons, fps;
    public float distance, size;

    public ConfigurationSettings(bool sorting, bool randomize, float dist, float size, bool fps)
    {
        sortingLevelsInMenu = sorting;
        randomizePositionOfIcons = randomize;
        distance = dist;
        this.size = size;
        this.fps = fps;
    }
}