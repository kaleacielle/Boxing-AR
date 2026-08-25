using System.Collections;
using UnityEngine;

public class PoseComparisonManager : MonoBehaviour
{
    private enum GameState
    {
        WaitingForWave,
        Countdown,
        MatchingPose,
        PoseCompleted,
        Finished
    }

    [Header("References")]
    public PlayerSkeletonRenderer player;
    public CoachSkeletonRenderer coach;
    public UIManager uiManager;
    public CoachManager coachManager;

    [Header("Pose Matching Settings")]
    [Tooltip("Higher values make pose matching more forgiving.")]
    [Range(10f, 300f)]
    public float perfectDistance = 100f;

    [Tooltip("Internal score needed to successfully complete the pose.")]
    [Range(1f, 100f)]
    public float completionScore = 70f;

    [Tooltip("How long the successful pose must be held.")]
    [Range(0.1f, 5f)]
    public float requiredHoldTime = 0.75f;

    [Header("Wave Detection")]
    [Tooltip("The wrist must move this far horizontally before it counts as a direction change.")]
    [Range(5f, 200f)]
    public float waveMovementDistance = 35f;

    [Tooltip("Number of side-to-side direction changes needed to start.")]
    [Range(1, 6)]
    public int requiredWaveDirectionChanges = 2;

    [Tooltip("The player must finish waving within this amount of time.")]
    [Range(0.5f, 5f)]
    public float waveTimeLimit = 2f;

    [Tooltip("Allows a raised hand to begin the experience even if the wave movement is small.")]
    public bool allowRaisedHandFallback = true;

    [Tooltip("How long a hand must stay raised to use the fallback start.")]
    [Range(0.2f, 3f)]
    public float raisedHandStartTime = 1f;

    [Header("Countdown")]
    [Range(0.1f, 3f)]
    public float getReadyDuration = 1f;

    [Range(0.1f, 2f)]
    public float countdownNumberDuration = 0.8f;

    [Range(0.1f, 3f)]
    public float matchPoseMessageDuration = 0.8f;

    [Header("Completion")]
    [Range(0.5f, 5f)]
    public float completedMessageDuration = 2f;

    [Header("Runtime Information")]
    [Range(0f, 100f)]
    public float currentScore;

    public string worstJoint;

    private GameState currentState = GameState.WaitingForWave;

    private int currentPoseIndex = 0;

    private float rawScore;
    private float poseHoldTimer;

    private float previousLeftWristX;
    private float previousRightWristX;

    private int leftWaveDirection;
    private int rightWaveDirection;

    private int leftDirectionChanges;
    private int rightDirectionChanges;

    private float waveTimer;
    private float raisedHandTimer;

    private bool hasInitialWristPositions;

    public bool IsExperienceActive =>
        currentState == GameState.MatchingPose;

    private void Start()
    {
        if (coachManager == null)
        {
            coachManager =
                FindFirstObjectByType<CoachManager>();
        }

        ResetToWaitingForWave();
    }

    private void Update()
    {
        if (player == null || coach == null)
            return;

        switch (currentState)
        {
            case GameState.WaitingForWave:
                DetectWave();
                break;

            case GameState.Countdown:
                // No scoring during countdown.
                break;

            case GameState.MatchingPose:
                UpdatePoseComparison();
                break;

            case GameState.PoseCompleted:
                // Coroutine controls transition.
                break;

            case GameState.Finished:
                break;
        }
    }

    // =========================================================
    // WAVE START
    // =========================================================

