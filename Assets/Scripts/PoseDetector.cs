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
        if (!UDPReceiver.bodyDetected)
        {
            CurrentPose = BoxingPose.None;
            return;
        }

        DetectPose();
    }

    void DetectPose()
    {
        //--------------------------------------------------
        // GUARD
        //--------------------------------------------------

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

            return;
        }

        //--------------------------------------------------
        // LEAD JAB
        //--------------------------------------------------

        float leftExtension =
            Mathf.Abs(
                UDPReceiver.leftWrist.x -
                UDPReceiver.leftShoulder.x
            );

        float rightDistance =
            Vector2.Distance(
                UDPReceiver.rightWrist,
                UDPReceiver.rightShoulder
            );

        bool leadJab =
            leftExtension > 0.20f &&
            rightDistance < 0.15f;

        if (leadJab)
        {
            if (CurrentPose != BoxingPose.LeadJab)
            {
                CurrentPose = BoxingPose.LeadJab;
                Debug.Log("🥊 LEAD JAB");
            }

            return;
        }

        //--------------------------------------------------
        // NOTHING DETECTED
        //--------------------------------------------------

        CurrentPose = BoxingPose.None;
    }
}