using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContextMovement : MonoBehaviour
{
    public ContextSteerer steerer;

    public float movementSpeed;

    public float WieghtUpdateFrequency;

    private void OnEnable()
    {
        StartCoroutine(UpdateWeights());
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = transform.position + steerer.Direction.ToVector3() * Time.deltaTime * movementSpeed;
    }

    IEnumerator UpdateWeights()
    {
        yield return new WaitForEndOfFrame();
        while (true)
        {
            steerer.UpdateWeights();
            yield return new WaitForSeconds(WieghtUpdateFrequency);
        }
    }
}