    private void DetectWave()
    {
        if (!hasInitialWristPositions)
        {
            previousLeftWristX =
                player.LeftWrist.x;

            previousRightWristX =
                player.RightWrist.x;

            hasInitialWristPositions = true;

            return;
        }

        bool leftHandRaised =
            IsHandRaised(
                player.LeftWrist,
                player.LeftShoulder
            );

        bool rightHandRaised =
            IsHandRaised(
                player.RightWrist,
                player.RightShoulder
            );

        if (leftHandRaised || rightHandRaised)
        {
            waveTimer += Time.deltaTime;

            if (leftHandRaised)
            {
                CheckWaveMovement(
                    player.LeftWrist.x,
                    ref previousLeftWristX,
                    ref leftWaveDirection,
                    ref leftDirectionChanges
                );
            }
            else
            {
                previousLeftWristX =
                    player.LeftWrist.x;

                leftWaveDirection = 0;
                leftDirectionChanges = 0;
            }

            if (rightHandRaised)
            {
                CheckWaveMovement(
                    player.RightWrist.x,
                    ref previousRightWristX,
                    ref rightWaveDirection,
                    ref rightDirectionChanges
                );
            }
            else
            {
                previousRightWristX =
                    player.RightWrist.x;

                rightWaveDirection = 0;
                rightDirectionChanges = 0;
            }

            bool waveDetected =
                leftDirectionChanges >=
                    requiredWaveDirectionChanges ||
                rightDirectionChanges >=
                    requiredWaveDirectionChanges;

            if (allowRaisedHandFallback)
            {
                raisedHandTimer +=
                    Time.deltaTime;
            }
            else
            {
                raisedHandTimer = 0f;
            }

            bool raisedHandDetected =
                allowRaisedHandFallback &&
                raisedHandTimer >=
                    raisedHandStartTime;

            if (waveDetected || raisedHandDetected)
            {
                Debug.Log("👋 WAVE DETECTED");

                BeginCountdown();

                return;
            }

            if (waveTimer > waveTimeLimit)
            {
                ResetWaveDetection();
            }
        }
        else
        {
            raisedHandTimer = 0f;

            previousLeftWristX =
                player.LeftWrist.x;

            previousRightWristX =
                player.RightWrist.x;

            if (waveTimer > 0f)
            {
                waveTimer += Time.deltaTime;

                if (waveTimer > waveTimeLimit)
                {
                    ResetWaveDetection();
                }
            }
        }
    }

    private bool IsHandRaised(
        Vector2 wrist,
        Vector2 shoulder
    )
    {
        return wrist.y > shoulder.y;
    }

    private void CheckWaveMovement(
        float currentWristX,
        ref float previousWristX,
        ref int previousDirection,
        ref int directionChanges
    )
    {
        float movement =
            currentWristX -
            previousWristX;

        if (
            Mathf.Abs(movement) <
            waveMovementDistance
        )
        {
            return;
        }

        int newDirection =
            movement > 0f ? 1 : -1;

        if (
            previousDirection != 0 &&
            newDirection != previousDirection
        )
        {
            directionChanges++;
        }

        previousDirection =
            newDirection;

        previousWristX =
            currentWristX;
    }

    // =========================================================
    // FIRST COUNTDOWN
    // =========================================================

    private void BeginCountdown()
    {
        if (
            currentState !=
            GameState.WaitingForWave
        )
        {
            return;
        }

        Debug.Log(
            "🟡 STARTING FIRST COUNTDOWN"
        );

        currentState =
            GameState.Countdown;

        ResetWaveDetection();

        StartCoroutine(
            CountdownRoutine()
        );
    }

    private IEnumerator CountdownRoutine()
    {
        Debug.Log(
            "⏱ COUNTDOWN STARTED FOR POSE: " +
            currentPoseIndex
        );

        rawScore = 0f;
        currentScore = 0f;
        poseHoldTimer = 0f;
        worstJoint = "";

        if (uiManager != null)
        {
            uiManager.SetPoseScore(0f);
            uiManager.SetPoseScoreVisible(false);
            uiManager.SetCoachingUIVisible(false);

            uiManager.ShowReadyMessage(
                "GET READY!"
            );
        }

        Debug.Log("GET READY!");

        yield return new WaitForSeconds(
            getReadyDuration
        );

        Debug.Log("3️");

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage("3");
        }

        yield return new WaitForSeconds(
            countdownNumberDuration
        );

