using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AboutUs : EditorWindow {

    Texture2D background;
    Rect backRect;
    Color backColor = new Color(82f/255f, 82f/255f, 86f/255f, 1);


    [MenuItem("Dudle/About us.")]
    public static void Window()
    {
        EditorWindow aboutWindow = GetWindow<AboutUs>("About us");
        aboutWindow.minSize = new Vector2(400, 500);
        aboutWindow.maxSize = new Vector2(400, 500);
    }
    private void OnEnable()
    {
        Init();
    }
    void Init()
    {
        background = new Texture2D(1, 1);
        background.SetPixel(0, 0, backColor);
        backRect.x = 0;
        backRect.y = 170;
        backRect.width = Screen.width;
        backRect.height = 100;
        background.Apply();
    }
    private void OnGUI()
    {
        Texture2D logo = Resources.Load<Texture2D>("Dudle_logo");
        GUILayout.Label(logo);

        GUI.DrawTexture(backRect, background);

        DrawAboutZone();

    }
    void DrawAboutZone()
    {
        GUILayout.BeginArea(backRect);

        GUILayout.Label("");
        


        GUILayout.EndArea();
    }
}
