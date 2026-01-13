// Thank you to WendelinReich1 from Unity forums
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

public class Misc
{
    public static bool IsInLayerMask(GameObject obj, LayerMask mask) => (mask.value & (1 << obj.layer)) != 0;
    public static bool IsInLayerMask(int layer, LayerMask mask) => (mask.value & (1 << layer)) != 0;

    public static int GetNavMeshAgentID(string name)
    {
        for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
        {
            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(index: i);
            if (name == NavMesh.GetSettingsNameFromID(agentTypeID: settings.agentTypeID))
            {
                return settings.agentTypeID;
            }
        }

        return -1;
    }

    public static T RouletteRandom<T>(List<Tuple<float, T>> outcomes)
    {
        float total = 0;
        foreach (Tuple<float, T> outcome in outcomes)
        {
            total += outcome.Item1;
        }

        float roll = UnityEngine.Random.Range(0, total);
        float runningTotal = 0;
        foreach (Tuple<float, T> outcome in outcomes)
        {
            runningTotal += outcome.Item1;

            if (roll < runningTotal)
            {
                return outcome.Item2;
            }
        }

        return default(T);
    }

    public static Color RandomColor()
    {
        Color value = new();
        value.r = UnityEngine.Random.Range(0.5F, 1.0F);
        value.g = UnityEngine.Random.Range(0.5F, 1.0F);
        value.b = UnityEngine.Random.Range(0.5F, 1.0F);
        value.a = 1.0F;
        return value;
    }
}

public static class ExtensionMethods
{
    public static bool IsNaN(this Vector3 value)
    {
        return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z);
    }
    public static float Remap(this float value, float inMin, float inMax, float outMin, float outMax)
    {
        return (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
    }

    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;

        Vector2 newVector = v;
        newVector.x = (cos * tx) - (sin * ty);
        newVector.y = (sin * tx) + (cos * ty);
        return newVector;
    }

    public static float GetRelativeBearingAngle(this GameObject me, GameObject target) => GetRelativeBearingAngle(me, target.transform.position);
    public static float GetRelativeBearingAngle(this GameObject me, Vector3 targetPosition)
    {
        Vector3 referenceDirection = me.transform.forward;
        Vector3 direction = targetPosition - me.transform.position;

        Vector3 localReferenceDirection = me.transform.InverseTransformDirection(referenceDirection);
        Vector3 localDirection = me.transform.InverseTransformVector(direction);

        float referenceBearing = Mathf.Atan2(localReferenceDirection.x, localReferenceDirection.z) * Mathf.Rad2Deg;
        float targetBearing = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

        float bearingDelta = Mathf.DeltaAngle(referenceBearing, targetBearing);

        return bearingDelta;
    }

    public static float GetRelativePitchAngle(this GameObject me, GameObject target) => GetRelativePitchAngle(me, target.transform.position);
    public static float GetRelativePitchAngle(this GameObject me, Vector3 targetPosition)
    {

        Vector3 referenceDirection = me.transform.forward;
        Vector3 direction = targetPosition - me.transform.position;

        Vector3 localReferenceDirection = me.transform.InverseTransformDirection(referenceDirection);
        Vector3 localDirection = me.transform.InverseTransformVector(direction);

        float referencePitch = Mathf.Atan2(localReferenceDirection.y, localReferenceDirection.z) * Mathf.Rad2Deg;
        float targetPitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;

        float pitchDelta = Mathf.DeltaAngle(referencePitch, targetPitch);

        return Mathf.Clamp(pitchDelta, -90, 90);
    }

    
    public static T GetCopyOf<T>(this Component comp, T other) where T : Component
    {
        Type type = comp.GetType();
        if (type != other.GetType()) return null; // type mis-match
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos) {
            if (pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos) {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }
}