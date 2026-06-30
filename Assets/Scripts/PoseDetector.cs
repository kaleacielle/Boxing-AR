using UnityEngine;

public enum BoxingPose
{
    None,
    Guard,
    LeadJab,
    ComboPunch
}

public class PoseDetector : MonoBehaviour
{
    public BoxingPose CurrentPose = BoxingPose.None;

    void Update()
    {
        DetectGuard();
    }

    void DetectGuard()
    {
        if (!UDPReceiver.bodyDetected)
        {
            CurrentPose = BoxingPose.None;
            return;
        }

        bool leftGuard =
            UDPReceiver.leftWrist.y <
            UDPReceiver.leftShoulder.y;

        bool rightGuard =
            UDPReceiver.rightWrist.y <
            UDPReceiver.rightShoulder.y;

        if (leftGuard && rightGuard)
        {
            if (CurrentPose != BoxingPose.Guard)
            {
                CurrentPose = BoxingPose.Guard;
                Debug.Log("🥊 GUARD");
            }
        }
        else
        {
            CurrentPose = BoxingPose.None;
        }
    }
}