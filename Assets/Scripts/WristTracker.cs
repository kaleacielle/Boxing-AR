using UnityEngine;

public class WristTracker : MonoBehaviour
{
    public bool isLeftHand = false;

    void Update()
    {
        Vector2 wrist = isLeftHand
            ? UDPReceiver.leftWrist
            : UDPReceiver.rightWrist;

        transform.position = new Vector3(
            wrist.x * 10f,
            wrist.y * 5f,
            0f
        );
    }
}