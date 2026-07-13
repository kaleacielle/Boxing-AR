using UnityEngine;

public class PoseComparisonManager : MonoBehaviour
{
    [Header("References")]
    public PlayerSkeletonRenderer player;
    public CoachSkeletonRenderer coach;

    [Header("Settings")]
    [Range(10f, 300f)]
    public float perfectDistance = 60f;

    public float currentScore;

    public string worstJoint;

    public UIManager uiManager;

    void Update()
    {
        if (player == null || coach == null)
            return;

        float head = Compare(player.Head, coach.Head);
        float leftShoulder = Compare(player.LeftShoulder, coach.LeftShoulder);
        float rightShoulder = Compare(player.RightShoulder, coach.RightShoulder);
        float leftElbow = Compare(player.LeftElbow, coach.LeftElbow);
        float rightElbow = Compare(player.RightElbow, coach.RightElbow);
        float leftWrist = Compare(player.LeftWrist, coach.LeftWrist);
        float rightWrist = Compare(player.RightWrist, coach.RightWrist);

        currentScore =
            (head +
            leftShoulder +
            rightShoulder +
            leftElbow +
            rightElbow +
            leftWrist +
            rightWrist) / 7f;

        float[] scores =
        {
            head,
            leftShoulder,
            rightShoulder,
            leftElbow,
            rightElbow,
            leftWrist,
            rightWrist
        };

        string[] names =
        {
            "Head",
            "Left Shoulder",
            "Right Shoulder",
            "Left Elbow",
            "Right Elbow",
            "Left Wrist",
            "Right Wrist"
        };

        int worst = 0;

        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] < scores[worst])
                worst = i;
        }

        worstJoint = names[worst];
        if (uiManager != null)
{
        uiManager.SetPoseScore(currentScore);
        uiManager.SetFeedback(GetFeedback());
        uiManager.SetHint(worstJoint);
    }
        }

    float Compare(Vector2 playerJoint, Vector2 coachJoint)
    {
        float distance = Vector2.Distance(playerJoint, coachJoint);

        float score = Mathf.Clamp01(1f - (distance / perfectDistance));

        return score * 100f;
    }

    public bool IsExcellent()
    {
        return currentScore >= 90f;
    }

    public bool IsGood()
    {
        return currentScore >= 75f;
    }

    public bool IsOkay()
    {
        return currentScore >= 60f;
    }

   public string GetFeedback()
    {
        switch (worstJoint)
        {
            case "Head":
                return "Keep your head steady.";

            case "Left Shoulder":
                return "Raise your left shoulder.";

            case "Right Shoulder":
                return "Raise your right shoulder.";

            case "Left Elbow":
                return "Keep your left elbow tucked in.";

            case "Right Elbow":
                return "Keep your right elbow tucked in.";

            case "Left Wrist":
                return "Raise your left hand to protect your face.";

            case "Right Wrist":
                return "Raise your right hand to protect your face.";

            default:
                return "Excellent!";
        }
    }

    public string GetRating()
    {
        if (currentScore >= 98)
            return " PERFECT";

        if (currentScore >= 90)
            return " EXCELLENT";

        if (currentScore >= 80)
            return " GOOD";

        if (currentScore >= 65)
            return " KEEP GOING";

        return " TRY AGAIN";
    }
}