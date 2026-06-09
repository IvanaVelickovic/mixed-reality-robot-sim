using UnityEngine;
[CreateAssetMenu(fileName = "AppConfig", menuName = "Config/AppConfig")]
public class AppConfig : ScriptableObject
{
    public string robotId;
    public string apiKey;
    public string baseUrl;
    public string wsUrl;
}