        Debug.Log("2");

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage("2");
        }

        yield return new WaitForSeconds(
            countdownNumberDuration
        );

        Debug.Log("1️");

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage("1");
        }

        yield return new WaitForSeconds(
            countdownNumberDuration
        );

        Debug.Log(
            "MATCH THE POSE"
        );

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage(
                "MATCH THE POSE!"
            );
        }

        yield return new WaitForSeconds(
            matchPoseMessageDuration
        );

        Debug.Log(
            "🎬 NOW PLAYING COACH POSE: " +
            currentPoseIndex
        );

        PlayCurrentPose();

        yield return null;

        if (uiManager != null)
        {
            uiManager.HideReadyMessage();

            uiManager.SetPoseScore(0f);

            uiManager.SetPoseScoreVisible(
                true
            );

            uiManager.SetCoachingUIVisible(
                true
            );

            uiManager.SetFeedback(
                "Copy the coach's pose."
            );

            uiManager.SetHint("");
        }

        currentState =
            GameState.MatchingPose;

        Debug.Log(
            "MATCHING POSE INDEX: " +
            currentPoseIndex
        );
    }

    // =========================================================
    // POSE SEQUENCE
    // =========================================================

    private void PlayCurrentPose()
    {
        if (coachManager == null)
        {
            Debug.LogError(
                " CoachManager is missing!"
            );

            return;
        }

        switch (currentPoseIndex)
        {
            case 0:

                Debug.Log(
                    " COACH = GUARD"
                );

                coachManager.PlayIdle();

                break;

            case 1:

                Debug.Log(
                    "COACH = LEAD JAB"
                );

                coachManager.PlayLeadJab();

                break;

            case 2:

                Debug.Log(
                    " COACH = COMBINATION"
                );

                coachManager.PlayComboPunch();

                break;
        }
    }

    // =========================================================
    // POSE COMPARISON
    // =========================================================

    private void UpdatePoseComparison()
    {
        float head =
            Compare(
                player.Head,
                coach.Head
            );

        float leftShoulder =
            Compare(
                player.LeftShoulder,
                coach.LeftShoulder
            );

        float rightShoulder =
            Compare(
                player.RightShoulder,
                coach.RightShoulder
            );

        float leftElbow =
            Compare(
                player.LeftElbow,
                coach.LeftElbow
            );

        float rightElbow =
            Compare(
                player.RightElbow,
                coach.RightElbow
            );

        float leftWrist =
            Compare(
                player.LeftWrist,
                coach.LeftWrist
            );

        float rightWrist =
            Compare(
                player.RightWrist,
                coach.RightWrist
            );

        // -----------------------------------------
        // RAW INTERNAL SCORE
        // -----------------------------------------

        rawScore =
            (
                head +
                leftShoulder +
                rightShoulder +
                leftElbow +
                rightElbow +
                leftWrist +
                rightWrist
            ) / 7f;

        // -----------------------------------------
        // FIND WORST JOINT
        // -----------------------------------------

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

        for (
            int i = 1;
            i < scores.Length;
            i++
        )
        {
            if (
                scores[i] <
                scores[worst]
            )
            {
                worst = i;
            }
        }

        worstJoint =
            names[worst];

        // -----------------------------------------
        // NORMALISE EASY SCORE TO 100%
        //
        // completionScore = 70
        //
        // raw 35 = 50%
        // raw 56 = 80%
        // raw 70 = 100%
        // -----------------------------------------

        float displayScore =
            Mathf.Clamp01(
                rawScore /
                Mathf.Max(
                    completionScore,
                    0.01f
                )
            ) * 100f;

        currentScore =
            displayScore;

        int displayedPercentage =
            Mathf.Clamp(
                Mathf.RoundToInt(
                    displayScore
                ),
                0,
                100
            );

        if (uiManager != null)
        {
            uiManager.SetPoseScore(
                displayScore
            );

            uiManager.SetFeedback(
                GetFeedback()
            );

            uiManager.SetHint(
                GetHint()
            );
        }

        // -----------------------------------------
        // SUCCESS
        // -----------------------------------------

        if (displayedPercentage >= 100)
        {
            poseHoldTimer +=
                Time.deltaTime;

            if (uiManager != null)
            {
                uiManager.SetPoseScore(
                    100f
                );

                float holdProgress =
                    Mathf.Clamp01(
                        poseHoldTimer /
                        requiredHoldTime
                    );

                uiManager.SetFeedback(
                    "Hold it! " +
                    Mathf.RoundToInt(
                        holdProgress * 100f
                    ) +
                    "%"
                );
            }

            if (
                poseHoldTimer >=
                requiredHoldTime
            )
            {
                Debug.Log(
                    "✅ POSE REACHED 100%: " +
                    currentPoseIndex
                );

                CompletePose();
            }
        }
        else
        {
            poseHoldTimer =
                Mathf.Max(
                    0f,
                    poseHoldTimer -
                    Time.deltaTime * 2f
                );
        }
    }

    private float Compare(
        Vector2 playerJoint,
        Vector2 coachJoint
    )
    {
        float distance =
            Vector2.Distance(
                playerJoint,
                coachJoint
            );

        float score =
            Mathf.Clamp01(
                1f -
                distance /
                perfectDistance
            );

        return score * 100f;
    }

    // =========================================================
    // POSE COMPLETION
    // =========================================================

    private void CompletePose()
    {
        if (
            currentState !=
            GameState.MatchingPose
        )
        {
            return;
        }

        Debug.Log(
            " COMPLETE POSE CALLED: " +
            currentPoseIndex
        );

        currentState =
            GameState.PoseCompleted;

        StartCoroutine(
            PoseCompletedRoutine()
        );
    }

    private IEnumerator PoseCompletedRoutine()
    {
        Debug.Log(
            "✅ POSE COMPLETED ROUTINE START: " +
            currentPoseIndex
        );

        currentScore = 100f;

        if (uiManager != null)
        {
            uiManager.SetPoseScore(
                100f
            );

            uiManager.SetPoseScoreVisible(
                true
            );

            uiManager.SetCoachingUIVisible(
                false
            );

            uiManager.ShowReadyMessage(
                "GREAT JOB!"
            );
        }

        Debug.Log(
            "⭐ SHOWING GREAT JOB"
        );

        yield return new WaitForSeconds(
            completedMessageDuration
        );

        Debug.Log(
            "➡️ GREAT JOB FINISHED"
        );

        // -----------------------------------------
        // FINISHED ALL POSES
        // -----------------------------------------

        if (currentPoseIndex >= 2)
        {
            Debug.Log(
                " ALL 3 POSES COMPLETE"
            );

            currentState =
                GameState.Finished;

            if (uiManager != null)
            {
                uiManager.SetPoseScoreVisible(
                    false
                );

                uiManager.SetCoachingUIVisible(
                    false
                );

                uiManager.ShowReadyMessage(
                    "LESSON COMPLETE!"
                );
            }

            yield break;
        }

        // -----------------------------------------
        // NEXT POSE
        // -----------------------------------------

        currentPoseIndex++;

        Debug.Log(
            "➡️ MOVING TO POSE INDEX: " +
            currentPoseIndex
        );

        rawScore = 0f;
        currentScore = 0f;
        poseHoldTimer = 0f;
        worstJoint = "";

        currentState =
            GameState.Countdown;

        if (uiManager != null)
        {
            uiManager.SetPoseScore(0f);

            uiManager.SetPoseScoreVisible(
                false
            );

            uiManager.SetCoachingUIVisible(
                false
            );

            uiManager.ShowReadyMessage(
                "GET READY!"
            );
        }

        Debug.Log(
            " BETWEEN POSE GET READY"
        );

        yield return new WaitForSeconds(
            getReadyDuration
        );

        // 3
        Debug.Log("3️⃣");

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage(
                "3"
            );
        }

        yield return new WaitForSeconds(
            countdownNumberDuration
        );

        // 2
        Debug.Log("2️⃣");

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage(
                "2"
            );
        }

        yield return new WaitForSeconds(
            countdownNumberDuration
        );

        // 1
        Debug.Log("1️⃣");

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage(
                "1"
            );
        }

        yield return new WaitForSeconds(
            countdownNumberDuration
        );

        Debug.Log(
            "BETWEEN POSE MATCH THE POSE"
        );

        if (uiManager != null)
        {
            uiManager.ShowReadyMessage(
                "MATCH THE POSE!"
            );
        }

        yield return new WaitForSeconds(
            matchPoseMessageDuration
        );

        // -----------------------------------------
        // ONLY NOW CHANGE COACH POSE
        // -----------------------------------------

        Debug.Log(
            "🎬 PLAYING NEXT COACH POSE: " +
            currentPoseIndex
        );

        PlayCurrentPose();

        // Give Animator one frame.
        yield return null;

        if (uiManager != null)
        {
            uiManager.HideReadyMessage();

            uiManager.SetPoseScore(
                0f
            );

            uiManager.SetPoseScoreVisible(
                true
            );

            uiManager.SetCoachingUIVisible(
                true
            );

            uiManager.SetFeedback(
                "Copy the coach's pose."
            );

            uiManager.SetHint("");
        }

        currentState =
            GameState.MatchingPose;

        Debug.Log(
            " NOW MATCHING POSE: " +
            currentPoseIndex
        );
    }

    // =========================================================
    // RESET
    // =========================================================

    private void ResetToWaitingForWave()
    {
        StopAllCoroutines();

        currentState =
            GameState.WaitingForWave;

        currentPoseIndex = 0;

        currentScore = 0f;
        rawScore = 0f;

        poseHoldTimer = 0f;

        worstJoint = "";

        ResetWaveDetection();

        if (uiManager != null)
        {
            uiManager.SetPoseScore(
                0f
            );

            uiManager.SetPoseScoreVisible(
                false
            );

            uiManager.SetCoachingUIVisible(
                false
            );

            uiManager.ShowReadyMessage(
                "WAVE YOUR HAND TO BEGIN"
            );
        }

        Debug.Log(
            "🔄 EXPERIENCE RESET"
        );
    }

    public void RestartExperience()
    {
        ResetToWaitingForWave();
    }

    private void ResetWaveDetection()
    {
        waveTimer = 0f;
        raisedHandTimer = 0f;

        leftWaveDirection = 0;
        rightWaveDirection = 0;

        leftDirectionChanges = 0;
        rightDirectionChanges = 0;

        if (player != null)
        {
            previousLeftWristX =
                player.LeftWrist.x;

            previousRightWristX =
                player.RightWrist.x;

            hasInitialWristPositions =
                true;
        }
        else
        {
            hasInitialWristPositions =
                false;
        }
    }

    // =========================================================
    // FEEDBACK
    // =========================================================

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
        if (currentScore >= 90f)
        {
            return "Excellent! Almost there!";
        }

        if (currentScore >= 75f)
        {
            return "Great pose! Keep going!";
        }

        switch (worstJoint)
        {
            case "Head":

                return "Keep your head steady.";

            case "Left Shoulder":

                return "Adjust your left shoulder.";

            case "Right Shoulder":

                return "Adjust your right shoulder.";

            case "Left Elbow":

                return "Move your left elbow closer to the coach.";

            case "Right Elbow":

                return "Move your right elbow closer to the coach.";

            case "Left Wrist":

                return "Move your left hand closer to the coach.";

            case "Right Wrist":

                return "Move your right hand closer to the coach.";

            default:

                return "Copy the coach's pose.";
        }
    }

    public string GetHint()
    {
        switch (worstJoint)
        {
            case "Head":

                return "Check your head position.";

            case "Left Shoulder":

                return "Check your left shoulder.";

            case "Right Shoulder":

                return "Check your right shoulder.";

            case "Left Elbow":

                return "Check your left elbow.";

            case "Right Elbow":

                return "Check your right elbow.";

            case "Left Wrist":

                return "Check your left hand.";

            case "Right Wrist":

                return "Check your right hand.";

            default:

                return "";
        }
    }

    public string GetRating()
    {
        if (currentScore >= 100f)
            return "PERFECT";

        if (currentScore >= 85f)
            return "EXCELLENT";

        if (currentScore >= 70f)
            return "GOOD";

        if (currentScore >= 50f)
            return "KEEP GOING";

        return "TRY AGAIN";
    }
}