using UnityEngine;
using UnityEngine.UI;
public class CoachSkeletonRenderer:MonoBehaviour{
public RawImage webcamImage; public Transform boxerRoot; public Color skeletonColor=Color.white;
RectTransform canvasRect; Transform head,neck,ls,la,lf,lh,rs,ra,rf,rh; Image ih,inck,ils,ile,ilw,irs,ire,irw,bn,bs,blu,bll,bru,brl;
public Vector2 Head { get; private set; }
public Vector2 LeftShoulder { get; private set; }
public Vector2 RightShoulder { get; private set; }
public Vector2 LeftElbow { get; private set; }
public Vector2 RightElbow { get; private set; }
public Vector2 LeftWrist { get; private set; }
public Vector2 RightWrist { get; private set; }
void Start(){canvasRect=webcamImage.rectTransform;head=F("mixamorig:Head");neck=F("mixamorig:Neck");ls=F("mixamorig:LeftShoulder");la=F("mixamorig:LeftArm");lf=F("mixamorig:LeftForeArm");lh=F("mixamorig:LeftHand");rs=F("mixamorig:RightShoulder");ra=F("mixamorig:RightArm");rf=F("mixamorig:RightForeArm");rh=F("mixamorig:RightHand");ih=J("Head");inck=J("Neck");ils=J("LS");ile=J("LE");ilw=J("LH");irs=J("RS");ire=J("RE");irw=J("RH");bn=B();bs=B();blu=B();bll=B();bru=B();brl=B();}
void Update(){if(head==null)return;M(ih,head.position);M(inck,neck.position);M(ils,ls.position);M(ile,lf.position);M(ilw,lh.position);M(irs,rs.position);M(ire,rf.position);M(irw,rh.position);D(bn,head.position,neck.position);D(bs,ls.position,rs.position);D(blu,ls.position,la.position);D(bll,lf.position,lh.position);D(bru,rs.position,ra.position);D(brl,rf.position,rh.position);}
Transform F(string n){foreach(var t in boxerRoot.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}
Image J(string n){var o=new GameObject(n);o.transform.SetParent(transform,false);var i=o.AddComponent<Image>();i.color=skeletonColor;i.rectTransform.sizeDelta=new Vector2(5,5);return i;}
Image B(){var o=new GameObject("Bone");o.transform.SetParent(transform,false);var i=o.AddComponent<Image>();i.color=skeletonColor;i.rectTransform.sizeDelta=new Vector2(4,40);return i;}
Vector2 C(Vector3 w){RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Camera.main.WorldToScreenPoint(w),null,out Vector2 p);return p;}
void M(Image i,Vector3 w){i.rectTransform.anchoredPosition=C(w);}
void D(Image b,Vector3 a,Vector3 c){Vector2 p1=C(a),p2=C(c),d=p2-p1;b.rectTransform.sizeDelta=new Vector2(1.5f,d.magnitude);b.rectTransform.anchoredPosition=(p1+p2)*.5f;b.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg-90f);}

void Move(Image img, Vector3 world)
{
    Vector2 pos = C(world);

    img.rectTransform.anchoredPosition = pos;

    if (img == ih) Head = pos;
    else if (img == ils) LeftShoulder = pos;
    else if (img == irs) RightShoulder = pos;
    else if (img == ile) LeftElbow = pos;
    else if (img == ire) RightElbow = pos;
    else if (img == ilw) LeftWrist = pos;
    else if (img == irw) RightWrist = pos;
}
}