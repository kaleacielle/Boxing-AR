using UnityEngine;

public class PoseComparer : MonoBehaviour
{
    public int ComparePose(string expectedPose)
    {
        if (UDPReceiver.latestPose == expectedPose)
        {
            return 100;
        }

        return 0;
    }

    public string GetResult(int score)
    {
        if (score == 100)
            return "PERFECT!";

        return "MISS!";
    }
}