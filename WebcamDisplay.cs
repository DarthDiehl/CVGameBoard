using UnityEngine;
using UnityEngine.UI;

public class WebcamDisplay : MonoBehaviour
{
    public RawImage display;

    private WebCamTexture webcam;

    void Start()
    {
        webcam = new WebCamTexture();
        display.texture = webcam;

        webcam.Play();
    }

    void OnDestroy()
    {
        if (webcam != null && webcam.isPlaying)
        {
            webcam.Stop();
        }
    }
}