using UnityEngine;

public class CameraContext : MonoBehaviour
{
    readonly Vector3 DEFAULT_OFFSET = new Vector3(.3F, .5F, 0);

    [SerializeField] bool constrainY;
    [SerializeField] float yLowerBound;
    [SerializeField] float yUpperBound;

    [SerializeField] bool constrainX;
    [SerializeField] float xLowerBound;
    [SerializeField] float xUpperBound;
    
    Vector2 currentRotation = new(0.0F, 0.0F);


    Transform originalParentTransform;
    Vector3 originalLocalOffset;


    public bool offsetXFlipped = false;

    bool parentToEntity = false;

    float distanceFromEntity = 0;

    Vector3 lastParentPosition = Vector3.zero;

    Vector3 offsetFromEntity = -Vector3.forward;


    void Start()
    {
        Entity entity = transform.parent.gameObject.GetComponent<Entity>();
        originalLocalOffset = transform.parent.InverseTransformPoint(entity.GetWorldPosCenter()) + DEFAULT_OFFSET * entity.GetHeight();

        lastParentPosition = transform.parent.position;

        currentRotation = transform.parent.rotation.eulerAngles;

        originalParentTransform = transform.parent;
        if (!parentToEntity)
        {
            transform.SetParent(null);
        }
    }

    public void FixedUpdate()
    {
        // transform.rotation = Quaternion.Euler(currentRotation);
    }


    public void LateUpdate()
    {
        // Calculate the current position of our original offset position
        Quaternion rotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0);
        Vector3 rotatedOriginalOffset = rotation * originalLocalOffset;
        
        transform.rotation = Quaternion.Euler(currentRotation);


        // Vector3 currentOffset = offsetXFlipped ? new Vector3 (-localOffset.x, localOffset.y, localOffset.z) : localOffset;

        // Vector3 desiredPosition = rotatedOriginalOffset;
        // if (!parentToEntity)
        // {
        //     // Desired position is merely an offset of the parent
        //     desiredPosition = originalParentTransform.TransformPoint(desiredPosition);
        // }
        // else
        // {
        //     desiredPosition = originalParentTransform.position + originalLocalOffset + rotatedOriginalOffset;
        // }

        Vector3 desiredPosition = originalParentTransform.position + rotatedOriginalOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, 20F * Time.deltaTime);
    }

    public void AddOffset(Vector3 offset)
    {
        originalLocalOffset += offset;
    }

    public void RemoveOffset(Vector3 offset)
    {
        originalLocalOffset -= offset;
    }

    public void ClearOffset()
    {
    }


    /// <summary>
    /// Add rotation in degrees
    /// </summary>
    /// <param name="rotationChange"></param>
    public void AddRotation(Vector2 rotationChange)
    {
        Vector2 constrainedRotation = currentRotation + new Vector2(rotationChange.y, rotationChange.x);

        if (constrainX)
        {
            constrainedRotation.y = Mathf.Clamp(constrainedRotation.y, xLowerBound, xUpperBound);
        }

        if (constrainY)
        {
            constrainedRotation.x = Mathf.Clamp(constrainedRotation.x, yLowerBound, yUpperBound);
        }

        currentRotation = constrainedRotation;

    }

    public void SetRotation(Vector2 newRotation)
    {
        currentRotation = newRotation;
    }

    public Vector2 GetRotation()
    {
        return currentRotation;
    }
}
