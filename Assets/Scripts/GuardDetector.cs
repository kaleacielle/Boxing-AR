using UnityEngine;

public class GuardDetector : MonoBehaviour
{
    private bool guardDetected = false;

    void Update()
    {
        if (!UDPReceiver.bodyDetected)
            return;

        bool leftGuard =
            UDPReceiver.leftWrist.y < UDPReceiver.leftShoulder.y;

        bool rightGuard =
            UDPReceiver.rightWrist.y < UDPReceiver.rightShoulder.y;

        bool inGuard = leftGuard && rightGuard;

        if (inGuard != guardDetected)
        {
            guardDetected = inGuard;

            if (guardDetected)
            {
                Debug.Log(" GUARD DETECTED");
            }
            else
            {
                Debug.Log(" Hands Down");
            }
        }
    }
}