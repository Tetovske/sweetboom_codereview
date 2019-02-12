using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ApiAiSDK;
using ApiAiSDK.Model;
using ApiAiSDK.Unity;
using fastJSON;
using System;
using UnityEngine.UI;

public class Assistant : MonoBehaviour {

    ApiAiUnity apiAiUnity;
    AudioSource audio;

    private void Awake()
    {
        

        Application.RequestUserAuthorization(UserAuthorization.Microphone);
        const string ACCESS_TOKEN = "37dd216cc86047d282318730c81db3a7";
        var config = new AIConfiguration(ACCESS_TOKEN, SupportedLanguage.Russian);
        apiAiUnity = new ApiAiUnity();
        apiAiUnity.Initialize(config);

        apiAiUnity.OnResult += HandleOnResult;
        apiAiUnity.OnError += HandleOnError;
    }
    private void Start()
    {
        audio = GetComponent<AudioSource>();
        
        audio.Play();
        foreach (string d in Microphone.devices)
        {
            Debug.Log(d);
        }
    }

    private void OnMouseDown()
    {
        gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1);
        try
        {
            audio.clip = Microphone.Start(null, false, 10, 44100);
            apiAiUnity.StartListening(audio);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    private void OnMouseUp()
    {
        gameObject.GetComponent<Image>().color = new Color(1, 1, 1, 1);
        try
        {
            apiAiUnity.StopListening();
            //audio.clip = Microphone.End();
            audio.Play();
            Debug.Log("sdhflasdh");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
    void HandleOnResult(object sender, AIResponseEventArgs e)
    {
        var aiResponse = e.Response;
        if (aiResponse != null)
        {
            // get data from aiResponse
        }
        else
        {
            Debug.LogError("Response is null");
        }
    }

    void HandleOnError(object sender, AIErrorEventArgs e)
    {
        Debug.LogException(e.Exception);
    }

    private void ApiAiUnity_OnError(object sender, AIErrorEventArgs e)
    {
        
    }
}
