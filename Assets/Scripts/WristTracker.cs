using UnityEngine;

public class WristTracker : MonoBehaviour
{
    void Update()
    {
        Vector2 wrist = UDPReceiver.wristPosition;

        transform.position = new Vector3(
            wrist.x * 10f,
            wrist.y * 5f,
            0f   
        );
        Debug.Log(UDPReceiver.wristPosition);
    }
}