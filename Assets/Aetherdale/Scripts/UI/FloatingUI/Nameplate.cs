using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class Nameplate : FloatingUIElement
{
    NonPlayerCharacter owner;
    [SerializeField] TextMeshProUGUI tmpro;

    public override void Hide()
    {
        tmpro.enabled = false;
    }

    public override void Show()
    {
        tmpro.enabled = true;
    }


    public void SetDisplayedName(string newName)
    {
        tmpro.text = newName;
    }

    public void SetOwner(NonPlayerCharacter owner)
    {
        this.owner = owner;
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
