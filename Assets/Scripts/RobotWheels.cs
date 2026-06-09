using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotWheels : MonoBehaviour
{
    [SerializeField] private GameObject[] wheels;
    public float wheelRotationSpeed = 500f;

    public void RotateWheels(float movementSpeed)
    {
        float rotationAmount = movementSpeed * wheelRotationSpeed * Time.deltaTime;

        foreach (GameObject wheel in wheels)
        {
            if (wheel != null)
            {
                wheel.transform.Rotate(Vector3.down * rotationAmount);
            }
        }
    }

    public void RotateWheelsInPlace(float rotationDirection)
    {
        foreach (GameObject wheel in wheels)
        {
            wheel.transform.Rotate(Vector3.right * rotationDirection * wheelRotationSpeed * Time.deltaTime);
        }
    }
}
