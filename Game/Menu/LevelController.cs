using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;

public class LevelController : MonoBehaviour {

    [HideInInspector]
    private LevelManager manager;
    [HideInInspector]
    public int levelID;
    private RectTransform allObjAttachedToStars;
    private GameObject[] stars = new GameObject[3];

    public void OnMouseUpAsButton()
    {
        try { manager = FindObjectOfType<LevelManager>(); }
        catch { }
        finally { manager.LevelSelected(this.levelID); }
    }
    public void SetStars(int activeStars)
    {
        RectTransform[] parent = gameObject.GetComponentsInChildren<RectTransform>();
        
        foreach(RectTransform obj in parent) for (int i = 0; i < 3; i++)
                if (obj.gameObject.name == $"#star{i + 1}complete") stars[i] = obj.gameObject;

        for(int i = 0; i < 3; i++)
        {
            stars[i].gameObject.SetActive(false);
            if (i < activeStars) stars[i].gameObject.SetActive(true);
        }
    }
}
