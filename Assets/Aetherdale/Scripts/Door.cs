using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] bool OpenByDefault = false;

    public void Start()
    {
        if (OpenByDefault)
        {
            Open();
        }
    }

    public void Open()
    {
        //networkAnimator.SetTrigger("Open");
        //currentlyOpen = true;
    }

    public void Close()
    {
        //networkAnimator.SetTrigger("Close");
        //currentlyOpen = false;
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }
}
