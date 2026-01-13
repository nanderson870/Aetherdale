using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratedCloud : MonoBehaviour
{
    public GameObject unit;
    public Vector3 unitSpawnRange;
    public Vector3 unitMinScale;
    public Vector3 unitMaxScale;
    public int numUnits;


    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < numUnits; i++)
        {
            Vector3 relativePos = new Vector3(
                Random.Range(-(0.5F * unitSpawnRange.x), (0.5F * unitSpawnRange.x)),
                Random.Range(-(0.5F * unitSpawnRange.y), (0.5F * unitSpawnRange.y)),
                Random.Range(-(0.5F * unitSpawnRange.z), (0.5F * unitSpawnRange.z))
            );

            Vector3 localScale = new Vector3 (
                Random.Range(unitMinScale.x, unitMaxScale.x),
                Random.Range(unitMinScale.y, unitMaxScale.y),
                Random.Range(unitMinScale.z, unitMaxScale.z)
            );

            GameObject newUnit = Instantiate(unit, transform);
            newUnit.transform.localPosition = relativePos;
            newUnit.transform.localScale = localScale;

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
