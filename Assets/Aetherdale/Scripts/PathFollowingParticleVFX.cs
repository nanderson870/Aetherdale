using UnityEngine;
using UnityEngine.VFX;

public class PathFollowingParticleVFX : MonoBehaviour
{
    [SerializeField] Transform pos1;
    [SerializeField] Transform pos2;
    [SerializeField] Transform pos3;
    [SerializeField] Transform pos4;
    

    public void SetPositions(Vector3 start, Vector3 end)
    {
        pos1.position = start;

        float height = (end-start).magnitude * 0.25F;
        pos2.position = Vector3.Lerp(start, end, 0.33F) + Vector3.up * height;
        pos3.position = Vector3.Lerp(start, end, 0.66F) + Vector3.up * height;
        
        pos4.position = end;
    }

    public void Play()
    {
        GetComponent<VisualEffect>().SendEvent("Play");
    }
}
