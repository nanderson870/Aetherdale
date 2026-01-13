using UnityEngine;

public class LinkOpener : MonoBehaviour
{
    [SerializeField] string url;

    public void OpenLink()
    {
        Application.OpenURL(url);
    }
}
