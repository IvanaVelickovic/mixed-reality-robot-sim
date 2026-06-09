using UnityEngine;
using Meta.XR.MRUtilityKit;
using Meta.XR;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class SceneCollision : MonoBehaviour
{
    [SerializeField] private OVRInput.Button button = OVRInput.Button.One;
    [SerializeField] private Transform rayStartPoint;
    [SerializeField] private float rayLength = 5;
    [SerializeField] private EnvironmentRaycastManager rayManager;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private GameObject objectPrefab;
    [SerializeField] private RobotCommands robotCommands;
    [SerializeField] private Canvas myCanvas;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private GameObject togglePrefab;
    [SerializeField] private GameObject mapPrefab;
    private GameObject spawnedObject;
    private int calibrationStep = 0;
    private Vector3 pointOrigin;
    private Vector3 pointForward;

    private bool connection = false;
    private GameObject map;
    private GameObject display;
    private Toggle activeToggle;
    private GameObject toggleObj;
    private Vector3 savedMapPosition;
    private Quaternion savedMapRotation;
    [SerializeField] private WebRequestsManager webRequestsManager;

    void Update()
    {
        if (OVRInput.GetDown(button))
        {
            Ray ray = new Ray(rayStartPoint.position, rayStartPoint.forward);
            MRUKRoom room = MRUK.Instance.GetCurrentRoom();
            MRUKAnchor floorAnchor = room?.FloorAnchors?.FirstOrDefault();

            bool hasHit = rayManager.Raycast(ray, out var hit, rayLength);

            if (hasHit)
            {
                float floorY = floorAnchor != null
                    ? floorAnchor.transform.position.y
                    : hit.point.y;

                //Vector3 currentHitPoint = new Vector3(hit.point.x, floorY + 0.01f, hit.point.z);
                Vector3 currentHitPoint = new Vector3(hit.point.x, floorY + 0.06f, hit.point.z);

                if (calibrationStep == 0)
                {
                    pointOrigin = currentHitPoint;

                    if (spawnedObject == null)
                    {
                        spawnedObject = Instantiate(objectPrefab);
                        robotCommands.SetSpawnedObject(spawnedObject);
                    }

                    spawnedObject.transform.position = currentHitPoint;
                }
                else if (calibrationStep == 1)
                {
                    pointForward = currentHitPoint;
                    FinalizeCalibration();
                }
            }
        }

    }

    private void FinalizeCalibration()
    {
        if (spawnedObject == null) return;

        // računanje vektora koji pokazuje prema naprijed
        Vector3 forwardDir = (pointForward - pointOrigin).normalized;

        forwardDir.y = 0;
        forwardDir.Normalize(); //normalizacija

        //primjena rotacije
        spawnedObject.transform.rotation = Quaternion.LookRotation(forwardDir, Vector3.up);

        //savedMapPosition = spawnedObject.transform.position + new Vector3(0, -0.047f, 0);
        savedMapPosition = spawnedObject.transform.position + new Vector3(0, -0.01f, 0);
        savedMapRotation = spawnedObject.transform.rotation;
    }

    public void OnButtonClick()
    {
        if (calibrationStep == 0)
        {
            calibrationStep = 1;
            buttonText.text = "Set Direction";
            Debug.Log("Confirm clicked");
        }
        else if (calibrationStep == 1)
        {
            calibrationStep = 2;
            buttonText.text = "Restart";

            //spawnanje displaya
            display = Instantiate(messagePrefab, myCanvas.transform);

            //spawnanje togglea
            toggleObj = Instantiate(togglePrefab, myCanvas.transform);
            activeToggle = toggleObj.GetComponentInChildren<Toggle>();

            if (activeToggle != null)
            {
                activeToggle.onValueChanged.AddListener(OnToggleChanged);
            }

            //spawnanje mape 
            map = Instantiate(mapPrefab, savedMapPosition, savedMapRotation);

            robotCommands.SetDisplay(display);

            if (connection == false)
            {
                webRequestsManager.ConnectionSetup();
                connection = true;
            }

        }
        else if (calibrationStep == 2)
        {
            // STARI KOD - destroy objekta
            //calibrationStep = 0;
            //buttonText.text = "Confirm Position";
            //if (spawnedObject != null) Destroy(spawnedObject);
            //if (map != null) Destroy(map);
            //if (display != null) Destroy(display);
            //if (toggleObj != null) Destroy(toggleObj);

            if (spawnedObject != null) Destroy(spawnedObject);
            Vector3 spawnedObjectPosition = savedMapPosition + new Vector3(0, 0.052f, 0);
            spawnedObject = Instantiate(objectPrefab, spawnedObjectPosition, savedMapRotation);
            robotCommands.SetSpawnedObject(spawnedObject);
        }
    }

    private void OnToggleChanged(bool status)
    {
        if (map != null)
        {
            map.SetActive(status);
        }
    }
}
