using UnityEngine;
using TMPro;

public class PoseGameManager : MonoBehaviour
{
    public PoseManager poseManager;
    public PoseComparer poseComparer;

    public TMP_Text scoreText;
    public TMP_Text resultText;

    public void CheckPose()
    {
        string targetPose = poseManager.GetCurrentPose().poseName;

        int score = poseComparer.ComparePose(targetPose);

        scoreText.text = score + "%";
        resultText.text = poseComparer.GetResult(score);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckPose();
        }
    }
}