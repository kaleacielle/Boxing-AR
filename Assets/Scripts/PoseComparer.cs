using UnityEngine;

public class PoseComparer : MonoBehaviour
{
    [Range(50,100)]
    public int minimumScore = 60;

    [Range(50,100)]
    public int maximumScore = 100;

    public int ComparePose()
    {
        return Random.Range(minimumScore, maximumScore + 1);
    }

    public string GetResult(int score)
    {
        if(score >= 90)
            return "PERFECT!";

        if(score >= 75)
            return "GOOD!";

        if(score >= 60)
            return "OK!";

        return "MISS!";
    }
}