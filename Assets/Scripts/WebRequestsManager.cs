using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class WebRequestsManager : MonoBehaviour
{
    [SerializeField] private AppConfig config;
    [SerializeField] private WebSocketManager webSocketManager;

    private string tokenValue;

    [System.Serializable]
    private class ActivationResponse
    {
        public string Token;
    }

    [System.Serializable]
    private class PrintRequest
    {
        public string Timestamp;
        public string Text;
    }

    [System.Serializable]
    public class LogRequest
    {
        public string RobotId;
        public string Timestamp;
        public string LogLevel;
        public string Message;
    }

    public void ConnectionSetup()
    {
        Debug.Log("Pokrenut ConnectionSetup");
        StartCoroutine(PostActivationCoroutine(config.baseUrl + "/" + config.robotId + "/activate"));
    }

    private IEnumerator PostActivationCoroutine(string url)
    {
        Debug.Log("Pokrenut PostActivation");
        var request = new UnityWebRequest(url, "POST");

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Connection", "keep-alive");
        request.SetRequestHeader("Api-Key", config.apiKey);

        string bodyString = "{\"SSID\":\"fer\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyString);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);  //za slanje
        request.downloadHandler = new DownloadHandlerBuffer(); //za primanje

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            ActivationResponse parsed = JsonUtility.FromJson<ActivationResponse>(request.downloadHandler.text);
            tokenValue = parsed.Token;
            webSocketManager.OpenSocketConnection(tokenValue);

        }
        else
        {
            Debug.LogError(request.error);
        }
    }

    public void PostOutput(string printText)
    {
        StartCoroutine(PostOutputCoroutine(printText));
    }

    public IEnumerator PostOutputCoroutine(string printText)
    {
        string url = config.baseUrl + "/" + config.robotId + "/print";
        Debug.Log("Pokrenut PostOutput");
        var request = new UnityWebRequest(url, "POST");

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Connection", "keep-alive");
        request.SetRequestHeader("Token", tokenValue);

        string isoTimestamp = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        PrintRequest body = new PrintRequest
        {
            Timestamp = isoTimestamp,
            Text = printText
        };
        string bodyString = JsonUtility.ToJson(body);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyString);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);  //za slanje
        request.downloadHandler = new DownloadHandlerBuffer(); //za primanje

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Print message sent");
        }
        else
        {
            Debug.LogError(request.error);
        }
    }

    public void PostLog(LogRequest entry)
    {
        StartCoroutine(PostLogCoroutine(entry));
    }

    private IEnumerator PostLogCoroutine(LogRequest entry)
    {
        string url = config.baseUrl + "/" + config.robotId + "/log";
        Debug.Log("Pokrenut PostLog");
        var request = new UnityWebRequest(url, "POST");

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Connection", "keep-alive");
        request.SetRequestHeader("Token", tokenValue);

        string bodyString = JsonUtility.ToJson(entry);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(bodyString);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);  //za slanje
        request.downloadHandler = new DownloadHandlerBuffer(); //za primanje

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Log sent");
        }
        else
        {
            Debug.LogError(request.error);
        }
    }

    public void PostDeactivate()
    {
        StartCoroutine(PostDeactivateCoroutine());
    }

    private IEnumerator PostDeactivateCoroutine()
    {
        string url = config.baseUrl + "/" + config.robotId + "/deactivate";
        Debug.Log("Pokrenut deactivate");
        var request = new UnityWebRequest(url, "POST");

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Connection", "keep-alive");
        request.SetRequestHeader("Token", tokenValue);


        request.uploadHandler = new UploadHandlerRaw(null);  //za slanje
        request.downloadHandler = new DownloadHandlerBuffer(); //za primanje

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("deactivation message sent");
        }
        else
        {
            Debug.LogError(request.error);
        }
    }
}
