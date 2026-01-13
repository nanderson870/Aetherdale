using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Status Library", menuName = "Aetherdale/Libraries/Status Library", order = 0)]
public class StatusLibrary : ScriptableObject
{
    public static StatusLibrary GetLibrary()
    {
        return Resources.Load<StatusLibrary>("Statuses");
    }

    [SerializeField] List<Status> statuses;

    public static Status GetStatus(int index)
    {
        StatusLibrary lib = GetLibrary();

        if (lib.statuses.Count - 1 >= index)
        {
            return lib.statuses[index];
        }
        
        return null;
    }

    public static int GetIndex(Status status)
    {
        StatusLibrary lib = GetLibrary();

        if (lib.statuses.Contains(status))
        {
            return lib.statuses.IndexOf(status);
        }

        return -1;
    }
}