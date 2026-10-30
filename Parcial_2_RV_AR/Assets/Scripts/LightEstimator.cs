using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;

public class LightEstimator : MonoBehaviour
{
    [Header("Referencias")]
    public ARCameraManager arCameraManager;
    public Light sunLight;

    void OnEnable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        if (arCameraManager != null)
            arCameraManager.frameReceived -= OnCameraFrameReceived;
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        // 1. Ajustar Brillo
        if (args.lightEstimation.averageBrightness.HasValue)
        {
            sunLight.intensity = args.lightEstimation.averageBrightness.Value;
        }

        // 2. Ajustar Temperatura de Color
        if (args.lightEstimation.averageColorTemperature.HasValue)
        {
            sunLight.colorTemperature = args.lightEstimation.averageColorTemperature.Value;
        }

        // 3. Ajustar Color General
        if (args.lightEstimation.mainLightColor.HasValue)
        {
            sunLight.color = args.lightEstimation.mainLightColor.Value;
        }

        // 4. Ajustar Dirección de la luz
        if (args.lightEstimation.mainLightDirection.HasValue)
        {
            sunLight.transform.rotation = Quaternion.LookRotation(args.lightEstimation.mainLightDirection.Value);
        }
    }
}