using TMPro;
using UnityEngine;

public class ChatBubble : FloatingUIElement
{
    [SerializeField] TextMeshProUGUI textTMP;

    [SerializeField] float minFontSize = 9.0F;
    [SerializeField] float maxFontSize = 16.0F;
    DialogueAgent owner;
    Vector3 originalScale;

    public void SetOwner(DialogueAgent owner)
    {
        this.owner = owner;
    }

    public void SetText(string text)
    {
        textTMP.text = text;
    }

    public override void Start()
    {
        base.Start();

        originalScale = transform.localScale;
    }

    public override void LateUpdate()
    {
        base.LateUpdate();

        SetWorldPosition(owner.GetChatBubbleTransform().position);
    }

}
