using TMPro;
using UnityEngine;

public class PortalPercentageUI : MonoBehaviour
{
    TextMeshPro tmp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tmp = GetComponentInChildren<TextMeshPro>();

        GetComponentInParent<AreaPortal>().OnRebuildValueChanged += UpdateRebuild;
    }

    void UpdateRebuild(AreaPortal portal, float newValue)
    {
        if (newValue > 1.0F) newValue = 1.0F;
        
        tmp.text = $"{(int) (newValue * 100)}%";
    }
}
