using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Human : MonoBehaviour
{
    public GameObject headSlotItem;
    public GameObject rightHandSlotItem;

    [SerializeField] GameObject headNode;
    [SerializeField] GameObject rightHandNode;


    // Start is called before the first frame update
    void Start()
    {
        UpdateEquipmentSlot(headNode.transform, headSlotItem);
        UpdateEquipmentSlot(rightHandNode.transform, rightHandSlotItem);
    }

    private void UpdateEquipmentSlot(Transform node, GameObject equippedObject)
    {
        for (int i = node.childCount; i > 0; i--)
        {
            GameObject childObject = node.GetChild(i - 1).gameObject;
            Destroy(childObject);
        }

        if (equippedObject != null)
        {
            Instantiate(equippedObject, node);
        }
    }
}
