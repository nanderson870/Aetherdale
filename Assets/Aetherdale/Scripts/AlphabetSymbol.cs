using UnityEngine;

public class AlphabetSymbol : MonoBehaviour
{

    [SerializeField] int index = 0;
    [SerializeField] Color color;

// #if UNITY_EDITOR
//         void OnValidate()
//         {
//             SetColor(color);
//         }
// #endif

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {
    
    }

    public void SetColor(Color color)
    {
        if (TryGetComponent(out Renderer renderer) && renderer.material != null)
        {
            renderer.material.SetColor("_Emission_Color", color);
        }
    }

    public void SetIndex(int index)
    {
        if (TryGetComponent(out Renderer renderer) && renderer.material != null)
        {
            renderer.material.SetFloat("_Symbol_Index", index);
        }
    }
}
