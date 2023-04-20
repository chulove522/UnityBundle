using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ConnectServer : MonoBehaviour
{
    void Start()
    {
        string uri = "http://18.180.240.3:8180/api/echo";  //method: get/post

        /*
        string[] pages = uri.Split('/');
        int page = pages.Length - 1;


        UnityWebRequest webRequest = UnityWebRequest.Get(uri);
        switch (webRequest.result) {
            case UnityWebRequest.Result.ConnectionError:
            case UnityWebRequest.Result.DataProcessingError:
                Debug.LogError(pages[page] + ": Error: " + webRequest.error);
                break;
            case UnityWebRequest.Result.ProtocolError:
                Debug.LogError(pages[page] + ": HTTP Error: " + webRequest.error);
                break;
            case UnityWebRequest.Result.Success:
                Debug.Log(pages[page] + ":\nReceived: " + webRequest.downloadHandler.text);
                break;
        }*/

    }
    private void echo() {


    }
}
