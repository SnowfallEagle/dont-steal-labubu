using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoreUtils
{
    /** === Begin Camera === */
    public static Vector3 ScreenToWorldPosition(Vector3 Position)
    {
        return Camera.main.ScreenToWorldPoint(Position);
    }
    /** === End Camera === */

    /** === Begin Object === */
    public static void DontDestroyOnLoad(GameObject Object)
    {
        Object.transform.parent = null;
        GameObject.DontDestroyOnLoad(Object);
    }
    /** === End Object === */

    /** === Begin Text === */
    public static string GetMinutesSecondsText(float TimeInSeconds)
    {
        int Minutes = (int)(TimeInSeconds / 60f);
        int Seconds = (int)(TimeInSeconds) % 60;

        return $"{Minutes:D2}:{Seconds:D2}";
    }
    /** === End Text === */

    /** === Begin Randomization === */
    public static bool RandBool()
    {
        return UnityEngine.Random.Range(0, 2) == 1 ? true : false; 
    }

    public static float RandSign()
    {
        return UnityEngine.Random.Range(0, 2) == 1 ? 1f : -1f; 
    }

    public static List<int> RandIndices(int Count)
    {
        List<int> Results = new();
        List<int> Indices = new();

        for (int i = 0; i < Count; ++i)
        {
            Indices.Add(i);
        }

        while (Indices.Count > 0)
        {
            int Idx = UnityEngine.Random.Range(0, Indices.Count);
            Results.Add(Indices[Idx]);
            Indices.RemoveAt(Idx);
        }

        return Results;
    }
    /** === End Randomization === */
}
