using UnityEngine;

public class BodyDetector : MonoBehaviour
{
    private bool previousState = false;

    void Update()
    {
        if (UDPReceiver.bodyDetected != previousState)
        {
            previousState = UDPReceiver.bodyDetected;

            if (previousState)
            {
                Debug.Log("PLAYER DETECTED");
            }
            else
            {
                Debug.Log("PLAYER LOST");
            }
        }
    }
}