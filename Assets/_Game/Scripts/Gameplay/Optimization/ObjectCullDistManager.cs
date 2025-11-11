using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCullDistManager : Singleton<ObjectCullDistManager>
{
    private List<ObjectCullDist> m_Objects = new();

    public void RegisterObject(ObjectCullDist Object)
    {
        if (IsValidObject(Object) && !m_Objects.Contains(Object))
        {
            m_Objects.Add(Object);
        }
    }

    public bool IsValidObject(ObjectCullDist Object) => Object != null && Object.gameObject != null;

    private void Update()
    {
        m_Objects.RemoveAll((x) => x == null || x.gameObject == null);

        Vector3 CamPos = Camera.main.transform.position;

        foreach (var o in m_Objects)
        {
            if (!IsValidObject(o))
            {
                continue;
            }

            Vector3 Diff = CamPos - o.transform.position;

            o.gameObject.SetActive(Diff.sqrMagnitude < o.CullDist * o.CullDist);
        }
    }
}
