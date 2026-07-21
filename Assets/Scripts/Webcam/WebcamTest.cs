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

        // Flip the webcam horizontally (unmirror it)
        webcamImage.uvRect = new Rect(1, 0, -1, 1);

        webcamTexture.Play();
    }
}