using UnityEngine;
using TMPro;

public class PoseGameManager : MonoBehaviour
{
    public PoseManager poseManager;
    public PoseComparer poseComparer;

    public TMP_Text scoreText;
    public TMP_Text resultText;

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            CheckPose();
        }
    }

    public void CheckPose()
    {
        int score = poseComparer.ComparePose();

        scoreText.text = score + "%";
        resultText.text = poseComparer.GetResult(score);
    }
}