using UnityEngine;

public sealed class PaddleHandAnimationEventRelay : MonoBehaviour
{
    [SerializeField] private PaddleHandAttachment paddleHandAttachment;
    [SerializeField] private string attachmentObjectName = "\u8239\u68680";

    private void Awake()
    {
        ResolveAttachment();
    }

    private void OnEnable()
    {
        ResolveAttachment();
    }

    public void SwapHands()
    {
        ResolveAttachment();

        if (paddleHandAttachment != null)
        {
            paddleHandAttachment.SwapHands();
        }
    }

    public void SwapHandsFromAnimationEvent()
    {
        SwapHands();
    }

    public void SetLeftHandAsPivot()
    {
        ResolveAttachment();

        if (paddleHandAttachment != null)
        {
            paddleHandAttachment.SetLeftHandAsPivot();
        }
    }

    public void SetRightHandAsPivot()
    {
        ResolveAttachment();

        if (paddleHandAttachment != null)
        {
            paddleHandAttachment.SetRightHandAsPivot();
        }
    }

    private void ResolveAttachment()
    {
        if (paddleHandAttachment != null)
        {
            return;
        }

        GameObject attachmentObject = GameObject.Find(attachmentObjectName);
        if (attachmentObject != null)
        {
            paddleHandAttachment = attachmentObject.GetComponent<PaddleHandAttachment>() ??
                attachmentObject.GetComponentInChildren<PaddleHandAttachment>();
        }

        if (paddleHandAttachment == null)
        {
            paddleHandAttachment = Object.FindAnyObjectByType<PaddleHandAttachment>();
        }
    }
}
