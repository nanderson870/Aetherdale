using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudManager : MonoBehaviour
{
    
    [SerializeField] Vector3 extents = new(10, 10, 10);

    [SerializeField] List<GameObject> cloudPrefabs = new List<GameObject>(); // randomly chosen from
    [SerializeField] float cloudSpacing;
    [SerializeField] float cloudHeight; // global y
    [SerializeField] float cloudUnitMinScale = 0.8F;
    [SerializeField] float cloudUnitMaxScale = 1.2F;
    [SerializeField] Vector3 windSpeed = new Vector3();

    List<GameObject> clouds = new List<GameObject>();

    // clientside
    // Start is called before the first frame update
    void Start()
    {
        CreateClouds();
    }

    // clientside
    // Update is called once per frame
    void Update()
    {
        foreach (GameObject cloud in clouds)
        {
            cloud.transform.Translate(windSpeed * Time.deltaTime);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, extents);
    }

    // clientside
    void CreateClouds()
    {
        if (cloudPrefabs.Count == 0)
        {
            return;
        }

        float terrainSquareUnits = extents.x * extents.z;

        float minX = transform.position.x - extents.x;
        float maxX = transform.position.x + extents.x;
        
        float minZ = transform.position.z - extents.z;
        float maxZ = transform.position.z + extents.z;

        int numCloudsToMake = (int) (terrainSquareUnits / Mathf.Pow(cloudSpacing, 2.0F));
        for (int i = 0; i < numCloudsToMake; i++)
        {
            Vector3 cloudPosition = new Vector3();
            cloudPosition.x = Random.Range(minX, maxX);
            cloudPosition.y = cloudHeight;
            cloudPosition.z = Random.Range(minZ, maxZ);

            GameObject currentCloudUnit = Instantiate(cloudPrefabs[Random.Range(0, cloudPrefabs.Count)], transform);
            currentCloudUnit.transform.localPosition = cloudPosition;
            
            currentCloudUnit.transform.localScale = Vector3.one * Random.Range(cloudUnitMinScale, cloudUnitMaxScale);
            clouds.Add(currentCloudUnit);
        }
    }

}
