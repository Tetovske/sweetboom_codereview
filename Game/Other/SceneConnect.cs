using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public static class SceneConnect {

    static LevelData returnData;

	public static void OpenScene(int lvl)
    {
        try
        {
            var gameData = Save.InitGameData();
            foreach (var level in gameData.levels)
            {
                if (level.levelNum == lvl)
                    returnData = new LevelData(level, lvl);
            }
        }
        catch
        {
            Debug.LogError("[Sweet Boom Editor] Can't load game scene.");
        }
    }
    public static LevelData GetLevelData() => returnData;

    public class LevelData
    {
        public int levelNum;
        public Save.Level levelInfo;
        //public Save.GameSave.SaveSlot gameSave { get; set; }

        public LevelData(Save.Level levelInfo, int levelnum)
        {
            this.levelInfo = levelInfo;
            this.levelNum = levelnum;
        }
    }
}
