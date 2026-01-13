using System;
using UnityEngine;

public class Corpse : MonoBehaviour
{
    public Type entityType;


    public void OnOwnerRevived()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}