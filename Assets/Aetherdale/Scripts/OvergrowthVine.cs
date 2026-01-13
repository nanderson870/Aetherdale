using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class OvergrowthVine : NetworkBehaviour
{
    Transform startTransform = null;
    Transform endTransform = null;

    Vector3 originalSize;

    [Client]
    public void SetStartTransform(Transform startTransform)
    {
        this.startTransform = startTransform;
    }

    [Client]
    public void SetEndTransform(Transform endTransform)
    {
        this.endTransform = endTransform;
    }

    // Start is called before the first frame update
    void Start()
    {
        originalSize = GetComponent<MeshRenderer>().bounds.extents * 2;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (endTransform == null)
        {
            return;
        }
        
        Vector3 offset = endTransform.position - startTransform.position;
        Vector3 newPosition = startTransform.position + (offset / 2);
        transform.position = newPosition;

        float distanceBetweenPositions = Vector3.Distance(startTransform.position, endTransform.position);
        transform.localScale = new(originalSize.x, originalSize.y, distanceBetweenPositions / originalSize.z);

        transform.LookAt(endTransform);
    }
}
