using UnityEngine;
using System.Collections;

public class ErrorLogger : MonoBehaviour
{
    [SerializeField] private WebRequestsManager webRequestsManager;
    [SerializeField] private AppConfig config;

    public enum Level { ERROR, WARNING, DEBUG }

    public void Log(Level level, string message)
    {
        var entry = new WebRequestsManager.LogRequest
        {
            RobotId = config.robotId,
            Timestamp = System.DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            LogLevel = level.ToString(),
            Message = message
        };
        webRequestsManager.PostLog(entry);
    }
}