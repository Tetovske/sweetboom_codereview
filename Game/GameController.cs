using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.IO;
/// <summary>
/// <para>[EN] Main class with game logic.</para>
/// <para>[RU] Основной класс с игровой логикой.</para>
/// </summary>
public class GameController : MonoBehaviour, IGameController
{
    [Header("Play field margin. (0.4 - default)"), Range(0.2f, 1f)]
    public float fieldMarginLR, fieldMarginTopBottom;
    [Header("Animation speed")]
    public int animationSpeed;
    [Header("Falling speed")]
    public int fallingSpeed;
    [SerializeField]
    public ItemData itemData;
    [SerializeField]
    private GameObject pausePopup, faderS;
    private GameObject musicSwitcher, soundSwitcher;
    private static GameObject fader;
    public Camera mainCamera;
    public TextMeshProUGUI scoreTxt, movesTxt, levelTxt, fpsCounter;
    public GameObject blockPrefab, playFieldParent, upperHeader, downHeader, goalCandyPrefab, goalHeader;
    public Sprite blockPrefabSprite, maskSprite;
    public Canvas mainCanvas;
    [Header("Scripts")] [SerializeField] private UIController UIController;
    private static Func<object> func;
    private bool canTouch, canContinue;

    private Vector3 screenSize;
    private Vector3[] upperHeaderCorners = new Vector3[4], downHeaderCorners = new Vector3[4];

    public static event Action candiesDeleted, candiesOperationsEnded, candiesOperationsStarts, onGameOver, onGameWin;
    public static event Action<FieldCell, Direction> nothingToDelete;
    
    private static Dictionary<Save.CandyType, GoalInfo> targetInfo = new Dictionary<Save.CandyType, GoalInfo>();

    public static float[] candySize { get; private set; }
    public static float[] modSize { get; private set; }
    float blockSize, firstPosX, firstPosY;
    public static float CandiesDistance { get; private set; }

    #region GameControl
    private Vector3 touchBeginPosition = new Vector3(), touchEndPosition = new Vector3(), endTouch;
    private Ray rayTouch;

    private RaycastHit touchHit;
    private FieldCell firstTouchObj, endTouchObj;
    #endregion

    public static GameObject[,] blocks { get; private set; } // Insantiated blocks array
    public static FieldCell[,] sweets { get; private set; } // array of sweets in play field
    public static FieldCell[,] mods { get; private set; } // array of rock blocks, ice cubes etc. in play field

    public static SceneConnect.LevelData levelData;
    private bool initStatus;

    public enum Direction
    {
        up,
        left,
        down,
        right
    }

    private async void Awake()
    {
        if (Save.configuration != null) if (Save.configuration.fps) fpsCounter.gameObject.SetActive(true);
        fader = faderS;
        StartCoroutine(GameSceneFader(() =>  StartCoroutine(UIController.GetComponent<UIController>().ShowMessage($"Raise shields for candy! Level: {levelData.levelNum}", 1, global::UIController.ShowUI.defaultAlert)), GameController.FaderFunctions.fromBlack));
        InitializeVariables();
        levelData = SceneConnect.GetLevelData();
        if (levelData != null) initStatus = await SetupLevel();
        else canTouch = false;
    }

