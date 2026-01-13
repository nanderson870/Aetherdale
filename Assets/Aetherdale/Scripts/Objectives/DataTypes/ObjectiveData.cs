using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/* 
The base class for an objective, which is used in multiple contexts, to give a
player something that they must accomplish
*/

[System.Serializable]
public abstract class ObjectiveData : ScriptableObject 
{
    [SerializeField] string objectiveID;
    [SerializeField] string description;
    [SerializeField] int repetitionsRequired = 1;
    
    public abstract Objective GetInstance();


    public virtual void OnValidate()
    {
        #if UNITY_EDITOR
            if (objectiveID == "")
            {
                objectiveID = GUID.Generate().ToString();
                EditorUtility.SetDirty(this);
            }
            
        #endif
    }


    public string GetDescription()
    {
        return description;
    }

    public int GetRepetitionsRequired()
    {
        return repetitionsRequired;
    }
}
