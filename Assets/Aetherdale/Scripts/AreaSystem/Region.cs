

using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "New Region", menuName = "Aetherdale/Region", order = 0)]
public class Region : ScriptableObject
{
    public string regionName = "";
    public List<Area> areas = new();

    // TODO we may revert this if we go back to one track per area
    public List<EventReference> musicTracks = new();

    public SpawnList spawnList;
    public Faction defaultFaction;
    public Material portalPlaneMaterial;
    public int minDangerLevel = 0;


    public Boss GetBoss(int level)
    {
        return spawnList.GetBoss(level);
    }

    public EventReference GetRandomMusicTrack(int index = -1)
    {
        if (index >= 0)
        {
            return musicTracks[index];
        }
        
        return musicTracks[Random.Range(0, musicTracks.Count)];
    }

}
