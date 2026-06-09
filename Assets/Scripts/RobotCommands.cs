using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RobotCommands : MonoBehaviour
{
    private GameObject spawnedObject;
    private GameObject display;
    private TextMeshProUGUI displayText;
    private RobotWheels robotWheels;
    private Image displayBackground;

    private Dictionary<int, string> objectIndexes = new Dictionary<int, string>()
    {
        // animals
        { 0, "bird" },
        { 12, "bear" },
        { 25, "dog" },
        { 27, "elephant" },
        { 28, "zebra" },
        { 36, "giraffe" },
        { 38, "horse" },
        { 41, "cat" },
        { 51, "sheep" },
        { 53, "cow" },
        // other objects
        { 7, "bench" },
        { 10, "person" },
        { 11, "sumske_zivotinje" },
        { 22, "donut" },
        { 30, "fence_green" },
        { 33, "ljubimci" },
        { 35, "safari" },
        { 52, "farma" },
        { 56, "restaurant_zoo" },
        { 63, "cellphone" },

    };

    private int currDirection;
    private int currIndex;

    public void SetSpawnedObject(GameObject spawned)
    {
        spawnedObject = spawned;
        currDirection = 90;
        currIndex = 16;
        robotWheels = spawnedObject.GetComponent<RobotWheels>();
        DisplayClear();
    }

    public void SetDisplay(GameObject myDisplay)
    {
        display = myDisplay;
        displayText = display.GetComponentInChildren<TextMeshProUGUI>();
        displayBackground = display.GetComponent<Image>();
    }

    public IEnumerator Forward()
    {
        yield return StartCoroutine(MoveInDirection(1));
    }

    public IEnumerator Back()
    {
        yield return StartCoroutine(MoveInDirection(-1));
    }

    public IEnumerator TurnLeft()
    {
        yield return StartCoroutine(RotateObject(-90));
    }

    public IEnumerator TurnRight()
    {
        yield return StartCoroutine(RotateObject(90));
    }

    IEnumerator MoveInDirection(int direction)
    {
        currIndex = calculateIndex(direction);
        float totalDistance = 0.25f;
        float duration = 1f;
        float elapsed = 0f;
        float speed = totalDistance / duration;

        while (elapsed < duration)
        {
            spawnedObject.transform.Translate(0, 0, speed * Time.deltaTime * direction, Space.Self);
            if (robotWheels != null) robotWheels.RotateWheels(speed * direction);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator RotateObject(int angle)
    {
        calculateDirection(angle);
        float elapsed = 0f;
        float duration = 1f;
        Quaternion startRotation = spawnedObject.transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, angle, 0);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            spawnedObject.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, t);
            //if (robotWheels != null) robotWheels.RotateWheelsInPlace(Mathf.Sign(angle));
            yield return null;
        }
        spawnedObject.transform.rotation = targetRotation;
    }

    public void DisplayColor(string color)
    {
        if (color == "green")
        {
            displayBackground.color = Color.green;
        }
        else if (color == "red")
        {
            displayBackground.color = Color.red;
        }
    }

    public void DisplayClear()
    {
        displayBackground.color = Color.white;
        displayText.text = "";
    }

    public void DisplayText(string output)
    {
        displayText.text = output;
    }

    private void calculateDirection(int angle)
    {
        currDirection -= angle;
        if (currDirection < 0)
        {
            currDirection += 360;
        }
        else if (currDirection >= 360)
        {
            currDirection -= 360;
        }

    }

    private int calculateIndex(int direction)
    {
        switch (currDirection)
        {
            case 0:
                return currIndex - 8 * direction;
            case 90:
                return currIndex + 1 * direction;
            case 180:
                return currIndex + 8 * direction;
            case 270:
                return currIndex - 1 * direction;
            default:
                return 0;
        }
    }

    public string DetectObject()
    {
        int indexInfront = calculateIndex(1);
        if (objectIndexes.ContainsKey(indexInfront))
        {
            return objectIndexes[indexInfront];
        }
        return "";

    }

    public (string, double) DetectObjectConf()
    {
        int indexInfront = calculateIndex(1);
        if (objectIndexes.ContainsKey(indexInfront))
        {
            return (objectIndexes[indexInfront], 1.0);
        }
        return ("", 1.0);

    }
}