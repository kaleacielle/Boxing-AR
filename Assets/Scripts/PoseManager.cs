using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PoseManager : MonoBehaviour
{
    [Header("Pose Database")]
    public PoseData[] poses;

    [Header("UI")]
    public Image poseImage;
    public TMP_Text poseNameText;

    private PoseData currentPose;

    void Start()
    {
        ShowRandomPose();
    }

    public void ShowRandomPose()
    {
        if (poses.Length == 0)
            return;

        currentPose = poses[Random.Range(0, poses.Length)];

        poseImage.sprite = currentPose.poseSprite;
        poseNameText.text = currentPose.poseName;
    }

    public PoseData GetCurrentPose()
    {
        return currentPose;
    }
}