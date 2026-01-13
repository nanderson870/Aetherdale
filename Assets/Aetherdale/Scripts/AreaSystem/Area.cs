using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Mirror;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "Area", menuName = "Aetherdale/Area", order = 0)]
public class Area : ScriptableObject
{
    public string areaID;
    public string areaName;
    public Region region;
    [SerializeField] [Scene] string areaSceneName;

    [SerializeField] bool isBossArea = false;
    [SerializeField] bool isSafeArea = false;
    [SerializeField] bool dynamicDerboTables = true;
    [SerializeField] Sprite areaImage;
    [SerializeField] Material portalPlaneMaterial;

    [SerializeField] EventReference musicTrack;

    public void OnValidate()
    {
        #if UNITY_EDITOR
            if (areaID == "")
            {
                areaID = GUID.Generate().ToString();
                EditorUtility.SetDirty(this);
            }
            
        #endif
    }

    public string GetAreaID()
    {
        return areaID;
    }

    public string GetAreaName()
    {
        return areaName;
    }

    public string GetSceneName()
    {
        return areaSceneName;
    }

    public Sprite GetAreaImage()
    {
        return areaImage;
    }


    public bool IsBossArea()
    {
        return isBossArea;
    }

    public bool IsSafeArea()
    {
        return isSafeArea;
    }

    public bool UsesDynamicDerboTables()
    {
        return dynamicDerboTables;
    }

    public Material GetPortalPlaneMaterial()
    {
        if (portalPlaneMaterial != null)
        {
            return portalPlaneMaterial;
        }
        
        return region.portalPlaneMaterial;
    }

    public EventReference GetMusicTrack(int index = -1)
    {
        if (region == null)
        {
            return musicTrack;
        }
        
        return region.GetRandomMusicTrack(index);
        //return musicTrack;
    }


    public static Area GetArea(string id)
    {
        Object[] areas = Resources.LoadAll("Areas", typeof(Area));
        foreach(Object loaded in areas)
        {
            if (loaded is Area area && area.GetAreaID() == id)
            {
                return area;
            }
        }

        return null;
    }
}
