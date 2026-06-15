using UnityEngine;

[CreateAssetMenu(fileName = "New Pose", menuName = "Pose Match/Pose Data")]
public class PoseData : ScriptableObject
{
    public string poseName;
    public Sprite poseSprite;

    [TextArea]
    public string description;
}