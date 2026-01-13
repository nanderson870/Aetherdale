

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EntityLookup
{
    public static Entity GetEntityByName(string entityName)
    {
        string entityNameLowerNoSpaces = entityName.ToLower().Replace(" ", "");
        List<Entity> loadedEntities = AetherdaleData.GetAetherdaleData().entities.ToList();
        foreach (Entity entity in loadedEntities)
        {
            string checkedName = entity.GetName();
            string checkedNameLowerNoSpaces = checkedName.ToLower().Replace(" ", "");

            if (entityName == checkedName)
            {
                // Exact match
                return entity;
            }
            else if (entityNameLowerNoSpaces == checkedNameLowerNoSpaces)
            {
                // "Exact" re-formatted match
                return entity;
            }
        }

        // No exact match, check for substring match
        foreach (Entity entity in loadedEntities)
        {
            string checkedNameLowerNoSpaces = entity.GetName().ToLower().Replace(" ", "");
            if (checkedNameLowerNoSpaces.Contains(entityNameLowerNoSpaces))
            {
                // Substring match
                return entity;
            }
        }

        return null;
    }
}