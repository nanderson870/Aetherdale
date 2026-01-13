using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class NpcExclamation : FloatingUIElement
{
    NonPlayerCharacter owner;
    Image image;

    public override void Start()
    {
        base.Start();

        image = GetComponent<Image>();
    }

    public void SetOwner(NonPlayerCharacter npc)
    {
        owner = npc;
    }

    public override void Hide()
    {
        image.enabled = false;
    }

    public override void Show()
    {
        image.enabled = true;
    }

    public override void LateUpdate()
    {
        base.LateUpdate();

        if (NetworkClient.active)
        {
            if (owner == null)
            {
                Destroy(gameObject);
                return;
            }
            SetWorldPosition(owner.GetNamePlateTransform().position);

            if (Camera.main == null)
            {
                // No camera right now. Not necessarily an error, maybe just loading still.
                return;
            }

            if (!owner.gameObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            float fractionOfSize = 1.0F / (GetDistanceFromCamera() + 1);
            float size = maxScale * fractionOfSize;

            transform.localScale = Vector3.one * size;
            
            if (OnScreen() && InRange())
            {
                Show();
            }
        }
    }
}
