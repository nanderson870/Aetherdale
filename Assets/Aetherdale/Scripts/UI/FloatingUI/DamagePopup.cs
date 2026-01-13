using UnityEngine;
using TMPro;
using Mirror;
using System;
using UnityEngine.Serialization;

public class DamagePopup : MonoBehaviour
{

    public float lifespan;

    float minScale = 0.0F;
    float maxScale = 1.2F;

    public Vector3 initialVelocity = new(); // initial velocity on appearance
    public Vector3 acceleration = new(); // acceleration per second of existence
    
    public TextMeshProUGUI textmesh;

    public Action<DamagePopup> OnLifespanEnded;

    Vector3 originPosition;
    Vector3 screenPosCurrentOffset = new Vector3();

    public float lifespanRemaining;
    Vector3 currentVelocity = new();

    
    public void Initialize(Vector3 worldPosition, HitResult hitResult, int damage, Element damageType, Transform parentTransform, bool critical = false)
    {
        // Add some position randomization
        Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 1.0F;
        Vector3 randomizedWorldPos = new(worldPosition.x + randomOffset.x, worldPosition.y + randomOffset.y, worldPosition.z);

        if (hitResult == HitResult.Absorbed)
        {
            textmesh.fontSize = 10;
            textmesh.text = "ABSORBED";
        }
        else
        {

            textmesh.fontSize = 16;

            if (damageType == Element.TrueDamage)
            {
                textmesh.text = damage.ToString();
            }
            else
            {
                textmesh.text = $"<sprite name=\"{damageType.ToString()}\" tint=1>" + damage.ToString();
                
                if (critical) 
                {
                    textmesh.fontStyle = FontStyles.Bold;
                    textmesh.text += "!";

                    textmesh.outlineColor = Color.red;
                    textmesh.outlineWidth = 3;
                }

            }
        }

        Color primary = ColorPalette.GetPrimaryColorForElement(damageType);
        Color secondary = ColorPalette.GetSecondaryColorForElement(damageType);
        textmesh.enableVertexGradient = true;
        textmesh.colorGradient = new(primary, primary, secondary, secondary);

        originPosition = randomizedWorldPos;
        screenPosCurrentOffset = new();
        transform.position = Camera.main.WorldToScreenPoint(originPosition) + screenPosCurrentOffset;

        currentVelocity = initialVelocity;
        transform.localScale = CalculateScaleBasedOnDistance();
    }

    // Start is called before the first frame update
    void Start()
    {
        textmesh = GetComponent<TextMeshProUGUI>();
        lifespanRemaining = lifespan;
        transform.localScale = CalculateScaleBasedOnDistance();

        transform.SetAsFirstSibling(); // Sets this behind other UI elements
    }

    // Update is called once per frame
    void Update()
    {
        lifespanRemaining -= Time.deltaTime;

        if (lifespanRemaining <= 0.0F)
        {
            OnLifespanEnded?.Invoke(this);
            return;
        }

        if (Camera.main == null)
        {
            // No camera for some reason
            return;
        }

        screenPosCurrentOffset += currentVelocity * Time.deltaTime;
        currentVelocity += acceleration * Time.deltaTime;

        transform.position = Camera.main.WorldToScreenPoint(originPosition) + screenPosCurrentOffset;
        if (transform.position.z < 0)
        {
            gameObject.SetActive(false);
        }

        transform.localScale = CalculateScaleBasedOnDistance();
    }

    public float GetDistanceFromCamera()
    {
        return Vector3.Distance(Camera.main.transform.position, originPosition);
    }

    public Vector3 CalculateScaleBasedOnDistance()
    {
        float fractionOfSize = Mathf.Clamp(PlayerUI.floatingUIRenderDistance / GetDistanceFromCamera(), 0.0F, 1.0F);

        float size = minScale + ((maxScale - minScale) * fractionOfSize);

        return Vector3.one * size;
    }
}
