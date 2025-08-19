using System;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "WebcamTexture", menuName = "Particle/Textures/Webcam", order = 0)]
public class WebcamPixels : ATexProvider
{
    [SerializeField,Tooltip("Will be enough unless multiple cameras are available and you need a specific or virtual one")] private bool _useDefaultCamera = true;
    [SerializeField,HideIf(nameof(_useDefaultCamera))] private string deviceName;
    private WebCamTexture _webcamTexture;
    private DateTime _startTime;
    public override string Name => "webcam_" + _startTime.ToShortDateString().Replace('/', '_');

    public override void Create()
    {
        _webcamTexture = _useDefaultCamera || string.IsNullOrEmpty(deviceName) ? new WebCamTexture() : new  WebCamTexture(deviceName);
        _webcamTexture.Play();
        _startTime = DateTime.Now;
    }

    public override Texture Texture
    {
        get => _webcamTexture;
    }

    public override Color[] GetPixels()
    {
        return _webcamTexture.GetPixels();
    }
}