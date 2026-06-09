using UnityEngine;
using NativeWebSocket;
using TMPro;

public class WebSocketManager : MonoBehaviour
{
    [SerializeField] private AppConfig config;
    [SerializeField] private CommandParser commandParser;
    [SerializeField] private WebRequestsManager webRequestsManager;
    WebSocket webSocket;
    [System.Serializable]
    private class ResponseJson
    {
        public string CommandType;
        public string CodeText;
    }

    async public void OpenSocketConnection(string tokenValue)
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>
        {
            {"Token", tokenValue}
        };
        webSocket = new WebSocket(config.wsUrl + "/" + config.robotId + "/commands", headers);

        webSocket.OnOpen += () => Debug.Log("Connection open!");
        webSocket.OnError += (e) => Debug.Log("Error! " + e);
        webSocket.OnClose += (code) => Debug.Log("Connection closed!");

        webSocket.OnMessage += async (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            ResponseJson parsed = JsonUtility.FromJson<ResponseJson>(message);
            Debug.Log("Received: " + parsed.CodeText);
            if (parsed.CommandType == "SHUTDOWN")
            {
                webRequestsManager.PostDeactivate();
                await webSocket.Close();
            }
            else if (parsed.CommandType == "ABORT")
            {
                commandParser.StopInterpreter();
            }
            else
            {
                commandParser.ParseCommands(parsed.CodeText);
            }

        };

        await webSocket.Connect();
    }

    private async void OnApplicationQuit()

    {
        if (webSocket != null) await webSocket.Close();

    }

}