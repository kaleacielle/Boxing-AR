using UnityEngine;
using UnityEngine.UI;

public class PlayerSkeletonRenderer : MonoBehaviour
{
    public RawImage webcamImage;

    private RectTransform canvasRect;

    private Image head;
    private Image leftShoulder;
    private Image rightShoulder;
    private Image leftElbow;
    private Image rightElbow;
    private Image leftWrist;
    private Image rightWrist;

    void Start()
    {
        canvasRect = webcamImage.rectTransform;

        head = CreateJoint("Head");
        leftShoulder = CreateJoint("Left Shoulder");
        rightShoulder = CreateJoint("Right Shoulder");
        leftElbow = CreateJoint("Left Elbow");
        rightElbow = CreateJoint("Right Elbow");
        leftWrist = CreateJoint("Left Wrist");
        rightWrist = CreateJoint("Right Wrist");
    }

    void Update()
    {
        if (!UDPReceiver.bodyDetected)
            return;

        Move(head, UDPReceiver.head);
        Move(leftShoulder, UDPReceiver.leftShoulder);
        Move(rightShoulder, UDPReceiver.rightShoulder);
        Move(leftElbow, UDPReceiver.leftElbow);
        Move(rightElbow, UDPReceiver.rightElbow);
        Move(leftWrist, UDPReceiver.leftWrist);
        Move(rightWrist, UDPReceiver.rightWrist);
    }

    Image CreateJoint(string name)
    {
        GameObject obj = new GameObject(name);

        obj.transform.SetParent(transform, false);

        Image img = obj.AddComponent<Image>();

        img.color = Color.green;

        RectTransform rt = img.rectTransform;

        rt.sizeDelta = new Vector2(16, 16);

        return img;
    }
void Move(Image img, Vector2 point)
    {
        RectTransform rt = img.rectTransform;

        float x = Mathf.Lerp(
            -canvasRect.rect.width * 0.5f,
            canvasRect.rect.width * 0.5f,
            point.x
        );

        float y = Mathf.Lerp(
            canvasRect.rect.height * 0.5f,
            -canvasRect.rect.height * 0.5f,
            point.y
        );

        rt.anchoredPosition = new Vector2(x, y);
    }
}