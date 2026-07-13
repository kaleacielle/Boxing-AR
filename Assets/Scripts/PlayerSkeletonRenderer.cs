using UnityEngine;
using UnityEngine.UI;

public class PlayerSkeletonRenderer : MonoBehaviour
{
    public RawImage webcamImage;
    private RectTransform canvasRect;
    private Image head,leftShoulder,rightShoulder,leftElbow,rightElbow,leftWrist,rightWrist;
    private Image neck,shoulders,leftUpperArm,leftLowerArm,rightUpperArm,rightLowerArm;
    public Vector2 Head { get; private set; }
    public Vector2 LeftShoulder { get; private set; }
    public Vector2 RightShoulder { get; private set; }
    public Vector2 LeftElbow { get; private set; }
    public Vector2 RightElbow { get; private set; }
    public Vector2 LeftWrist { get; private set; }
    public Vector2 RightWrist { get; private set; }
    void Start(){canvasRect=webcamImage.rectTransform;head=CreateJoint("Head");leftShoulder=CreateJoint("Left Shoulder");rightShoulder=CreateJoint("Right Shoulder");leftElbow=CreateJoint("Left Elbow");rightElbow=CreateJoint("Right Elbow");leftWrist=CreateJoint("Left Wrist");rightWrist=CreateJoint("Right Wrist");neck=CreateBone("Neck");shoulders=CreateBone("Shoulders");leftUpperArm=CreateBone("Left Upper Arm");leftLowerArm=CreateBone("Left Lower Arm");rightUpperArm=CreateBone("Right Upper Arm");rightLowerArm=CreateBone("Right Lower Arm");}
    void Update(){if(!UDPReceiver.bodyDetected)return;Move(head,UDPReceiver.head);Move(leftShoulder,UDPReceiver.leftShoulder);Move(rightShoulder,UDPReceiver.rightShoulder);Move(leftElbow,UDPReceiver.leftElbow);Move(rightElbow,UDPReceiver.rightElbow);Move(leftWrist,UDPReceiver.leftWrist);Move(rightWrist,UDPReceiver.rightWrist);DrawBone(neck,UDPReceiver.head,Mid(UDPReceiver.leftShoulder,UDPReceiver.rightShoulder));DrawBone(shoulders,UDPReceiver.leftShoulder,UDPReceiver.rightShoulder);DrawBone(leftUpperArm,UDPReceiver.leftShoulder,UDPReceiver.leftElbow);DrawBone(leftLowerArm,UDPReceiver.leftElbow,UDPReceiver.leftWrist);DrawBone(rightUpperArm,UDPReceiver.rightShoulder,UDPReceiver.rightElbow);DrawBone(rightLowerArm,UDPReceiver.rightElbow,UDPReceiver.rightWrist);}
    Image CreateJoint(string n){var o=new GameObject(n);o.transform.SetParent(transform,false);var i=o.AddComponent<Image>();i.color=Color.green;i.rectTransform.sizeDelta=new Vector2(5,5);return i;}
    Image CreateBone(string n){var o=new GameObject(n);o.transform.SetParent(transform,false);var i=o.AddComponent<Image>();i.color=Color.green;i.rectTransform.sizeDelta=new Vector2(4,40);return i;}
    void Move(Image img, Vector2 point)
    {
        Vector2 pos = Convert(point);

        img.rectTransform.anchoredPosition = pos;

        if (img == head) Head = pos;
        else if (img == leftShoulder) LeftShoulder = pos;
        else if (img == rightShoulder) RightShoulder = pos;
        else if (img == leftElbow) LeftElbow = pos;
        else if (img == rightElbow) RightElbow = pos;
        else if (img == leftWrist) LeftWrist = pos;
        else if (img == rightWrist) RightWrist = pos;
    }
    Vector2 Mid(Vector2 a,Vector2 b){return(a+b)*0.5f;}
    Vector2 Convert(Vector2 p){float x=Mathf.Lerp(-canvasRect.rect.width*.5f,canvasRect.rect.width*.5f,p.x);float y=Mathf.Lerp(canvasRect.rect.height*.5f,-canvasRect.rect.height*.5f,p.y);return new Vector2(x,y);}
    void DrawBone(Image bone,Vector2 s,Vector2 e){RectTransform rt=bone.rectTransform;Vector2 a=Convert(s),b=Convert(e),d=b-a;rt.sizeDelta=new Vector2(2f,d.magnitude);rt.anchoredPosition=(a+b)*.5f;rt.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90f);}
}