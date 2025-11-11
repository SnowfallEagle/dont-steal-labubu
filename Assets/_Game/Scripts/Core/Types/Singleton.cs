using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    static private T s_Instance;
    static public T Instance
    {
        get
        {
            if (s_Instance == null)
            {
                var ExistingSingleton = FindFirstObjectByType<Singleton<T>>();
                if (ExistingSingleton)
                {
                    s_Instance = ExistingSingleton.GetComponent<T>();
                }
                else
                {
                    var NewSingleton = new GameObject();
                    NewSingleton.name = typeof(T).Name;
                    s_Instance = NewSingleton.AddComponent<T>();
                }
            }

            return s_Instance;
        }
    }
}
