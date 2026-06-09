using System.Collections;
using UnityEngine;

public class CanvasSpawner : MonoBehaviour
{
    [SerializeField] private Transform centerEyeAnchor;
    void Start()
    {
        StartCoroutine(SpawnAndDetach());
    }

    IEnumerator SpawnAndDetach()
    {
        while (centerEyeAnchor.position == Vector3.zero)
        {
            yield return null;
        }

        yield return null;

        // x - desno, y -gore, z - naprijed
        Vector3 offset = new Vector3(-0.55f, -0.55f, 0.95f);
        transform.position = centerEyeAnchor.position + centerEyeAnchor.rotation * offset;
        transform.rotation = Quaternion.LookRotation(transform.position - centerEyeAnchor.position);

    }

}
