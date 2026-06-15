using UnityEngine;
using UnityEngine.UI;

public class WebcamTest : MonoBehaviour
{
    public RawImage webcamImage;

    private WebCamTexture webcamTexture;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        webcamImage.texture = webcamTexture;
        webcamTexture.Play();
    }
}