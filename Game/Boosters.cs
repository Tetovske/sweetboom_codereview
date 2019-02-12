using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class Boosters : MonoBehaviour
{
    [SerializeField] private BoosterObject[] boosters;
    [SerializeField] private GameController gameController;
    private Ray touchRay;
    private RaycastHit hitInfo;
    private BoosterObject currentMovingBooster;
    private float distanceBtwCandies;
    private RaycastHit[] hits; // used when you drag booster

    private void Start()
    {
        foreach (var b in boosters) b.defaultPosition = b.booster.transform.localPosition;
        distanceBtwCandies = GameController.CandiesDistance;
    }
    public enum BoosterID
    {
        bomb,
        colorFinder, 
        multiCandy
    }
    private void Update()
    {
        Debug.DrawRay(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector3.forward * 100);
        touchRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if(Input.GetMouseButtonDown(0))
        {
            if(Physics.Raycast(touchRay, out hitInfo))
            {
                switch(hitInfo.transform.name)
                {
                    case "#BombBooster":
                        BoosterSelected(BoosterID.bomb);
                        break;
                    case "#ColorFinder":
                        BoosterSelected(BoosterID.colorFinder);
                        break;
                    case "#MultiCandy":
                        BoosterSelected(BoosterID.multiCandy);
                        break;
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if(currentMovingBooster != null)
            {
                hits = Physics.RaycastAll(touchRay);
                foreach (var hit in hits)
                {
                    if(hit.transform.tag == "GameFieldObject" && hit.transform.name.Contains("block"))
                    {
                        GameController.Mapping deleteMap = new GameController.Mapping();
                        deleteMap.block = new List<GameController.Mapping.Item>();
                        if(currentMovingBooster.id == BoosterID.bomb)
                        {
                            int Counter = 0;
                            for (int i = hit.transform.GetComponent<FieldObject>().row - 1; i <= hit.transform.GetComponent<FieldObject>().row + 1; i++)
                            {
                                for (int j = hit.transform.GetComponent<FieldObject>().column - 1; j <= hit.transform.GetComponent<FieldObject>().column + 1; j++)
                                {
                                    if (i >= 0 && i < GameController.levelData.levelInfo.fieldHeight &&
                                        j >= 0 && j < GameController.levelData.levelInfo.fieldWidth && GameController.blocks[i, j] != null)
                                    {
                                        deleteMap.block.Add(new GameController.Mapping.Item()
                                        {
                                            i = i,
                                            j = j
                                        });
                                        Counter++;
                                    }
                                }
                            }
                            StartCoroutine(gameController.DeleteAll(deleteMap));
                        }
                        else if(currentMovingBooster.id == BoosterID.colorFinder)
                        {
                            int Counter = 0;
                            Save.CandyType t = GameController.sweets[hit.transform.GetComponent<FieldObject>().row, hit.transform.GetComponent<FieldObject>().column].candyID;
                            foreach (var sweet in GameController.sweets)
                            {
                                if(sweet?.candyID == t)
                                {
                                    deleteMap.block.Add(new GameController.Mapping.Item()
                                    {
                                        i = sweet.cell.GetComponent<FieldObject>().row,
                                        j = sweet.cell.GetComponent<FieldObject>().column
                                    });
                                    Counter++;
                                }
                            }
                            StartCoroutine(gameController.DeleteAll(deleteMap));
                        }
                        else if(currentMovingBooster.id == BoosterID.multiCandy)
                        {
                            if(GameController.mods[hit.transform.GetComponent<FieldObject>().row, hit.transform.GetComponent<FieldObject>().column].cell == null)
                            {
                                GameController.sweets[hit.transform.GetComponent<FieldObject>().row, hit.transform.GetComponent<FieldObject>().column].cell.GetComponent<SpriteRenderer>().sprite
                                = gameController.itemData.other[2].itemSprite;
                                GameController.sweets[hit.transform.GetComponent<FieldObject>().row, hit.transform.GetComponent<FieldObject>().column].candyID = Save.CandyType.multi;
                                GameController.sweets[hit.transform.GetComponent<FieldObject>().row, hit.transform.GetComponent<FieldObject>().column].cell.transform.localScale =
                                new Vector3(GameController.modSize[2] * 0.9f, GameController.modSize[2] * 0.9f, GameController.modSize[2] * 0.9f);
                                StartCoroutine(gameController.FindDuplicates(false, null, GameController.Direction.down));
                            }
                        }
                    }
                }

                StartCoroutine(SmoothMove(currentMovingBooster.booster, currentMovingBooster.defaultPosition));
                bool switcher = true;
                for (int i = 0; i < GameController.levelData.levelInfo.fieldHeight; i++)
                {
                    for (int j = 0; j < GameController.levelData.levelInfo.fieldWidth; j++)
                    {
                        try
                        {
                            if (switcher) GameController.blocks[i, j].GetComponent<SpriteRenderer>().color = GameController.levelData.levelInfo.levelColors[0];
                            else GameController.blocks[i, j].GetComponent<SpriteRenderer>().color = GameController.levelData.levelInfo.levelColors[1];
                        }
                        catch { }
                        switcher = !switcher;
                    }
                }
                currentMovingBooster = null;
            }
        }
        if(currentMovingBooster != null)
        {
            currentMovingBooster.booster.transform.position = new Vector3(touchRay.origin.x, touchRay.origin.y, currentMovingBooster.booster.transform.position.z);
            hits = Physics.RaycastAll(touchRay);
            foreach (var hit in hits)
            {
                if(hit.transform.tag == "GameFieldObject" && hit.transform.name.Contains("block"))
                {
                    int row = 0, column = 0;
                    for(int i = 0; i < GameController.levelData.levelInfo.fieldHeight; i++)
                    {
                        for(int j = 0; j < GameController.levelData.levelInfo.fieldWidth; j++)
                        {
                            if(hit.transform.position == GameController.blocks[i, j]?.transform.position)
                            {
                                row = i;
                                column = j;
                            }
                        }
                    }
                    bool switcher = true;
                    for(int i = 0; i < GameController.levelData.levelInfo.fieldHeight; i++)
                    {
                        for(int j = 0; j < GameController.levelData.levelInfo.fieldWidth; j++)
                        {
                            try
                            {
                                if (switcher) GameController.blocks[i, j].GetComponent<SpriteRenderer>().color = GameController.levelData.levelInfo.levelColors[0];
                                else GameController.blocks[i, j].GetComponent<SpriteRenderer>().color = GameController.levelData.levelInfo.levelColors[1];
                                if(currentMovingBooster.id == BoosterID.bomb)
                                {
                                    if (i >= row - 1 && i <= row + 1 && j >= column - 1 && j <= column + 1)
                                        GameController.blocks[i, j].transform.GetComponent<SpriteRenderer>().color = new Color(1f, 0.17f, 0.17f, 0.75f);
                                }
                                else if(currentMovingBooster.id == BoosterID.colorFinder)
                                {
                                    foreach (var sweet in GameController.sweets)
                                    {
                                        try
                                        {
                                            if (sweet.cell.GetComponent<FieldObject>().candyType == GameController.sweets[hit.transform.GetComponent<FieldObject>().row,
                                                hit.transform.GetComponent<FieldObject>().column].candyID)
                                            {
                                                GameController.blocks[sweet.cell.GetComponent<FieldObject>().row, sweet.cell.GetComponent<FieldObject>().column].transform.GetComponent<SpriteRenderer>().color
                                                    = new Color(1f, 0.17f, 0.17f, 0.75f);
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                else if(currentMovingBooster.id == BoosterID.multiCandy)
                                {
                                    hit.transform.GetComponent<SpriteRenderer>().color = new Color(1f, 0.17f, 0.17f, 0.75f);
                                }
                            }
                            catch { }
                            switcher = !switcher;
                        }
                    }
                }
            }
        }
            
    }
    private void BoosterSelected(BoosterID id) { foreach (var b in boosters) if (b.id == id) currentMovingBooster = b; }

    IEnumerator SmoothMove(GameObject mover, Vector3 target)
    {
        float lerpPercent = 0.8f * Time.deltaTime * 10;
        bool isOK = false;
        while(!isOK)
        {
            mover.transform.localPosition = new Vector3(Mathf.Lerp(mover.transform.localPosition.x, target.x, lerpPercent), Mathf.Lerp(mover.transform.localPosition.y, target.y, lerpPercent), target.z);
            if (Mathf.Abs(mover.transform.localPosition.x - target.x) < 1 && Mathf.Abs(mover.transform.localPosition.y - target.y) < 1)
            {
                mover.transform.localPosition = target;
                isOK = true;
            }
            yield return new WaitForEndOfFrame();
        }
    }
    [Serializable]
    public class BoosterObject
    {
        public GameObject booster, addButton;
        public TextMeshProUGUI count;
        public BoosterID id;
        [HideInInspector] public Vector3 defaultPosition;
    }
}

