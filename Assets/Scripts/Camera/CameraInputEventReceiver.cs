using UnityEngine;

public class CameraInputEventReceiver : MonoBehaviour
{
    [Header("Drag Cinemachine Input Axis Controller Here")]
    public Behaviour cameraInputController;

    private void OnEnable()
    {
        EventCenter.Instance.AddEventListener<bool>(
            E_EventType.E_Camera_InputEnable,
            SetCameraInput
        );
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<bool>(
            E_EventType.E_Camera_InputEnable,
            SetCameraInput
        );
    }

    private void SetCameraInput(bool state)
    {
        if (cameraInputController != null)
        {
            cameraInputController.enabled = state;
        }
    }
}