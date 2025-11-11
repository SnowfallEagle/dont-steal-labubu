using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCullDist : MonoBehaviour
{

    [SerializeField] private float m_CullDist = 100f;
    public float CullDist => m_CullDist;

    private void Awake()
    {
        ObjectCullDistManager.Instance.RegisterObject(this);
    }
}
