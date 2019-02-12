using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossSceneController : MonoBehaviour
{
    public static GameObject Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this.gameObject;
            Save.Init();
        }
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(Instance);
    }
}