    private void InitializeVariables()
    {
        rayTouch = new Ray(); // Main ray 
        rayTouch.direction = Vector3.forward * 100;
        if (animationSpeed <= 0) animationSpeed = 5;
        endTouchObj = new FieldCell();
        canTouch = true;
        candiesOperationsStarts += OnCandyProcessesStarts;
        candiesOperationsEnded += OnCandyProcessesEnded;
        candiesDeleted += OnCandyDeleted;
        nothingToDelete += OnNothingToDelete;
        onGameWin += OnGameWin;
        onGameOver += OnGameOver;
    }
    private void Update()
    {
        try
        {
            //Debug.DrawRay(mainCamera.ScreenToWorldPoint(Input.mousePosition), Vector3.forward * 100); // Use this if you want draw touch ray
            if (Input.GetMouseButtonDown(0))
            {
                touchBeginPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                rayTouch.origin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                if(canTouch)
                {
                    if (Physics.Raycast(rayTouch, out touchHit))
                    {
                        for (int i = 0; i < levelData.levelInfo.fieldHeight; i++)
                        {
                            for (int y = 0; y < levelData.levelInfo.fieldWidth; y++)
                            {
                                if (sweets[i, y]?.cell?.transform.position == touchHit.transform.position && touchHit.transform.tag
                                    == "GameFieldObject")
                                {
                                    firstTouchObj = new FieldCell();
                                    firstTouchObj.blockID = Save.BlockID.candy;
                                    firstTouchObj.candyID = sweets[i, y].candyID;
                                    firstTouchObj.cell = touchHit.transform.gameObject;
                                    firstTouchObj.index = sweets[i, y].index;
                                }
                            }
                        }
                    }
                }
            }
            else if(Input.GetMouseButtonUp(0))
            {
                touchEndPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                rayTouch.origin = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                endTouch = new Vector3(touchEndPosition.x - touchBeginPosition.x, touchEndPosition.y - touchBeginPosition.y, touchEndPosition.z - touchBeginPosition.z); // The vector calculated from touchBegin to touchEndPosition
                float dist = Mathf.Sqrt(Mathf.Pow(endTouch.x, 2) + Mathf.Pow(endTouch.y, 2));
                if(dist > 0.15f) // Swipe
                {
                    if (canTouch && firstTouchObj != null && firstTouchObj.cell != null)
                    {
                        if (endTouch.y > endTouch.x && endTouch.y > -endTouch.x) // Swipe UP
                        {
                            if (firstTouchObj?.index?.i > 0) ChangeCandiesPositions(true, firstTouchObj, Direction.up);
                        }
                        else if (endTouch.y > endTouch.x && endTouch.y < -endTouch.x) // Swipe LEFT
                        {
                            if (firstTouchObj?.index?.j > 0) ChangeCandiesPositions(true, firstTouchObj, Direction.left);
                        }
                        else if (endTouch.y < endTouch.x && endTouch.y < -endTouch.x) // Swipe DOWN
                        {
                            if (firstTouchObj?.index?.i < levelData.levelInfo.fieldHeight - 1) ChangeCandiesPositions(true, firstTouchObj, Direction.down);
                        }
                        else if (endTouch.y < endTouch.x && endTouch.y > -endTouch.x) // Swipe RIGHT
                        {
                            if (firstTouchObj?.index?.j < levelData.levelInfo.fieldWidth - 1) ChangeCandiesPositions(true, firstTouchObj, Direction.right);
                        }
                    }
                }
                else
                {
                    if(Physics.Raycast(rayTouch, out touchHit))
                    {
                        foreach (var item in sweets)
                        {
                            if(item?.cell?.transform.position == touchHit.transform.position)
                            {
                                float scaledObj = item.cell.transform.localScale.x * 0.7f, unScaledObj = item.cell.transform.localScale.x;
                                bool flag = false;
                                func = () => {
                                    if(item.cell != null)
                                    {
                                        if (!flag) item.cell.transform.localScale = new Vector3(Mathf.Lerp(item.cell.transform.localScale.x, scaledObj, 0.3f), Mathf.Lerp(item.cell.transform.localScale.y, scaledObj, 0.3f), item.cell.transform.localScale.z);
                                        else item.cell.transform.localScale = new Vector3(Mathf.Lerp(item.cell.transform.localScale.x, unScaledObj, 0.3f), Mathf.Lerp(item.cell.transform.localScale.y, unScaledObj, 0.3f), item.cell.transform.localScale.z);
                                        if (Math.Round(item.cell.transform.localScale.x, 1) == Math.Round(scaledObj, 1)) flag = true;
                                        else if (Math.Round(item.cell.transform.localScale.x, 1) == Math.Round(unScaledObj, 1) && flag == true)
                                        {
                                            item.cell.transform.localScale = new Vector3(unScaledObj, unScaledObj, item.cell.transform.localScale.z);
                                            return true;
                                        }
                                        return false;
                                    }
                                    return true;
                                };
                                StartCoroutine(StartLoopFunc(func));
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch(Exception e)
        {
            Debug.Log(e.ToString());
        }
    }
    private void FixedUpdate()
    {
        if(Save.configuration != null) if (Save.configuration.fps) fpsCounter.text = $"fps: {(int)(1 / Time.deltaTime)}";
    }
    private void ChangeCandiesPositions(bool withCheck, FieldCell obj, Direction direction) // 
    {
        candiesOperationsStarts();
        if (obj?.cell != null)
        {
            Direction d = Direction.left;
            FieldCell sweet1 = new FieldCell(), sweet2 = new FieldCell();
            Vector3 target1, target2, dir1 = new Vector3(0, 0, 0), dir2 = new Vector3(0, 0, 0);
            sweet1 = obj;
            for (int i = 0; i < levelData.levelInfo.fieldHeight; i++)
            {
                for (int y = 0; y < levelData.levelInfo.fieldWidth; y++)
                {
                    if (sweets[i, y]?.cell?.transform && sweets[i, y].index == obj.index)
                    {
                        switch (direction)
                        {
                            case Direction.up:
                                if (obj?.index.i - 1 >= 0)
                                {
                                    if (sweets[i - 1, y].cell == null || sweets[i - 1, y].blockID == Save.BlockID.empty) { candiesOperationsEnded(); return; } 
                                    else if (mods[i - 1, y].blockID != Save.BlockID.nil) if (mods[i - 1, y].blockID == Save.BlockID.ice) { candiesOperationsEnded(); return; }
                                    sweet2 = sweets[i - 1, y];
                                    sweets[i, y] = sweet2;
                                    sweets[i - 1, y] = sweet1;
                                    d = Direction.down;
                                    dir1 = Vector3.up;
                                    dir2 = Vector3.down;
                                }
                                break;
                            case Direction.left:
                                if (obj?.index.j - 1 >= 0)
                                {
                                    if (sweets[i, y - 1].cell == null || sweets[i, y - 1].blockID == Save.BlockID.empty) { candiesOperationsEnded(); return; }
                                    else if (mods[i, y - 1].blockID != Save.BlockID.nil) if (mods[i, y - 1].blockID == Save.BlockID.ice) { candiesOperationsEnded(); return; }
                                    sweet2 = sweets[i, y - 1];
                                    sweets[i, y] = sweet2;
                                    sweets[i, y - 1] = sweet1;
                                    d = Direction.right;
                                    dir1 = Vector3.left;
                                    dir2 = Vector3.right;
                                }
                                break;
                            case Direction.down:
                                if (obj?.index.i + 1 < levelData.levelInfo.fieldHeight)
                                {
                                    if (sweets[i + 1, y].cell == null || sweets[i + 1, y].blockID == Save.BlockID.empty) { candiesOperationsEnded(); return; }
                                    else if (mods[i + 1, y].blockID != Save.BlockID.nil) if (mods[i + 1, y].blockID == Save.BlockID.ice) { candiesOperationsEnded(); return; }
                                    sweet2 = sweets[i + 1, y];
                                    sweets[i, y] = sweet2;
                                    sweets[i + 1, y] = sweet1;
                                    d = Direction.up;
                                    dir1 = Vector3.down;
                                    dir2 = Vector3.up;
                                }
                                break;
                            case Direction.right:
                                if (obj?.index.j + 1 < levelData.levelInfo.fieldWidth)
                                {
                                    if (sweets[i, y + 1].cell == null || sweets[i, y + 1].blockID == Save.BlockID.empty) { candiesOperationsEnded(); return; }
                                    else if (mods[i, y + 1].blockID != Save.BlockID.nil) if (mods[i, y + 1].blockID == Save.BlockID.ice) { candiesOperationsEnded(); return; }
                                    sweet2 = sweets[i, y + 1];
                                    sweets[i, y] = sweet2;
                                    sweets[i, y + 1] = sweet1;
                                    d = Direction.left;
                                    dir1 = Vector3.right;
                                    dir2 = Vector3.left;
                                }
                                break;
                        }
                        for (int k = 0; k < levelData.levelInfo.fieldHeight; k++) for (int l = 0; l < levelData.levelInfo.fieldWidth; l++) if (sweets[k, l].blockID != Save.BlockID.empty) sweets[k, l].index = new FieldCell.Index(k, l);
                        target2 = sweet1.cell.transform.position;
                        target1 = sweet2.cell.transform.position;
                        func = () => {
                            sweet1.cell.transform.Translate(dir1 * Time.deltaTime * animationSpeed, Space.World);
                            sweet2.cell.transform.Translate(dir2 * Time.deltaTime * animationSpeed, Space.World);
                            if (direction == Direction.up || direction == Direction.right)
                            {
                                if ((sweet1.cell.transform.position.x > target1.x || sweet1.cell.transform.position.y > target1.y) && (sweet2.cell.transform.position.x < target2.x || sweet2.cell.transform.position.y < target2.y))
                                {
                                    sweet1.cell.transform.position = target1;
                                    sweet2.cell.transform.position = target2;
                                    firstTouchObj = null;
                                    if (withCheck) StartCoroutine(FindDuplicates(false, sweet1, d));
                                    return true;
                                }
                            }
                            else if (direction == Direction.down || direction == Direction.left)
                            {   if ((sweet1.cell.transform.position.x < target1.x || sweet1.cell.transform.position.y < target1.y) && (sweet2.cell.transform.position.x > target2.x || sweet2.cell.transform.position.y > target2.y))
                                {
                                    sweet1.cell.transform.position = target1;
                                    sweet2.cell.transform.position = target2;
                                    firstTouchObj = null;
                                    if (withCheck) StartCoroutine(FindDuplicates(false, sweet1, d));
                                    return true;
                                }
                            }
                            return false;
                        };
                        StartCoroutine(StartLoopFunc(func));
                        return;
                    }
                }
            }
        }
    }
    IEnumerator StartLoopFunc(Func<object> f) // Start function in loop (function breaks after returning 'true')
    {
        bool loopActive = true;
        int count = int.MinValue;
        while(loopActive)
        {
            bool res = (bool)f();
            if (res) loopActive = false; 
            yield return new WaitForEndOfFrame();
            count++;
            if (count > int.MaxValue - 3)
            {
                loopActive = false;
                throw new Exception("[Sweet Boom Editor] Loop overload");
            }
        }
    }
    public IEnumerator FindDuplicates(bool fast, FieldCell thisObjField, Direction objChangeDirection) // Call 'deleteDuplicates' if find candies
    {
        int localCount = 0, fromID = -1, toID = -1, noDuplicates = 0, changeI, changeJ, targetI, targetJ; 
        Save.CandyType candyID = Save.CandyType.nil, collectedColor = Save.CandyType.nil;
        bool vertical = true, isDeleted = false;
        int counter = 0;
        while (noDuplicates < 2)
        {
            counter++; 
            noDuplicates++;
            vertical = !vertical;
            if(vertical)
            {
                targetI = levelData.levelInfo.fieldWidth;
                targetJ = levelData.levelInfo.fieldHeight;
            }
            else
            {
                targetI = levelData.levelInfo.fieldHeight;
                targetJ = levelData.levelInfo.fieldWidth;
            }
            for (int i = 0; i < targetI; i++)
            {
                localCount = 0;
                candyID = Save.CandyType.nil;
                for (int j = 0; j < targetJ; j++)
                {
                    if (vertical) { changeI = j; changeJ = i; }
                    else { changeI = i; changeJ = j; }
                    if (sweets[changeI, changeJ]?.candyID != Save.CandyType.nil)
                    {
                        if ((candyID != sweets[changeI, changeJ].candyID || sweets[changeI, changeJ].blockID == Save.BlockID.empty) && candyID != Save.CandyType.multi && sweets[changeI, changeJ].candyID != Save.CandyType.multi)
                        {
                            if (localCount > 2)
                            {
                                toID = j - 1;
                                noDuplicates = 0;
                                isDeleted = true;
                                if (fast) ResetCandy(vertical, i, fromID, toID);
                                else
                                {
                                    DeleteDuplicates(vertical, i, fromID, toID);
                                    yield return new WaitUntil(() => canContinue);
                                    yield return new WaitForSeconds(0.5f);
                                }
                            }
                            localCount = 1;
                            fromID = j;
                            candyID = sweets[changeI, changeJ].candyID;
                            collectedColor = sweets[changeI, changeJ].candyID;
                        }
                        else if(sweets[changeI, changeJ].candyID == Save.CandyType.multi || candyID == Save.CandyType.multi)
                        {
                            if (candyID == Save.CandyType.multi && sweets[changeI, changeJ].candyID != Save.CandyType.multi) // candy and multi candy before
                            {
                                if (localCount > 2 && sweets[changeI, changeJ].candyID != collectedColor)
                                {
                                    toID = j - 1;
                                    noDuplicates = 0;
                                    isDeleted = true;
                                    if (fast) ResetCandy(vertical, i, fromID, toID);
                                    else
                                    {
                                        DeleteDuplicates(vertical, i, fromID, toID);
                                        yield return new WaitUntil(() => canContinue);
                                        yield return new WaitForSeconds(0.5f);
                                    }
                                    localCount = 1;
                                    candyID = sweets[changeI, changeJ].candyID;
                                    collectedColor = sweets[changeI, changeJ].candyID;
                                }
                                else if (sweets[changeI, changeJ].candyID == collectedColor)
                                {
                                    localCount++;
                                    candyID = collectedColor;
                                }
                                else
                                {
                                    candyID = sweets[changeI, changeJ].candyID;
                                    collectedColor = sweets[changeI, changeJ].candyID;
                                    localCount = 2;
                                    fromID = j - 1;
                                }
                            }
                            else if (sweets[changeI, changeJ].candyID == Save.CandyType.multi && candyID != Save.CandyType.multi)
                            {
                                localCount++;
                                if(fromID == -1) fromID = j;
                                if(candyID != Save.CandyType.nil) collectedColor = candyID;
                                candyID = Save.CandyType.multi;
                            }
                            else if (sweets[changeI, changeJ].candyID == Save.CandyType.multi && candyID == Save.CandyType.multi) { localCount++; candyID = Save.CandyType.multi; }
                            if (localCount > 2 && j == targetJ - 1)
                            {
                                toID = j;
                                noDuplicates = 0;
                                isDeleted = true;
                                if (fast) ResetCandy(vertical, i, fromID, toID);
                                else
                                {
                                    DeleteDuplicates(vertical, i, fromID, toID);
                                    yield return new WaitUntil(() => canContinue);
                                    yield return new WaitForSeconds(0.5f);
                                }
                            }
                        }
                        else if (candyID == sweets[changeI, changeJ].candyID)
                        {
                            localCount++;
                            if (localCount > 2 && j == targetJ - 1) 
                            {
                                toID = j;
                                noDuplicates = 0;
                                isDeleted = true;
                                if (fast) ResetCandy(vertical, i, fromID, toID);
                                else
                                {
                                    DeleteDuplicates(vertical, i, fromID, toID);
                                    yield return new WaitUntil(() => canContinue);
                                    yield return new WaitForSeconds(0.5f);
                                }
                            }
                        }
                    }
                    else if (sweets[changeI, changeJ].candyID == Save.CandyType.nil)
                    {
                        if (localCount > 2)
                        {
                            toID = j - 1;
                            noDuplicates = 0;
                            isDeleted = true;
                            if (fast) ResetCandy(vertical, i, fromID, toID);
                            else
                            {
                                DeleteDuplicates(vertical, i, fromID, toID);
                                yield return new WaitUntil(() => canContinue);
                                yield return new WaitForSeconds(0.5f);
                            }
                        }
                        localCount = 0;
                        candyID = Save.CandyType.nil;
                    }
                    else Debug.Log($"candy [{changeI},{changeJ}] error");
                }
            }
            if (counter > 100) noDuplicates = 2;
        }
        if (!isDeleted) OnNothingToDelete(thisObjField, objChangeDirection);
        else OnCandyProcessesEnded();
    }
    private bool FindOutcome() // Find possible outcomes for current game situation. 'true' if exist
    {
        for(int i = 0; i < levelData.levelInfo.fieldHeight; i++)
        {
            for(int j = 0; j < levelData.levelInfo.fieldWidth; j++)
            {
                if (sweets[i, j].blockID == Save.BlockID.candy && sweets[i, j].candyID != Save.CandyType.nil)
                {
                    if(j + 1 < levelData.levelInfo.fieldWidth) // horizontally right
                    {
                        if (sweets[i, j + 1].candyID == sweets[i, j].candyID)
                        {
                            if (i - 1 >= 0 && j + 2 < levelData.levelInfo.fieldWidth)
                                if (sweets[i - 1, j + 2].candyID == sweets[i, j].candyID && mods[i - 1, j + 2].blockID != Save.BlockID.ice && sweets[i, j + 2].blockID == Save.BlockID.candy && mods[i, j + 2].blockID != Save.BlockID.ice) return true;
                            if (i + 1 < levelData.levelInfo.fieldHeight && j + 2 < levelData.levelInfo.fieldWidth)
                                if (sweets[i + 1, j + 2].candyID == sweets[i, j].candyID && mods[i + 1, j + 2].blockID != Save.BlockID.ice && sweets[i, j + 2].blockID == Save.BlockID.candy && mods[i, j + 2].blockID != Save.BlockID.ice) return true;
                            if (j + 3 < levelData.levelInfo.fieldWidth)
                                if (sweets[i, j + 3].candyID == sweets[i, j].candyID && mods[i, j + 3].blockID != Save.BlockID.ice && sweets[i, j + 2].blockID == Save.BlockID.candy && mods[i, j + 2].blockID != Save.BlockID.ice) return true;
                        }
                        else if (sweets[i, j].candyID != sweets[i, j + 1].candyID)
                        {
                            if (i - 1 >= 0 && j + 2 < levelData.levelInfo.fieldWidth) if (sweets[i - 1, j + 1].candyID == sweets[i, j].candyID && mods[i - 1, j + 1].blockID != Save.BlockID.ice && sweets[i, j + 1].blockID == Save.BlockID.candy && mods[i, j + 1].blockID != Save.BlockID.ice
                                 && sweets[i, j + 2].candyID == sweets[i, j].candyID) return true;
                            if (i + 1 < levelData.levelInfo.fieldHeight && j + 2 < levelData.levelInfo.fieldWidth) if (sweets[i + 1, j + 1].candyID == sweets[i, j].candyID && mods[i + 1, j + 1].blockID != Save.BlockID.ice && sweets[i, j + 1].blockID == Save.BlockID.candy && mods[i, j + 1].blockID != Save.BlockID.ice
                                 && sweets[i, j + 2].candyID == sweets[i, j].candyID) return true;
                        }
                    }
                    if(j - 1 >= 0) // horizontally left
                    {
                        if (sweets[i, j - 1].candyID == sweets[i, j].candyID)
                        {
                            if (i - 1 >= 0 && j - 2 >= 0)
                                if (sweets[i - 1, j - 2].candyID == sweets[i, j].candyID && mods[i - 1, j - 2].blockID != Save.BlockID.ice && sweets[i, j - 2].blockID == Save.BlockID.candy && mods[i, j - 2].blockID != Save.BlockID.ice) return true;
                            if (i + 1 < levelData.levelInfo.fieldHeight && j - 2 >= 0)
                                if (sweets[i + 1, j - 2].candyID == sweets[i, j].candyID && mods[i + 1, j - 2].blockID != Save.BlockID.ice && sweets[i, j - 2].blockID == Save.BlockID.candy && mods[i, j -  2].blockID != Save.BlockID.ice) return true;
                            if (j - 3 >= 0)
                                if (sweets[i, j - 3].candyID == sweets[i, j].candyID && mods[i, j - 3].blockID != Save.BlockID.ice && sweets[i, j - 2].blockID == Save.BlockID.candy && mods[i, j - 2].blockID != Save.BlockID.ice) return true;
                        }
                        else if (sweets[i, j].candyID != sweets[i, j - 1].candyID)
                        {
                            if (i - 1 >= 0 && j - 2 >= 0) if (sweets[i - 1, j - 1].candyID == sweets[i, j].candyID && mods[i - 1, j - 1].blockID != Save.BlockID.ice && sweets[i, j - 1].blockID == Save.BlockID.candy && mods[i, j - 1].blockID != Save.BlockID.ice
                                 && sweets[i, j - 2].candyID == sweets[i, j].candyID) return true;
                            if (i + 1 < levelData.levelInfo.fieldHeight && j - 2 >= 0) if (sweets[i + 1, j - 1].candyID == sweets[i, j].candyID && mods[i + 1, j - 1].blockID != Save.BlockID.ice && sweets[i, j - 1].blockID == Save.BlockID.candy && mods[i, j - 1].blockID != Save.BlockID.ice
                                 && sweets[i, j - 2].candyID == sweets[i, j].candyID) return true;
                        }
                    }
                    if(i + 1 < levelData.levelInfo.fieldHeight) // vertically down
                    {
                        if (sweets[i + 1, j].candyID == sweets[i, j].candyID)
                        {
                            if (j - 1 >= 0 && i + 2 < levelData.levelInfo.fieldHeight)
                                if (sweets[i + 2, j - 1].candyID == sweets[i, j].candyID && mods[i + 2, j - 1].blockID != Save.BlockID.ice && sweets[i + 2, j].blockID == Save.BlockID.candy && mods[i + 2, j].blockID != Save.BlockID.ice) return true;
                            if (j + 1 < levelData.levelInfo.fieldWidth && i + 2 < levelData.levelInfo.fieldHeight)
                                if (sweets[i + 2, j + 1].candyID == sweets[i, j].candyID && mods[i + 2, j + 1].blockID != Save.BlockID.ice && sweets[i + 2, j].blockID == Save.BlockID.candy && mods[i + 2, j].blockID != Save.BlockID.ice) return true;
                            if (i + 3 < levelData.levelInfo.fieldHeight)
                                if (sweets[i + 3, j].candyID == sweets[i, j].candyID && mods[i + 3, j].blockID != Save.BlockID.ice && sweets[i + 2, j].blockID == Save.BlockID.candy && mods[i + 2, j].blockID != Save.BlockID.ice) return true;
                        }
                        else if (sweets[i, j].candyID != sweets[i + 1, j].candyID)
                        {
                            if (j - 1 >= 0 && i + 2 < levelData.levelInfo.fieldHeight) if (sweets[i + 1, j - 1].candyID == sweets[i, j].candyID && mods[i + 1, j - 1].blockID != Save.BlockID.ice && sweets[i + 1, j].blockID == Save.BlockID.candy && mods[i + 1, j].blockID != Save.BlockID.ice
                                 && sweets[i + 2, j].candyID == sweets[i, j].candyID) return true;
                            if (j + 1 < levelData.levelInfo.fieldWidth && i + 2 < levelData.levelInfo.fieldHeight) if (sweets[i + 1, j + 1].candyID == sweets[i, j].candyID && mods[i + 1, j + 1].blockID != Save.BlockID.ice && sweets[i + 1, j].blockID == Save.BlockID.candy && mods[i + 1, j].blockID != Save.BlockID.ice
                                 && sweets[i + 2, j].candyID == sweets[i, j].candyID) return true;
                        }
                    }
                    if (i - 1 >= 0) // vertically up
                    {
                        if (sweets[i - 1, j].candyID == sweets[i, j].candyID)
                        {
                            if (j - 1 >= 0 && i - 2 >= 0)
                                if (sweets[i - 2, j - 1].candyID == sweets[i, j].candyID && mods[i - 2, j - 1].blockID != Save.BlockID.ice && sweets[i - 2, j].blockID == Save.BlockID.candy && mods[i - 2, j].blockID != Save.BlockID.ice) return true;
                            if (j + 1 < levelData.levelInfo.fieldWidth && i - 2 >= 0)
                                if (sweets[i - 2, j + 1].candyID == sweets[i, j].candyID && mods[i - 2, j + 1].blockID != Save.BlockID.ice && sweets[i - 2, j].blockID == Save.BlockID.candy && mods[i - 2, j].blockID != Save.BlockID.ice) return true;
                            if (i - 3 >= 0)
                                if (sweets[i - 3, j].candyID == sweets[i, j].candyID && mods[i - 3, j].blockID != Save.BlockID.ice && sweets[i - 2, j].blockID == Save.BlockID.candy && mods[i - 2, j].blockID != Save.BlockID.ice) return true;
                        }
                        else if (sweets[i, j].candyID != sweets[i - 1, j].candyID)
                        {
                            if (j - 1 >= 0 && i - 2 >= 0) if (sweets[i - 1, j - 1].candyID == sweets[i, j].candyID && mods[i - 1, j - 1].blockID != Save.BlockID.ice && sweets[i - 1, j].blockID == Save.BlockID.candy && mods[i - 1, j].blockID != Save.BlockID.ice
                                 && sweets[i - 2, j].candyID == sweets[i, j].candyID) return true;
                            if (j + 1 < levelData.levelInfo.fieldWidth && i - 2 >= 0) if (sweets[i - 1, j + 1].candyID == sweets[i, j].candyID && mods[i - 1, j + 1].blockID != Save.BlockID.ice && sweets[i - 1, j].blockID == Save.BlockID.candy && mods[i - 1, j].blockID != Save.BlockID.ice
                                 && sweets[i - 2, j].candyID == sweets[i, j].candyID) return true;
                        }
                    }
                }
            }
        }
        return false;
    }
    private void ResetCandy(bool vertical, int rowNum, int from, int to) // Call in the beggining of the game
    {
        for (int i = from; i <= to; i++)
        {
            int r = Save.Randomizer(0, itemData.candies.Length - 1);
            if (vertical && sweets[i, rowNum].blockID != Save.BlockID.empty && sweets[i, rowNum].cell != null)
            {
                sweets[i, rowNum].candyID = (Save.CandyType)r;
                sweets[i, rowNum].cell.GetComponent<SpriteRenderer>().sprite = itemData.candies[r].itemSprite;
                sweets[i, rowNum].cell.transform.localScale = new Vector3(candySize[r] * 0.9f, candySize[r] * 0.9f, candySize[r] * 0.9f);
            }
            else if(!vertical && sweets[rowNum, i].blockID != Save.BlockID.empty && sweets[rowNum, i].cell != null)
            {
                sweets[rowNum, i].candyID = (Save.CandyType)r;
                sweets[rowNum, i].cell.GetComponent<SpriteRenderer>().sprite = itemData.candies[r].itemSprite;
                sweets[rowNum, i].cell.transform.localScale = new Vector3(candySize[r] * 0.9f, candySize[r] * 0.9f, candySize[r] * 0.9f);
            }
        }
    }
    private void MixCandies()
    {
        StartCoroutine(UIController.GetComponent<UIController>().ShowMessage("Refresh...", true, global::UIController.ShowUI.defaultAlert));
        for (int i = 0; i < 3; i++)
        {
            bool isCandy = false;
            while(!isCandy)
            {
                int iR = Save.Randomizer(0, levelData.levelInfo.fieldHeight - 1);
                int jR = Save.Randomizer(0, levelData.levelInfo.fieldWidth - 1);
                int candy = Save.Randomizer(0, itemData.candies.Length - 1);
                if (sweets[iR, jR].blockID == Save.BlockID.candy && sweets[iR, jR].candyID != Save.CandyType.nil && mods[iR, jR].blockID != Save.BlockID.block)
                {
                    sweets[iR, jR].candyID = (Save.CandyType)candy;
                    sweets[iR, jR].cell.GetComponent<SpriteRenderer>().sprite = itemData.candies[candy].itemSprite;
                    sweets[iR, jR].cell.transform.localScale = new Vector3(candySize[candy] * 0.9f, candySize[candy] * 0.9f, candySize[candy] * 0.9f);
                    isCandy = true;
                }
            }
        }
        StartCoroutine(FindDuplicates(true, null, Direction.up));
    }
    private void CalculateScore(Save.CandyType candyType, int count)
    {
        if(targetInfo.ContainsKey(candyType))
        {
            targetInfo[candyType].targetCount -= count;
            targetInfo[candyType].targetCount = targetInfo[candyType].targetCount < 0 ? 0 : targetInfo[candyType].targetCount;
            targetInfo[candyType].targetCountText.text = targetInfo[candyType].targetCount.ToString();
        }
    }
    private void DeleteDuplicates(bool vertical, int rowNum, int from, int to) // Deletes the duplicates
    {
#if UNITY_EDITOR
        //Debug.Log($"vert: {vertical}, row: {rowNum}, from: {from}, to: {to}");
#endif
        SoundController.PlaySound(SoundController.SoundType.candyCrush);
        if (!vertical)
        {
            
            int multiCount = 0;
            for (int i = from; i <= to; i++)
            {
                if (sweets[rowNum, i].candyID != Save.CandyType.multi) { CalculateScore(sweets[rowNum, i].candyID, to - from + 1); break; }
                else if(sweets[rowNum, i].candyID == Save.CandyType.multi) multiCount++;
            }
            if(multiCount == to - from + 1) CalculateScore((Save.CandyType)Save.Randomizer(0, itemData.candies.Length - 1), to - from + 1);
            
            for (int i = from; i <= to; i++)
            {
                sweets[rowNum, i].candyID = Save.CandyType.nil;
                Destroy(sweets[rowNum, i].cell);
            }
        }
        else if(vertical)
        {
            
            int multiCount = 0;
            for (int i = from; i <= to; i++)
            {
                if (sweets[i, rowNum].candyID != Save.CandyType.multi) { CalculateScore(sweets[i, rowNum].candyID, to - from + 1); break; }
                else if(sweets[rowNum, i].candyID == Save.CandyType.multi) multiCount++;
            }
            if (multiCount == to - from + 1) CalculateScore((Save.CandyType)Save.Randomizer(0, itemData.candies.Length - 1), to - from + 1);
            
            for (int i = from; i <= to; i++)
            {
                sweets[i, rowNum].candyID = Save.CandyType.nil;
                Destroy(sweets[i, rowNum].cell);
            }
        }
        for (int i = 0; i < levelData.levelInfo.fieldHeight; i++)
        {
            for (int j = 0; j < levelData.levelInfo.fieldWidth; j++)
            {
                if (mods[i, j]?.blockID == Save.BlockID.ice && sweets[i, j]?.candyID == Save.CandyType.nil)
                {
                    Destroy(mods[i, j].cell);
                    SoundController.PlaySound(SoundController.SoundType.candyCrush);
                    mods[i, j].blockID = Save.BlockID.nil;
                    break;
                }
                if(mods[i, j].blockID == Save.BlockID.block)
                {
                    if(vertical && (((j - 1 == rowNum || j + 1 == rowNum) && i >= from && i <= to) || (j == rowNum && ((i - 1 >= from && i - 1 <= to) || (i + 1 >= from && i + 1 <= to)))))
                    {
                        mods[i, j].blockID = Save.BlockID.nil;
                        sweets[i, j].candyID = Save.CandyType.nil;
                        sweets[i, j].blockID = Save.BlockID.candy;
                        Destroy(mods[i, j].cell);
                        SoundController.PlaySound(SoundController.SoundType.iceCrush);
                    }
                    else if(!vertical && (((i - 1 == rowNum || i + 1 == rowNum) && j >= from && j <= to) || (i == rowNum && ((j - 1 >= from && j - 1 <= to) || (j + 1 >= from && j + 1 <= to)))))
                    {
                        mods[i, j].blockID = Save.BlockID.nil;
                        sweets[i, j].candyID = Save.CandyType.nil;
                        sweets[i, j].blockID = Save.BlockID.candy;
                        Destroy(mods[i, j].cell);
                        SoundController.PlaySound(SoundController.SoundType.stoneCrush);
                    }
                }
            }
        }

        
        AddCandies();
    }
    public IEnumerator DeleteAll(Mapping mapInfo)
    {
        bool soundCandy = false, soundStone = false, soundIce = false;
        foreach (var bl in mapInfo.block)
        {
            if (bl.i >= 0 && bl.i < levelData.levelInfo.fieldHeight && bl.j >= 0 && bl.j < levelData.levelInfo.fieldWidth)
            {
                if(sweets[bl.i, bl.j].blockID == Save.BlockID.candy)
                {
                    if (!soundCandy) { soundCandy = true; SoundController.PlaySound(SoundController.SoundType.candyCrush); }
                    CalculateScore(sweets[bl.i, bl.j].candyID, 1);
                    Destroy(sweets[bl.i, bl.j].cell);
                    sweets[bl.i, bl.j].candyID = Save.CandyType.nil;
                }
                if(mods[bl.i, bl.j].blockID != Save.BlockID.empty && mods[bl.i, bl.j].blockID != Save.BlockID.nil)
                {
                    if (mods[bl.i, bl.j].blockID == Save.BlockID.ice) { if (!soundIce) { soundIce = true; SoundController.PlaySound(SoundController.SoundType.iceCrush); } }
                    else if (mods[bl.i, bl.j].blockID == Save.BlockID.block) { if (!soundStone) { soundStone = true; SoundController.PlaySound(SoundController.SoundType.stoneCrush); } }
                    Destroy(mods[bl.i, bl.j].cell);
                    mods[bl.i, bl.j].blockID = Save.BlockID.nil;
                }
            }
        }
        canContinue = false;
        AddCandies();
        yield return new WaitUntil(() => canContinue);
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(FindDuplicates(false, null, Direction.down));
    }
    private void AddCandies() // Finding empty slots and fill them
    {
        for (int i = 0; i < levelData.levelInfo.fieldWidth; i++)
        {
            for (int j = levelData.levelInfo.fieldHeight - 1; j >= 0; j--) // finding empty slots and changing indexes
            {
                if(sweets[j, i].blockID == Save.BlockID.candy && sweets[j, i].candyID == Save.CandyType.nil && j > 0 && mods[j, i].blockID != Save.BlockID.block)
                {
                    for(int p = j - 1; p >= 0; p--)
                    {
                        if (mods[p, i].blockID == Save.BlockID.block || sweets[p, i].blockID == Save.BlockID.empty)
                        {
                            if(p + 1 < levelData.levelInfo.fieldHeight)
                            {
                                for (int y = p + 1; y < levelData.levelInfo.fieldHeight; y++)
                                {
                                    if (sweets[y, i].candyID == Save.CandyType.nil && sweets[y, i].blockID == Save.BlockID.candy) sweets[y, i].cell = null;
                                    else if (sweets[y, i].blockID == Save.BlockID.candy && sweets[y, i].candyID != Save.CandyType.nil) break;
                                }
                            }
                            break;
                        }
                        else if (sweets[p, i].blockID == Save.BlockID.candy && sweets[p, i].candyID != Save.CandyType.nil)
                        {
                            sweets[j, i].cell = sweets[p, i].cell;
                            sweets[j, i].candyID = sweets[p, i].candyID;
                            sweets[p, i].candyID = Save.CandyType.nil;
                            break;
                        }
                    }
                }
            }
            float posY = firstPosY + blockSize;
            int from = levelData.levelInfo.fieldHeight - 1;
            bool isFirstCandyInColumnFinded = false;
            for (int j = 0; j < levelData.levelInfo.fieldHeight; j++) // finding block in the column
            {
                if (isFirstCandyInColumnFinded)
                {
                    if ((mods[j, i].blockID == Save.BlockID.block || mods[j, i].blockID == Save.BlockID.empty) && j - 1 >= 0)
                    {
                        from = j - 1;
                        break;
                    }
                    else if (mods[j, i].blockID == Save.BlockID.block && j - 1 < 0)
                    {
                        from = 0;
                        break;
                    }
                }
                else if (sweets[j, i].blockID == Save.BlockID.candy || mods[j, i].blockID == Save.BlockID.block) isFirstCandyInColumnFinded = true;
            }
            for (int j = from; j >= 0; j--)
            {
                if (sweets[j, i].blockID != Save.BlockID.empty && sweets[j, i].candyID == Save.CandyType.nil && mods[j, i].blockID != Save.BlockID.block)
                {
                    int r = Save.Randomizer(0, itemData.candies.Length - 1);
                    sweets[j, i] = new FieldCell();
                    sweets[j, i].cell = Instantiate(new GameObject(), new Vector3(0, 0, 0), Quaternion.identity);
                    sweets[j, i].blockID = Save.BlockID.candy;
                    sweets[j, i].candyID = (Save.CandyType)r;
                    sweets[j, i].cell.AddComponent(typeof(BoxCollider));
                    sweets[j, i].cell.AddComponent(typeof(SpriteRenderer));
                    sweets[j, i].cell.AddComponent(typeof(FieldObject));
                    sweets[j, i].cell.GetComponent<FieldObject>().row = j;
                    sweets[j, i].cell.GetComponent<FieldObject>().column = i;
                    sweets[j, i].cell.GetComponent<FieldObject>().candyType = sweets[j, i].candyID;
                    sweets[j, i].cell.GetComponent<SpriteRenderer>().sprite = itemData.candies[r].itemSprite;
                    sweets[j, i].cell.transform.parent = playFieldParent.transform;
                    sweets[j, i].cell.GetComponent<BoxCollider>().size = new Vector2(blockSize * sweets[j, i].cell.GetComponent<SpriteRenderer>().sprite.bounds.size.x * 1.7f, blockSize * sweets[j, i].cell.GetComponent<SpriteRenderer>().sprite.bounds.size.x * 1.7f);
                    sweets[j, i].cell.transform.position = new Vector3(blocks[j, i].transform.position.x, posY, blocks[j, i].transform.position.z - 5f);
                    sweets[j, i].cell.transform.localScale = new Vector3(candySize[r] * 0.9f, candySize[r] * 0.9f, candySize[r] * 0.9f);
                    sweets[j, i].cell.gameObject.name = $"candy[{j},{i}]";
                    sweets[j, i].cell.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                    posY += blockSize;
                }
            }
        }
        for (int i = 0; i < levelData.levelInfo.fieldHeight; i++) for (int j = 0; j < levelData.levelInfo.fieldWidth; j++)
            {
                if (sweets[i, j].blockID != Save.BlockID.empty) { sweets[i, j].index = new FieldCell.Index(i, j); }
                if (sweets[i, j].cell != null) sweets[i, j].cell.tag = "GameFieldObject";
                if (mods[i, j].cell != null) mods[i, j].cell.tag = "GameFieldObject";
            }
        func = () =>
        {
            bool allCandiesInPositions = true;
            for (int i = 0; i < levelData.levelInfo.fieldHeight; i++)
            {
                for (int j = 0; j < levelData.levelInfo.fieldWidth; j++)
                {
                    if (sweets[i, j]?.blockID != Save.BlockID.empty && sweets[i, j]?.cell?.transform.position.y > blocks[i, j]?.transform.position.y && sweets[i, j] != null && sweets[i, j].cell != null)
                    {
                        sweets[i, j].cell.transform.Translate(Vector2.down * Time.deltaTime * fallingSpeed, Space.World);
                        allCandiesInPositions = false;
                    }
                    else if (sweets[i, j].blockID != Save.BlockID.empty && sweets[i, j].cell?.transform.position.y < blocks[i, j].transform.position.y)
                        sweets[i, j].cell.transform.position = new Vector3(blocks[i, j].transform.position.x, blocks[i, j].transform.position.y, blocks[i, j].transform.position.z - 5f);
                }
            }
            if (allCandiesInPositions)
            {
                OnCandyDeleted();
                return true;
            }
            else return false;
        };
        StartCoroutine(StartLoopFunc(func));
        canContinue = false;
    }
    public void OnCandyProcessesStarts() => canTouch = false;    
    public void OnCandyProcessesEnded()
    {
        for (int i = 0; i < levelData.levelInfo.fieldHeight; i++) for (int j = 0; j < levelData.levelInfo.fieldWidth; j++) if (sweets[i, j].blockID != Save.BlockID.empty) sweets[i, j].index = new FieldCell.Index(i, j);
        int check = 0;
        
        foreach(var headerItem in targetInfo)
        {
            if (headerItem.Value.targetCount <= 0)
            {
                check++;
                if (check == targetInfo.Count)
                {
                    onGameWin();
                    return;
                }
            }
            else break;
        }
        if (FindOutcome()) canTouch = true;
        else
        {
            while(!FindOutcome()) MixCandies();
            StartCoroutine(UIController.GetComponent<UIController>().ShowMessage("Refresh...", false, global::UIController.ShowUI.defaultAlert));
            canTouch = true;
        }
        for(int i = 0; i < levelData.levelInfo.fieldHeight; i++)
        {
            for(int j = 0; j < levelData.levelInfo.fieldWidth; j++)
            {
                if(sweets[i, j] != null && sweets[i, j].cell != null)
                {
                    sweets[i, j].cell.GetComponent<FieldObject>().candyType = sweets[i, j].candyID;
                    sweets[i, j].cell.GetComponent<FieldObject>().column = j;
                    sweets[i, j].cell.GetComponent<FieldObject>().row = i;
                }
            }
        }
        //StartCoroutine(FindDuplicates(false, null, Direction.down));
    }
    public void OnCandyDeleted()
    {
        canContinue = true;
    }
    public void OnNothingToDelete(FieldCell obj, Direction dir)
    {
        ChangeCandiesPositions(false, obj, dir);
        candiesOperationsEnded();
    }
    public void OnGameWin()
    {
        StartCoroutine(UIController.GetComponent<UIController>().ShowMessage($"Your score: {scoreTxt.text}", true, global::UIController.ShowUI.gameWin, 3));
    }
    public void OnGameOver()
    {

    }

    private Task<bool> SetupLevel() // LEVEL INITIALIZATION (LOAD DATA: CANDYES, BLOCKS etc.)
    {
        blocks = new GameObject[levelData.levelInfo.fieldHeight, levelData.levelInfo.fieldWidth];
        sweets = new FieldCell[levelData.levelInfo.fieldHeight, levelData.levelInfo.fieldWidth];
        mods = new FieldCell[levelData.levelInfo.fieldHeight, levelData.levelInfo.fieldWidth];
        for (int i = 0; i < levelData.levelInfo.fieldHeight; i++)
        {
            for (int j = 0; j < levelData.levelInfo.fieldWidth; j++)
            {
                sweets[i, j] = new FieldCell();
                mods[i, j] = new FieldCell();
            }
        }
        screenSize = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        upperHeader.GetComponent<RectTransform>().GetWorldCorners(upperHeaderCorners);
        downHeader.GetComponent<RectTransform>().GetWorldCorners(downHeaderCorners);
        
        float centerY = upperHeaderCorners[0].y - fieldMarginTopBottom - ((upperHeaderCorners[0].y - fieldMarginTopBottom) + (Math.Abs(downHeaderCorners[1].y) - fieldMarginTopBottom)) / 2;
        float blockSize2 = (Math.Abs(upperHeaderCorners[0].y) + Math.Abs(downHeaderCorners[1].y) - 2 * fieldMarginTopBottom) / levelData.levelInfo.fieldHeight;
        float blockSize1 = (screenSize.x * 2 - fieldMarginLR * 2) / levelData.levelInfo.fieldWidth;
        blockSize = blockSize1 <= blockSize2 ? blockSize1 : blockSize2;
        CandiesDistance = blockSize;
        if (blockSize > 0.7f) blockSize = 0.7f;
        blockPrefab.transform.localScale = new Vector3(blockSize / blockPrefab.GetComponent<SpriteRenderer>().sprite.bounds.size.x,
            blockSize / blockPrefab.GetComponent<SpriteRenderer>().sprite.bounds.size.x, 0);
        candySize = new float[itemData.candies.Length];
        modSize = new float[itemData.other.Length];
        for (int i = 0; i < candySize.Length; i++) candySize[i] = blockSize / itemData.candies[i].itemSprite.bounds.size.x;
        for (int i = 0; i < modSize.Length; i++) modSize[i] = blockSize / itemData.other[i].itemSprite.bounds.size.x;
        float posX, posY;
        try
        {
            GameObject maskPlayField = Instantiate(new GameObject(), new Vector3(0, centerY, 65), new Quaternion(0, 0, 0, 0));
            maskPlayField.AddComponent(typeof(SpriteMask));
            maskPlayField.GetComponent<SpriteMask>().sprite = maskSprite;
            maskPlayField.transform.localScale = new Vector3(blockSize * levelData.levelInfo.fieldWidth * maskPlayField.transform.localScale.x / maskPlayField.GetComponent<SpriteMask>().sprite.bounds.size.x,
                blockSize * levelData.levelInfo.fieldHeight * maskPlayField.transform.localScale.y / maskPlayField.GetComponent<SpriteMask>().sprite.bounds.size.y, 1);
            
            levelTxt.text = levelData.levelInfo.levelNum.ToString();
            movesTxt.text = levelData.levelInfo.target[0].ToString();
            
            if (levelData.levelInfo.fieldWidth % 2 == 0) posX = -1 * (levelData.levelInfo.fieldWidth / 2 * blockSize - blockSize / 2);
            else posX = -1 * (levelData.levelInfo.fieldWidth / 2 * blockSize);

            if (levelData.levelInfo.fieldHeight % 2 == 0) posY = centerY + (levelData.levelInfo.fieldHeight / 2 * blockSize - blockSize / 2);
            else posY = centerY + (levelData.levelInfo.fieldHeight / 2 * blockSize);
            bool swicther = true;
            float memX = posX;
            firstPosX = posX;
            firstPosY = posY;

            for (int i = 0; i < levelData.levelInfo.fieldHeight; i++)
            {
                posX = memX;
                if (levelData.levelInfo.fieldWidth % 2 == 0) swicther = !swicther;
                for (int y = 0; y < levelData.levelInfo.fieldWidth; y++)
                {
                    foreach (var fieldBlock in levelData.levelInfo.savedBlock)
                    {
                        if(fieldBlock.x == i && fieldBlock.y == y)
                        {
                            if (fieldBlock.z != (int)Save.BlockID.empty && fieldBlock.z != (int)Save.BlockID.nil)
                            {
                                GameObject block = Instantiate(blockPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                                block.transform.parent = playFieldParent.transform;
                                if (swicther) block.GetComponent<SpriteRenderer>().color = levelData.levelInfo.levelColors[0];
                                else block.GetComponent<SpriteRenderer>().color = levelData.levelInfo.levelColors[1];
                                block.transform.localPosition = new Vector3(posX, posY, -10);
                                block.gameObject.name = $"block#{i}&{y}";
                                block.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                blocks[i, y] = block;
                                blocks[i, y].tag = "GameFieldObject";
                                blocks[i, y].AddComponent(typeof(FieldObject));
                                blocks[i, y].GetComponent<FieldObject>().column = y;
                                blocks[i, y].GetComponent<FieldObject>().row = i;
                                if (fieldBlock.z == (int)Save.BlockID.candy || fieldBlock.z == (int)Save.BlockID.ice)
                                {
                                    int ran = Save.Randomizer(0, itemData.candies.Length - 1);
                                    sweets[i, y].cell = Instantiate(new GameObject(), new Vector3(0, 0, 0), Quaternion.identity);
                                    sweets[i, y].blockID = Save.BlockID.candy;
                                    sweets[i, y].candyID = (Save.CandyType)ran;
                                    sweets[i, y].cell.AddComponent(typeof(BoxCollider));
                                    sweets[i, y].cell.AddComponent(typeof(SpriteRenderer));
                                    sweets[i, y].cell.AddComponent(typeof(FieldObject));
                                    sweets[i, y].cell.GetComponent<FieldObject>().row = i;
                                    sweets[i, y].cell.GetComponent<FieldObject>().column = y;
                                    sweets[i, y].cell.GetComponent<FieldObject>().candyType = sweets[i, y].candyID;
                                    sweets[i, y].cell.GetComponent<SpriteRenderer>().sprite = itemData.candies[ran].itemSprite;
                                    sweets[i, y].cell.transform.parent = playFieldParent.transform;
                                    sweets[i, y].cell.GetComponent<BoxCollider>().size = new Vector2(blockSize * sweets[i, y].cell.GetComponent<SpriteRenderer>().sprite.bounds.size.x * 1.7f, blockSize * sweets[i, y].cell.GetComponent<SpriteRenderer>().sprite.bounds.size.x * 1.7f);
                                    sweets[i, y].cell.transform.position = new Vector3(block.transform.position.x, block.transform.position.y, block.transform.position.z - 5f);
                                    sweets[i, y].cell.transform.localScale = new Vector3(candySize[ran] * 0.9f, candySize[ran] * 0.9f, candySize[ran] * 0.9f);
                                    sweets[i, y].cell.gameObject.name = $"candy[{i},{y}]";
                                    sweets[i, y].cell.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                }
                                else if(fieldBlock.z == (int)Save.BlockID.empty || fieldBlock.z == (int)Save.BlockID.nil)
                                {
                                    sweets[i, y].blockID = Save.BlockID.empty;
                                    sweets[i, y].candyID = Save.CandyType.nil;
                                    sweets[i, y].cell.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                    mods[i, y].blockID = Save.BlockID.empty;
                                    mods[i, y].cell.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                }
                                if(fieldBlock.z == (int)Save.BlockID.ice || fieldBlock.z == (int)Save.BlockID.block)
                                {
                                    mods[i, y].cell = Instantiate(new GameObject(), new Vector3(0, 0, 0), Quaternion.identity);
                                    mods[i, y].cell.AddComponent(typeof(BoxCollider));
                                    mods[i, y].cell.AddComponent(typeof(SpriteRenderer));
                                    mods[i, y].cell.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                                    if (fieldBlock.z == (int)Save.BlockID.block)
                                    {
                                        mods[i, y].cell.GetComponent<SpriteRenderer>().sprite = itemData.other[0].itemSprite;
                                        mods[i, y].cell.gameObject.name = $"rock[{i},{y}]";
                                        mods[i, y].blockID = Save.BlockID.block;
                                        sweets[i, y].blockID = Save.BlockID.nil;
                                        sweets[i, y].candyID = Save.CandyType.nil;
                                    }
                                    else if (fieldBlock.z == (int)Save.BlockID.ice)
                                    {
                                        mods[i, y].cell.GetComponent<SpriteRenderer>().sprite = itemData.other[1].itemSprite;
                                        mods[i, y].cell.gameObject.name = $"ice[{i},{y}]";
                                        mods[i, y].blockID = Save.BlockID.ice;
                                    }
                                    mods[i, y].cell.transform.parent = playFieldParent.transform;
                                    mods[i, y].cell.GetComponent<BoxCollider>().size = new Vector2(blockSize * mods[i, y].cell.GetComponent<SpriteRenderer>().sprite.bounds.size.x * 1.7f, blockSize * mods[i, y].cell.GetComponent<SpriteRenderer>().sprite.bounds.size.x * 1.7f);
                                    mods[i, y].cell.transform.position = new Vector3(block.transform.position.x, block.transform.position.y, block.transform.position.z - 10f);
                                    mods[i, y].cell.transform.localScale = new Vector3(modSize[0] * 0.93f, modSize[0] * 0.93f, modSize[0] * 0.93f);
                                }
                                swicther = !swicther;
                                posX += blockSize;
                            }
                            else
                            {
                                blocks[i, y] = null;
                                sweets[i, y].blockID = Save.BlockID.empty;
                                sweets[i, y].candyID = Save.CandyType.nil;
                                mods[i, y].blockID = Save.BlockID.empty;
                                swicther = !swicther;
                                posX += blockSize;
                            }
                            sweets[i, y].index = new FieldCell.Index(i, y);
                            mods[i, y].index = new FieldCell.Index(i, y);
                            if(sweets[i, y].cell != null) sweets[i, y].cell.tag = "GameFieldObject";
                            if (mods[i, y].cell != null) mods[i, y].cell.tag = "GameFieldObject";
                            break;
                        }
                    }
                }
                posY -= blockSize;
            }
            StartCoroutine(FindDuplicates(true, null, Direction.up));
            // SETUP GOALS IN HEADER

            int goalCount = 0;
            float fromX = 0, fromY = 0, candyHeaderSize = 0, distance = 5, baseValue;
            Vector3[] corners = new Vector3[4];
            goalHeader.GetComponent<RectTransform>().GetLocalCorners(corners);
            for (int i = 0; i < levelData.levelInfo.target.Length; i++) if (levelData.levelInfo.target[i] > 0) goalCount++;

            if (goalCount % 2 == 0)
            {
                if(goalCount < 4)
                {
                    candyHeaderSize = 27;
                    fromX = (goalCount / 2 - 1) * -candyHeaderSize * distance - distance / 2 - candyHeaderSize / 2;
                    fromY = 0;
                }
                else if(goalCount >= 4)
                {
                    candyHeaderSize = 21;
                    fromX = -3 / 2 * candyHeaderSize - (3 / 2 * distance);
                    fromY = distance / 2 + candyHeaderSize / 2;
                }
            }
            else if (goalCount % 2 == 1)
            {
                if (goalCount < 4)
                {
                    candyHeaderSize = 27;
                    fromX = -goalCount / 2 * candyHeaderSize - (goalCount / 2 * distance);
                    fromY = 0;
                }
                else if (goalCount >= 4)
                {
                    candyHeaderSize = 21;
                    fromX = -3 / 2 * candyHeaderSize - (3 / 2 * distance);
                    fromY = distance / 2 + candyHeaderSize / 2;
                }
            }
            baseValue = fromX;
            int counter = 0;
            for(int i = 0; i < levelData.levelInfo.target.Length; i++)
            {
                if(levelData.levelInfo.target[i] > 0)
                {
                    counter++;
                    var goal = Instantiate(goalCandyPrefab, Vector3.zero, Quaternion.identity);
                    goal.transform.parent = goalHeader.transform;
                    goal.transform.Find("TargetScore").GetComponent<TextMeshProUGUI>().text = levelData.levelInfo.target[i].ToString();
                    goal.GetComponent<Image>().sprite = itemData.candies[i].itemSprite;
                    goal.GetComponent<RectTransform>().sizeDelta = new Vector2(candyHeaderSize, candyHeaderSize);
                    goal.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
                    goal.GetComponent<RectTransform>().localPosition = new Vector3(fromX, fromY, 0);
                    targetInfo.Add((Save.CandyType)i, new GoalInfo {
                        targetCount = levelData.levelInfo.target[i],
                        targetCountText = goal.transform.Find("TargetScore").GetComponent<TextMeshProUGUI>()
                    });
                    fromX += distance + candyHeaderSize;
                    if (counter == 3)
                    {
                        fromY -= distance + candyHeaderSize;
                        fromX = baseValue;
                    }
                }
            }
            return Task.FromResult(true);
        }
        catch(Exception e)
        {
            Debug.Log($"Level setup failed: {e.ToString()}");
            return Task.FromResult(false);
        }
    }
    
    public enum FaderFunctions
    {
        toBlackAndBack = 1,
        fromBlack = 2,
        imperfectBlack = 3,
        fromImperfectBlack = 4,
        toBlack = 5
    }
    /// <summary>
    /// Fader functionality. You can fade screen and call function which you pass and select fader mode.
    /// </summary>
    /// <param name="function"></param>
    /// <param name="functionIndex"></param>
    /// <returns></returns>
    public static IEnumerator GameSceneFader(Menu.FaderMethods function, GameController.FaderFunctions functionIndex)
    {
        switch (functionIndex)
        {
            case FaderFunctions.toBlackAndBack:
                fader.gameObject.SetActive(true);
                while (fader.GetComponent<Image>().color.a < 1)
                {
                    fader.GetComponent<Image>().color = new Color(0, 0, 0, fader.GetComponent<Image>().color.a + 0.05f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                function?.Invoke();
                while (fader.GetComponent<Image>().color.a > 0)
                {
                    fader.GetComponent<Image>().color = new Color(0, 0, 0, fader.GetComponent<Image>().color.a - 0.05f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                fader.gameObject.SetActive(false);
                yield return 0;
                break;
            case FaderFunctions.fromBlack:
                fader.gameObject.SetActive(true);
                fader.GetComponent<Image>().color = new Color(0, 0, 0, 1);
                while (fader.GetComponent<Image>().color.a > 0)
                {
                    fader.GetComponent<Image>().color = new Color(0, 0, 0, fader.GetComponent<Image>().color.a - 0.05f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                fader.gameObject.SetActive(false);
                function?.Invoke();
                break;
            case FaderFunctions.imperfectBlack:
                fader.gameObject.SetActive(true);
                fader.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                while (fader.GetComponent<Image>().color.a < 0.25f)
                {
                    fader.GetComponent<Image>().color = new Color(0, 0, 0, fader.GetComponent<Image>().color.a + 0.025f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                break;
            case FaderFunctions.fromImperfectBlack:
                fader.gameObject.SetActive(true);
                fader.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);
                while (fader.GetComponent<Image>().color.a > 0)
                {
                    fader.GetComponent<Image>().color = new Color(0, 0, 0, fader.GetComponent<Image>().color.a - 0.025f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                fader.gameObject.SetActive(false);
                break;
            case FaderFunctions.toBlack:
                fader.gameObject.SetActive(true);
                fader.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                while (fader.GetComponent<Image>().color.a < 1)
                {
                    fader.GetComponent<Image>().color = new Color(0, 0, 0, fader.GetComponent<Image>().color.a + 0.05f);
                    yield return new WaitForSeconds(0.0001f * Time.deltaTime);
                }
                function?.Invoke();
                break;
        }
    }
    [Serializable]
    public class FieldCell
    {
        public Index index;
        public Save.BlockID blockID;
        public Save.CandyType candyID;
        public GameObject cell;

        public class Index
        {
            public float i, j;
            public Index(int i, int j)
            {
                this.i = i;
                this.j = j;
            }
        }
    }
    [Serializable]
    public class ItemData
    {
        public Blocks[] candies;
        public Blocks[] other;

        public ItemData(Blocks[] candies, Blocks[] other)
        {
            this.candies = candies;
            this.other = other;
        }
    }
    [Serializable]
    private class GoalInfo
    {
        public int targetCount;
        public TextMeshProUGUI targetCountText;
    }
    [Serializable]
    public class Mapping
    {
        internal List<Item> block;
        public class Item { public int i, j; }
    }
}
public interface IGameController
{
    void OnCandyProcessesStarts();
    void OnCandyProcessesEnded();
    void OnNothingToDelete(GameController.FieldCell obj, GameController.Direction dir);
    void OnCandyDeleted();
    void OnGameWin();
    void OnGameOver();
}
