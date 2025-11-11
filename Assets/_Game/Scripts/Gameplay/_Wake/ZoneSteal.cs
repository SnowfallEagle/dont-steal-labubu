using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cook;

public class ZoneSteal : MonoBehaviour
{
    public BoxCollider Collider;
    public List<Controller> Controllers = new();

    private void OnTriggerEnter(Collider Other)
    {
        if (Other == null)
        {
            return;
        }

        var Controller = Other.GetComponent<Controller>();
        if (Controller == null)
        {
            return;
        }

        if (!Controllers.Contains(Controller))
        {
            Controllers.Add(Controller);
        }
    }

    private void OnTriggerExit(Collider Other)
    {
        if (Other == null)
        {
            return;
        }

        var Controller = Other.GetComponent<Controller>();
        if (Controller == null)
        {
            return;
        }

        Controllers.Remove(Controller);
    }
}
