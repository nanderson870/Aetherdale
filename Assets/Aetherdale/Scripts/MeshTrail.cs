using System.Collections;
using UnityEngine;

public class MeshTrail : MonoBehaviour
{
    public float updateInterval = 0.03F;
    public float imageLifespan = 0.06F;
    float maxAlpha = 0.5F;
    public Material material;

    bool active = false;

    float timeRemaining = 0;

    SkinnedMeshRenderer[] renderers;

    public void StartTrail(float duration)
    {
        timeRemaining = Mathf.Clamp(timeRemaining + duration, 0, duration);

        if (!active)
        {
            StartCoroutine(RenderTrail());
        }
    }

    IEnumerator RenderTrail()
    {
        while (timeRemaining > 0)
        {
            timeRemaining -= updateInterval;

            renderers ??= GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                CreateMeshImage(renderers[i]);
            }

            yield return new WaitForSeconds(updateInterval);
        }
    }

    void CreateMeshImage(SkinnedMeshRenderer renderer)
    {
        GameObject newObj = new();

        MeshRenderer mr = newObj.AddComponent<MeshRenderer>();
        mr.material = material;

        MeshFilter mf = newObj.AddComponent<MeshFilter>();
        Mesh mesh = new();
        renderer.BakeMesh(mesh);
        mf.mesh = mesh;

        newObj.transform.position = renderer.transform.position;
        newObj.transform.rotation = renderer.transform.rotation;

        StartCoroutine(ManageImage(newObj));

        newObj.AddComponent<AutoDestroy>().lifespan = imageLifespan + 0.4F;
    }

    IEnumerator ManageImage(GameObject image)
    {
        float imageTimeRemaining = imageLifespan;

        while (imageTimeRemaining > 0)
        {
            foreach (MeshRenderer renderer in image.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.material.SetFloat("_Alpha", Mathf.Clamp((imageTimeRemaining / imageLifespan) * maxAlpha, 0, maxAlpha));
            }

            imageTimeRemaining -= Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }

        Destroy(image);
    }
}
